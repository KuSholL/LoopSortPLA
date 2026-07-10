using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class GiftBoxVisual : MonoBehaviour
{
	[SerializeField]
	private Transform giftBoxRenderer;

	[SerializeField]
	private Transform lidRenderer;

	[SerializeField]
	private List<MeshRenderer> ribbonRenderers = new List<MeshRenderer>();

	[SerializeField]
	private float lidDuration = 0.25f;

	[SerializeField]
	private Ease lidEase = Ease.OutQuad;

	[SerializeField]
	private float giftBoxDuration = 0.25f;

	[SerializeField]
	private Ease giftBoxEase = Ease.OutQuad;

	private Vector3 _initialLidScale;

	private Vector3 _initialGiftBoxScale;

	private readonly List<Vector3> _initialRibbonScales = new List<Vector3>();

	private Sequence _sequence;

	private Tween _tieTween;

	private bool _initialized;

	private void Awake()
	{
		Initialize();
	}

	private void OnDisable()
	{
		KillAnimation();
	}

	public void ResetVisual()
	{
		Initialize();
		KillAnimation();
		if (lidRenderer != null)
		{
			lidRenderer.gameObject.SetActive(true);
			lidRenderer.localScale = _initialLidScale;
		}
		if (giftBoxRenderer != null)
		{
			giftBoxRenderer.gameObject.SetActive(true);
			giftBoxRenderer.localScale = _initialGiftBoxScale;
		}
		for (int i = 0; i < ribbonRenderers.Count; i++)
		{
			MeshRenderer ribbon = ribbonRenderers[i];
			if (!(ribbon == null))
			{
				ribbon.gameObject.SetActive(true);
				ribbon.transform.localScale = ((i < _initialRibbonScales.Count) ? _initialRibbonScales[i] : Vector3.one);
			}
		}
		AlignRibbonRotation();
	}

	public void PlayOpenAnimation(Action onComplete)
	{
		KillAnimation();
		_sequence = DOTween.Sequence();
		for (int i = 0; i < ribbonRenderers.Count; i++)
		{
			if (ribbonRenderers[i] != null)
			{
				ribbonRenderers[i].gameObject.SetActive(false);
			}
		}
		if (lidRenderer != null)
		{
			_sequence.Append(lidRenderer.DOScale(new Vector3(lidRenderer.localScale.x, lidRenderer.localScale.y, 0f), lidDuration).SetEase(lidEase));
			_sequence.AppendCallback(delegate
			{
				lidRenderer.gameObject.SetActive(false);
			});
		}
		if (giftBoxRenderer != null)
		{
			_sequence.Append(giftBoxRenderer.DOScale(new Vector3(giftBoxRenderer.localScale.x, 0f, giftBoxRenderer.localScale.z), giftBoxDuration).SetEase(giftBoxEase));
			_sequence.Append(giftBoxRenderer.DOScale(Vector3.zero, giftBoxDuration).SetEase(giftBoxEase));
			_sequence.AppendCallback(delegate
			{
				giftBoxRenderer.gameObject.SetActive(false);
			});
		}
		_sequence.OnComplete(delegate
		{
			_sequence = null;
			onComplete?.Invoke();
		});
	}

	public void PlayRibbonDisappearAnimation(Action onComplete)
	{
		KillAnimation();
		bool hasRibbon = false;
		for (int j = 0; j < ribbonRenderers.Count; j++)
		{
			MeshRenderer ribbon = ribbonRenderers[j];
			if (!(ribbon == null))
			{
				hasRibbon = true;
			}
		}
		if (!hasRibbon)
		{
			onComplete?.Invoke();
			return;
		}
		_sequence = DOTween.Sequence();
		for (int i = 0; i < ribbonRenderers.Count; i++)
		{
			MeshRenderer ribbon2 = ribbonRenderers[i];
			if (!(ribbon2 == null))
			{
				_sequence.Join(ribbon2.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
			}
		}
		_sequence.OnComplete(delegate
		{
			for (int k = 0; k < ribbonRenderers.Count; k++)
			{
				if (ribbonRenderers[k] != null)
				{
					ribbonRenderers[k].gameObject.SetActive(false);
				}
			}
			_sequence = null;
			onComplete?.Invoke();
		});
	}

	public void PlayTieScaleAnimation()
	{
		Initialize();
		if (ribbonRenderers.Count != 0 && !(ribbonRenderers[0] == null))
		{
			Transform target = ribbonRenderers[0].transform;
			Vector3 initial = ((_initialRibbonScales.Count > 0) ? _initialRibbonScales[0] : target.localScale);
			if (_tieTween != null)
			{
				_tieTween.Kill();
			}
			target.localScale = initial;
			_tieTween = DOTween.Sequence().Append(target.DOScale(initial * 1.13f, 0.08f).SetEase(Ease.OutQuad)).Append(target.DOScale(initial, 0.08f).SetEase(Ease.OutQuad))
				.OnComplete(delegate
				{
					_tieTween = null;
				});
		}
	}

	public void ApplyColor(EBlockColorType colorType)
	{
		StylizedColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetStylizedColorConfig() : null);
		if (config == null)
		{
			return;
		}
		StylizedColorEntry entry = config.GetColorEntry(colorType);
		if (entry == null)
		{
			return;
		}
		for (int rendererIndex = 0; rendererIndex < ribbonRenderers.Count; rendererIndex++)
		{
			MeshRenderer target = ribbonRenderers[rendererIndex];
			if (!(target == null))
			{
				for (int materialIndex = 0; materialIndex < target.sharedMaterials.Length; materialIndex++)
				{
					target.ApplyColorEntry(entry, materialIndex);
				}
			}
		}
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			if (lidRenderer != null)
			{
				_initialLidScale = lidRenderer.localScale;
			}
			if (giftBoxRenderer != null)
			{
				_initialGiftBoxScale = giftBoxRenderer.localScale;
			}
			_initialRibbonScales.Clear();
			for (int i = 0; i < ribbonRenderers.Count; i++)
			{
				_initialRibbonScales.Add((ribbonRenderers[i] != null) ? ribbonRenderers[i].transform.localScale : Vector3.one);
			}
		}
	}

	private void AlignRibbonRotation()
	{
		if (ribbonRenderers.Count > 0 && ribbonRenderers[0] != null)
		{
			ribbonRenderers[0].transform.rotation = Quaternion.identity;
		}
	}

	private void KillAnimation()
	{
		if (_sequence != null)
		{
			_sequence.Kill();
			_sequence = null;
		}
		if (_tieTween != null)
		{
			_tieTween.Kill();
			_tieTween = null;
		}
	}
}
