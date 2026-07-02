using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public sealed class BoosterUndoSystem : MonoSingleton<BoosterUndoSystem>
{
    #region Fields

    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private GameObject splashDecayVfxPf;

    private UndoUnloadRecord _record;
    private bool _lastPublishedCanUndo;
    private bool _isUndoAnimating;
    private MaterialPropertyBlock _undoVfxPropertyBlock;

    public bool IsUndoAnimating => _isUndoAnimating;

    #endregion

    #region Public API

    public void ResetState()
    {
        _isUndoAnimating = false;
        ClearRecord();
    }

    public void BeginUnloadRecord(CarrierUnloadRequest unloadRequest)
    {
        if (!CanRecordUnload(unloadRequest))
        {
            ClearRecord();
            return;
        }

        _record = UndoUnloadRecord.FromRequest(unloadRequest);
        PublishAvailability();
    }

    public void CancelRecord(int undoBatchId)
    {
        if (!MatchesRecord(undoBatchId)) return;
        ClearRecord();
    }

    public bool TryUseUndoBooster()
    {
        if (!CanUndo()) return false;

        _isUndoAnimating = true;
        PublishAvailability();
        UseUndoAsync().Forget();
        return true;
    }

    #endregion

    #region Notify From Conveyor

    // Cube đã bay anim xong và trở thành cube thật trên conveyor.
    public void NotifyCubeSpawnedOnConveyor(int undoBatchId, Cube cube)
    {
        if (!MatchesRecord(undoBatchId) || _record.IsInvalid) return;

        if (cube == null)
        {
            InvalidateRecord();
            return;
        }

        _record.PendingToConveyor = Mathf.Max(0, _record.PendingToConveyor - 1);
        _record.PendingOnConveyor++;
        _record.AddConveyorCube(cube);
        PublishAvailability();
    }

    // Gọi sau khi source carrier đã cleanup block unload xong.
    public void NotifyUnloadAnimationsCompleted(int undoBatchId)
    {
        if (!MatchesRecord(undoBatchId) || _record.IsInvalid) return;
        PublishAvailability();
    }

    // Cube bắt đầu rời conveyor để bay vào carrier.
    public void NotifyCubePickupStarted(int undoBatchId, Cube cube, CarrierBase targetCarrier, Block targetBlock)
    {
        InvalidateIfReservedSourceSlot(targetCarrier, targetBlock);
        InvalidateIfExternalTargetCarrierMutation(undoBatchId, targetCarrier);

        if (!MatchesRecord(undoBatchId) || _record.IsInvalid) return;
        if (targetCarrier == null || targetBlock == null || cube == null)
        {
            InvalidateRecord();
            return;
        }

        if (!_record.RemoveConveyorCube(cube) || targetCarrier == _record.SourceCarrier)
        {
            InvalidateRecord();
            return;
        }

        if (_record.TargetCarrier == null) _record.TargetCarrier = targetCarrier;
        else if (_record.TargetCarrier != targetCarrier)
        {
            InvalidateRecord();
            return;
        }

        _record.PendingOnConveyor = Mathf.Max(0, _record.PendingOnConveyor - 1);
        _record.PendingInFlight++;
        PublishAvailability();
    }

    // Cube đã bay vào target carrier xong.
    public void NotifyCubeReceived(int undoBatchId, CarrierBase targetCarrier, Block targetBlock, EBlockColorType colorType)
    {
        InvalidateIfExternalTargetCarrierMutation(undoBatchId, targetCarrier);
        if (!MatchesRecord(undoBatchId) || _record.IsInvalid) return;

        if (targetCarrier == null || targetBlock == null || targetCarrier != _record.TargetCarrier)
        {
            InvalidateRecord();
            return;
        }

        if (colorType != _record.ColorType || targetBlock.GetBlockColorType() != _record.ColorType)
        {
            InvalidateRecord();
            return;
        }

        var blockIndex = targetCarrier.BlockController.GetBlockIndex(targetBlock);
        if (blockIndex < 0)
        {
            InvalidateRecord();
            return;
        }

        _record.PendingInFlight = Mathf.Max(0, _record.PendingInFlight - 1);
        _record.ReceivedCount++;
        _record.AddTargetReceive(blockIndex);
        PublishAvailability();
    }

    #endregion

    #region Undo

    private async UniTaskVoid UseUndoAsync()
    {
        InputController.Disable();
        var success = false;

        CustomTimeScaleGroup.Instance?.ApplyTimeScale(0f);
        try
        {
            if (CanUndoAllOnConveyor()) success = await UndoAllOnConveyorAsync();
            else if (CanUndoCompletedBlocks()) success = await UndoCompletedBlocksAsync();
        }
        finally
        {
            _isUndoAnimating = false;
            CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
            if (success)
            {
                ClearRecord();
                GameEventBus.OnUndoSuccess?.Invoke();
            }
            else PublishAvailability();
            
            InputController.Enable();
        }
    }

    private async UniTask<bool> UndoAllOnConveyorAsync()
    {
        if (!await PlayConveyorCubesRemovedAnimationAsync()) return false;
        RestoreSourceBlocks();
        await PlaySourceBlocksRestoredAnimationAsync();
        RefreshCarrierAfterUndo(_record.SourceCarrier, _record.GetFirstSourceBlockIndex(), true);
        return true;
    }

    private async UniTask<bool> UndoCompletedBlocksAsync()
    {
        await PlayTargetBlocksRemovedAnimationAsync();
        RestoreTargetBlocks();

        RestoreSourceBlocks();
        await PlaySourceBlocksRestoredAnimationAsync();

        RefreshCarrierAfterUndo(_record.TargetCarrier, _record.GetFirstTargetBlockIndex(), false);
        RefreshCarrierAfterUndo(_record.SourceCarrier, _record.GetFirstSourceBlockIndex(), true);
        return true;
    }

    #endregion

    #region Conditions

    private bool CanUndo()
    {
        if (_isUndoAnimating) return false;
        if (_record == null || _record.IsInvalid) return false;
        if (LevelManager.Instance != null && LevelManager.Instance.IsGameEnded && !LevelManager.Instance.IsPreloseDelay) return false;
        if (_record.SourceCarrier == null || _record.SourceBlocks.Count <= 0) return false;
        if (_record.TargetCarrier != null && _record.TargetCarrier.RuntimeState?.IsFinished == true) return false;
        if (!AreSourceSlotsStillEmpty()) return false;

        return CanUndoAllOnConveyor() || CanUndoCompletedBlocks();
    }

    private bool CanUndoAllOnConveyor()
    {
        if (_record == null || _record.IsInvalid) return false;
        if (_record.PendingToConveyor > 0) return false; // cube còn đang anim ra ray
        if (_record.PendingOnConveyor != _record.TotalCubeCount) return false;
        if (_record.PendingInFlight > 0 || _record.ReceivedCount > 0) return false;
        if (_record.TargetCarrier != null || _record.TargetBlocks.Count > 0) return false;
        if (_record.ConveyorCubes.Count != _record.TotalCubeCount) return false;
        return AreAllConveyorCubesStillActive();
    }

    private bool CanUndoCompletedBlocks()
    {
        if (_record == null || _record.IsInvalid) return false;
        if (_record.TargetCarrier == null) return false;
        if (_record.PendingToConveyor > 0) return false;
        if (_record.PendingOnConveyor > 0 || _record.PendingInFlight > 0) return false;
        if (_record.ReceivedCount != _record.TotalCubeCount) return false;
        if (_record.TargetBlocks.Count != _record.SourceBlocks.Count) return false;
        return AreTargetBlocksFullFromThisBatch();
    }

    private static bool CanRecordUnload(CarrierUnloadRequest unloadRequest)
    {
        return unloadRequest != null
               && unloadRequest.SourceCarrier != null
               && unloadRequest.CubeCount > 0
               && unloadRequest.SourceBlocks != null
               && unloadRequest.SourceBlocks.Count > 0;
    }

    private bool AreSourceSlotsStillEmpty()
    {
        for (var i = 0; i < _record.SourceBlocks.Count; i++)
        {
            var source = _record.SourceBlocks[i];
            var block = _record.SourceCarrier.BlockLayout.GetBlockByIndex(source.BlockIndex);
            if (block == null || !block.IsEmptyAndStable()) return false;
        }

        return true;
    }

    private bool AreTargetBlocksFullFromThisBatch()
    {
        var targetReceivedTotal = 0;
        for (var i = 0; i < _record.TargetBlocks.Count; i++)
        {
            var target = _record.TargetBlocks[i];
            var block = _record.TargetCarrier.BlockLayout.GetBlockByIndex(target.BlockIndex);
            if (block == null || !block.HasContent || !block.IsFull()) return false;
            if (block.IsReceiving() || block.IsOpened) return false;
            if (block.GetBlockColorType() != _record.ColorType) return false;
            if (target.ReceivedCount != block.GetMaxCubes()) return false;
            targetReceivedTotal += target.ReceivedCount;
        }

        return targetReceivedTotal == _record.TotalCubeCount;
    }

    private bool AreAllConveyorCubesStillActive()
    {
        var deliverySystem = ConveyorDeliverySystem.Instance;
        if (deliverySystem == null) return false;

        for (var i = 0; i < _record.ConveyorCubes.Count; i++)
        {
            var cube = _record.ConveyorCubes[i];
            if (cube == null || !deliverySystem.ContainsDeliveryCube(cube)) return false;
        }

        return true;
    }

    #endregion

    #region Apply State

    private void RestoreTargetBlocks()
    {
        for (var i = 0; i < _record.TargetBlocks.Count; i++)
        {
            var target = _record.TargetBlocks[i];
            var block = _record.TargetCarrier.BlockLayout.GetBlockByIndex(target.BlockIndex);
            block?.ClearContent();
        }
    }

    private void RestoreSourceBlocks()
    {
        for (var i = 0; i < _record.SourceBlocks.Count; i++)
        {
            var source = _record.SourceBlocks[i];
            var block = _record.SourceCarrier.BlockLayout.GetBlockByIndex(source.BlockIndex);
            if (block != null)
            {
                block.RestoreFullBlock(
                    source.ColorType,
                    source.Color,
                    source.ShadowColor,
                    source.CubeCount,
                    true);
                block.RemoveLinkMechanic();

                // Restore swapping state but make it inactive as per the rules
                if (source.SwapGroupId >= 0)
                {
                    block.ConfigureSwapMechanic(source.SwapGroupId, false);
                }
            }
        }
    }

    private static void RefreshCarrierAfterUndo(CarrierBase carrier, int wakeBlockIndex, bool animateWakeTarget)
    {
        if (carrier == null) return;
        carrier.RuntimeState.ClearFinished();
        if (wakeBlockIndex >= 0) carrier.BlockController.Wake(wakeBlockIndex);
        carrier.BlockController.RefreshVisibleBlockVisuals();
        if (animateWakeTarget)
            carrier.LinkedBlockVisualController?.RefreshAfterUnload(wakeBlockIndex);
        else
            carrier.LinkedBlockVisualController?.Refresh(true);
        carrier.RefreshMechanicVisualState();
    }

    #endregion

    #region Animation

    // Animate cube trên conveyor rồi mới remove về pool.
    private async UniTask<bool> PlayConveyorCubesRemovedAnimationAsync()
    {
        var deliverySystem = ConveyorDeliverySystem.Instance;
        if (deliverySystem == null) return false;

        var animationTasks = new List<UniTask<bool>>(_record.ConveyorCubes.Count);
        for (var i = 0; i < _record.ConveyorCubes.Count; i++)
        {
            var cube = _record.ConveyorCubes[i];
            animationTasks.Add(PlayConveyorCubeRemovedAnimationAsync(cube));
        }

        var results = await UniTask.WhenAll(animationTasks);
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i]) continue;
            InvalidateRecord();
            return false;
        }

        for (var i = _record.ConveyorCubes.Count - 1; i >= 0; i--)
        {
            if (!deliverySystem.TryRemoveDeliveryCube(_record.ConveyorCubes[i]))
            {
                InvalidateRecord();
                return false;
            }
        }

        _record.ConveyorCubes.Clear();
        _record.PendingOnConveyor = 0;
        return true;
    }

    private async UniTask<bool> PlayConveyorCubeRemovedAnimationAsync(Cube cube)
    {
        if (cube == null) return false;

        var baseScale = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetCubeDefaultScale()
            : Vector3.one;
        var punchScale = baseScale * 1.2f;

        cube.Trans.localScale = baseScale;

        await LMotion.Create(baseScale, punchScale, duration)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.OutQuad)
            .Bind(scale => cube.Trans.localScale = scale)
            .ToUniTask();

        await LMotion.Create(punchScale, Vector3.zero, duration)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.InQuad)
            .Bind(scale => cube.Trans.localScale = scale)
            .ToUniTask();

        if (splashDecayVfxPf != null)
        {
            var spawnedVfx = Instantiate(splashDecayVfxPf, cube.Trans.position, Quaternion.identity);
            if (_record != null && _record.ColorType != EBlockColorType.None)
            {
                var colorConfig = ConfigManager.Instance != null ? ConfigManager.Instance.GetColorConfig() : null;
                var colorEntry = colorConfig != null ? colorConfig.GetColorEntry(_record.ColorType) : null;
                if (colorEntry != null)
                {
                    var particleSystems = spawnedVfx.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        
                        var main = ps.main;
                        main.startColor = colorEntry.Color;

                        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                        if (psRenderer != null)
                        {
                            _undoVfxPropertyBlock ??= new MaterialPropertyBlock();
                            psRenderer.GetPropertyBlock(_undoVfxPropertyBlock);
                            _undoVfxPropertyBlock.SetColorEntry(colorEntry);
                            psRenderer.SetPropertyBlock(_undoVfxPropertyBlock);
                        }
                    }
                }
            }
            Destroy(spawnedVfx, 2f);
        }

        return true;
    }

    // Chờ anim target block biến mất. Hiện là hook trống để gắn tween/particle sau.
    private async UniTask PlayTargetBlocksRemovedAnimationAsync()
    {
        await UniTask.CompletedTask;
    }

    // Chờ anim source block hiện lại. Hiện là hook trống để gắn tween/particle sau.
    private async UniTask PlaySourceBlocksRestoredAnimationAsync()
    {
        await UniTask.CompletedTask;
    }

    #endregion

    #region Record

    private void InvalidateIfReservedSourceSlot(CarrierBase targetCarrier, Block targetBlock)
    {
        if (_record == null || _record.IsInvalid) return;
        if (targetCarrier == null || targetBlock == null) return;
        if (targetCarrier != _record.SourceCarrier) return;

        var targetBlockIndex = targetCarrier.BlockController.GetBlockIndex(targetBlock);
        if (_record.ContainsSourceBlock(targetBlockIndex)) InvalidateRecord();
    }

    private void InvalidateIfExternalTargetCarrierMutation(int undoBatchId, CarrierBase targetCarrier)
    {
        if (_record == null || _record.IsInvalid) return;
        if (_record.TargetCarrier == null || targetCarrier == null) return;
        if (targetCarrier != _record.TargetCarrier) return;
        if (MatchesRecord(undoBatchId)) return;

        // Another batch has started mutating the recorded target carrier,
        // so the undo snapshot is no longer safe to apply.
        InvalidateRecord();
    }

    private bool MatchesRecord(int undoBatchId)
    {
        return _record != null && undoBatchId != 0 && _record.UndoBatchId == undoBatchId;
    }

    private void InvalidateRecord()
    {
        if (_record == null) return;
        _record.IsInvalid = true;
        PublishAvailability();
    }

    public void InvalidateIfCarrierMutated(CarrierBase carrier)
    {
        if (_record == null || _record.IsInvalid || carrier == null) return;
        if (carrier == _record.SourceCarrier || carrier == _record.TargetCarrier)
        {
            InvalidateRecord();
        }
    }

    private void ClearRecord()
    {
        _record = null;
        PublishAvailability();
    }

    #endregion

    #region UI Debug

    public void PublishAvailability()
    {
        var canUndo = CanUndo();
        if (_lastPublishedCanUndo == canUndo) return;

        _lastPublishedCanUndo = canUndo;
        GameEventBus.OnUndoBoosterAvailabilityChanged?.Invoke(canUndo);
        //LogAvailability(canUndo);
    }

    private void LogAvailability(bool canUndo)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[UndoBooster] Button {(canUndo ? "ON" : "OFF")} | {GetDebugState()}");
    }

    private string GetDebugState()
    {
        if (_record == null) return "no record";

        var mode = "mixed";
        if (CanUndoAllOnConveyor()) mode = "all cubes on conveyor";
        else if (CanUndoCompletedBlocks()) mode = "completed blocks";

        return $"mode={mode}, invalid={_record.IsInvalid}, animating={_isUndoAnimating}, " +
               $"batch={_record.UndoBatchId}, total={_record.TotalCubeCount}, " +
               $"toConveyor={_record.PendingToConveyor}, onConveyor={_record.PendingOnConveyor}, " +
               $"inFlight={_record.PendingInFlight}, received={_record.ReceivedCount}, " +
               $"sourceBlocks={_record.SourceBlocks.Count}, targetBlocks={_record.TargetBlocks.Count}, " +
               $"conveyorCubes={_record.ConveyorCubes.Count}, sourceEmpty={AreSourceSlotsStillEmpty()}";
    }

    #endregion
}

