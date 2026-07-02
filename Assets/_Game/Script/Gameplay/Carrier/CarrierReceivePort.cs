using UnityEngine;

/// <summary>
/// Handles cubes flying from the conveyor into a carrier.
/// </summary>
public sealed class CarrierReceivePort
{
    private readonly CarrierBase _carrier;

    public CarrierReceivePort(CarrierBase carrier)
    {
        _carrier = carrier;
    }

    public bool TryReserveReceive(
        EBlockColorType blockColorType,
        out CarrierReceiveReservation reservation,
        int undoBatchId = 0)
    {
        reservation = default(CarrierReceiveReservation);
        if (_carrier.RuntimeState.IsFinished) return false;
        if (!_carrier.CanReceiveByMechanic(blockColorType)) return false;

        var targetBlock = _carrier.BlockController.GetReceiveBlock(blockColorType);
        if (targetBlock == null) return false;
        if (!targetBlock.TryReserveReceive(blockColorType, out var worldPosition)) return false;

        reservation = new CarrierReceiveReservation(targetBlock, worldPosition, blockColorType, undoBatchId);
        return true;
    }

    public bool CanPotentiallyReceive(EBlockColorType color)
    {
        if (_carrier.RuntimeState.IsFinished) return false;
        if (!_carrier.CanReceiveByMechanic(color)) return false;
        var block = _carrier.BlockController.GetReceiveBlock(color);
        return block != null && block.IsAvailableForReceive(color);
    }

    public void CompleteReceive(CarrierReceiveReservation reservation, Color color)
    {
        var targetBlock = reservation.TargetBlock;
        if (targetBlock == null || _carrier.RuntimeState.IsFinished) return;
        if (!targetBlock.TryReceiveCube(reservation.BlockColorType, color)) return;

        var blockIndex = GetBlockIndex(targetBlock);
        if (blockIndex >= 0) _carrier.BlockController.Wake(blockIndex);
        EvaluateFinishCondition();
    }

    private int GetBlockIndex(Block targetBlock)
    {
        if (_carrier.BlockLayout == null || targetBlock == null) return -1;
        for (var i = 0; i < _carrier.MaxBlockCount; i++)
        {
            if (_carrier.BlockLayout.GetBlockByIndex(i) == targetBlock) return i;
        }
        return -1;
    }

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
            if (block != null) block.RemoveLinkMechanic();
        }

        CarrierMechanicEventHub.Publish(new CarrierFinishedColorEvent(firstColor));
        GameEventBus.OnCarrierFinished?.Invoke(firstColor);
    }
}
