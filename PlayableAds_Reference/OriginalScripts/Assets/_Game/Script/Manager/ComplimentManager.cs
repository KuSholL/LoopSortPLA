using UnityEngine;

public class ComplimentManager : MonoSingleton<ComplimentManager>
{
    [SerializeField] private ComplimentConfigSO config;

    private int _currentStreak = 0;
    private float _lastCompletionTime = -9999f;

    protected override void Awake()
    {
        base.Awake();
        GameEventBus.OnInitLoadLevel += ResetStreak;
    }

    private void OnEnable()
    {
        // Lắng nghe sự kiện khen thưởng chuyên biệt
        GameEventBus.OnCarrierComplimentTrigger += HandleCarrierCompliment;
    }

    private void OnDisable()
    {
        GameEventBus.OnCarrierComplimentTrigger -= HandleCarrierCompliment;
        GameEventBus.OnInitLoadLevel -= ResetStreak;
    }

    private void ResetStreak()
    {
        _currentStreak = 0;
        _lastCompletionTime = -9999f;
    }

    private void HandleCarrierCompliment(CarrierBase carrier)
    {
        if (config == null || carrier == null) return;

        float currentTime = Time.time;
        float elapsed = currentTime - _lastCompletionTime;

        // Tính toán combo dựa trên thời gian thực tế
        if (_currentStreak > 0 && elapsed <= config.completionTimeLimit)
        {
            _currentStreak++;
        }
        else
        {
            _currentStreak = 1;
        }

        _lastCompletionTime = currentTime;

        var complimentConfig = config.GetConfigForStreak(_currentStreak);
        if (complimentConfig == null || complimentConfig.prefab == null) return;

        // Phát hiệu ứng tại vị trí của Carrier
        SpawnEffect(complimentConfig.prefab, carrier.Trans.position);

        // Phát âm thanh nếu có cấu hình tương ứng
        if (complimentConfig.soundName != AudioClipName.None && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot(complimentConfig.soundName);
        }
    }

    private void SpawnEffect(ComplimentEffect prefab, Vector3 spawnWorldPos)
    {
        if (PoolManagerNew.Instance == null) return;

        var effect = PoolManagerNew.Instance.PopFromPool(prefab);
        if (effect == null) return;

        // Định vị trí ngay phía trên Carrier để người chơi dễ quan sát
        effect.transform.position = spawnWorldPos + Vector3.up * 3f;

        effect.PlayEffect(config);
    }
}