public sealed class UndoUnloadRecord
{
    public int UndoBatchId;
    public CarrierBase SourceCarrier;
    public CarrierBase TargetCarrier;
    public EBlockColorType ColorType;
    public int TotalCubeCount;
    public int PendingToConveyor;
    public int PendingOnConveyor;
    public int PendingInFlight;
    public int ReceivedCount;
    public bool IsInvalid;
    public readonly List<Cube> ConveyorCubes = new();
    public readonly List<UndoSourceBlockSnapshot> SourceBlocks = new();
    public readonly List<UndoTargetBlockSnapshot> TargetBlocks = new();

    public static UndoUnloadRecord FromRequest(CarrierUnloadRequest unloadRequest)
    {
        var record = new UndoUnloadRecord
        {
            UndoBatchId = unloadRequest.UndoBatchId,
            SourceCarrier = unloadRequest.SourceCarrier,
            ColorType = unloadRequest.SourceBlocks[0].ColorType,
            TotalCubeCount = unloadRequest.CubeCount,
            PendingToConveyor = unloadRequest.CubeCount
        };

        for (var i = 0; i < unloadRequest.SourceBlocks.Count; i++)
            record.SourceBlocks.Add(unloadRequest.SourceBlocks[i].Clone());

        return record;
    }

    public void AddTargetReceive(int blockIndex)
    {
        var target = GetTargetBlock(blockIndex);
        if (target == null)
        {
            target = new UndoTargetBlockSnapshot { BlockIndex = blockIndex };
            TargetBlocks.Add(target);
        }

        target.ReceivedCount++;
    }

