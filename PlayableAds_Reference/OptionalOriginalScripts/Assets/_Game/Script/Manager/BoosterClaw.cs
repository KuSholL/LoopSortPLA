using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BoosterClaw : MonoBehaviour
{
    private static readonly string[] SourceLayerNames = { "Slime1x", "Slime2x", "Slime3x", "Slime4x" };
    private static readonly string[] SelectedSourceLayerNames =
        { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };
    private Block _startBlock;
    private CarrierBase _startCarrier;
    private ClawTransferPlan _pendingPlan;
    private int _maxEmptyBlockSize;
    private int _selectedSourceSize;
    private LinkedBlockVisual _selectedLinkedVisual;
    private List<Block> _selectedSourceBlocks = new();
    private List<int> _selectedSourceIndices = new();
    private readonly Dictionary<Block, Vector3> _previewBlockPositions = new();
    private bool _isAnimating;
    public bool IsAnimating => _isAnimating;
    private bool _isPreparingTargetSelection;
    private bool _shouldKeepTransferredSourceHidden;
    private bool _isExecutingTutorialClaw;
    private Vector3 _startBlockWorldPosition;
    public CarrierBase CurrentSourceCarrier => _startCarrier;
    
    public void SelectBooster()
    {
        _maxEmptyBlockSize = CarrierSystem.Instance != null ? CarrierSystem.Instance.GetMaxClawTargetBlockCount() : 0;
        
        if (!CarrierSystem.Instance || _maxEmptyBlockSize <= 0) return;
        
        CancelSelection();
        _isExecutingTutorialClaw = LevelManager.Instance != null && LevelManager.Instance.IsTutorial;
        _maxEmptyBlockSize = CarrierSystem.Instance.GetMaxClawTargetBlockCount();
        CarrierSystem.Instance.SetClawSourceSelectionLayers();
        CameraManager.Instance.SetHighlightCameraActive(true, GetSourceHighlightSizes());
        CustomTimeScaleGroup.Instance.ApplyTimeScale(0f);
    }

    public void SelectStartBlock(Vector3 worldPosition, Block block, CarrierBase carrier)
    {
        if (_isAnimating || _isPreparingTargetSelection) return;
        _startBlock = block;
        _startCarrier = carrier;
        _startBlockWorldPosition = worldPosition;
        CacheSelectedSourceVisual();
        GameEventBus.OnSelectStartBlock?.Invoke(worldPosition);
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
        SwitchToTargetHighlight();
        BoosterSystem.Instance.SetClawMode(EClawSelectionMode.SelectTargetCarrier);
    }

    public void SelectTargetCarrier(Vector3 worldPosition, CarrierBase carrier)
    {
        if (_isAnimating || _isPreparingTargetSelection || !_startBlock || !_startCarrier) return;
        if (carrier == _startCarrier) return;
        if (!ClawTransferUtility.TryCreatePlan(_startBlock, _startCarrier, carrier, out _pendingPlan)) return;
        _isAnimating = true;
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
        BoosterSystem.Instance.SetClawMode(EClawSelectionMode.SelectBooster);
        var targetPosition = GetAnimationTargetPosition(worldPosition, _pendingPlan);
        GameEventBus.OnSelectTargetBlock?.Invoke(targetPosition);
        PlaySequentialTransferAnimation(targetPosition).Forget();
    }

    public void CancelSelection()
    {
        RefreshSelectedSourceVisual();
        RestoreSelectedSourceLayer();
        ResetSourceHighlight();
        CarrierSystem.Instance?.SetClawTargetSelectionLayers(false, _startCarrier);
        _startBlock = null;
        _startCarrier = null;
        _pendingPlan = null;
        _maxEmptyBlockSize = 0;
        _selectedSourceSize = 0;
        _selectedLinkedVisual = null;
        _selectedSourceBlocks.Clear();
        _selectedSourceIndices.Clear();
        _previewBlockPositions.Clear();
        _isAnimating = false;
        _isPreparingTargetSelection = false;
        _shouldKeepTransferredSourceHidden = false;
        CarrierSystem.Instance?.ClawBoosterAnimator?.RestoreMainLayers();
    }

    private void HandleTransferAnimationCompleted()
    {
        if (!_isAnimating || _pendingPlan == null) return;
        _shouldKeepTransferredSourceHidden = true;
        BoosterSystem.Instance.CancelClawBooster();
        BoosterUndoSystem.Instance.PublishAvailability();
    }

    private async UniTaskVoid PlaySequentialTransferAnimation(Vector3 targetWorldPosition)
    {
        var animator = CarrierSystem.Instance != null ? CarrierSystem.Instance.ClawBoosterAnimator : null;
        if (animator == null)
        {
            CompleteTransferVisuals();
            HandleTransferAnimationCompleted();
            return;
        }

        var dynamicOffset = GetDynamicLocalOffset();
        
        // Phase 1: Move claw down to grab the block
        await animator.MoveMainFromTopScreenToWorldPosition(
            _startBlockWorldPosition,
            carrierRotation: _startCarrier.transform.rotation,
            onComplete: HandleSourceCaptured,
            localOffset: dynamicOffset);

        // Phase 2: Move claw from grab position to target slot to drop and return
        await animator.MoveMainToWorldPosition(
            targetWorldPosition,
            carrierRotation: _pendingPlan?.TargetCarrier != null ? _pendingPlan.TargetCarrier.transform.rotation : Quaternion.identity,
            onComplete: CompleteTransferVisuals,
            localOffset: dynamicOffset);
        
        HandleTransferAnimationCompleted();
    }

    private Vector3 GetDynamicLocalOffset()
    {
        if (_selectedSourceSize <= 1)
        {
            if (_startBlock == null) return Vector3.zero;
            var meshFilter = _startBlock.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter == null || meshFilter.sharedMesh == null) return Vector3.zero;
            return _startBlock.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(meshFilter.sharedMesh.bounds.center));
        }
        else
        {
            if (_selectedLinkedVisual == null) return Vector3.zero;
            
            var skinnedMesh = _selectedLinkedVisual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                return _selectedLinkedVisual.transform.InverseTransformPoint(skinnedMesh.transform.TransformPoint(skinnedMesh.sharedMesh.bounds.center));
            }
            
            var meshFilter = _selectedLinkedVisual.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter == null || meshFilter.sharedMesh == null) return Vector3.zero;
            return _selectedLinkedVisual.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(meshFilter.sharedMesh.bounds.center));
        }
    }

    private void CompleteTransferVisuals()
    {
        if (_pendingPlan == null) return;
        RefreshTransferredSourceVisual();
        ClawTransferUtility.ExecutePlan(_pendingPlan);
        // Sau ExecutePlan, CompactSourceCarrierRuntime đã chuyển runtime data (bao gồm LinkGroupId)
        // sang Block object khác trong layout. BlockLinkVisual còn đang giữ reference tới
        // Block object cũ đã bị ClearContent -> phải rebuild lại toàn bộ link visuals.
        BlockLinkVisualManager.Instance?.SetupLevelLinks();
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_placeCube);
        if (CameraManager.Instance != null) CameraManager.Instance.SetHighlightCameraActive(false);

        if (!_isExecutingTutorialClaw)
        {
            DataManager.ChangeBooster(RewardType.ClawMachineBooster, -1);
        }
    }

    private bool IsSingleBlockSelection()
    {
        return _selectedSourceSize == 1;
    }

    private void HandleSourceCaptured()
    {
        var sourceRuntimeData = _startBlock != null ? _startBlock.CaptureRuntimeData() : null;
        var sourceColorType = _startBlock != null ? _startBlock.GetBlockColorType() : EBlockColorType.None;
        HideSelectedSourceVisual();

        PreviewRemainingSourceBlocks();
        _startCarrier?.LinkedBlockVisualController?.Refresh(true);
        BlockLinkVisualManager.Instance?.RefreshAllVisualPositions();

        var animator = CarrierSystem.Instance != null ? CarrierSystem.Instance.ClawBoosterAnimator : null;
        if (animator == null) return;
        if (IsSingleBlockSelection()) animator.ShowCarriedBlock(_startBlock, sourceRuntimeData);
        else animator.ShowCarriedLinkedVisual(_selectedLinkedVisual, sourceColorType, _selectedSourceSize);
    }

    private static Vector3 GetAnimationTargetPosition(Vector3 fallbackWorldPosition, ClawTransferPlan plan)
    {
        if (plan?.TargetCarrier == null || plan.TargetIndices.Count == 0) return fallbackWorldPosition;
        
        var targetBlock = plan.TargetCarrier.BlockLayout.GetBlockByIndex(plan.TargetIndices[0]);
        if (targetBlock != null)
        {
            var finalPos = targetBlock.transform.position;
            return finalPos;
        }
        return fallbackWorldPosition;
    }
    
    private void SwitchToTargetHighlight()
    {
        ResetSourceHighlight();
        var pickedColor = _startBlock != null ? _startBlock.GetBlockColorType() : EBlockColorType.None;
        CarrierSystem.Instance?.SetClawTargetSelectionLayers(true, _startCarrier, _selectedSourceSize, pickedColor);
        ApplySelectedSourceHighlight();
        CameraManager.Instance.SetHighlightCameraToCarrierAndSelectedSource();
    }

    private void ResetSourceHighlight()
    {
        if (CameraManager.Instance == null) return;
        CameraManager.Instance.ResetHighlightCameraMask();
    }

    private int[] GetSourceHighlightSizes()
    {
        var sizes = new List<int> { 1 };
        for (var size = 2; size <= _maxEmptyBlockSize; size++)
        {
            if (size == 4) continue;
            sizes.Add(size);
        }
        return sizes.ToArray();
    }

    private void CacheSelectedSourceVisual()
    {
        _selectedLinkedVisual = null;
        _selectedSourceBlocks.Clear();
        _selectedSourceIndices.Clear();
        _selectedSourceSize = ClawTransferUtility.GetSourceCount(_startBlock, _startCarrier);
        if (_startCarrier == null || _selectedSourceSize <= 0) return;
        var sourceIndices = ClawTransferUtility.GetSourceIndices(_startBlock, _startCarrier);
        _selectedSourceIndices.AddRange(sourceIndices);
        foreach (var index in sourceIndices)
        {
            var block = _startCarrier.BlockLayout.GetBlockByIndex(index);
            if (block != null) _selectedSourceBlocks.Add(block);
        }
        if (_selectedSourceSize <= 1) return;
        _selectedLinkedVisual = _startCarrier.LinkedBlockVisualController?
            .GetVisual(_startCarrier, _startBlock, _selectedSourceSize);
    }

    private void ApplySelectedSourceHighlight()
    {
        var layer = GetLayer(SelectedSourceLayerNames, _selectedSourceSize);
        if (layer < 0) return;
        foreach (var block in _selectedSourceBlocks)
            block?.SetLayer(layer);
        _selectedLinkedVisual?.SetLayer(layer);
    }

    private void RestoreSelectedSourceLayer()
    {
        var layer = GetLayer(SourceLayerNames, _selectedSourceSize);
        if (layer < 0) return;
        foreach (var block in _selectedSourceBlocks)
            block?.SetLayer(layer);
        _selectedLinkedVisual?.SetLayer(layer);
    }

    private void HideSelectedSourceVisual()
    {
        foreach (var block in _selectedSourceBlocks)
            block?.SetHiddenForClawBooster(true);
        _selectedLinkedVisual?.SetVisible(false);
        _startCarrier?.LinkedBlockVisualController?.Refresh(true);
    }

    private void RefreshSelectedSourceVisual()
    {
        if (_shouldKeepTransferredSourceHidden)
        {
            RefreshTransferredSourceVisual();
            return;
        }
        RestoreCancelledSourceVisual();
    }

    private void RestoreCancelledSourceVisual()
    {
        foreach (var block in _selectedSourceBlocks)
            block?.SetHiddenForClawBooster(false, true);
        RestorePreviewBlockPositions();
        _selectedLinkedVisual?.SetVisible(true);
        _startCarrier?.BlockController.RefreshVisibleBlockVisuals();
        _startCarrier?.LinkedBlockVisualController?.Refresh(true);
        BlockLinkVisualManager.Instance?.RefreshAllVisualPositions();
    }

    private void RefreshTransferredSourceVisual()
    {
        foreach (var block in _selectedSourceBlocks)
            block?.SetHiddenForClawBooster(false, true);
        RestorePreviewBlockPositions();
        _startCarrier?.BlockController.RefreshVisibleBlockVisuals();
        _startCarrier?.LinkedBlockVisualController?.Refresh(true);
        // Chỉ cần RefreshAllVisualPositions ở đây vì ExecutePlan chưa chạy,
        // data vẫn còn trên Block object gốc -> SetupLevelLinks sẽ được gọi sau ExecutePlan.
        BlockLinkVisualManager.Instance?.RefreshAllVisualPositions();
    }

    private void PreviewRemainingSourceBlocks()
    {
        if (_startCarrier?.BlockLayout == null || _selectedSourceIndices.Count == 0) return;
        var firstSourceIndex = _selectedSourceIndices[0];
        var sourceCount = _selectedSourceIndices.Count;
        for (var i = 0; i < firstSourceIndex; i++)
        {
            var block = _startCarrier.BlockLayout.GetBlockByIndex(i);
            var targetBlock = _startCarrier.BlockLayout.GetBlockByIndex(i + sourceCount);
            if (block == null || targetBlock == null || !block.HasContent) continue;
            if (!_previewBlockPositions.ContainsKey(block))
                _previewBlockPositions[block] = block.transform.localPosition;
            block.transform.localPosition = targetBlock.transform.localPosition;
        }
    }

    private void RestorePreviewBlockPositions()
    {
        foreach (var pair in _previewBlockPositions)
        {
            if (pair.Key == null) continue;
            pair.Key.transform.localPosition = pair.Value;
        }
        _previewBlockPositions.Clear();
    }

    private static int GetLayer(string[] layerNames, int size)
    {
        if (size < 1 || size > layerNames.Length) return -1;
        return LayerMask.NameToLayer(layerNames[size - 1]);
    }
}

