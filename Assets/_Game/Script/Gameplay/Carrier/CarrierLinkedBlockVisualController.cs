using System.Collections.Generic;
using UnityEngine;

public sealed class CarrierLinkedBlockVisualController
{
    private readonly Carrier _carrier;
    private readonly CarrierLinkedBlockVisualConfigSO _config;
    private readonly List<ActiveGroup> _activeGroups = new List<ActiveGroup>();
    private readonly Dictionary<int, List<LinkedBlockVisual>> _visualPool = new Dictionary<int, List<LinkedBlockVisual>>();
    private int _pendingSplitAnimationBlockIndex = -1;

    public CarrierLinkedBlockVisualController(Carrier carrier, CarrierLinkedBlockVisualConfigSO config)
    {
        _carrier = carrier;
        _config = config;
    }

    public void Refresh(bool suppressPlacementAnimation = false)
    {
        _pendingSplitAnimationBlockIndex = -1;
        RefreshInternal(-1, suppressPlacementAnimation, false, false);
    }

    public void RefreshAfterReceive(int receivedBlockIndex)
    {
        _pendingSplitAnimationBlockIndex = -1;
        RefreshInternal(receivedBlockIndex, true, receivedBlockIndex >= 0, false);
    }

    public void RefreshAfterUnload(int revealedHiddenBlockIndex = -1)
    {
        var splitAnimationBlockIndex = _pendingSplitAnimationBlockIndex;
        _pendingSplitAnimationBlockIndex = -1;
        var animationTargetBlockIndex = revealedHiddenBlockIndex >= 0
            ? revealedHiddenBlockIndex
            : splitAnimationBlockIndex;

        if (splitAnimationBlockIndex >= 0 && revealedHiddenBlockIndex < 0)
        {
            SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_merge);
        }

