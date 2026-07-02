using System.Collections.Generic;

/// <summary>
/// Gom toàn bộ payload cube cần unload từ chuỗi block cùng màu của carrier.
/// </summary>
public sealed class CarrierUnloadPayloadCollector
{
    #region Fields

    private readonly CarrierBase _carrier;

    #endregion

    #region Constructor

    /// <summary>
    /// Khởi tạo collector bằng context runtime của carrier.
    /// </summary>
    public CarrierUnloadPayloadCollector(CarrierBase carrier)
    {
        _carrier = carrier;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Thử gom payload unload và đóng gói thành request gửi sang conveyor.
    /// </summary>
    public bool TryCollect(int maxUnloadCubeCount, out CarrierUnloadRequest unloadRequest)
    {
        unloadRequest = null;
        if (maxUnloadCubeCount <= 0) return false;
        
        if (_carrier.RuntimeState.State != CarrierStateType.Idle) return false;
        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(_carrier)) return false;

        var startBlock = _carrier.BlockController.GetCurrentBlock();
        if (startBlock == null) return false;

        var unloadColorType = startBlock.GetBlockColorType();
        _carrier.RuntimeState.SetDeliveryColor(unloadColorType);

        var unloadingBlocks = new List<Block>();
        var payloads = CollectPayloadsFromMatchingBlocks(
            unloadColorType,
            maxUnloadCubeCount,
            unloadingBlocks);
        if (payloads.Count <= 0)
        {
            _carrier.RuntimeState.ClearDeliveryColor();
            return false;
        }

        _carrier.LinkedBlockVisualController?.PrepareForUnload(unloadingBlocks);
        unloadRequest = new CarrierUnloadRequest(_carrier, payloads);
        return true;
    }

    #endregion

    #region Collect Flow

    /// <summary>
    /// Lấy payload từ từng block cùng màu liên tiếp, bắt đầu tại current block.
    /// </summary>
    private List<CarrierCubePayload> CollectPayloadsFromMatchingBlocks(
        EBlockColorType unloadColorType,
        int maxUnloadCubeCount,
        List<Block> unloadingBlocks)
    {
        var payloads = new List<CarrierCubePayload>();
        while (_carrier.BlockController.TryGetCurrentMatchingBlock(unloadColorType, out var block))
        {
            var blockCubeCount = block.GetExpectedUnloadCount();
            if (blockCubeCount <= 0) break;
            if (payloads.Count + blockCubeCount > maxUnloadCubeCount) break;

            if (!CollectPayloadsFromBlock(block, payloads)) break;
            unloadingBlocks?.Add(block);
            _carrier.BlockController.CompleteCurrentBlock();
        }
        _carrier.RuntimeState.ClearDeliveryColor();
        return payloads;
    }

    /// <summary>
    /// Mở một block và lấy hết cube unload của block đó.
    /// </summary>
    private static bool CollectPayloadsFromBlock(Block block, List<CarrierCubePayload> payloads)
    {
        if (!block.TryBeginUnload()) return false;
        var startPayloadCount = payloads.Count;
        
        foreach (var blockPayload in block.GetUnloadCubePayloadSnapshot())
        {
            payloads.Add(CreateCarrierPayload(block, blockPayload));
        }

        return payloads.Count > startPayloadCount;
    }

    #endregion

    #region Payload Factory

    /// <summary>
    /// Chuyển payload cấp block sang payload cấp carrier để conveyor sử dụng.
    /// </summary>
    private static CarrierCubePayload CreateCarrierPayload(Block block, BlockCubePayload blockPayload)
    {
        return new CarrierCubePayload(
            block,
            blockPayload.WorldPosition,
            blockPayload.Color,
            blockPayload.ColorType);
    }

    #endregion
}
