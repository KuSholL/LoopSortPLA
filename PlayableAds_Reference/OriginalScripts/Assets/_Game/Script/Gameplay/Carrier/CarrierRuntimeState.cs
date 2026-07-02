/// <summary>
/// Lưu trạng thái chạy hiện tại của carrier trong một object duy nhất để dễ kiểm soát transition.
/// </summary>
public sealed class CarrierRuntimeState
{
    public CarrierStateType State { get; private set; } = CarrierStateType.Idle;
    public int CurrentBlockIndex { get; private set; }
    public bool HasDeliveryColor { get; private set; }
    public EBlockColorType DeliveryColor { get; private set; }
    public bool HasCompletedAllBlocks { get; private set; }
    public bool IsIdle => State == CarrierStateType.Idle;
    public bool IsCompleted => State == CarrierStateType.Completed;
    public bool IsUnloading => State == CarrierStateType.Unloading;
    public bool IsFinished => State == CarrierStateType.Finished;

    /// <summary>
    /// Đưa carrier về trạng thái ban đầu khi spawn hoặc tái sử dụng từ pool.
    /// </summary>
    public void Reset()
    {
        State = CarrierStateType.Idle;
        CurrentBlockIndex = 0;
        HasDeliveryColor = false;
        DeliveryColor = default;
        HasCompletedAllBlocks = false;
    }

    /// <summary>
    /// Ghi nhận màu đang unload để mở tiếp các block cùng màu liên tiếp.
    /// </summary>
    public void SetDeliveryColor(EBlockColorType colorType)
    {
        DeliveryColor = colorType;
        HasDeliveryColor = true;
    }

    /// <summary>
    /// Xóa màu đang unload khi kết thúc luồng delivery hoặc không còn block phù hợp.
    /// </summary>
    public void ClearDeliveryColor()
    {
        HasDeliveryColor = false;
        DeliveryColor = default;
    }
    
    /// <summary>
    /// Chuyển carrier sang trạng thái đang unload cube ra conveyor.
    /// </summary>
    public void MarkUnloading()
    {
        State = CarrierStateType.Unloading;
    }

    /// <summary>
    /// Kết thúc unload và đưa carrier về Idle nếu chưa hoàn tất toàn bộ block.
    /// </summary>
    public void FinishUnloading()
    {
        ClearDeliveryColor();
        if (State != CarrierStateType.Unloading) return;
        State = HasCompletedAllBlocks ? CarrierStateType.Completed : CarrierStateType.Idle;
    }

    /// <summary>
    /// Đánh dấu carrier đã unload hết các block đang active.
    /// </summary>
    public void MarkCompleted()
    {
        HasCompletedAllBlocks = true;
        if (State != CarrierStateType.Unloading) State = CarrierStateType.Completed;
    }

    /// <summary>
    /// Đánh dấu carrier đã hoàn thành điều kiện full cùng màu và không còn nhận input.
    /// </summary>
    public void MarkFinished()
    {
        State = CarrierStateType.Finished;
    }

    /// <summary>
    /// Gỡ trạng thái finished khi cần reset hoặc tái sử dụng carrier.
    /// </summary>
    public void ClearFinished()
    {
        if (State == CarrierStateType.Finished) State = CarrierStateType.Idle;
    }

    /// <summary>
    /// Cập nhật index block hiện tại sau khi block cũ bị ẩn hoặc carrier được wake.
    /// </summary>
    public void SetCurrentBlockIndex(int blockIndex)
    {
        CurrentBlockIndex = blockIndex;
    }

    /// <summary>
    /// Đánh thức lại carrier khi một block đã ẩn bắt đầu nhận cube mới.
    /// </summary>
    public void WakeAtBlock(int blockIndex)
    {
        HasCompletedAllBlocks = false;
        if (State == CarrierStateType.Completed) State = CarrierStateType.Idle;
        if (CurrentBlockIndex > blockIndex) CurrentBlockIndex = blockIndex;
    }
}
