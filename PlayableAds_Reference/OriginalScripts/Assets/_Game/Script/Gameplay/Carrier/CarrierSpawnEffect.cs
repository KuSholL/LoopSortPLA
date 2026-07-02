using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

public class CarrierSpawnEffect : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem spawnVfxPrefab;
    [SerializeField] private float duration = 0.5f;

    private MotionHandle _spawnMotionHandle;

    private void OnDisable()
    {
        if (_spawnMotionHandle.IsActive())
        {
            _spawnMotionHandle.TryCancel();
        }
    }

    public async UniTask PlaySpawnEffect()
    {
        var spawnVfx = PoolManagerNew.Instance.PopFromPool(spawnVfxPrefab, transform);
        spawnVfx.transform.localPosition = Vector3.zero;
        spawnVfx.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        spawnVfx.Play();
        
        try
        {
            _spawnMotionHandle = LMotion.Create(Vector3.zero, Vector3.one, duration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(transform);
            await _spawnMotionHandle.ToUniTask(this.GetCancellationTokenOnDestroy());
        }
        catch (System.OperationCanceledException)
        {
            // Ignore cancellation
        }
        finally
        {
            if (spawnVfx != null)
            {
                PoolManagerNew.Instance.PushToPool(spawnVfx);
            }
        }
    }
}
