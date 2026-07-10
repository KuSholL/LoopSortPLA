using System;
using UnityEngine;
using DG.Tweening;

public sealed class SpawnerRemainingSlimeAnimator : MonoBehaviour, ICustomTimeScaleTarget
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float scaleDuration = 0.35f;
    [SerializeField] private DG.Tweening.Ease scaleEase = DG.Tweening.Ease.OutBack;
    [SerializeField] private bool enablePulseOnScaleUp = true;
    [SerializeField] private float pulseScaleAmount = 1.15f;
    [SerializeField] private float pulseDuration = 0.15f;
    [SerializeField] private DG.Tweening.Ease pulseEase = DG.Tweening.Ease.OutQuad;

    private Vector3 _initialScale = Vector3.one;
    private Tween _scaleTween;
    private float _customTimeScale = 1f;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (CustomTimeScaleGroup.Instance != null)
            CustomTimeScaleGroup.Instance.AddTarget(this);
    }

    private void OnDisable()
    {
        Cancel();
        if (CustomTimeScaleGroup.Instance != null)
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.timeScale = _customTimeScale;
        }
    }

    public void SetScaleImmediate(bool hasNext)
    {
        Initialize();
        Cancel();
        targetTransform.localScale = hasNext ? _initialScale : Vector3.zero;
    }

    public void PlayScaleUp(Action onComplete = null)
    {
        Initialize();
        Cancel();
        targetTransform.localScale = Vector3.zero;
        if (enablePulseOnScaleUp)
        {
            _scaleTween = DOTween.Sequence()
                .Append(targetTransform
                    .DOScale(_initialScale * pulseScaleAmount, Mathf.Max(0.01f, scaleDuration - pulseDuration))
                    .SetEase(scaleEase))
                .Append(targetTransform
                    .DOScale(_initialScale, Mathf.Max(0.01f, pulseDuration))
                    .SetEase(pulseEase))
                .OnComplete(() =>
                {
                    _scaleTween = null;
                    onComplete?.Invoke();
                });
        }
        else
        {
            _scaleTween = targetTransform
                .DOScale(_initialScale, scaleDuration)
                .SetEase(scaleEase)
                .OnComplete(() =>
                {
                    _scaleTween = null;
                    onComplete?.Invoke();
                });
        }
        _scaleTween.SetUpdate(true);
        _scaleTween.timeScale = _customTimeScale;
    }

    public void PlayScaleDown(float? customDuration = null)
    {
        Initialize();
        Cancel();
        _scaleTween = targetTransform
            .DOScale(Vector3.zero, Mathf.Max(0.01f, customDuration ?? scaleDuration))
            .SetEase(DG.Tweening.Ease.InQuad)
            .SetUpdate(true);
        _scaleTween.timeScale = _customTimeScale;
    }

    public void Cancel()
    {
        if (_scaleTween != null)
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (targetTransform == null) targetTransform = transform;
        _initialScale = targetTransform.localScale;
    }

}
