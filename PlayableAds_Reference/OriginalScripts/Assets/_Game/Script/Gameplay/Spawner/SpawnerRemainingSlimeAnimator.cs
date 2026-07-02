using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class SpawnerRemainingSlimeAnimator : MonoBehaviour, ICustomTimeScaleTarget
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float scaleDuration = 0.35f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    
    [Header("Pulse Settings")]
    [SerializeField] private bool enablePulseOnScaleUp = true;
    [SerializeField] private float pulseScaleAmount = 1.15f;
    [SerializeField] private float pulseDuration = 0.15f;
    [SerializeField] private Ease pulseEase = Ease.OutQuad;

    private Vector3 _initialScale = Vector3.one;
    private MotionHandle _scaleHandle;
    private bool _hasInit;
    private float _customTimeScale = 1f;

    private void Awake()
    {
        InitIfNeeded();
    }

    private void OnEnable()
    {
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.AddTarget(this);
        }
    }

    private void InitIfNeeded()
    {
        if (_hasInit) return;
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
        _initialScale = targetTransform.localScale;
        _hasInit = true;
    }

    private void OnDisable()
    {
        Cancel();
        if (CustomTimeScaleGroup.Instance != null)
        {
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
        }
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        bool isActive = _scaleHandle.IsActive();
        Debug.Log($"[SlimeAnimator] SetCustomTimeScale({timeScale}) _customTimeScale={_customTimeScale} isActive={isActive}");
        if (isActive)
        {
            _scaleHandle.PlaybackSpeed = _customTimeScale;
        }
    }

    public void Cancel()
    {
        if (_scaleHandle.IsActive())
        {
            _scaleHandle.TryCancel();
        }
    }

    public void SetScaleImmediate(bool hasNext)
    {
        InitIfNeeded();
        Cancel();
        if (targetTransform == null) return;
        targetTransform.localScale = hasNext ? _initialScale : Vector3.zero;
    }

    public void PlayScaleUp(System.Action onComplete = null)
    {
        InitIfNeeded();
        Cancel();
        if (targetTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        targetTransform.localScale = Vector3.zero;
        
        if (enablePulseOnScaleUp)
        {
            // Phase 1: scale up to pulse size, then chain phase 2 via WithOnComplete
            _scaleHandle = LMotion.Create(Vector3.zero, _initialScale * pulseScaleAmount, scaleDuration * 0.6f)
                .WithEase(scaleEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    // Phase 2: scale down to normal — only if not cancelled
                    if (targetTransform == null) return;
                    _scaleHandle = LMotion.Create(_initialScale * pulseScaleAmount, _initialScale, scaleDuration * 0.4f)
                        .WithEase(pulseEase)
                        .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                        .WithOnComplete(() => onComplete?.Invoke())
                        .BindToLocalScale(targetTransform);
                    _scaleHandle.PlaybackSpeed = _customTimeScale;
                })
                .BindToLocalScale(targetTransform);
            _scaleHandle.PlaybackSpeed = _customTimeScale;
        }
        else
        {
            var motion = LMotion.Create(Vector3.zero, _initialScale, scaleDuration)
                .WithEase(scaleEase)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                
            if (onComplete != null)
            {
                motion = motion.WithOnComplete(onComplete);
            }
            
            _scaleHandle = motion.BindToLocalScale(targetTransform);
            _scaleHandle.PlaybackSpeed = _customTimeScale;
        }
    }

    public void PlayScaleDown(float? customDuration = null)
    {
        InitIfNeeded();
        Cancel();
        if (targetTransform == null) return;

        float duration = customDuration ?? scaleDuration;
        Debug.Log($"[SlimeAnimator] PlayScaleDown(duration={duration}) _customTimeScale={_customTimeScale}");
        _scaleHandle = LMotion.Create(targetTransform.localScale, Vector3.zero, duration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(targetTransform);
        _scaleHandle.PlaybackSpeed = _customTimeScale;
        Debug.Log($"[SlimeAnimator] PlayScaleDown handle PlaybackSpeed={_scaleHandle.PlaybackSpeed}");
    }
}
