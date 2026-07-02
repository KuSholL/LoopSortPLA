using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComplimentConfig", menuName = "Config/ComplimentConfig")]
public class ComplimentConfigSO : ScriptableObject
{
    [Header("Combo Rules")]
    [Tooltip("Thời gian giới hạn giữa 2 lần hoàn thành liên tiếp để tính combo")]
    public float completionTimeLimit = 3.0f;

    [Header("Prefabs & Sounds")]
    [Tooltip("Danh sách cấu hình hiệu ứng và âm thanh tương ứng từng cấp độ combo (Index 0 cho Combo x1, Index 1 cho Combo x2...)")]
    public List<ComplimentData> complimentConfigs = new();

    [Header("Pool Return Settings")]
    [Tooltip("Thời gian tồn tại mặc định của hiệu ứng trước khi trả về pool (nếu không tìm thấy ParticleSystem)")]
    public float defaultEffectDuration = 2.0f;

    public ComplimentData GetConfigForStreak(int streak)
    {
        if (complimentConfigs == null || complimentConfigs.Count == 0) return null;
        int index = Mathf.Clamp(streak - 1, 0, complimentConfigs.Count - 1);
        return complimentConfigs[index];
    }
}

[System.Serializable]
public class ComplimentData
{
    public ComplimentEffect prefab;
    public AudioClipName soundName;
}
