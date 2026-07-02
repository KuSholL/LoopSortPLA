using UnityEngine;

/// <summary>
/// Chứa thông tin đặt chỗ một cell trong carrier để cube conveyor bay vào chính xác vị trí.
/// </summary>
public struct CarrierReceiveReservation
{
    public readonly Block TargetBlock;
    public readonly Vector3 TargetPosition;
    public readonly EBlockColorType BlockColorType;
    public readonly int UndoBatchId;

    /// <summary>
    /// Khởi tạo reservation cho một cube đang chuẩn bị bay vào carrier.
    /// </summary>
    public CarrierReceiveReservation(
        Block targetBlock,
        Vector3 targetPosition,
        EBlockColorType blockColorType,
        int undoBatchId = 0)
    {
        TargetBlock = targetBlock;
        TargetPosition = targetPosition;
        BlockColorType = blockColorType;
        UndoBatchId = undoBatchId;
    }
}
