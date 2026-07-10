using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class HiddenCarrierVisual : CarrierMechanicVisual
{
	[SerializeField]
	private List<Renderer> targetRenderers = new List<Renderer>();

	[SerializeField]
	private float flyDistance = 1.5f;

	[SerializeField]
	private float flyDuration = 0.3f;

	[SerializeField]
	private float rotateDuration = 0.2f;

	[SerializeField]
	private float flyOutDuration = 0.3f;

	[SerializeField]
	private float screenMargin = 120f;

	private Sequence _sequence;

	private Action _beforeDisappearCallback;

	private void OnDisable()
	{
		CancelAnimation();
		ResetVisualTransform();
	}

	public override void ApplyVisualRequest(CarrierVisualRequest request)
	{
		ColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCubeColorConfig() : null);
		EBlockColorType color = request?.ColorType ?? EBlockColorType.None;
		ColorEntry entry = ((config != null) ? config.GetColorEntry(color) : PlayableColorFallback.CreateColorEntry(color));
		if (entry == null)
		{
			return;
		}
		for (int i = 0; i < targetRenderers.Count; i++)
		{
			Renderer target = targetRenderers[i];
			if (!(target == null))
			{
				target.ApplyColorEntry(entry, 0);
			}
		}
	}

	public override void SetBeforeDisappearCallback(Action callback)
	{
		_beforeDisappearCallback = callback;
	}

	public override void PlayDisappearAnimation(Action onComplete)
	{
		CancelAnimation();
		Vector3 flyOutTarget = GetFlyOutTargetPosition();
		_sequence = DOTween.Sequence().SetUpdate(true);
		_sequence.Append(base.transform.DOLocalMoveY(flyDistance, flyDuration).SetEase(Ease.OutQuad));
		_sequence.AppendCallback(delegate
		{
			if (MonoSingleton<SoundManager>.Instance != null)
			{
				MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_hiddenbox_whoosh);
			}
			_beforeDisappearCallback?.Invoke();
			_beforeDisappearCallback = null;
		});
		_sequence.Append(base.transform.DOLocalRotate(new Vector3(0f, base.transform.localEulerAngles.y + 180f, 0f), rotateDuration).SetEase(Ease.OutQuad));
		_sequence.Append(base.transform.DOMove(flyOutTarget, flyOutDuration).SetEase(Ease.InQuad));
		_sequence.OnComplete(delegate
		{
			_sequence = null;
			ResetVisualTransform();
			onComplete?.Invoke();
		});
	}

	private Vector3 GetFlyOutTargetPosition()
	{
		Camera camera = ((MonoSingleton<CameraManager>.Instance != null) ? MonoSingleton<CameraManager>.Instance.MainCamera : Camera.main);
		if (camera == null)
		{
			return base.transform.position + Vector3.right * flyDistance;
		}
		Vector3 screen = camera.WorldToScreenPoint(base.transform.position);
		screen.x = ((screen.x < (float)Screen.width * 0.5f) ? (0f - screenMargin) : ((float)Screen.width + screenMargin));
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
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
	}
}
