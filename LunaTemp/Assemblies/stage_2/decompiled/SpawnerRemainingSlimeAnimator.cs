using System;
using DG.Tweening;
using UnityEngine;

public sealed class SpawnerRemainingSlimeAnimator : MonoBehaviour, ICustomTimeScaleTarget
{
	[SerializeField]
	private Transform targetTransform;

	[SerializeField]
	private float scaleDuration = 0.35f;

	[SerializeField]
	private Ease scaleEase = Ease.OutBack;

	[SerializeField]
	private bool enablePulseOnScaleUp = true;

	[SerializeField]
	private float pulseScaleAmount = 1.15f;

	[SerializeField]
	private float pulseDuration = 0.15f;

	[SerializeField]
	private Ease pulseEase = Ease.OutQuad;

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
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(this);
		}
	}

	private void OnDisable()
	{
		Cancel();
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.RemoveTarget(this);
		}
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
		targetTransform.localScale = (hasNext ? _initialScale : Vector3.zero);
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
		if (_scaleTween != null)
		{
			_scaleTween.Kill();
			_scaleTween = null;
		}
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			if (targetTransform == null)
			{
				targetTransform = base.transform;
			}
			_initialScale = targetTransform.localScale;
		}
	}
}
