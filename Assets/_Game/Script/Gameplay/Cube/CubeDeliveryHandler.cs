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

    private void OnValidate()
    {
        Trans = GetComponent<Transform>();
    }

    private void Awake()
    {
        if (Trans == null) Trans = transform;
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
        var defaultScale = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetCubeDefaultScale()
            : Vector3.one;
        Trans.localScale = defaultScale;
    }

    public void FlyToTarget(Vector3 targetPos, Action onComplete, float? customHeight = null, float? customDuration = null)
    {
        CancelTween();

        if (Trans == null) Trans = transform;
        _startPos = Trans.position;
        _targetPos = targetPos;

        var duration = customDuration.HasValue ? customDuration.Value : 0.5f;
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
        _flyTween.timeScale = _customTimeScale;
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        if (_flyTween != null && _flyTween.IsActive())
        {
            _flyTween.timeScale = _customTimeScale;
        }
    }

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
    public float Speed = 10f;
    public DG.Tweening.Ease EaseType = DG.Tweening.Ease.OutSine;
    public float Height = 1f;
}
