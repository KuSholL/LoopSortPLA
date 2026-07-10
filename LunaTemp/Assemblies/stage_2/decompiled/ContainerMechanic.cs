using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class ContainerMechanic : MonoBehaviour
{
	private static readonly List<ContainerMechanic> ActiveContainers = new List<ContainerMechanic>();

	[SerializeField]
	private int containerId = -1;

	[SerializeField]
	private bool isOpen;

	[SerializeField]
	private EBlockColorType unlockColor = EBlockColorType.None;

	[SerializeField]
	private List<CarrierContainerMember> carriers = new List<CarrierContainerMember>();

	[SerializeField]
	private GiftBoxVisual giftBoxVisual1X;

	[SerializeField]
	private GiftBoxVisual giftBoxVisual2X;

	[SerializeField]
	private GiftBoxVisual giftBoxVisual3X;

	[SerializeField]
	private GameObject ribbonParticle;

	[SerializeField]
	private Key3DCodeAnimator keyAnimator;

	[SerializeField]
	private ParticleSystem[] particleSystems;

	[SerializeField]
	private float delayVfx = 0.5f;

	private Vector3 _targetScale = Vector3.one;

	private Tween _scaleTween;

	private Coroutine _openRoutine;

	private bool _isOpening;

	private bool _isLidOpening;

	private bool _isAssignedToUnlock;

	public Vector3 TargetScale => _targetScale;

	public Key3DCodeAnimator KeyAnimator => keyAnimator;

	public bool IsOpen => isOpen;

	public bool IsOpening => _isOpening;

	public bool IsLidOpening => _isLidOpening;

	public int ContainerId => containerId;

	public bool IsAssignedToUnlock
	{
		get
		{
			return _isAssignedToUnlock;
		}
		set
		{
			_isAssignedToUnlock = value;
		}
	}

	public static ContainerMechanic FindTarget(int targetContainerId, EBlockColorType colorType)
	{
		for (int i = 0; i < ActiveContainers.Count; i++)
		{
			ContainerMechanic container = ActiveContainers[i];
			if (container != null && container.containerId == targetContainerId && container.CanOpenWith(colorType) && !container.IsOpen && !container.IsOpening)
			{
				return container;
			}
		}
		return null;
	}

	public static void UnlockTarget(int targetContainerId, EBlockColorType colorType)
	{
		ContainerMechanic target = FindTarget(targetContainerId, colorType);
		if (target != null)
		{
			target.BeginOpen();
		}
	}

	private void Awake()
	{
		BindExistingCarriers();
	}

	private void OnEnable()
	{
		if (!ActiveContainers.Contains(this))
		{
			ActiveContainers.Add(this);
		}
		ResetRuntimeVisuals();
	}

	private void OnDisable()
	{
		ActiveContainers.Remove(this);
		CancelAnimations();
	}

	public void Configure(int targetContainerId, EBlockColorType colorType)
	{
		containerId = targetContainerId;
		unlockColor = colorType;
		isOpen = false;
		_isOpening = false;
		_isLidOpening = false;
		_isAssignedToUnlock = false;
		carriers.Clear();
		ApplyVisuals(colorType);
		ResetGiftBoxVisual(giftBoxVisual1X, colorType);
		ResetGiftBoxVisual(giftBoxVisual2X, colorType);
		ResetGiftBoxVisual(giftBoxVisual3X, colorType);
		if (keyAnimator != null)
		{
			keyAnimator.SetActiveState(false);
		}
		if (ribbonParticle != null)
		{
			ribbonParticle.SetActive(false);
		}
		RefreshVisualState();
	}

	public void SetTargetScale(Vector3 scale)
	{
		_targetScale = scale;
	}

	public void SetScaleTween(Tween tween)
	{
		if (_scaleTween != null)
		{
			_scaleTween.Kill();
		}
		_scaleTween = tween;
	}

	public void SetOpenSilently()
	{
		isOpen = true;
		_isOpening = false;
		_isLidOpening = false;
		_isAssignedToUnlock = false;
		RefreshVisualState();
		RefreshCarriers();
	}

	public void AddCarrier(CarrierContainerMember carrier)
	{
		if (!(carrier == null) && !carriers.Contains(carrier))
		{
			carriers.Add(carrier);
			carrier.Bind(this);
			RefreshCarrier(carrier);
			UpdateGiftBoxVisuals();
		}
	}

	public void StartUnlockSequence(Vector3 startPosition, Quaternion startRotation, Vector3 startScale, float flyDuration, Ease flyEase)
	{
		if (isOpen || _isOpening)
		{
			return;
		}
		_isOpening = true;
		SetCollidersEnabled(false);
		if (keyAnimator == null)
		{
			BeginOpen();
			return;
		}
		keyAnimator.PlayFlyAndUnlockAnimation(startPosition, startRotation, startScale, flyDuration, flyEase, PlayTieScaleAnimation, delegate
		{
			PlayTieScaleAnimation();
			BeginOpen();
		});
	}

	public void PlayTieScaleAnimation()
	{
		GiftBoxVisual activeVisual = GetActiveGiftBoxVisual();
		if (activeVisual != null)
		{
			activeVisual.PlayTieScaleAnimation();
		}
	}

	public void UpdateGiftBoxVisuals()
	{
		int activeIndex = Mathf.Clamp((carriers != null) ? carriers.Count : 0, 1, 3);
		if (giftBoxVisual1X != null)
		{
			giftBoxVisual1X.gameObject.SetActive(activeIndex == 1);
		}
		if (giftBoxVisual2X != null)
		{
			giftBoxVisual2X.gameObject.SetActive(activeIndex == 2);
		}
		if (giftBoxVisual3X != null)
		{
			giftBoxVisual3X.gameObject.SetActive(activeIndex == 3);
		}
	}

	private void BeginOpen()
	{
		if (!isOpen)
		{
			_isOpening = true;
			if (_openRoutine != null)
			{
				StopCoroutine(_openRoutine);
			}
			_openRoutine = StartCoroutine(OpenRoutine());
		}
	}

	private IEnumerator OpenRoutine()
	{
		SetCollidersEnabled(false);
		if (delayVfx > 0f)
		{
			yield return new WaitForSeconds(delayVfx);
		}
		GiftBoxVisual visual = GetActiveGiftBoxVisual();
		bool ribbonDone = false;
		if (visual != null)
		{
			visual.PlayRibbonDisappearAnimation(delegate
			{
				ribbonDone = true;
			});
			while (!ribbonDone)
			{
				yield return null;
			}
		}
		if (ribbonParticle != null)
		{
			ribbonParticle.SetActive(true);
		}
		_isLidOpening = true;
		bool openDone = false;
		if (visual != null)
		{
			visual.PlayOpenAnimation(delegate
			{
				openDone = true;
			});
			while (!openDone)
			{
				yield return null;
			}
		}
		isOpen = true;
		_isOpening = false;
		_isLidOpening = false;
		_isAssignedToUnlock = false;
		_openRoutine = null;
		RefreshVisualState();
		RefreshCarriers();
		if (GameEventBus.OnContainerUnlocked != null)
		{
			GameEventBus.OnContainerUnlocked();
		}
	}

	private void ResetRuntimeVisuals()
	{
		_isOpening = false;
		_isLidOpening = false;
		_isAssignedToUnlock = false;
		ApplyVisuals(unlockColor);
		ResetGiftBoxVisual(giftBoxVisual1X, unlockColor);
		ResetGiftBoxVisual(giftBoxVisual2X, unlockColor);
		ResetGiftBoxVisual(giftBoxVisual3X, unlockColor);
		if (keyAnimator != null)
		{
			keyAnimator.SetActiveState(false);
		}
		if (ribbonParticle != null)
		{
			ribbonParticle.SetActive(false);
		}
		RefreshVisualState();
	}

	private static void ResetGiftBoxVisual(GiftBoxVisual visual, EBlockColorType color)
	{
		if (!(visual == null))
		{
			visual.ResetVisual();
			visual.ApplyColor(color);
		}
	}

	private GiftBoxVisual GetActiveGiftBoxVisual()
	{
		if (giftBoxVisual1X != null && giftBoxVisual1X.gameObject.activeInHierarchy)
		{
			return giftBoxVisual1X;
		}
		if (giftBoxVisual2X != null && giftBoxVisual2X.gameObject.activeInHierarchy)
		{
			return giftBoxVisual2X;
		}
		if (giftBoxVisual3X != null && giftBoxVisual3X.gameObject.activeInHierarchy)
		{
			return giftBoxVisual3X;
		}
		return null;
	}

	private bool CanOpenWith(EBlockColorType colorType)
	{
		return unlockColor != EBlockColorType.None && unlockColor == colorType;
	}

	private void ApplyVisuals(EBlockColorType colorType)
	{
		ColorConfigSO colorConfig = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetColorConfig() : null);
		ColorEntry entry = ((colorConfig != null) ? colorConfig.GetColorEntry(colorType) : PlayableColorFallback.CreateColorEntry(colorType));
		if (entry != null && particleSystems != null)
		{
			for (int i = 0; i < particleSystems.Length; i++)
			{
				if (!(particleSystems[i] == null))
				{
					ParticleSystem.MainModule main = particleSystems[i].main;
					main.startColor = entry.Color;
				}
			}
		}
		if (keyAnimator != null)
		{
			keyAnimator.SetKeyColor(colorType);
		}
	}

	private void BindExistingCarriers()
	{
		for (int i = 0; i < carriers.Count; i++)
		{
			if (!(carriers[i] == null))
			{
				carriers[i].Bind(this);
				RefreshCarrier(carriers[i]);
			}
		}
		UpdateGiftBoxVisuals();
	}

	private void RefreshCarriers()
	{
		for (int i = 0; i < carriers.Count; i++)
		{
			RefreshCarrier(carriers[i]);
		}
	}

	private static void RefreshCarrier(CarrierContainerMember member)
	{
		if (member != null && member.Carrier != null)
		{
			member.Carrier.RefreshMechanicVisualState();
		}
	}

	private void RefreshVisualState()
	{
		SetRenderersVisible(!isOpen);
		SetCollidersEnabled(!isOpen && !_isOpening);
	}

	private void SetRenderersVisible(bool visible)
	{
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = visible;
		}
	}

	private void SetCollidersEnabled(bool enabled)
	{
		Collider[] colliders = GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < colliders.Length; i++)
		{
			colliders[i].enabled = enabled;
		}
	}

	private void CancelAnimations()
	{
		if (_openRoutine != null)
		{
			StopCoroutine(_openRoutine);
			_openRoutine = null;
		}
		if (_scaleTween != null)
		{
			_scaleTween.Kill();
			_scaleTween = null;
		}
	}
}