    public void AddConveyorCube(Cube cube)
    {
        if (cube == null || ConveyorCubes.Contains(cube)) return;
        ConveyorCubes.Add(cube);
    }

    public bool RemoveConveyorCube(Cube cube)
    {
        return cube != null && ConveyorCubes.Remove(cube);
    }

    public bool ContainsSourceBlock(int blockIndex)
    {
        for (var i = 0; i < SourceBlocks.Count; i++)
            if (SourceBlocks[i].BlockIndex == blockIndex)
                return true;

        return false;
    }

    public int GetFirstSourceBlockIndex()
    {
        return SourceBlocks.Count > 0 ? SourceBlocks[0].BlockIndex : -1;
    }

    public int GetFirstTargetBlockIndex()
    {
        return TargetBlocks.Count > 0 ? TargetBlocks[0].BlockIndex : -1;
    }

    private UndoTargetBlockSnapshot GetTargetBlock(int blockIndex)
    {
        for (var i = 0; i < TargetBlocks.Count; i++)
            if (TargetBlocks[i].BlockIndex == blockIndex)
                return TargetBlocks[i];

        return null;
    }
}

public sealed class UndoSourceBlockSnapshot
{
    public int BlockIndex;
    public EBlockColorType ColorType;
    public Color Color;
    public Color ShadowColor;
    public int CubeCount;
    public bool IsSwappingActive;
    public int SwapGroupId;

    public UndoSourceBlockSnapshot Clone()
    {
        return new UndoSourceBlockSnapshot
        {
            BlockIndex = BlockIndex,
            ColorType = ColorType,
            Color = this.Color,
            ShadowColor = this.ShadowColor,
            CubeCount = CubeCount,
            IsSwappingActive = IsSwappingActive,
            SwapGroupId = SwapGroupId
        };
    }
}

public sealed class UndoTargetBlockSnapshot
{
    public int BlockIndex;
    public int ReceivedCount;
}
