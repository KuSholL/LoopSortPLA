using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

public abstract class CarrierBase : MonoBehaviour, IClickableObject
{
	public Transform Trans;

	[Header("Block Config")]
	[SerializeField]
	protected Transform pivot;

	[Header("Pickup Settings")]
	[Tooltip("Offset of the pickup point from the spline position in meters (can be positive or negative)")]
	[SerializeField]
	protected float pickupDistanceOffset = -1f;

	protected SplineContainer _cachedSplineContainer;

	protected float _cachedSplineLength = -1f;

	protected bool _isSplineCached;

	protected CarrierRuntimeState _runtimeState;

	protected CarrierBlockController _blockController;

	protected CarrierUnloadPort _unloadPort;

	protected CarrierReceivePort _receivePort;

	protected int maxBlockCount = 4;

	protected Tween _scaleTween;

	private Collider[] _clickColliders;

	private static int _globalSessionId;

	public float SplineProgress { get; protected set; }

	public int SessionId { get; private set; }

	public virtual bool IsDelivering => _runtimeState != null && _runtimeState.IsUnloading;

	public abstract CarrierBlockLayoutBase BlockLayout { get; }

	protected ColorConfigSO colorConfigSO => ColorConfig;

	public ColorConfigSO ColorConfig
	{
		get
		{
			if (MonoSingleton<ConfigManager>.Instance != null)
			{
				ColorConfigSO config = MonoSingleton<ConfigManager>.Instance.GetColorConfig();
				if (config != null)
				{
					return config;
				}
			}
			return null;
		}
	}

	public CarrierRuntimeState RuntimeState => _runtimeState;

	public CarrierBlockController BlockController => _blockController;

	public abstract CarrierLinkedBlockVisualController LinkedBlockVisualController { get; }

	public abstract int MaxBlockCount { get; }

	public Transform Pivot => (pivot != null) ? pivot : base.transform;

	public bool Interactable { get; set; } = true;


	public bool LockPick { get; set; } = false;


	public virtual CarrierMechanicContainer MechanicContainer => null;

	public void IncrementSessionId()
	{
		SessionId = ++_globalSessionId;
	}

	public virtual bool IsSpecialReceiverForColor(EBlockColorType colorType)
	{
		return false;
	}

	public virtual bool IsLockedByContainer()
	{
		return false;
	}

	public virtual List<LinkedBlockVisual> GetLinkedBlockVisuals(int size)
	{
		return new List<LinkedBlockVisual>();
	}

	public virtual bool TryGetSpecialReceiverTargetColor(out EBlockColorType colorType)
	{
		colorType = EBlockColorType.None;
		return false;
	}

	public float GetActualPickupProgress()
	{
		if (!_isSplineCached)
		{
			_cachedSplineContainer = ((MonoSingleton<ConveyorDeliverySystem>.Instance != null) ? MonoSingleton<ConveyorDeliverySystem>.Instance.SplineContainer : null);
			if (_cachedSplineContainer != null && _cachedSplineContainer.Spline != null)
			{
				_cachedSplineLength = _cachedSplineContainer.Spline.CalculateLength(_cachedSplineContainer.transform.localToWorldMatrix);
			}
			_isSplineCached = true;
		}
		if (_cachedSplineLength > 0.001f)
		{
			float progressOffset = pickupDistanceOffset / _cachedSplineLength;
			return Mathf.Repeat(SplineProgress + progressOffset, 1f);
		}
		return Mathf.Repeat(SplineProgress, 1f);
	}

	protected virtual void Awake()
	{
		EnsureRuntime();
	}

	protected virtual void OnEnable()
	{
		LockPick = false;
		_isSplineCached = false;
	}

	protected virtual void OnDisable()
	{
		CancelScaleAnimation();
		base.transform.localScale = Vector3.one;
		_cachedSplineContainer = null;
		_cachedSplineLength = -1f;
		_isSplineCached = false;
	}

	public abstract bool CanBeClicked();

	public abstract void OnObjectClicked();

	public abstract void OnClickBlocked();

	public abstract void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false);

	public virtual void SetSplineProgress(float progress)
	{
		SplineProgress = Mathf.Repeat(progress, 1f);
	}

	public abstract void FinishUnloadCarrier();

	public abstract bool TryReserveReceive(EBlockColorType blockColorType, out CarrierReceiveReservation reservation, int undoBatchId = 0);

	public abstract bool CanPotentiallyReceive(EBlockColorType color);

	public abstract void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color);

	public abstract void EvaluateFinishState();

	public abstract bool CanUnloadByMechanic();

	public abstract bool CanReceiveByMechanic(EBlockColorType colorType);

	public abstract void RefreshMechanicVisualState();

	public abstract int GetClawTargetBlockCount();

	public abstract bool CanBeClawTarget();

	protected abstract void EnsureRuntime();

	protected abstract void ResetRuntime();

	public void SetScaleMotionHandle(Tween tween)
	{
		if (_scaleTween != null && _scaleTween.IsActive())
		{
			_scaleTween.Kill();
		}
		_scaleTween = tween;
	}

	public void CancelScaleAnimation()
	{
		if (_scaleTween != null && _scaleTween.IsActive())
		{
			_scaleTween.Kill();
		}
		_scaleTween = null;
	}

	public void SetClickCollidersEnabled(bool isEnabled)
	{
		if (_clickColliders == null || _clickColliders.Length == 0)
		{
			_clickColliders = GetComponents<Collider>();
		}
		if (_clickColliders == null)
		{
			return;
		}
		for (int i = 0; i < _clickColliders.Length; i++)
		{
			Collider target = _clickColliders[i];
			if (target != null)
			{
				target.enabled = isEnabled;
			}
		}
	}

	public virtual void SetLayer(int layer)
	{
		base.gameObject.layer = layer;
		SetChildrenLayer(base.transform, layer);
	}

	protected void SetChildrenLayer(Transform target, int layer)
	{
		if (!(target == null))
		{
			target.gameObject.layer = layer;
			for (int i = 0; i < target.childCount; i++)
			{
				SetChildrenLayer(target.GetChild(i), layer);
			}
		}
	}

	protected int GetConfiguredBlockCount()
	{
		CarrierConfigSO config = GetCarrierConfig();
		return (config != null) ? config.GetBlockCount() : maxBlockCount;
	}

	protected CarrierConfigSO GetCarrierConfig()
	{
		return (MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCarrierConfig() : null;
	}
}