public enum EClawSelectionMode
{
    SelectBooster,
    SelectStartBlock,
    SelectTargetCarrier,
}

public sealed class ClawTransferPlan
{
    public CarrierBase SourceCarrier;
    public CarrierBase TargetCarrier;
    public List<int> SourceIndices = new();
    public List<int> TargetIndices = new();
    public List<BlockRuntimeData> SourceBlocks = new();
    public List<BlockRuntimeData> TargetBlocks = new();
}

public static class ClawTransferUtility
{
    public static bool IsValidClawTargetColor(CarrierBase targetCarrier, EBlockColorType pickedColor)
    {
        if (targetCarrier == null || pickedColor == EBlockColorType.None) return true;

        // Check if the picked color is a special color in the level (has a corresponding SpecialColorReceiver)
        bool isSpecialColorInLevel = false;
        if (CarrierSystem.Instance != null && CarrierSystem.Instance.SpawnedCarriers != null)
        {
            foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
            {
                if (carrier == null || carrier.MechanicContainer?.Mechanics == null) continue;
                foreach (var mechanic in carrier.MechanicContainer.Mechanics)
                {
                    if (mechanic != null && mechanic.Type == ECarrierMechanic.SpecialColorReceiver && mechanic is ISpecialColorReceiverMechanic specialReceiver)
                    {
                        if (specialReceiver.TargetColor == pickedColor)
                        {
                            isSpecialColorInLevel = true;
                            break;
                        }
                    }
                }
                if (isSpecialColorInLevel) break;
            }
        }

        if (isSpecialColorInLevel)
        {
            // If the color is a special color in the level, it can ONLY be transferred to its matching SpecialColorReceiver carrier
            return targetCarrier.IsSpecialReceiverForColor(pickedColor);
        }

        // 1. One-Way Carrier Check
        bool isOneWay = false;
        if (targetCarrier.MechanicContainer?.Mechanics != null)
        {
            foreach (var mechanic in targetCarrier.MechanicContainer.Mechanics)
            {
                if (mechanic != null && mechanic.Type == ECarrierMechanic.OneWay)
                {
                    isOneWay = true;
                    break;
                }
            }
        }
        if (isOneWay)
        {
            if (targetCarrier.BlockLayout != null)
            {
                EBlockColorType existingColor = EBlockColorType.None;
                for (int i = 0; i < targetCarrier.MaxBlockCount; i++)
                {
                    var block = targetCarrier.BlockLayout.GetBlockByIndex(i);
                    if (block != null && block.HasContent)
                    {
                        existingColor = block.GetBlockColorType();
                        break;
                    }
                }
                if (existingColor != EBlockColorType.None && pickedColor != existingColor) return false;
            }
        }

        // 2. One-Color Carrier Check
        bool isSpecialReceiver = false;
        EBlockColorType targetColor = EBlockColorType.None;
        if (targetCarrier.MechanicContainer?.Mechanics != null)
        {
            foreach (var mechanic in targetCarrier.MechanicContainer.Mechanics)
            {
                if (mechanic != null && mechanic.Type == ECarrierMechanic.SpecialColorReceiver && mechanic is ISpecialColorReceiverMechanic specialReceiver)
                {
                    isSpecialReceiver = true;
                    targetColor = specialReceiver.TargetColor;
                    break;
                }
            }
        }
        if (isSpecialReceiver && pickedColor != targetColor) return false;

        return true;
    }

