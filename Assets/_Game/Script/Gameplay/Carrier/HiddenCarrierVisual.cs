using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class HiddenCarrierVisual : CarrierMechanicVisual
{
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();
    [SerializeField] private float flyDistance = 1.5f;
    [SerializeField] private float flyDuration = 0.3f;
    [SerializeField] private float rotateDuration = 0.2f;
    [SerializeField] private float flyOutDuration = 0.3f;
    [SerializeField] private float screenMargin = 120f;

    private Sequence _sequence;
    private Action _beforeDisappearCallback;

    private void OnDisable()
    {
        CancelAnimation();
        ResetVisualTransform();
    }

    public override void ApplyVisualRequest(CarrierVisualRequest request)
    {
        var config = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetCubeColorConfig()
            : null;
        var color = request != null ? request.ColorType : EBlockColorType.None;
        var entry = config != null
            ? config.GetColorEntry(color)
            : PlayableColorFallback.CreateColorEntry(color);
        if (entry == null) return;

        for (var i = 0; i < targetRenderers.Count; i++)
        {
            var target = targetRenderers[i];
            if (target == null) continue;
            target.ApplyColorEntry(entry, 0);
        }
    }

    public override void SetBeforeDisappearCallback(Action callback)
    {
        _beforeDisappearCallback = callback;
    }

    public override void PlayDisappearAnimation(Action onComplete)
    {
        CancelAnimation();
        var flyOutTarget = GetFlyOutTargetPosition();
        _sequence = DOTween.Sequence().SetUpdate(true);
        _sequence.Append(transform.DOLocalMoveY(flyDistance, flyDuration).SetEase(DG.Tweening.Ease.OutQuad));
        _sequence.AppendCallback(() =>
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayOneShot(AudioClipName.sfx_hiddenbox_whoosh);
            _beforeDisappearCallback?.Invoke();
            _beforeDisappearCallback = null;
        });
        _sequence.Append(transform
            .DOLocalRotate(new Vector3(0f, transform.localEulerAngles.y + 180f, 0f), rotateDuration)
            .SetEase(DG.Tweening.Ease.OutQuad));
        _sequence.Append(transform.DOMove(flyOutTarget, flyOutDuration).SetEase(DG.Tweening.Ease.InQuad));
        _sequence.OnComplete(() =>
        {
            _sequence = null;
            ResetVisualTransform();
            onComplete?.Invoke();
        });
    }

    private Vector3 GetFlyOutTargetPosition()
    {
        var camera = CameraManager.Instance != null ? CameraManager.Instance.MainCamera : Camera.main;
        if (camera == null) return transform.position + Vector3.right * flyDistance;
        var screen = camera.WorldToScreenPoint(transform.position);
        screen.x = screen.x < Screen.width * 0.5f ? -screenMargin : Screen.width + screenMargin;
        return camera.ScreenToWorldPoint(screen);
    }

    private void CancelAnimation()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }
    }

    private void ResetVisualTransform()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

}
