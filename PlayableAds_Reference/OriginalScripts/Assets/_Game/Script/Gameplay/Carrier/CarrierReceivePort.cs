using LitMotion;
using UnityEngine;

/// <summary>
/// Là cổng xử lý carrier nhận cube từ conveyor bay vào block phù hợp.
/// </summary>
public sealed class CarrierReceivePort
{
    private readonly CarrierBase _carrier;
    private MotionHandle _closedHandle;

    /// <summary>
    /// Khởi tạo receive port với context runtime của carrier.
    /// </summary>
    public CarrierReceivePort(CarrierBase carrier)
    {
        _carrier = carrier;
    }

    /// <summary>
    /// Đặt trước một vị trí nhận cube để tránh nhiều cube cùng bay vào một cell.
    /// </summary>
    public bool TryReserveReceive(
        EBlockColorType blockColorType,
        out CarrierReceiveReservation reservation,
        int undoBatchId = 0)
    {
        reservation = default;
        if (_carrier.RuntimeState.IsFinished) return false;
        if (!_carrier.CanReceiveByMechanic(blockColorType)) return false;

        var targetBlock = _carrier.BlockController.GetReceiveBlock(blockColorType);
        if (targetBlock == null) return false;
        if (!targetBlock.TryReserveReceive(blockColorType, out var worldPosition)) return false;

        int targetIndex = GetBlockIndex(targetBlock);
        if (targetIndex >= 0)
        {
            if (SwappingBlockManager.Instance != null)
            {
                SwappingBlockManager.Instance.CheckAndDisableSwappingBlocksOnCarrier(_carrier);
            }
        }

        reservation = new CarrierReceiveReservation(targetBlock, worldPosition, blockColorType, undoBatchId);
        return true;
    }

    /// <summary>
    /// Kiểm tra carrier có khả năng nhận màu cube truyền vào hay không.
    /// </summary>
    public bool CanPotentiallyReceive(EBlockColorType color)
    {
        if (_carrier.RuntimeState.IsFinished) return false;
        if (!_carrier.CanReceiveByMechanic(color)) return false;
        var block = _carrier.BlockController.GetReceiveBlock(color);
        return block != null && block.IsAvailableForReceive(color);
    }

    /// <summary>
    /// Hoàn tất nhận cube vào block đã reserve và kiểm tra điều kiện finish của carrier.
    /// </summary>
    public void CompleteReceive(CarrierReceiveReservation reservation, Color color)
    {
        var targetBlock = reservation.TargetBlock;
        if (targetBlock == null || _carrier.RuntimeState.IsFinished) return;
        if (!targetBlock.TryReceiveCube(reservation.BlockColorType, color)) return;

        var blockIndex = GetBlockIndex(targetBlock);
        if (blockIndex >= 0) _carrier.BlockController.Wake(blockIndex);
        EvaluateFinishCondition();
        BoosterUndoSystem.Instance.NotifyCubeReceived(
            reservation.UndoBatchId,
            _carrier,
            targetBlock,
            reservation.BlockColorType);
    }

    /// <summary>
    /// Tìm index của block trong layout để wake carrier đúng vị trí.
    /// </summary>
    private int GetBlockIndex(Block targetBlock)
    {
        if (_carrier.BlockLayout == null || targetBlock == null) return -1;
        for (var i = 0; i < _carrier.MaxBlockCount; i++)
            if (_carrier.BlockLayout.GetBlockByIndex(i) == targetBlock) return i;
        return -1;
    }

    /// <summary>
    /// Kiểm tra carrier đã đầy bốn block cùng màu chưa để chuyển sang trạng thái Finished.
    /// </summary>
    public void EvaluateFinishCondition()
    {
        if (_carrier.RuntimeState.IsFinished || _carrier.BlockLayout == null) return;
        var blocks = _carrier.BlockLayout.Blocks;
        if (blocks.Count != _carrier.MaxBlockCount) return;

        var firstColor = blocks[0].GetBlockColorType();
        if (firstColor == EBlockColorType.None) return;

        foreach (var block in blocks)
        {
            if (block == null) return;
            if (block.GetBlockColorType() != firstColor) return;
            if (!block.IsReadyForFinish()) return;
        }

        _carrier.RuntimeState.MarkFinished();
        foreach (var block in blocks)
        {
            block?.RemoveLinkMechanic();
        }
        
        //bỏ sound này vì đã có sound khen thưởng
        //SoundManager.Instance.PlayOneShot(AudioClipName.sfx_complete_box);
        CarrierMechanicEventHub.Publish(new CarrierFinishedColorEvent(firstColor));
        GameEventBus.OnCarrierFinished?.Invoke(firstColor);
        GameEventBus.OnCarrierComplimentTrigger?.Invoke(_carrier);
    }

    /// <summary>
    /// Hủy animation đóng cũ trước khi chạy animation mới.
    /// </summary>
    private void CancelCarrierClosingMotion()
    {
        if (_closedHandle.IsActive()) _closedHandle.Cancel();
    }
}
