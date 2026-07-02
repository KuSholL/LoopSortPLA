using UnityEngine;

/// <summary>
/// Lưu trạng thái của một cube đang chạy trên conveyor sau khi được unload từ carrier.
/// </summary>
public sealed class DeliveryCubeState
{
    public readonly Cube Cube;
    public readonly CarrierBase SourceCarrier;
    public readonly EBlockColorType BlockColorType;
    public readonly Color Color;
    public readonly int UndoBatchId;
    public bool IsPickedUp;

    public float PreviousProgress;
    public float PreviousProgressCorner;

    /// <summary>
    /// Khởi tạo state cho cube đang chạy trên conveyor.
    /// </summary>
    public DeliveryCubeState(Cube cube, CarrierBase sourceCarrier, EBlockColorType blockColorType, Color color, int undoBatchId)
    {
        Cube = cube;
        SourceCarrier = sourceCarrier;
        BlockColorType = blockColorType;
        Color = color;
        UndoBatchId = undoBatchId;
    }
}
