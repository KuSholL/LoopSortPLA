using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public class HiddenCarrierVisual : CarrierMechanicVisual
{
    [SerializeField] private List<Renderer> targetRenderers = new();

    [Header("Disappear Animation Config")] 
    [SerializeField] private float flyDistance = 1.5f;
    [SerializeField] private float flyDuration = 0.3f;
    [SerializeField] private float rotateDuration = 0.2f;
    [SerializeField] private float flyOutDuration = 0.3f;
    [SerializeField] private float screenMargin = 120f;
    
    private MaterialPropertyBlock _materialBlock;
    private MotionHandle _disappearHandle;
    private CancellationTokenSource _cancelTokenSource;
    private Action _beforeDisappearCallback;

    private void OnValidate()
    {
        if (targetRenderers.Count == 0) targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }

    private void OnDisable()
    {
        CancelAnimation();
        ResetVisualTransform();
    }

    private void OnDestroy() => CancelAnimation();

    public override void ApplyVisualRequest(CarrierVisualRequest request)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetCubeColorConfig() : null;
        var colorType = request != null ? request.ColorType : EBlockColorType.None;
        var colorEntry = config != null ? config.GetColorEntry(colorType) : null;
        ApplyColors(colorEntry ?? new ColorEntry());
    }

    public override void SetBeforeDisappearCallback(Action callback)
    {
        _beforeDisappearCallback = callback;
    }

    public override async UniTask PlayDisappearAnimationAsync()
    {
        CancelAnimation();
        _cancelTokenSource = new CancellationTokenSource();
        try
        {
            await PlayMoveUpAsync();
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_hiddenbox_whoosh);
            await PlayRotateY180Async();
            await PlayFlyOutOfScreenAsync();
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            ResetVisualTransform();
        }
    }

    private void CancelAnimation()
    {
        if (_disappearHandle.IsActive()) _disappearHandle.TryCancel();
        if (_cancelTokenSource == null) return;
        _cancelTokenSource.Cancel();
        _cancelTokenSource.Dispose();
        _cancelTokenSource = null;
    }

    private void ResetVisualTransform()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private UniTask PlayMoveUpAsync()
    {
        _disappearHandle = LMotion.Create(Vector3.zero, new Vector3(0f, flyDistance, 0f), flyDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(x => transform.localPosition = x);
        return _disappearHandle.ToUniTask(cancellationToken: _cancelTokenSource.Token);
    }

    private UniTask PlayRotateY180Async()
    {
        _beforeDisappearCallback?.Invoke();
        _beforeDisappearCallback = null;
        var startY = transform.localEulerAngles.y;
        var targetY = startY + 180f;
        _disappearHandle = LMotion.Create(startY, targetY, rotateDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(y =>
            {
                var localEuler = transform.localEulerAngles;
                transform.localRotation = Quaternion.Euler(localEuler.x, y, localEuler.z);
            });
        return _disappearHandle.ToUniTask(cancellationToken: _cancelTokenSource.Token);
    }

    private UniTask PlayFlyOutOfScreenAsync()
    {
        var targetPosition = GetFlyOutTargetPosition();
        _disappearHandle = LMotion.Create(transform.position, targetPosition, flyOutDuration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(x => transform.position = x);
        return _disappearHandle.ToUniTask(cancellationToken: _cancelTokenSource.Token);
    }

    private Vector3 GetFlyOutTargetPosition()
    {
        var cam = CameraManager.Instance != null ? CameraManager.Instance.MainCamera : Camera.main;
        if (cam == null) return transform.position + (Vector3.right * flyDistance);

        var screenPosition = cam.WorldToScreenPoint(transform.position);
        var isNearLeftSide = screenPosition.x < Screen.width * 0.5f;
        var targetScreenX = isNearLeftSide ? -screenMargin : Screen.width + screenMargin;
        var targetScreenPosition = new Vector3(targetScreenX, screenPosition.y, screenPosition.z);
        return cam.ScreenToWorldPoint(targetScreenPosition);
    }

    private void ApplyColors(ColorEntry entry)
    {
        _materialBlock ??= new MaterialPropertyBlock();
        foreach (var targetRenderer in targetRenderers)
        {
            if (targetRenderer == null) continue;
            targetRenderer.GetPropertyBlock(_materialBlock, 0);
            SetMaterialColors(entry);
            targetRenderer.SetPropertyBlock(_materialBlock, 0);
        }
    }

    private void SetMaterialColors(ColorEntry entry)
    {
        _materialBlock.SetColorEntry(entry);
    }
}
