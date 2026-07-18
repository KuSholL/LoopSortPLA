using System;
using UnityEngine;
using DG.Tweening;

public class CubeDeliveryHandler : MonoBehaviour, ICustomTimeScaleTarget
{
    public Transform Trans;
    [SerializeField] private CubeDeliveryConfig config;

    private Tween _flyTween;
    private float _customTimeScale = 1f;
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private Vector3 _authoredScale = Vector3.one;

    private void OnValidate()
    {
        Trans = GetComponent<Transform>();
    }

    private void Awake()
    {
        if (Trans == null) Trans = transform;
        _authoredScale = Trans.localScale;
    }

    private void OnDestroy()
    {
        CancelTween();
    }

    private void OnDisable()
    {
        CancelTween();
        ResetCubeScale();
        _customTimeScale = 1f;
    }

    private void CancelTween()
    {
        if (_flyTween == null) return;
        _flyTween.Kill();
        _flyTween = null;
    }

    private void ResetCubeScale()
    {
        if (Trans == null) return;
        Trans.localScale = _authoredScale;
    }

    public void FlyToTarget(Vector3 targetPos, Action onComplete, float? customHeight = null, float? customDuration = null)
    {
        CancelTween();

        if (Trans == null) Trans = transform;
        _startPos = Trans.position;
        _targetPos = targetPos;

        var duration = customDuration.HasValue ? customDuration.Value : DeliveryDuration;
        var flightHeight = customHeight.HasValue ? customHeight.Value : (config != null ? config.Height : 1f);
        var ease = config != null ? config.EaseType : DG.Tweening.Ease.OutSine;

        _flyTween = DOTween.To(
                () => 0f,
                progress => UpdateCubePosition(_startPos, _targetPos, progress, flightHeight),
                1f,
                duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() =>
            {
                _flyTween = null;
                onComplete?.Invoke();
            });
        _flyTween.timeScale = GetPlaybackSpeed();
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        if (_flyTween != null && _flyTween.IsActive())
        {
            _flyTween.timeScale = GetPlaybackSpeed();
        }
    }

    private float GetPlaybackSpeed()
    {
        if (_customTimeScale <= 1f) return _customTimeScale;

        var multiplier = config != null ? config.ScaleSpeedMultiplier : 0.3f;
        return 1f + (_customTimeScale - 1f) * multiplier;
    }

    public float DeliveryDuration => config != null ? config.DeliveryDuration : 0.5f;
    public float SpawnerDuration => config != null ? config.SpawnerDuration : 0.15f;

    private void UpdateCubePosition(Vector3 startPos, Vector3 targetPos, float progress, float height)
    {
        if (Trans == null) return;
        var nextPos = Vector3.Lerp(startPos, targetPos, progress);
        nextPos.y += Mathf.Sin(progress * Mathf.PI) * height;
        Trans.position = nextPos;
    }
}

[Serializable]
public class CubeDeliveryConfig
{
    public DG.Tweening.Ease EaseType = DG.Tweening.Ease.OutSine;
    public float Height = 1f;
    public float DeliveryDuration = 0.5f;
    public float SpawnerDuration = 0.15f;
    public float ScaleSpeedMultiplier = 0.3f;
}
