using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using LitMotion;

public abstract class CarrierBase : MonoBehaviour, IClickableObject
{
    #region Fields
    public Transform Trans;
    
    [Header("Block Config")]
    [SerializeField] protected Transform pivot;

    [Header("Pickup Settings")]
    [Tooltip("Offset of the pickup point from the spline position in meters (can be positive or negative)")]
    [SerializeField] protected float pickupDistanceOffset = -1f;

    protected SplineContainer _cachedSplineContainer;
    protected float _cachedSplineLength = -1f;
    protected bool _isSplineCached;
    protected CarrierRuntimeState _runtimeState;
    protected CarrierBlockController _blockController;
    protected CarrierUnloadPort _unloadPort;
    protected CarrierReceivePort _receivePort;

    protected int maxBlockCount = 4;
    protected MotionHandle _scaleMotionHandle;

    #endregion

    #region Properties & Actions

    public float SplineProgress { get; protected set; }
    public int SessionId { get; private set; }
    private static int _globalSessionId = 0;

    public void IncrementSessionId()
    {
        SessionId = ++_globalSessionId;
    }

    public virtual bool IsDelivering => _runtimeState != null && _runtimeState.IsUnloading;
    public abstract CarrierBlockLayoutBase BlockLayout { get; }
    
    protected ColorConfigSO colorConfigSO => ColorConfig;
    public ColorConfigSO ColorConfig
    {
        get
        {
            if (ConfigManager.Instance != null)
            {
                var config = ConfigManager.Instance.GetColorConfig();
                if (config != null) return config;
            }
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ColorConfigSO>("Assets/_Game/Config/CoreGameConfig/ColorConfigSO.asset");
#else
            return null;
#endif
        }
    }
    public CarrierRuntimeState RuntimeState => _runtimeState;
    public CarrierBlockController BlockController => _blockController;
    public abstract CarrierLinkedBlockVisualController LinkedBlockVisualController { get; }
    public abstract int MaxBlockCount { get; }
    public Transform Pivot => pivot != null ? pivot : transform;
    public bool Interactable { get; set; } = true;
    public bool LockPick { get; set; } = false;
    public virtual CarrierMechanicContainer MechanicContainer => null;
    public virtual bool IsSpecialReceiverForColor(EBlockColorType colorType) => false;
    public virtual bool IsLockedByContainer() => false;
    public virtual List<LinkedBlockVisual> GetLinkedBlockVisuals(int size) => new List<LinkedBlockVisual>();
    public virtual bool TryGetSpecialReceiverTargetColor(out EBlockColorType colorType)
    {
        colorType = EBlockColorType.None;
        return false;
    }

    public float GetActualPickupProgress()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var container = ConveyorDeliverySystem.Instance != null 
                ? ConveyorDeliverySystem.Instance.SplineContainer 
                : FindObjectOfType<SplineContainer>();
            float length = -1f;
            if (container != null && container.Spline != null)
            {
                length = container.Spline.CalculateLength(container.transform.localToWorldMatrix);
            }
            if (length > 0.001f)
            {
                float progressOffset = pickupDistanceOffset / length;
                return Mathf.Repeat(SplineProgress + progressOffset, 1f);
            }
            return Mathf.Repeat(SplineProgress, 1f);
        }
#endif

        if (!_isSplineCached)
        {
            _cachedSplineContainer = ConveyorDeliverySystem.Instance != null 
                ? ConveyorDeliverySystem.Instance.SplineContainer 
                : FindObjectOfType<SplineContainer>();
            
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

    #endregion

    #region Unity Lifecycle

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
        transform.localScale = Vector3.one;
        _cachedSplineContainer = null;
        _cachedSplineLength = -1f;
        _isSplineCached = false;
    }

    public abstract bool CanBeClicked();
    public abstract void OnObjectClicked();
    public abstract void OnClickBlocked();

    #endregion

    #region Public Methods

    public abstract void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false);

    public virtual void SetSplineProgress(float progress)
    {
        SplineProgress = Mathf.Repeat(progress, 1f);
    }

    public abstract void FinishUnloadCarrier();

    public abstract bool TryReserveReceive(
        EBlockColorType blockColorType,
        out CarrierReceiveReservation reservation,
        int undoBatchId = 0);

    public abstract bool CanPotentiallyReceive(EBlockColorType color);

    public abstract void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color);

    public abstract void EvaluateFinishState();

    public abstract bool CanUnloadByMechanic();

    public abstract bool CanReceiveByMechanic(EBlockColorType colorType);

    public abstract void RefreshMechanicVisualState();

    public abstract int GetClawTargetBlockCount();

    public abstract bool CanBeClawTarget();

    #endregion

    #region Helper & Internal

    protected abstract void EnsureRuntime();
    protected abstract void ResetRuntime();

    public void SetScaleMotionHandle(MotionHandle handle)
    {
        if (_scaleMotionHandle.IsActive())
        {
            _scaleMotionHandle.TryCancel();
        }
        _scaleMotionHandle = handle;
    }

    public void CancelScaleAnimation()
    {
        if (_scaleMotionHandle.IsActive())
        {
            _scaleMotionHandle.TryCancel();
        }
    }

    public virtual void SetLayer(int layer)
    {
        gameObject.layer = layer;
        SetChildrenLayer(transform, layer);
    }

    protected void SetChildrenLayer(Transform target, int layer)
    {
        if (target == null) return;
        target.gameObject.layer = layer;
        for (var i = 0; i < target.childCount; i++)
            SetChildrenLayer(target.GetChild(i), layer);
    }

    protected int GetConfiguredBlockCount()
    {
        var config = GetCarrierConfig();
        return config != null ? config.GetBlockCount() : maxBlockCount;
    }

    protected CarrierConfigSO GetCarrierConfig()
    {
        return ConfigManager.Instance != null ? ConfigManager.Instance.GetCarrierConfig() : null;
    }

    #endregion
}
