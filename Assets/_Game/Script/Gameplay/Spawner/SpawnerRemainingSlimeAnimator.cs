using System;
using UnityEngine;

public sealed class SpawnerRemainingSlimeAnimator : MonoBehaviour, ICustomTimeScaleTarget
{
    [SerializeField] private Transform targetTransform;

    private Vector3 _initialScale = Vector3.one;
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
        targetTransform.localScale = _initialScale;
        onComplete?.Invoke();
    }

    public void PlayScaleDown(float? customDuration = null)
    {
        Initialize();
        Cancel();
        targetTransform.localScale = Vector3.zero;
    }

    public void Cancel()
    {
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (targetTransform == null) targetTransform = transform;
        _initialScale = targetTransform.localScale;
    }

}