        RefreshInternal(animationTargetBlockIndex, true, animationTargetBlockIndex >= 0, true);
    }

    private void RefreshInternal(
        int animationTargetBlockIndex,
        bool suppressPlacementAnimation,
        bool animateTargetGroup,
        bool forceFullAnimationForTargetGroup)
    {
        ClearActiveGroups(true);
        if (_carrier == null || _config == null || _carrier.BlockLayout == null) return;

        var blockCount = _carrier.MaxBlockCount;
        var animatedWakeTarget = false;
        var index = 0;
        while (index < blockCount)
        {
            var runStart = index;
            var runColor = EBlockColorType.None;
            var runLength = GetFullSameColorRunLength(runStart, out runColor);

            if (runLength >= 2)
            {
                CreateGroupsFromRun(
                    runStart,
                    runLength,
                    runColor,
                    suppressPlacementAnimation,
                    animateTargetGroup,
                    animationTargetBlockIndex,
                    forceFullAnimationForTargetGroup,
                    ref animatedWakeTarget);
                index = runStart + runLength;
                continue;
            }

            index++;
        }

        if (!animateTargetGroup || animatedWakeTarget) return;

        var wakeBlock = GetBlock(animationTargetBlockIndex);
        if (!CanUseForLinkedVisual(wakeBlock)) return;
        wakeBlock.PlayFullRevealAnimation();
    }

    public void PrepareForUnload(IReadOnlyList<Block> unloadingBlocks)
    {
        _pendingSplitAnimationBlockIndex = -1;
        if (unloadingBlocks == null || unloadingBlocks.Count == 0) return;

        for (var i = _activeGroups.Count - 1; i >= 0; i--)
        {
            var group = _activeGroups[i];
            if (!Intersects(group, unloadingBlocks)) continue;

            if (group.CanAnimateUnloadProgress())
            {
                TryMarkSplitAnimationTarget(group, unloadingBlocks);
                group.BeginUnload();
                UpdateUnloadProgress(group);
                continue;
            }

            ReleaseGroup(group);
            _activeGroups.RemoveAt(i);
        }
    }

    public void NotifyBlockUnloadProgress(Block block)
    {
        if (block == null) return;

        for (var i = 0; i < _activeGroups.Count; i++)
        {
            var group = _activeGroups[i];
            if (!group.IsUnloading || !group.Contains(block)) continue;
            UpdateUnloadProgress(group);
            return;
        }
    }

    public void Reset()
    {
        _pendingSplitAnimationBlockIndex = -1;
        ClearActiveGroups();
        foreach (var pair in _visualPool)
        {
            var visuals = pair.Value;
            if (visuals == null) continue;
            for (var i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] == null) continue;
                visuals[i].SetVisible(false);
                visuals[i].gameObject.SetActive(false);
            }
        }
    }

    public int GetGroupCount(int blockSize)
    {
        if (_carrier == null || _carrier.BlockLayout == null || blockSize < 2) return 0;
        var count = 0;
        var index = 0;
        while (index < _carrier.MaxBlockCount)
        {
            var runLength = GetFullSameColorRunLength(index, out _);
            if (runLength < 2)
            {
                index++;
                continue;
            }
            count += CountGroupsFromRun(runLength, blockSize);
            index += runLength;
        }
        return count;
    }

    public List<LinkedBlockVisual> GetVisuals(int blockSize)
    {
        var result = new List<LinkedBlockVisual>();
        if (blockSize < 2) return result;
        foreach (var group in _activeGroups)
        {
            if (group.BlockCount != blockSize || group.Visual == null) continue;
            result.Add(group.Visual);
        }
        return result;
    }

    public LinkedBlockVisual GetVisual(CarrierBase carrier, Block anchorBlock, int blockSize)
    {
        if (blockSize < 2) return null;
        foreach (var group in _activeGroups)
        {
            if (group.BlockCount != blockSize || group.Visual == null) continue;
            if (group.Visual.MatchesSelection(carrier, anchorBlock)) return group.Visual;
        }
        return null;
    }

    public bool TryPlayBlockedFullAnimation(Block anchorBlock)
    {
        if (anchorBlock == null) return false;

        foreach (var group in _activeGroups)
        {
            if (!group.Contains(anchorBlock) || group.Visual == null) continue;
            if (group.BlockCount < 2 || group.BlockCount > 3) return false;

            group.Visual.PlayBlockedFullAnimation();
            return true;
        }

        return false;
    }

    public bool TryPlayFinishedBlock4XActiveAnimation()
    {
        foreach (var group in _activeGroups)
        {
            if (group.BlockCount != 4 || group.Visual == null) continue;
            group.Visual.PlayTriggerActiveAnimation();
            return true;
        }

        return false;
    }

    public List<Block> GetSingleBlocks()
    {
        var result = new List<Block>();
        if (_carrier == null || _carrier.BlockLayout == null) return result;
        var index = 0;
        while (index < _carrier.MaxBlockCount)
        {
            var runLength = GetFullSameColorRunLength(index, out _);
            if (runLength == 1)
            {
                var block = GetBlock(index);
                if (block != null) result.Add(block);
            }
            index += runLength > 0 ? runLength : 1;
        }
        return result;
    }

    private int GetFullSameColorRunLength(int startIndex, out EBlockColorType colorType)
    {
        colorType = EBlockColorType.None;
        var startBlock = GetBlock(startIndex);
        if (!CanUseForLinkedVisual(startBlock)) return 0;

        colorType = startBlock.GetBlockColorType();
        var length = 0;

        for (var i = startIndex; i < _carrier.MaxBlockCount; i++)
        {
            var block = GetBlock(i);
            if (!CanUseForLinkedVisual(block)) break;
            if (block.GetBlockColorType() != colorType) break;

            length++;
        }

        return length;
    }

    private void CreateGroupsFromRun(
        int runStart,
        int runLength,
        EBlockColorType colorType,
        bool suppressPlacementAnimation,
        bool animateTargetGroup,
        int animationTargetBlockIndex,
        bool forceFullAnimationForTargetGroup,
        ref bool animatedWakeTarget)
    {
        var remaining = runLength;
        var start = runStart;

        while (remaining >= 2)
        {
            var groupLength = Mathf.Min(remaining, 4);
            if (groupLength == 1) break;

            var isAnimatedGroup = animateTargetGroup
                              && !animatedWakeTarget
                              && animationTargetBlockIndex >= start
                              && animationTargetBlockIndex < start + groupLength;
            var shouldSuppressAnimation = suppressPlacementAnimation && !isAnimatedGroup;
            var shouldForceFullAnimation = isAnimatedGroup && forceFullAnimationForTargetGroup;

            if (TryCreateGroup(start, groupLength, colorType, shouldSuppressAnimation, shouldForceFullAnimation))
            {
                animatedWakeTarget |= isAnimatedGroup;
                start += groupLength;
                remaining -= groupLength;
                continue;
            }

            groupLength--;
            if (groupLength < 2) break;

            isAnimatedGroup = animateTargetGroup
                          && !animatedWakeTarget
                          && animationTargetBlockIndex >= start
                          && animationTargetBlockIndex < start + groupLength;
            shouldSuppressAnimation = suppressPlacementAnimation && !isAnimatedGroup;
            shouldForceFullAnimation = isAnimatedGroup && forceFullAnimationForTargetGroup;

            if (!TryCreateGroup(start, groupLength, colorType, shouldSuppressAnimation, shouldForceFullAnimation)) break;
            animatedWakeTarget |= isAnimatedGroup;
            start += groupLength;
            remaining -= groupLength;
        }
    }

    private static int CountGroupsFromRun(int runLength, int targetSize)
    {
        var count = 0;
        var remaining = runLength;
        while (remaining >= 2)
        {
            var groupLength = Mathf.Min(remaining, 4);
            if (groupLength == 1) break;
            if (groupLength < 2) break;
            count += groupLength == targetSize ? 1 : 0;
            remaining -= groupLength;
        }
        return count;
    }

    private bool TryCreateGroup(
        int startIndex,
        int blockCount,
        EBlockColorType colorType,
        bool suppressPlacementAnimation,
        bool forceFullAnimation)
    {
        var entry = _config.GetEntry(blockCount);
        var prefab = entry != null ? entry.Prefab : null;
        if (prefab == null) return false;

        var visual = GetOrCreateVisual(blockCount, prefab);
        if (visual == null) return false;

        var blocks = new List<Block>(blockCount);
        for (var i = 0; i < blockCount; i++)
        {
            var block = GetBlock(startIndex + i);
            if (!CanUseForLinkedVisual(block) || block.GetBlockColorType() != colorType)
                return false;

            blocks.Add(block);
        }

        PlaceVisual(visual.transform, blocks, entry.LocalOffset);
        var colorEntry = GetColorEntry(colorType);
        var catColorEntry = ConfigManager.Instance.GetCatColorEntryByType(colorType);
        visual.BindSelectionContext(_carrier, blocks[0]);
        visual.gameObject.SetActive(true);

        visual.Apply(
            colorEntry,
            catColorEntry,
            suppressPlacementAnimation,
            forceFullAnimation,
            false,
            EBlockColorType.None);

        for (var i = 0; i < blocks.Count; i++)
            blocks[i].SetLinkedVisualSuppressed(true);

        _activeGroups.Add(new ActiveGroup(startIndex, blockCount, blocks, visual));

        return true;
    }

    private LinkedBlockVisual GetOrCreateVisual(int blockCount, LinkedBlockVisual prefab)
    {
        List<LinkedBlockVisual> visuals;
        if (!_visualPool.TryGetValue(blockCount, out visuals) || visuals == null)
        {
            visuals = new List<LinkedBlockVisual>();
            _visualPool[blockCount] = visuals;
        }

        for (var i = 0; i < visuals.Count; i++)
        {
            var cached = visuals[i];
            if (cached != null && !cached.gameObject.activeSelf)
                return cached;
        }

        var parent = _carrier.BlockLayout != null ? _carrier.BlockLayout.Root : _carrier.transform;
        LinkedBlockVisual visual = null;
        if (prefab != null)
        {
            var instanceObject = Object.Instantiate(prefab.gameObject, parent);
            visual = instanceObject.GetComponent<LinkedBlockVisual>();
        }

        if (visual == null) return null;
        visual.name = $"{prefab.name}_{blockCount}x_{visuals.Count}";
        LunaMaterialUtility.NormalizeRenderers(visual.gameObject);
        visuals.Add(visual);
        return visual;
    }

    private void PlaceVisual(Transform visualTransform, IReadOnlyList<Block> blocks, Vector3 localOffset)
    {
        if (visualTransform == null || blocks == null || blocks.Count == 0) return;

        var root = _carrier.BlockLayout != null ? _carrier.BlockLayout.Root : _carrier.transform;
        var firstBlock = blocks[0];
        var lastBlock = blocks[blocks.Count - 1];

        var firstLocal = root.InverseTransformPoint(firstBlock.transform.position);
        var lastLocal = root.InverseTransformPoint(lastBlock.transform.position);

        visualTransform.SetParent(root, false);
        visualTransform.localPosition = (firstLocal + lastLocal) * 0.5f + localOffset;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = Vector3.one;
    }

    private void ClearActiveGroups(bool suppressBlockAnimations = false)
    {
        for (var i = _activeGroups.Count - 1; i >= 0; i--)
            ReleaseGroup(_activeGroups[i], suppressBlockAnimations);

        _activeGroups.Clear();
    }

    private static void UpdateUnloadProgress(ActiveGroup group)
    {
        if (group == null || !group.IsUnloading || group.Visual == null) return;

        var initialCubeCount = Mathf.Max(1, group.InitialCubeCount);
        var remainingCubeCount = Mathf.Max(0, GetTotalCubeCount(group.Blocks));
        group.Visual.SetProgress((float)remainingCubeCount / initialCubeCount);
    }

    private static void ReleaseGroup(ActiveGroup group, bool suppressBlockAnimations = false)
    {
        if (group == null) return;

        if (group.Blocks != null)
        {
            for (var i = 0; i < group.Blocks.Count; i++)
            {
                if (group.Blocks[i] == null) continue;
                group.Blocks[i].SetLinkedVisualSuppressed(false, suppressBlockAnimations);
            }
        }

        if (group.Visual == null) return;
        group.Visual.BindSelectionContext(null, null);
        group.Visual.SetVisible(false);
        group.Visual.gameObject.SetActive(false);
    }

    private static bool Intersects(ActiveGroup group, IReadOnlyList<Block> blocks)
    {
        if (group?.Blocks == null || blocks == null) return false;

        for (var i = 0; i < group.Blocks.Count; i++)
        {
            var groupBlock = group.Blocks[i];
            if (groupBlock == null) continue;

            for (var j = 0; j < blocks.Count; j++)
                if (groupBlock == blocks[j])
                    return true;
        }

        return false;
    }

    private void TryMarkSplitAnimationTarget(ActiveGroup group, IReadOnlyList<Block> unloadingBlocks)
    {
        if (_pendingSplitAnimationBlockIndex >= 0 || group?.Blocks == null || unloadingBlocks == null) return;

        var unloadingCount = 0;
        for (var i = 0; i < group.Blocks.Count; i++)
        {
            if (ContainsBlock(unloadingBlocks, group.Blocks[i]))
                unloadingCount++;
        }

        if (unloadingCount <= 0 || unloadingCount >= group.Blocks.Count) return;

        for (var i = 0; i < group.Blocks.Count; i++)
        {
            var block = group.Blocks[i];
            if (ContainsBlock(unloadingBlocks, block)) continue;

            _pendingSplitAnimationBlockIndex = group.StartIndex + i;
            return;
        }
    }

    private static bool ContainsBlock(IReadOnlyList<Block> blocks, Block targetBlock)
    {
        if (blocks == null || targetBlock == null) return false;

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == targetBlock)
                return true;
        }

        return false;
    }

    private static int GetTotalCubeCount(IReadOnlyList<Block> blocks)
    {
        if (blocks == null) return 0;

        var totalCubeCount = 0;
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null) continue;
            totalCubeCount += Mathf.Max(0, block.GetCurrentCubes());
        }

        return totalCubeCount;
    }

    private Block GetBlock(int index)
    {
        return _carrier.BlockLayout != null ? _carrier.BlockLayout.GetBlockByIndex(index) : null;
    }

    private ColorEntry GetColorEntry(EBlockColorType colorType)
    {
        var config = _carrier != null ? _carrier.ColorConfig : null;
        return config != null
            ? config.GetColorEntry(colorType)
            : PlayableColorFallback.CreateColorEntry(colorType);
    }
    
    public LinkedBlockVisual GetLinkedVisualContainingBlock(Block block)
    {
        if (block == null) return null;
        for (var i = 0; i < _activeGroups.Count; i++)
        {
            if (_activeGroups[i].Contains(block))
                return _activeGroups[i].Visual;
        }
        return null;
    }

    public bool CanBlockMergeWithNeighbors(int targetBlockIndex)
    {
        var targetBlock = GetBlock(targetBlockIndex);
        if (!CanUseForLinkedVisual(targetBlock)) return false;

        var colorType = targetBlock.GetBlockColorType();
        if (colorType == EBlockColorType.None) return false;

        if (targetBlockIndex > 0)
        {
            var leftBlock = GetBlock(targetBlockIndex - 1);
            if (CanUseForLinkedVisual(leftBlock) && leftBlock.GetBlockColorType() == colorType)
            {
                return true;
            }
        }

        if (targetBlockIndex < _carrier.MaxBlockCount - 1)
        {
            var rightBlock = GetBlock(targetBlockIndex + 1);
            if (CanUseForLinkedVisual(rightBlock) && rightBlock.GetBlockColorType() == colorType)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanUseForLinkedVisual(Block block)
    {
        return block != null
               && block.HasContent
               && block.IsFull()
               && !block.IsOpened
               && !block.IsReceiving()
               && !block.IsHiddenForClawBooster()
               && !block.IsHiddenVisualActive()
               && block.GetBlockColorType() != EBlockColorType.None;
    }

    private sealed class ActiveGroup
    {
        public readonly int StartIndex;
        public readonly int BlockCount;
        public readonly List<Block> Blocks;
        public readonly LinkedBlockVisual Visual;
        public bool IsUnloading { get; private set; }
        public int InitialCubeCount { get; private set; }

        public ActiveGroup(int startIndex, int blockCount, List<Block> blocks, LinkedBlockVisual visual)
        {
            StartIndex = startIndex;
            BlockCount = blockCount;
            Blocks = blocks;
            Visual = visual;
            IsUnloading = false;
            InitialCubeCount = 0;
        }

        public bool CanAnimateUnloadProgress()
        {
            return !IsUnloading
                   && Visual != null
                   && (BlockCount == 2 || BlockCount == 3);
        }

        public void BeginUnload()
        {
            if (IsUnloading) return;
            IsUnloading = true;
            InitialCubeCount = GetTotalCubeCount(Blocks);
        }

        public bool Contains(Block block)
        {
            if (block == null || Blocks == null) return false;

            for (var i = 0; i < Blocks.Count; i++)
                if (Blocks[i] == block)
                    return true;

            return false;
        }
    }
}
