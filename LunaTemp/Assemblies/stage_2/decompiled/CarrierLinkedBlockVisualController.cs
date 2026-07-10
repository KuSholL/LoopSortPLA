using System.Collections.Generic;
using UnityEngine;

public sealed class CarrierLinkedBlockVisualController
{
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
			return !IsUnloading && Visual != null && (BlockCount == 2 || BlockCount == 3);
		}

		public void BeginUnload()
		{
			if (!IsUnloading)
			{
				IsUnloading = true;
				InitialCubeCount = GetTotalCubeCount(Blocks);
			}
		}

		public bool Contains(Block block)
		{
			if (block == null || Blocks == null)
			{
				return false;
			}
			for (int i = 0; i < Blocks.Count; i++)
			{
				if (Blocks[i] == block)
				{
					return true;
				}
			}
			return false;
		}
	}

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
		int splitAnimationBlockIndex = _pendingSplitAnimationBlockIndex;
		_pendingSplitAnimationBlockIndex = -1;
		int animationTargetBlockIndex = ((revealedHiddenBlockIndex >= 0) ? revealedHiddenBlockIndex : splitAnimationBlockIndex);
		if (splitAnimationBlockIndex >= 0 && revealedHiddenBlockIndex < 0)
		{
			MonoSingleton<SoundManager>.Instance?.PlayOneShot(AudioClipName.sfx_merge);
		}
		RefreshInternal(animationTargetBlockIndex, true, animationTargetBlockIndex >= 0, true);
	}

	private void RefreshInternal(int animationTargetBlockIndex, bool suppressPlacementAnimation, bool animateTargetGroup, bool forceFullAnimationForTargetGroup)
	{
		ClearActiveGroups(true);
		if (_carrier == null || _config == null || _carrier.BlockLayout == null)
		{
			return;
		}
		int blockCount = _carrier.MaxBlockCount;
		bool animatedWakeTarget = false;
		int index = 0;
		while (index < blockCount)
		{
			int runStart = index;
			EBlockColorType runColor = EBlockColorType.None;
			int runLength = GetFullSameColorRunLength(runStart, out runColor);
			if (runLength >= 2)
			{
				CreateGroupsFromRun(runStart, runLength, runColor, suppressPlacementAnimation, animateTargetGroup, animationTargetBlockIndex, forceFullAnimationForTargetGroup, ref animatedWakeTarget);
				index = runStart + runLength;
			}
			else
			{
				index++;
			}
		}
		if (!(!animateTargetGroup || animatedWakeTarget))
		{
			Block wakeBlock = GetBlock(animationTargetBlockIndex);
			if (CanUseForLinkedVisual(wakeBlock))
			{
				wakeBlock.PlayFullRevealAnimation();
			}
		}
	}

	public void PrepareForUnload(IReadOnlyList<Block> unloadingBlocks)
	{
		_pendingSplitAnimationBlockIndex = -1;
		if (unloadingBlocks == null || unloadingBlocks.Count == 0)
		{
			return;
		}
		for (int i = _activeGroups.Count - 1; i >= 0; i--)
		{
			ActiveGroup group = _activeGroups[i];
			if (Intersects(group, unloadingBlocks))
			{
				if (group.CanAnimateUnloadProgress())
				{
					TryMarkSplitAnimationTarget(group, unloadingBlocks);
					group.BeginUnload();
					UpdateUnloadProgress(group);
				}
				else
				{
					ReleaseGroup(group);
					_activeGroups.RemoveAt(i);
				}
			}
		}
	}

	public void NotifyBlockUnloadProgress(Block block)
	{
		if (block == null)
		{
			return;
		}
		for (int i = 0; i < _activeGroups.Count; i++)
		{
			ActiveGroup group = _activeGroups[i];
			if (group.IsUnloading && group.Contains(block))
			{
				UpdateUnloadProgress(group);
				break;
			}
		}
	}

	public void Reset()
	{
		_pendingSplitAnimationBlockIndex = -1;
		ClearActiveGroups();
		foreach (KeyValuePair<int, List<LinkedBlockVisual>> item in _visualPool)
		{
			List<LinkedBlockVisual> visuals = item.Value;
			if (visuals == null)
			{
				continue;
			}
			for (int i = 0; i < visuals.Count; i++)
			{
				if (!(visuals[i] == null))
				{
					visuals[i].SetVisible(false);
					visuals[i].gameObject.SetActive(false);
				}
			}
		}
	}

	public int GetGroupCount(int blockSize)
	{
		if (_carrier == null || _carrier.BlockLayout == null || blockSize < 2)
		{
			return 0;
		}
		int count = 0;
		int index = 0;
		while (index < _carrier.MaxBlockCount)
		{
			EBlockColorType colorType;
			int runLength = GetFullSameColorRunLength(index, out colorType);
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
		List<LinkedBlockVisual> result = new List<LinkedBlockVisual>();
		if (blockSize < 2)
		{
			return result;
		}
		foreach (ActiveGroup group in _activeGroups)
		{
			if (group.BlockCount == blockSize && !(group.Visual == null))
			{
				result.Add(group.Visual);
			}
		}
		return result;
	}

	public LinkedBlockVisual GetVisual(CarrierBase carrier, Block anchorBlock, int blockSize)
	{
		if (blockSize < 2)
		{
			return null;
		}
		foreach (ActiveGroup group in _activeGroups)
		{
			if (group.BlockCount != blockSize || group.Visual == null || !group.Visual.MatchesSelection(carrier, anchorBlock))
			{
				continue;
			}
			return group.Visual;
		}
		return null;
	}

	public bool TryPlayBlockedFullAnimation(Block anchorBlock)
	{
		if (anchorBlock == null)
		{
			return false;
		}
		foreach (ActiveGroup group in _activeGroups)
		{
			if (!group.Contains(anchorBlock) || group.Visual == null)
			{
				continue;
			}
			if (group.BlockCount < 2 || group.BlockCount > 3)
			{
				return false;
			}
			group.Visual.PlayBlockedFullAnimation();
			return true;
		}
		return false;
	}

	public bool TryPlayFinishedBlock4XActiveAnimation()
	{
		foreach (ActiveGroup group in _activeGroups)
		{
			if (group.BlockCount != 4 || group.Visual == null)
			{
				continue;
			}
			group.Visual.PlayTriggerActiveAnimation();
			return true;
		}
		return false;
	}

	public List<Block> GetSingleBlocks()
	{
		List<Block> result = new List<Block>();
		if (_carrier == null || _carrier.BlockLayout == null)
		{
			return result;
		}
		int runLength;
		for (int index = 0; index < _carrier.MaxBlockCount; index += ((runLength <= 0) ? 1 : runLength))
		{
			runLength = GetFullSameColorRunLength(index, out var _);
			if (runLength == 1)
			{
				Block block = GetBlock(index);
				if (block != null)
				{
					result.Add(block);
				}
			}
		}
		return result;
	}

	private int GetFullSameColorRunLength(int startIndex, out EBlockColorType colorType)
	{
		colorType = EBlockColorType.None;
		Block startBlock = GetBlock(startIndex);
		if (!CanUseForLinkedVisual(startBlock))
		{
			return 0;
		}
		colorType = startBlock.GetBlockColorType();
		int length = 0;
		for (int i = startIndex; i < _carrier.MaxBlockCount; i++)
		{
			Block block = GetBlock(i);
			if (!CanUseForLinkedVisual(block) || block.GetBlockColorType() != colorType)
			{
				break;
			}
			length++;
		}
		return length;
	}

	private void CreateGroupsFromRun(int runStart, int runLength, EBlockColorType colorType, bool suppressPlacementAnimation, bool animateTargetGroup, int animationTargetBlockIndex, bool forceFullAnimationForTargetGroup, ref bool animatedWakeTarget)
	{
		int remaining = runLength;
		int start = runStart;
		while (remaining >= 2)
		{
			int groupLength = Mathf.Min(remaining, 4);
			if (groupLength == 1)
			{
				break;
			}
			bool isAnimatedGroup = animateTargetGroup && !animatedWakeTarget && animationTargetBlockIndex >= start && animationTargetBlockIndex < start + groupLength;
			bool shouldSuppressAnimation = suppressPlacementAnimation && !isAnimatedGroup;
			bool shouldForceFullAnimation = isAnimatedGroup && forceFullAnimationForTargetGroup;
			if (TryCreateGroup(start, groupLength, colorType, shouldSuppressAnimation, shouldForceFullAnimation))
			{
				animatedWakeTarget |= isAnimatedGroup;
				start += groupLength;
				remaining -= groupLength;
				continue;
			}
			groupLength--;
			if (groupLength < 2)
			{
				break;
			}
			isAnimatedGroup = animateTargetGroup && !animatedWakeTarget && animationTargetBlockIndex >= start && animationTargetBlockIndex < start + groupLength;
			shouldSuppressAnimation = suppressPlacementAnimation && !isAnimatedGroup;
			shouldForceFullAnimation = isAnimatedGroup && forceFullAnimationForTargetGroup;
			if (!TryCreateGroup(start, groupLength, colorType, shouldSuppressAnimation, shouldForceFullAnimation))
			{
				break;
			}
			animatedWakeTarget |= isAnimatedGroup;
			start += groupLength;
			remaining -= groupLength;
		}
	}

	private static int CountGroupsFromRun(int runLength, int targetSize)
	{
		int count = 0;
		int remaining = runLength;
		while (remaining >= 2)
		{
			int groupLength = Mathf.Min(remaining, 4);
			if (groupLength == 1 || groupLength < 2)
			{
				break;
			}
			count += ((groupLength == targetSize) ? 1 : 0);
			remaining -= groupLength;
		}
		return count;
	}

	private bool TryCreateGroup(int startIndex, int blockCount, EBlockColorType colorType, bool suppressPlacementAnimation, bool forceFullAnimation)
	{
		LinkedBlockVisualEntry entry = _config.GetEntry(blockCount);
		LinkedBlockVisual prefab = entry?.Prefab;
		if (prefab == null)
		{
			return false;
		}
		LinkedBlockVisual visual = GetOrCreateVisual(blockCount, prefab);
		if (visual == null)
		{
			return false;
		}
		List<Block> blocks = new List<Block>(blockCount);
		for (int j = 0; j < blockCount; j++)
		{
			Block block = GetBlock(startIndex + j);
			if (!CanUseForLinkedVisual(block) || block.GetBlockColorType() != colorType)
			{
				return false;
			}
			blocks.Add(block);
		}
		PlaceVisual(visual.transform, blocks, entry.LocalOffset);
		ColorEntry colorEntry = GetColorEntry(colorType);
		CatColorEntry catColorEntry = MonoSingleton<ConfigManager>.Instance.GetCatColorEntryByType(colorType);
		visual.BindSelectionContext(_carrier, blocks[0]);
		visual.gameObject.SetActive(true);
		visual.Apply(colorEntry, catColorEntry, suppressPlacementAnimation, forceFullAnimation);
		for (int i = 0; i < blocks.Count; i++)
		{
			blocks[i].SetLinkedVisualSuppressed(true);
		}
		_activeGroups.Add(new ActiveGroup(startIndex, blockCount, blocks, visual));
		return true;
	}

	private LinkedBlockVisual GetOrCreateVisual(int blockCount, LinkedBlockVisual prefab)
	{
		if (!_visualPool.TryGetValue(blockCount, out var visuals) || visuals == null)
		{
			visuals = new List<LinkedBlockVisual>();
			_visualPool[blockCount] = visuals;
		}
		for (int i = 0; i < visuals.Count; i++)
		{
			LinkedBlockVisual cached = visuals[i];
			if (cached != null && !cached.gameObject.activeSelf)
			{
				return cached;
			}
		}
		Transform parent = ((_carrier.BlockLayout != null) ? _carrier.BlockLayout.Root : _carrier.transform);
		LinkedBlockVisual visual = ((!Application.isPlaying || !(MonoSingleton<PoolManagerNew>.Instance != null)) ? Object.Instantiate(prefab, parent) : MonoSingleton<PoolManagerNew>.Instance.PopFromPool(prefab, parent));
		if (visual == null)
		{
			return null;
		}
		visual.name = $"{prefab.name}_{blockCount}x_{visuals.Count}";
		visuals.Add(visual);
		return visual;
	}

	private void PlaceVisual(Transform visualTransform, IReadOnlyList<Block> blocks, Vector3 localOffset)
	{
		if (!(visualTransform == null) && blocks != null && blocks.Count != 0)
		{
			Transform root = ((_carrier.BlockLayout != null) ? _carrier.BlockLayout.Root : _carrier.transform);
			Block firstBlock = blocks[0];
			Block lastBlock = blocks[blocks.Count - 1];
			Vector3 firstLocal = root.InverseTransformPoint(firstBlock.transform.position);
			Vector3 lastLocal = root.InverseTransformPoint(lastBlock.transform.position);
			visualTransform.SetParent(root, false);
			visualTransform.localPosition = (firstLocal + lastLocal) * 0.5f + localOffset;
			visualTransform.localRotation = Quaternion.identity;
			visualTransform.localScale = Vector3.one;
		}
	}

	private void ClearActiveGroups(bool suppressBlockAnimations = false)
	{
		for (int i = _activeGroups.Count - 1; i >= 0; i--)
		{
			ReleaseGroup(_activeGroups[i], suppressBlockAnimations);
		}
		_activeGroups.Clear();
	}

	private static void UpdateUnloadProgress(ActiveGroup group)
	{
		if (group != null && group.IsUnloading && !(group.Visual == null))
		{
			int initialCubeCount = Mathf.Max(1, group.InitialCubeCount);
			int remainingCubeCount = Mathf.Max(0, GetTotalCubeCount(group.Blocks));
			group.Visual.SetProgress((float)remainingCubeCount / (float)initialCubeCount);
		}
	}

	private static void ReleaseGroup(ActiveGroup group, bool suppressBlockAnimations = false)
	{
		if (group == null)
		{
			return;
		}
		if (group.Blocks != null)
		{
			for (int i = 0; i < group.Blocks.Count; i++)
			{
				if (!(group.Blocks[i] == null))
				{
					group.Blocks[i].SetLinkedVisualSuppressed(false, suppressBlockAnimations);
				}
			}
		}
		if (!(group.Visual == null))
		{
			group.Visual.BindSelectionContext(null, null);
			group.Visual.SetVisible(false);
			group.Visual.gameObject.SetActive(false);
		}
	}

	private static bool Intersects(ActiveGroup group, IReadOnlyList<Block> blocks)
	{
		if (group?.Blocks == null || blocks == null)
		{
			return false;
		}
		for (int i = 0; i < group.Blocks.Count; i++)
		{
			Block groupBlock = group.Blocks[i];
			if (groupBlock == null)
			{
				continue;
			}
			for (int j = 0; j < blocks.Count; j++)
			{
				if (groupBlock == blocks[j])
				{
					return true;
				}
			}
		}
		return false;
	}

	private void TryMarkSplitAnimationTarget(ActiveGroup group, IReadOnlyList<Block> unloadingBlocks)
	{
		if (_pendingSplitAnimationBlockIndex >= 0 || group?.Blocks == null || unloadingBlocks == null)
		{
			return;
		}
		int unloadingCount = 0;
		for (int j = 0; j < group.Blocks.Count; j++)
		{
			if (ContainsBlock(unloadingBlocks, group.Blocks[j]))
			{
				unloadingCount++;
			}
		}
		if (unloadingCount <= 0 || unloadingCount >= group.Blocks.Count)
		{
			return;
		}
		for (int i = 0; i < group.Blocks.Count; i++)
		{
			Block block = group.Blocks[i];
			if (!ContainsBlock(unloadingBlocks, block))
			{
				_pendingSplitAnimationBlockIndex = group.StartIndex + i;
				break;
			}
		}
	}

	private static bool ContainsBlock(IReadOnlyList<Block> blocks, Block targetBlock)
	{
		if (blocks == null || targetBlock == null)
		{
			return false;
		}
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i] == targetBlock)
			{
				return true;
			}
		}
		return false;
	}

	private static int GetTotalCubeCount(IReadOnlyList<Block> blocks)
	{
		if (blocks == null)
		{
			return 0;
		}
		int totalCubeCount = 0;
		for (int i = 0; i < blocks.Count; i++)
		{
			Block block = blocks[i];
			if (!(block == null))
			{
				totalCubeCount += Mathf.Max(0, block.GetCurrentCubes());
			}
		}
		return totalCubeCount;
	}

	private Block GetBlock(int index)
	{
		return (_carrier.BlockLayout != null) ? _carrier.BlockLayout.GetBlockByIndex(index) : null;
	}

	private ColorEntry GetColorEntry(EBlockColorType colorType)
	{
		ColorConfigSO config = ((_carrier != null) ? _carrier.ColorConfig : null);
		return (config != null) ? config.GetColorEntry(colorType) : null;
	}

	public LinkedBlockVisual GetLinkedVisualContainingBlock(Block block)
	{
		if (block == null)
		{
			return null;
		}
		for (int i = 0; i < _activeGroups.Count; i++)
		{
			if (_activeGroups[i].Contains(block))
			{
				return _activeGroups[i].Visual;
			}
		}
		return null;
	}

	public bool CanBlockMergeWithNeighbors(int targetBlockIndex)
	{
		Block targetBlock = GetBlock(targetBlockIndex);
		if (!CanUseForLinkedVisual(targetBlock))
		{
			return false;
		}
		EBlockColorType colorType = targetBlock.GetBlockColorType();
		if (colorType == EBlockColorType.None)
		{
			return false;
		}
		if (targetBlockIndex > 0)
		{
			Block leftBlock = GetBlock(targetBlockIndex - 1);
			if (CanUseForLinkedVisual(leftBlock) && leftBlock.GetBlockColorType() == colorType)
			{
				return true;
			}
		}
		if (targetBlockIndex < _carrier.MaxBlockCount - 1)
		{
			Block rightBlock = GetBlock(targetBlockIndex + 1);
			if (CanUseForLinkedVisual(rightBlock) && rightBlock.GetBlockColorType() == colorType)
			{
				return true;
			}
		}
		return false;
	}

	private static bool CanUseForLinkedVisual(Block block)
	{
		return block != null && block.HasContent && block.IsFull() && !block.IsOpened && !block.IsReceiving() && !block.IsHiddenForClawBooster() && !block.IsHiddenVisualActive() && block.GetBlockColorType() != EBlockColorType.None;
	}
}
