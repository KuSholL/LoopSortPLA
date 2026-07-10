using UnityEngine;

[CreateAssetMenu(fileName = "ConveyorSpeedBoostConfigSO", menuName = "ScriptableObjects/ConveyorSpeedBoostConfig")]
public class ConveyorSpeedBoostConfigSO : ScriptableObject
{
	[Tooltip("Bật gizmo hiển thị các vùng boost phía trước carrier.")]
	public bool DrawBoostRanges = true;

	[Tooltip("Bật gizmo hiển thị vùng quanh spawn sẽ bị khóa trục Y sau unload.")]
	public bool DrawBehindLockRange = true;

	[Tooltip("Tốc độ cộng thêm cho các cube nằm phía trước carrier khi carrier bắt đầu nhả cube.")]
	[Range(0f, 10f)]
	public float AheadExtraSpeed = 7f;

	[Tooltip("Khoảng progress phía trước carrier dùng để chọn cube được boost.")]
	[Range(0f, 1f)]
	public float AheadRange = 0.2f;

	[Tooltip("Quãng đường boost áp dụng cho cube nằm phía trước carrier.")]
	[Range(0f, 1f)]
	public float AheadBoostDistance = 0.2f;

	[Tooltip("Tốc độ cộng thêm cho cube ngay sau khi đi qua một góc cua.")]
	[Range(0f, 10f)]
	public float CornerExtraSpeed = 5f;

	[Tooltip("Quãng đường boost áp dụng sau khi cube đi qua góc cua.")]
	[Range(0f, 1f)]
	public float CornerBoostDistance = 0.2f;

	[Tooltip("Tốc độ cộng thêm cho các cube vừa đi ra khỏi cổng (portal).")]
	[Range(0f, 10f)]
	public float PortalExtraSpeed = 5f;

	[Tooltip("Quãng đường boost áp dụng sau khi cube ra khỏi cổng (portal).")]
	[Range(0f, 1f)]
	public float PortalBoostDistance = 0.2f;

	[Tooltip("Khoảng progress bao quanh tâm vùng sẽ bị áp dụng logic sau khi carrier unload xong.")]
	[Range(0f, 1f)]
	public float PostUnloadFreezeAroundRange = 0.05f;

	[Tooltip("Offset progress để kéo tâm vùng này ra trước hoặc sau điểm spawn. Giá trị dương kéo ra trước, giá trị âm kéo về sau.")]
	[Range(-1f, 1f)]
	public float PostUnloadFreezeAroundOffset = 0f;

	[Tooltip("Hệ số tốc độ áp dụng cho các cube trong vùng quanh spawn trong lúc đang bị giảm tốc sau unload.")]
	[Range(0f, 1f)]
	public float PostUnloadFreezeSpeedMultiplier = 0.5f;

	[Tooltip("Thời gian khóa trục Y cho các cube nằm trong vùng quanh điểm spawn khi carrier unload xong.")]
	public float PostUnloadLockYDuration = 0.5f;

	[Tooltip("Thời gian giảm tốc cho các cube nằm trong vùng quanh điểm spawn khi carrier unload xong.")]
	public float PostUnloadSlowDuration = 0.5f;
}