    public static List<int> GetSourceIndices(Block sourceBlock, CarrierBase sourceCarrier)
    {
        if (!sourceBlock || !sourceCarrier) return new List<int>();
        var sourceIndex = sourceCarrier.BlockController.GetBlockIndex(sourceBlock);
        if (sourceIndex < 0 || !CanUseSourceBlock(sourceBlock)) return new List<int>();
        return GetSourceIndices(sourceCarrier, sourceIndex, sourceBlock.GetBlockColorType());
    }

    public static int GetSourceCount(Block sourceBlock, CarrierBase sourceCarrier)
    {
        return GetSourceIndices(sourceBlock, sourceCarrier).Count;
    }

    public static bool CanSelectSource(Block sourceBlock, CarrierBase sourceCarrier)
    {
        if (!sourceBlock || !sourceCarrier || CarrierSystem.Instance == null) return false;

        if (sourceCarrier is Spawner spawner)
        {
            if (spawner.SingleBlock != sourceBlock) return false;
            if (!spawner.CanBeClicked()) return false;
        }

        // Block clawing if the clicked block or any block in its same-color run is actively linked
        if (IsBlockLinked(sourceBlock))
            return false;

        var sourceIndices = GetSourceIndices(sourceBlock, sourceCarrier);
        foreach (var index in sourceIndices)
        {
            var b = sourceCarrier.BlockLayout.GetBlockByIndex(index);
            if (b != null && IsBlockLinked(b))
                return false;
        }
        if (sourceCarrier.RuntimeState.IsFinished) return false;
        if (sourceCarrier.RuntimeState.IsUnloading) return false;
        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(sourceCarrier)) return false;
        if (!sourceCarrier.CanUnloadByMechanic()) return false;
        var sourceCount = GetSourceCount(sourceBlock, sourceCarrier);
        return IsValidSourceCount(sourceCount)
               && HasAnyValidTargetForClaw(sourceBlock, sourceCarrier, sourceCount);
    }

    private static bool HasAnyValidTargetForClaw(Block sourceBlock, CarrierBase sourceCarrier, int sourceCount)
    {
        var spawnedCarriers = CarrierSystem.Instance.SpawnedCarriers;
        if (spawnedCarriers == null) return false;

        // 1. Gather all valid target carriers (excluding the source carrier)
        var validTargets = new List<CarrierBase>();
        foreach (var carrier in spawnedCarriers)
        {
            if (carrier == null || carrier == sourceCarrier) continue;
            if (!carrier.CanBeClawTarget()) continue;
            validTargets.Add(carrier);
        }

        if (validTargets.Count == 0) return false;

        // 2. Check if ALL valid target carriers are One-Way carriers
        bool allTargetsAreOneWay = true;
        foreach (var target in validTargets)
        {
            bool isOneWay = false;
            if (target.MechanicContainer?.Mechanics != null)
            {
                foreach (var mechanic in target.MechanicContainer.Mechanics)
                {
                    if (mechanic != null && mechanic.Type == ECarrierMechanic.OneWay)
                    {
                        isOneWay = true;
                        break;
                    }
                }
            }
            if (!isOneWay)
            {
                allTargetsAreOneWay = false;
                break;
            }
        }

        var pickedColor = sourceBlock.GetBlockColorType();

        // 3. If all target carriers are One-Way carriers, apply strict color + capacity checks
        if (allTargetsAreOneWay)
        {
            foreach (var target in validTargets)
            {
                if (target.GetClawTargetBlockCount() < sourceCount) continue;
                if (IsValidClawTargetColor(target, pickedColor))
                {
                    return true;
                }
            }
            return false;
        }

        // 4. Fallback to default capacity check if there are non-OneWay carriers
        foreach (var target in validTargets)
        {
            if (target.GetClawTargetBlockCount() >= sourceCount && IsValidClawTargetColor(target, pickedColor))
                return true;
        }
        return false;
    }

    public static bool TryCreatePlan(Block sourceBlock, CarrierBase sourceCarrier, CarrierBase targetCarrier, out ClawTransferPlan plan)
    {
        plan = null;
        if (!sourceBlock || !sourceCarrier || !targetCarrier) return false;
        if (sourceCarrier == targetCarrier || sourceCarrier.RuntimeState.IsFinished || targetCarrier.RuntimeState.IsFinished) return false;
        if (!targetCarrier.CanBeClawTarget()) return false;
        var sourceIndex = sourceCarrier.BlockController.GetBlockIndex(sourceBlock);
        if (sourceIndex < 0 || !CanSelectSource(sourceBlock, sourceCarrier)) return false;
        var colorType = sourceBlock.GetBlockColorType();
        if (!IsValidClawTargetColor(targetCarrier, colorType)) return false;
        var sourceIndices = GetSourceIndices(sourceCarrier, sourceIndex, colorType);
        if (sourceIndices.Count == 0) return false;
        plan = new ClawTransferPlan { SourceCarrier = sourceCarrier, TargetCarrier = targetCarrier, SourceIndices = sourceIndices };
        foreach (var index in sourceIndices)
        {
            var block = sourceCarrier.BlockLayout.GetBlockByIndex(index);
            if (block == null) return false;
            plan.SourceBlocks.Add(block.CaptureRuntimeData());
        }
        return TryBuildTargetPlan(plan, targetCarrier);
    }

    public static void ExecutePlan(ClawTransferPlan plan)
    {
        if (plan == null) return;
        if (BoosterUndoSystem.Instance != null)
        {
            BoosterUndoSystem.Instance.InvalidateIfCarrierMutated(plan.SourceCarrier);
            BoosterUndoSystem.Instance.InvalidateIfCarrierMutated(plan.TargetCarrier);
        }
        var shouldRevealHiddenBehindSource = IsOutermostSourceRun(plan.SourceCarrier, plan.SourceIndices);
        for (var i = 0; i < plan.SourceIndices.Count; i++)
            plan.SourceCarrier.BlockLayout.GetBlockByIndex(plan.SourceIndices[i])?.ClearContent();
        CompactSourceCarrierRuntime(plan.SourceCarrier, plan.SourceIndices);
        RevealHiddenBehindSource(plan.SourceCarrier, plan.SourceIndices, shouldRevealHiddenBehindSource);
        for (var i = 0; i < plan.TargetIndices.Count; i++)
            plan.TargetCarrier.BlockLayout.GetBlockByIndex(plan.TargetIndices[i])?.ApplyRuntimeData(plan.TargetBlocks[i], false);

        if (SwappingBlockManager.Instance != null)
        {
            SwappingBlockManager.Instance.RebindBlockReferences();
            SwappingBlockManager.Instance.CheckAndDisableSwappingBlocksOnCarrier(plan.SourceCarrier, true);
            SwappingBlockManager.Instance.CheckAndDisableSwappingBlocksOnCarrier(plan.TargetCarrier, true);
        }

        RefreshCarrier(plan.SourceCarrier, plan.SourceIndices[0], true);
        RefreshCarrierForTarget(plan.TargetCarrier, plan.TargetIndices[0]);
        plan.TargetCarrier.EvaluateFinishState();

    }

    private static List<int> GetSourceIndices(CarrierBase carrier, int centerIndex, EBlockColorType colorType)
    {
        var result = new List<int>();
        for (var i = centerIndex; i >= 0; i--)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (!CanUseSourceBlock(block) || block.GetBlockColorType() != colorType) break;
            result.Insert(0, i);
        }
        for (var i = centerIndex + 1; i < carrier.MaxBlockCount; i++)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (!CanUseSourceBlock(block) || block.GetBlockColorType() != colorType) break;
            result.Add(i);
        }
        return result;
    }

    private static bool CanUseSourceBlock(Block block)
    {
        return block
               && block.HasContent
               && block.IsFull()
               && !block.IsOpened
               && !block.IsReceiving()
               && !block.IsHiddenVisualActive()
               && block.GetBlockColorType() != EBlockColorType.None;
    }

    private static bool IsValidSourceCount(int sourceCount)
    {
        return sourceCount > 0
               && sourceCount != 4;
    }

    private static bool TryBuildTargetPlan(ClawTransferPlan plan, CarrierBase carrier)
    {
        var remainingBlockCount = plan.SourceBlocks.Count;
        FillTargetRange(plan, carrier, ref remainingBlockCount);
        return remainingBlockCount == 0 && plan.TargetIndices.Count == plan.SourceBlocks.Count;
    }

    private static void FillTargetRange(
        ClawTransferPlan plan,
        CarrierBase carrier,
        ref int remainingBlockCount)
    {
        var targetIndices = new List<int>();
        var foundTargetRange = false;

        for (var i = 0; i < carrier.MaxBlockCount; i++)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (!foundTargetRange)
            {
                if (!CanUseTargetBlock(block)) continue;
                foundTargetRange = true;
            }
            else if (!CanUseTargetBlock(block)) break;
            
            targetIndices.Add(i);
        }

        var sourceIndex = remainingBlockCount - 1;
        for (var i = targetIndices.Count - 1; i >= 0 && remainingBlockCount > 0; i--)
        {
            var targetIndex = targetIndices[i];
            var block = carrier.BlockLayout.GetBlockByIndex(targetIndex);
            var targetState = block.CaptureRuntimeData();
            var sourceVisual = plan.SourceBlocks[sourceIndex];
            ApplyTransferToTargetState(targetState, sourceVisual);
            plan.TargetIndices.Add(targetIndex);
            plan.TargetBlocks.Add(targetState);
            sourceIndex--;
            remainingBlockCount--;
        }
    }

    private static bool CanUseTargetBlock(Block block)
    {
        return block && block.IsEmptyAndStable();
    }

    private static bool IsOutermostSourceRun(CarrierBase carrier, IReadOnlyList<int> sourceIndices)
    {
        if (carrier?.BlockLayout?.Blocks == null || sourceIndices == null || sourceIndices.Count == 0) return false;
        var firstActiveIndex = GetFirstActiveBlockIndex(carrier);
        return firstActiveIndex >= 0 && firstActiveIndex == sourceIndices[0];
    }

    private static void RevealHiddenBehindSource(
        CarrierBase carrier,
        IReadOnlyList<int> sourceIndices,
        bool shouldRevealHiddenBehindSource)
    {
        if (!shouldRevealHiddenBehindSource || carrier?.BlockController == null || sourceIndices == null || sourceIndices.Count == 0)
            return;
        carrier.BlockController.RevealHiddenBlockAfterRelease(sourceIndices[^1]);
    }

    private static void CompactSourceCarrierRuntime(CarrierBase carrier, IReadOnlyList<int> sourceIndices)
    {
        if (!HasRuntimeCompactionRange(carrier, sourceIndices, out var firstSourceIndex, out var sourceCount)) return;
        for (var index = firstSourceIndex - 1; index >= 0; index--)
            MoveSourceBlockRuntime(carrier, index, index + sourceCount);
    }

    private static bool HasRuntimeCompactionRange(
        CarrierBase carrier,
        IReadOnlyList<int> sourceIndices,
        out int firstSourceIndex,
        out int sourceCount)
    {
        firstSourceIndex = -1;
        sourceCount = 0;
        if (carrier?.BlockLayout == null || sourceIndices == null || sourceIndices.Count == 0) return false;
        firstSourceIndex = sourceIndices[0];
        sourceCount = sourceIndices.Count;
        return firstSourceIndex > 0 && sourceCount > 0;
    }

    private static void MoveSourceBlockRuntime(CarrierBase carrier, int fromIndex, int toIndex)
    {
        var fromBlock = carrier.BlockLayout.GetBlockByIndex(fromIndex);
        var toBlock = carrier.BlockLayout.GetBlockByIndex(toIndex);
        if (fromBlock == null || toBlock == null) return;
        if (!fromBlock.HasContent)
        {
            toBlock.ClearContent();
            return;
        }

        var runtimeData = fromBlock.CaptureRuntimeData();
        toBlock.ApplyRuntimeData(runtimeData, true);
        fromBlock.ClearContent();
    }

    private static void ApplyTransferToTargetState(BlockRuntimeData targetState, BlockRuntimeData sourceVisual)
    {
        targetState.HasContent = true;
        targetState.BlockColorType = sourceVisual.BlockColorType;
        targetState.Color = sourceVisual.Color;
        targetState.ShadowColor = sourceVisual.ShadowColor;
        targetState.CubeCount = sourceVisual.CubeCount;
        targetState.IsHiddenRevealed = sourceVisual.IsHiddenRevealed;
        targetState.IsContainerKeyConsumed = sourceVisual.IsContainerKeyConsumed;
        targetState.Mechanics = Block.CloneMechanics(sourceVisual.Mechanics);
        targetState.IsSwappingActive = sourceVisual.IsSwappingActive;
        targetState.SwapGroupId = sourceVisual.SwapGroupId;
    }

    private static void RefreshCarrier(CarrierBase carrier, int wakeBlockIndex, bool suppressPlacementAnimation = false)
    {
        if (!carrier) return;
        carrier.RuntimeState.ClearFinished();
        carrier.BlockController.Wake(GetWakeBlockIndex(carrier, wakeBlockIndex));
        carrier.BlockController.RefreshVisibleBlockVisuals();
        carrier.LinkedBlockVisualController?.Refresh(suppressPlacementAnimation);
        carrier.RefreshMechanicVisualState();
    }

    private static void RefreshCarrierForTarget(CarrierBase carrier, int targetBlockIndex)
    {
        if (!carrier) return;
        carrier.RuntimeState.ClearFinished();
        carrier.BlockController.Wake(GetWakeBlockIndex(carrier, targetBlockIndex));
        carrier.BlockController.RefreshVisibleBlockVisuals();
        carrier.LinkedBlockVisualController?.RefreshAfterReceive(targetBlockIndex);
        carrier.RefreshMechanicVisualState();
    }
    
    private static int GetWakeBlockIndex(CarrierBase carrier, int fallbackIndex)
    {
        var firstActiveIndex = GetFirstActiveBlockIndex(carrier);
        return firstActiveIndex >= 0 ? firstActiveIndex : fallbackIndex;
    }

    private static int GetFirstActiveBlockIndex(CarrierBase carrier)
    {
        if (carrier?.BlockLayout?.Blocks == null) return -1;
        for (var i = 0; i < carrier.MaxBlockCount; i++)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (block != null && block.HasContent) return i;
        }
        return -1;
    }

    private static bool IsLinkGroupActive(int groupId)
    {
        if (groupId < 0) return false;
        if (CarrierSystem.Instance == null || CarrierSystem.Instance.SpawnedCarriers == null) return false;

        var uniqueCarriers = new System.Collections.Generic.HashSet<CarrierBase>();
        foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
        {
            if (carrier == null || carrier.BlockLayout == null || carrier.RuntimeState.IsFinished) continue;
            for (int i = 0; i < carrier.MaxBlockCount; i++)
            {
                var block = carrier.BlockLayout.GetBlockByIndex(i);
                if (block != null && block.HasContent && block.GetLinkGroupId() == groupId)
                {
                    uniqueCarriers.Add(carrier);
                    break;
                }
            }
        }
        return uniqueCarriers.Count >= 2;
    }

    private static bool IsBlockLinked(Block block)
    {
        if (block == null || !block.HasLinkGroupId()) return false;
        return IsLinkGroupActive(block.GetLinkGroupId());
    }
}
