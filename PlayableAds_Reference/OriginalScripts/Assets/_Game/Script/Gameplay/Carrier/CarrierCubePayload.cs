using UnityEngine;

/// <summary>
/// Chứa dữ liệu của một cube được lấy ra từ carrier để conveyor spawn ra đường chạy.
/// </summary>
public readonly struct CarrierCubePayload
{
    public readonly Block SourceBlock;
    public readonly Vector3 StartWorldPosition;
    public readonly Color Color;
    public readonly EBlockColorType BlockColorType;

    /// <summary>
    /// Khởi tạo payload cho một cube đã được lấy khỏi block.
    /// </summary>
    public CarrierCubePayload(Block sourceBlock, Vector3 startWorldPosition, Color color, EBlockColorType blockColorType)
    {
        SourceBlock = sourceBlock;
        StartWorldPosition = startWorldPosition;
        Color = color;
        BlockColorType = blockColorType;
    }
}
