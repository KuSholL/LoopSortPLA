using UnityEngine;
using Cysharp.Threading.Tasks;

public class ComplimentEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    public virtual void PlayEffect(ComplimentConfigSO config)
    {
        // Kích hoạt Particle System mặc định nếu có
        if (ps != null)
        {
            ps.Play();
        }

        // Tự động thu hồi về pool
        AutoReturnToPool(config).Forget();
    }

    protected virtual async UniTaskVoid AutoReturnToPool(ComplimentConfigSO config)
    {
        float duration = config != null ? config.defaultEffectDuration : 2.0f;

        if (ps != null)
        {
            var main = ps.main;
            duration = main.duration + main.startLifetime.constantMax;
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(duration));

        if (this != null && PoolManagerNew.Instance != null)
        {
            PoolManagerNew.Instance.PushToPool(this);
        }
    }
}
