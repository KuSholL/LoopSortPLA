using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CarrierSpawnEffect : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private ParticleSystem spawnVfxPrefab;
    [SerializeField] private float duration = 0.5f;

    private Tween _spawnTween;
    private Coroutine _spawnRoutine;

    private void OnDisable()
    {
        if (_spawnTween != null)
        {
            _spawnTween.Kill();
            _spawnTween = null;
        }

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    public void PlaySpawnEffect()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        _spawnRoutine = StartCoroutine(PlaySpawnEffectRoutine());
    }

    private IEnumerator PlaySpawnEffectRoutine()
    {
#if UNITY_LUNA
        transform.localScale = Vector3.one;
        yield break;
#endif
        ParticleSystem spawnVfx = null;
        if (spawnVfxPrefab != null && PoolManagerNew.Instance != null)
        {
            spawnVfx = PoolManagerNew.Instance.PopFromPool(spawnVfxPrefab, transform);
            spawnVfx.transform.localPosition = Vector3.zero;
            spawnVfx.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            spawnVfx.Play();
        }

        transform.localScale = Vector3.zero;
        if (_spawnTween != null) _spawnTween.Kill();
        _spawnTween = transform.DOScale(Vector3.one, duration).SetEase(DG.Tweening.Ease.OutBack);
        yield return _spawnTween.WaitForCompletion();
        _spawnTween = null;

        if (spawnVfx != null && PoolManagerNew.Instance != null)
        {
            PoolManagerNew.Instance.PushToPool(spawnVfx);
        }

        _spawnRoutine = null;
    }
}
