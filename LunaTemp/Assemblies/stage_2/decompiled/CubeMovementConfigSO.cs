using UnityEngine;

[CreateAssetMenu(fileName = "CubeMovementConfigSO", menuName = "ScriptableObjects/CubeMovementConfigSO")]
public class CubeMovementConfigSO : ScriptableObject
{
	[Tooltip("Tốc độ di chuyển cơ bản của cube khi chạy theo spline.")]
	public float Speed = 7f;

	[Header("Conveyor Speed Controls")]
	[Tooltip("Tốc độ cuộn shader khi KHÔNG có hạt nào trên băng.")]
	public float SlowShaderSpeed = 0.5f;

	[Tooltip("Tốc độ cuộn shader khi CÓ hạt trên băng.")]
	public float FastShaderSpeed = 2f;

	[Tooltip("Độ mạnh của lực dùng để kéo vận tốc của cube về hướng mong muốn.")]
	public float Acceleration = 10f;

	[Tooltip("Vận tốc đẩy ban đầu khi cube vừa được spawn lên conveyor.")]
	public float SpawnPushSpeed = 10f;

	[Tooltip("Lực giữ cube bám vào mặt đường để hạn chế bị lệch khỏi spline.")]
	public float RoadGripForce = 100f;

	[Tooltip("Thời gian chờ trước khi bắt đầu áp dụng lực bám đường.")]
	public float RoadGripDelay = 0.1f;

	[Tooltip("Khoảng thời gian giữa hai lần cập nhật chuyển động của cube.")]
	public float MovementInterval = 0.02f;

	[Header("Anti Stall")]
	[Tooltip("Nếu vận tốc tiến theo ray thấp hơn tỷ lệ này so với tốc độ mục tiêu, cube sẽ nhận thêm lực đẩy nhẹ để tránh kẹt chuỗi.")]
	[Range(0f, 1f)]
	public float StallAssistSpeedRatio = 0.55f;

	[Tooltip("Lực đẩy bổ sung theo hướng ray khi cube bị chậm/kẹt. Giữ nhỏ để vẫn bảo toàn va chạm cube-cube.")]
	[Min(0f)]
	public float StallAssistAcceleration = 12f;

	[Header("Flow Stabilizer")]
	[Range(0f, 1f)]
	public float MinimumForwardSpeedRatio = 0.82f;

	[Range(0f, 1f)]
	public float ForwardSpeedRecovery = 0.45f;

	[Min(0f)]
	public float MaxForwardCorrection = 2.5f;

	[Range(1f, 2f)]
	public float MaximumForwardSpeedRatio = 1.18f;

	[Range(0f, 1f)]
	public float ForwardOverspeedDamping = 0.25f;

	[Header("Road Boundary")]
	[Tooltip("Khoảng lệch ngang tối đa từ tâm ray mà cube được phép nằm trước khi bị kéo về lane.")]
	[Min(0.1f)]
	public float RoadMaxOffset = 0.68f;

	[Tooltip("Lực kéo cube về lane khi bị ép sát/tràn khỏi mép ray.")]
	[Min(0f)]
	public float RoadBoundaryForce = 260f;

	[Tooltip("Độ dập vận tốc ngang khi cube bị đẩy lệch khỏi hướng ray.")]
	[Min(0f)]
	public float RoadLateralDamping = 22f;

	[Tooltip("Nếu cube vượt quá RoadMaxOffset + giá trị này thì clamp cứng về mép an toàn để tránh lọt khỏi ray.")]
	[Min(0f)]
	public float RoadHardClampPadding = 0.12f;

	[Tooltip("Khoảng progress tối thiểu cube phải đi qua trước khi được phép nhận vào carrier.")]
	public float ReceiveOffset = 0.03f;

	[Tooltip("Sai số cho phép khi so sánh progress hiện tại với progress mục tiêu nhận cube.")]
	public float ReceiveThreshold = 0.01f;

	[Tooltip("Thời gian khóa trục Y ngay sau khi cube được spawn ra conveyor.")]
	public float SpawnFreezeYDuration = 0.3f;

	[Tooltip("Hệ số tăng tốc độ di chuyển của cube khi game tăng tốc (chỉ áp dụng khi time scale > 1). Mặc định là 0.2 (20%).")]
	public float ScaleSpeedMultiplier = 0.2f;
}
