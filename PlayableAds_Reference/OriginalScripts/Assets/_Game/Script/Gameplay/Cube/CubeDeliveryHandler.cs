using System;
using System.Threading;
using LitMotion;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CubeDeliveryHandler : MonoBehaviour, ICustomTimeScaleTarget
{
    public Transform Trans;
    [SerializeField] private CubeDeliveryConfig config;

    private MotionHandle _flyHandle;
    private CancellationTokenSource _flyTokenSource;
    private float _customTimeScale = 1f;
    private Vector3 _startPos;
    private Vector3 _targetPos;

    private void OnValidate()
    {
        Trans =  GetComponent<Transform>();
    }

    private void OnDestroy()
    {
        CancelHandle();
        CancelTokenSource();
    }

    private void OnDisable()
    {
        CancelHandle();
        CancelTokenSource();
        ResetCubeScale();
        _customTimeScale = 1f;
    }
    
    private void CancelHandle()
    {
        if (_flyHandle.IsActive()) _flyHandle.TryCancel();
    }

    private void CancelTokenSource()
    {
        _flyTokenSource?.Cancel();
        _flyTokenSource?.Dispose();
        _flyTokenSource = new CancellationTokenSource();
    }

    private void ResetCubeScale()
    {
        var defaultScale = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetCubeDefaultScale()
            : Vector3.one;
        Trans.localScale = defaultScale;
    }

    public async UniTask FlyToTarget(Vector3 targetPos, Action onComplete, float? customHeight = null, float? customDuration = null)
    {
        CancelHandle();
        CancelTokenSource();
        _startPos = Trans.position;
        _targetPos = targetPos;
        var duration = customDuration ?? 0.5f;
        var flightHeight = customHeight ?? (config != null ? config.Height : 1f);

        _flyHandle = LMotion.Create(0f, 1f, duration)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(config.EaseType)
            .Bind(this, (progress, handler) => handler.UpdateCubePosition(handler._startPos, handler._targetPos, progress, flightHeight));
        _flyHandle.PlaybackSpeed = _customTimeScale;
#if LITMOTION_SUPPORT_UNITASK
        try
        {
            await _flyHandle.ToUniTask(cancellationToken: _flyTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
#endif
        onComplete?.Invoke();
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        if (_flyHandle.IsActive()) _flyHandle.PlaybackSpeed = _customTimeScale;
    }

    private void UpdateCubePosition(Vector3 startPos, Vector3 targetPos, float progress, float height)
    {
        var nextPos = Vector3.Lerp(startPos, targetPos, progress);
        nextPos.y += Mathf.Sin(progress * Mathf.PI) * height;
        Trans.position = nextPos;
    }

    private float CaculateDuration(Vector3 startPos = default, Vector3 targetPos = default)
    {
        var distance = Vector3.Distance(startPos, targetPos);
        var speed = Mathf.Max(config.Speed, 0.01f);
        var duration = distance / speed;
        return duration;
    }
}

[Serializable]
public class CubeDeliveryConfig
{
    public float Speed = 10f;
    public Ease EaseType = Ease.OutSine;
    public float Height = 1f;
}
