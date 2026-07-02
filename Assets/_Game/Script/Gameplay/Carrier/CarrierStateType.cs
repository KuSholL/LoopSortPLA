/// <summary>
/// Khai báo các trạng thái chính của carrier để thay thế các biến bool rời rạc.
/// </summary>
public enum CarrierStateType
{
    Idle,
    Unloading,// Carrier dang xa cube ra conveyor, tam thoi khong nhan pickup.
    Completed,// Carrier da xa het cac block hien co, nhung van co the nhan cube moi de choi tiep.
    Finished,// Carrier da dat muc tieu cuoi cung: tat ca block day va cung mot mau, khong nhan input nua.
    Locked
}
