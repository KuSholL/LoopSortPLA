using UnityEngine;

[CreateAssetMenu(
    fileName = "ConveyorCornerDetectorConfigSO",
    menuName = "ScriptableObjects/ConveyorCornerDetectorConfig")]
public class ConveyorCornerDetectorConfigSO : ScriptableObject
{
    [Tooltip("Ngưỡng góc tối thiểu để xem một đoạn cong là góc cua.")]
    [Range(0f, 180f)] public float CornerAngleThreshold = 5f;

    [Tooltip("Khoảng lùi/tới quanh một progress để lấy 2 tiếp tuyến so sánh góc.")]
    [Range(0.001f, 0.1f)] public float CornerSampleOffset = .01f;

    [Tooltip("Bước quét dọc spline để tìm các cụm góc cua.")]
    [Range(0.001f, 0.05f)] public float CornerScanStep = .005f;

    [Tooltip("Độ lệch progress áp thêm sau khi tìm thấy góc cua.")]
    [Range(-0.2f, 0.2f)] public float CornerProgressOffset;

    [Tooltip("Bán kính sphere gizmo vẽ tại vị trí góc cua.")]
    [Range(0.01f, 2f)] public float GizmoRadius = .2f;

    [Tooltip("Chiều cao line gizmo dựng lên từ vị trí góc cua.")]
    [Range(0.01f, 5f)] public float GizmoHeight = .75f;
}
