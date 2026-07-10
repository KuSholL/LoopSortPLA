using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelEntryAnimConfigSO", menuName = "ScriptableObjects/Config/LevelEntryAnimConfigSO")]
public class LevelEntryAnimConfigSO : ScriptableObject
{
	[Header("Conveyor (Spline) Reveal Animation")]
	[Tooltip("Độ trễ (giây) trước khi bắt đầu hoạt ảnh xuất hiện đường ray (spline).")]
	public float ConveyorRevealDelay = 0.2f;

	[Tooltip("Thời gian (giây) chạy hoạt ảnh xuất hiện đường ray (spline).")]
	public float ConveyorRevealDuration = 1f;

	[Tooltip("Kiểu chuyển động (Ease) của hoạt ảnh xuất hiện đường ray (spline).")]
	public Ease ConveyorRevealEase = Ease.OutCubic;

	[Header("Container Scale Entry Animation")]
	[Tooltip("Độ trễ staggered (giây) xuất hiện giữa các container tiếp theo.")]
	public float ContainerScaleStagger = 0.1f;

	[Tooltip("Thời gian scale (giây) phóng to của container khi xuất hiện.")]
	public float ContainerScaleDuration = 0.35f;

	[Tooltip("Kiểu chuyển động (Ease) phóng to của container.")]
	public Ease ContainerScaleEase = Ease.OutBack;

	[Header("Carrier Scale Entry Animation")]
	[Tooltip("Thời gian scale (giây) phóng to của carrier khi xuất hiện.")]
	public float CarrierScaleDuration = 0.35f;

	[Tooltip("Độ trễ staggered (giây) xuất hiện giữa các carrier tiếp theo.")]
	public float CarrierScaleStagger = 0.1f;

	[Tooltip("Kiểu chuyển động (Ease) phóng to của carrier.")]
	public Ease CarrierScaleEase = Ease.OutBack;
}
