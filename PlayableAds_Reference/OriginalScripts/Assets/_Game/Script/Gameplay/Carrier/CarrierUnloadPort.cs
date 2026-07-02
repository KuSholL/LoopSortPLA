using System.Collections.Generic;

/// <summary>
/// Là cổng xử lý carrier unload cube ra conveyor.
/// </summary>
public sealed class CarrierUnloadPort
{
    #region Fields

    private readonly CarrierBase _carrier;
    private readonly CarrierUnloadPayloadCollector _payloadCollector;

    #endregion

    #region Constructor

    /// <summary>
    /// Khởi tạo cổng unload với context runtime của carrier.
    /// </summary>
    public CarrierUnloadPort(CarrierBase carrier)
    {
        _carrier = carrier;
        _payloadCollector = new CarrierUnloadPayloadCollector(carrier);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Bắt đầu unload block hiện tại nếu carrier đang ở trạng thái cho phép.
    /// </summary>
    public bool UnloadBlocks()
    {
        if (!_carrier.CanUnloadByMechanic()) return false;

        var topBlock = _carrier.BlockController.GetCurrentBlock();
        if (topBlock == null) return false;

        // Kiểm tra xem chuỗi cùng màu sắp unload có chứa block có liên kết không
        var initialRun = _carrier.BlockController.GetContiguousSameColorRun(topBlock);
        bool hasLinkInRun = false;
        int targetLinkId = -1;
        if (initialRun != null)
        {
            foreach (var block in initialRun)
            {
                if (block != null && block.HasLinkGroupId())
                {
                    hasLinkInRun = true;
                    targetLinkId = block.GetLinkGroupId();
                    break;
                }
            }
        }

        // Nếu chuỗi chứa cơ chế liên kết nhóm
        if (hasLinkInRun && BlockLinkSystem.Instance != null)
        {
            bool resolved = BlockLinkSystem.Instance.ResolveBlockLinkUnloadGroup(
                _carrier, 
                out var unloadGroups, 
                out var totalCubeCount, 
                out var isBlocked);

            if (!resolved || isBlocked)
            {
                TriggerGroupBlockedFeedback(targetLinkId);
                return false; // Chặn hoàn toàn hành động unload nếu nhóm bị chặn hoặc không giải quyết được
            }

            var capacityManager = CapacityManager.Instance;
            var remainingCapacity = capacityManager != null ? capacityManager.RemainingCubeCapacity : int.MaxValue;

            // Nếu không đủ sức chứa cho cả nhóm
            if (totalCubeCount > remainingCapacity)
            {
                TriggerGroupBlockedFeedback(targetLinkId);
                return false; // Trả về false kích hoạt sfx_fail ở lớp trên
            }

            // Thực hiện di chuyển đồng thời cho tất cả các Carrier trong nhóm
            foreach (var group in unloadGroups)
            {
                bool isClickedCarrier = (group.Carrier == _carrier);
                ExecuteCarrierGroupUnload(group, isClickedCarrier);
            }

            if (SwappingBlockManager.Instance != null)
            {
                SwappingBlockManager.Instance.SwapAllActivePairs();
            }

            return true;
        }

        // --- LUỒNG BÌNH THƯỜNG (Cho các block không có link) ---
        var capacityManagerNormal = CapacityManager.Instance;
        var maxUnloadCubeCount = capacityManagerNormal != null ? capacityManagerNormal.RemainingCubeCapacity : int.MaxValue;
        if (maxUnloadCubeCount <= 0) return false;

        if (!_payloadCollector.TryCollect(maxUnloadCubeCount, out var unloadRequest)) return false;

        BoosterUndoSystem.Instance.BeginUnloadRecord(unloadRequest);
        
        if (ConveyorDeliverySystem.Instance.TrySpawnCarrierUnload(unloadRequest))
        {
            if (topBlock != null && topBlock.HasSwappingMechanic())
            {
                if (SwappingBlockManager.Instance != null)
                {
                    SwappingBlockManager.Instance.DisablePair(topBlock.GetSwapGroupId());
                }
            }
            if (SwappingBlockManager.Instance != null)
            {
                SwappingBlockManager.Instance.SwapAllActivePairs();
            }

            if (capacityManagerNormal != null)
            {
                capacityManagerNormal.ReservePendingCubes(unloadRequest.CubeCount);
            }
            if (unloadRequest.CubePayloads != null)
            {
                foreach (var payload in unloadRequest.CubePayloads)
                {
                    if (payload.SourceBlock != null)
                    {
                        if (_carrier.LinkedBlockVisualController != null)
                        {
                            _carrier.LinkedBlockVisualController.RevealKeyInLinkedGroupOfBlock(payload.SourceBlock);
                        }
                        if (payload.SourceBlock.HasVisibleContainerKey())
                        {
                            payload.SourceBlock.RevealContainerKeyIfNeeded();
                        }
                    }
                }
            }

            _carrier.RuntimeState.MarkUnloading();
            GameEventBus.OnCarrierUnload?.Invoke();

            return true;
        }

        BoosterUndoSystem.Instance.CancelRecord(unloadRequest.UndoBatchId);
        _carrier.RuntimeState.ClearDeliveryColor();
        return false;
    }

    #endregion

    #region Helper Methods

    private void ExecuteCarrierGroupUnload(BlockLinkSystem.CarrierUnloadGroup group, bool isClickedCarrier)
    {
        var sourceBlocks = new List<UndoSourceBlockSnapshot>();
        var payloads = new List<CarrierCubePayload>();

        // Chuẩn bị cho visual của các block sắp unload biến mất hoặc cập nhật thanh tiến trình
        group.Carrier.LinkedBlockVisualController?.PrepareForUnload(group.RunBlocks);

        foreach (var block in group.RunBlocks)
        {
            var blockIndex = group.Carrier.BlockController.GetBlockIndex(block);
            if (blockIndex >= 0)
            {
                sourceBlocks.Add(new UndoSourceBlockSnapshot
                {
                    BlockIndex = blockIndex,
                    ColorType = block.GetBlockColorType(),
                    Color = block.GetBlockColor(),
                    ShadowColor = block.GetBlockShadowColor(),
                    CubeCount = block.GetCurrentCubes(),
                    IsSwappingActive = block.HasSwappingMechanic(),
                    SwapGroupId = block.GetSwapGroupId()
                });
            }

            if (block.TryBeginUnload())
            {
                foreach (var blockPayload in block.GetUnloadCubePayloadSnapshot())
                {
                    payloads.Add(new CarrierCubePayload(block, blockPayload.WorldPosition, blockPayload.Color, blockPayload.ColorType));
                }
            }
            group.Carrier.BlockController.CompleteCurrentBlock();
        }

        var request = new CarrierUnloadRequest(group.Carrier, payloads, sourceBlocks);

        // Chỉ ghi Undo cho Carrier mà người chơi Click thực sự
        if (isClickedCarrier)
        {
            BoosterUndoSystem.Instance.BeginUnloadRecord(request);
        }

        if (ConveyorDeliverySystem.Instance.TrySpawnCarrierUnload(request))
        {
            if (CapacityManager.Instance != null)
            {
                CapacityManager.Instance.ReservePendingCubes(request.CubeCount);
            }

            foreach (var payload in request.CubePayloads)
            {
                if (payload.SourceBlock != null)
                {
                    group.Carrier.LinkedBlockVisualController?.RevealKeyInLinkedGroupOfBlock(payload.SourceBlock);
                    if (payload.SourceBlock.HasVisibleContainerKey())
                    {
                        payload.SourceBlock.RevealContainerKeyIfNeeded();
                    }
                }
            }

            group.Carrier.RuntimeState.MarkUnloading();
            GameEventBus.OnCarrierUnload?.Invoke();
        }
    }

    private void TriggerGroupBlockedFeedback(int groupId)
    {
        if (groupId < 0 || BlockLinkSystem.Instance == null) return;

        var allLinkedBlocks = BlockLinkSystem.Instance.FindAllBlocksWithGroupId(groupId);
        foreach (var block in allLinkedBlocks)
        {
            PlayBlockedFeedbackForBlock(block);
        }
    }

    private void PlayBlockedFeedbackForBlock(Block block)
    {
        if (block == null) return;
        var carrier = block.OwnerCarrier;
        if (carrier == null) return;

        // Thử chạy hiệu ứng rung của khối gộp
        if (carrier.LinkedBlockVisualController != null 
            && carrier.LinkedBlockVisualController.TryPlayBlockedFullAnimation(block))
            return;

        // Nếu là block đơn lẻ, chạy hiệu ứng rung của block đơn lẻ
        if (!block.IsLinkedVisualSuppressed())
        {
            block.PlayFullRevealAnimation();
        }
    }

    #endregion
}
