/// <summary>
/// Lưu trạng thái các cube đang bay vào cùng một carrier trong lượt pickup hiện tại.
/// </summary>
public sealed class PickupState
{
    public readonly EBlockColorType BlockColorType;
    public int InFlightCount = 1;

    /// <summary>
    /// Khởi tạo trạng thái pickup theo màu cube đang được nhận.
    /// </summary>
    public PickupState(EBlockColorType blockColorType)
    {
        BlockColorType = blockColorType;
    }
}
