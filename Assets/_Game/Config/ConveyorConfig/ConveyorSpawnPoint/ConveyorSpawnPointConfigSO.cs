using UnityEngine;

[CreateAssetMenu(
    fileName = "ConveyorSpawnPointConfigSO",
    menuName = "ScriptableObjects/ConveyorSpawnPointConfig")]
public class ConveyorSpawnPointConfigSO : ScriptableObject
{
    [Tooltip("Khoảng đệm từ mép băng chuyền vào trong để tránh spawn cube sát viền.")]
    public float DeliveryEdgePadding = 0.08f;

    [Tooltip("Độ dàn theo phương tiến tối thiểu của cụm cube khi carrier nhả ra.")]
    public float DeliveryMinForwardSpread = 0.03f;

    [Tooltip("Độ dàn theo phương tiến tối đa của cụm cube khi carrier nhả ra.")]
    public float DeliveryMaxForwardSpread = 0.12f;

    [Tooltip("Độ nâng theo trục Y của vị trí spawn cube so với mặt băng chuyền.")]
    public float DeliveryLift = 0.1f;

    [Tooltip("Tỉ lệ dàn cube sang hai bên dựa trên nửa bề rộng usable của băng chuyền.")]
    [Range(0f, 1f)] public float DeliverySpreadSideRatio = 0.65f;

    [Tooltip("Tỉ lệ nhiễu ngẫu nhiên theo phương ngang để vị trí spawn bớt đều.")]
    [Range(0f, 1f)] public float DeliveryJitterSideRatio = 0.3f;

    [Tooltip("Tỉ lệ dùng chiều dài băng chuyền để tính độ dàn theo phương tiến.")]
    [Range(0f, 0.1f)] public float DeliveryForwardSpreadByLength = 0.01f;

    [Tooltip("Tỉ lệ nhiễu ngẫu nhiên theo phương tiến để vị trí spawn tự nhiên hơn.")]
    [Range(0f, 1f)] public float DeliveryJitterForwardRatio = 0.35f;
}
