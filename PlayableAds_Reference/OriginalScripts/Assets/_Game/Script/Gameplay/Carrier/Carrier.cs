using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using LitMotion;

/// <summary>
/// MonoBehaviour đại diện cho carrier và chỉ giữ lifecycle cùng API giao tiếp bên ngoài.
/// </summary>
public class Carrier : CarrierBase
{
    #region Fields

    [Header("Block Layout")]
    [SerializeField] protected CarrierBlockLayout blockLayout;

    [Header("Render")]
    [SerializeField] protected Transform hiddenVisualRoot;
    [SerializeField] protected CarrierMechanicVisualConfigSO mechanicVisualConfig;
    [SerializeField] protected CarrierLinkedBlockVisualConfigSO linkedBlockVisualConfig;
    [SerializeField] protected MeshRenderer[] specialColorReceiverCarrierMeshRenderer;

    protected CarrierVisualController _visualController;
    protected CarrierLinkedBlockVisualController _linkedBlockVisualController;
    protected readonly CarrierMechanicContainer _mechanicContainer = new();
    protected readonly CarrierActionGateResolver _actionGateResolver = new();
    protected readonly CarrierVisualResolver _visualResolver = new();

    protected bool _isWaitingHiddenCarrierReveal = false;

    #endregion

    #region Properties & Actions
    
    public override CarrierBlockLayoutBase BlockLayout => blockLayout;
    public override CarrierLinkedBlockVisualController LinkedBlockVisualController => _linkedBlockVisualController;
    public override int MaxBlockCount => GetConfiguredBlockCount();
    public override CarrierMechanicContainer MechanicContainer => _mechanicContainer;

    #endregion

    #region Unity Lifecycle

    protected override void OnEnable()
    {
        base.OnEnable();
        CarrierMechanicEventHub.OnEvent += HandleMechanicEvent;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CarrierMechanicEventHub.OnEvent -= HandleMechanicEvent;

        var member = GetComponent<CarrierContainerMember>();
        if (member != null)
        {
            member.Clear();
        }

        _linkedBlockVisualController?.Reset();
        _mechanicContainer.Reset(this);
        _visualController?.Reset();
        _isWaitingHiddenCarrierReveal = false;
        SetBlockLayoutRootVisible(true);
    }

    /// <summary>
    /// Kiểm tra xem carrier có đủ điều kiện để click (unload) hay không.
    /// </summary>
    public override bool CanBeClicked()
    {
        EnsureRuntime();
        if (BoosterSystem.Instance != null
            && BoosterSystem.Instance.UseClawBooster
            && BoosterSystem.Instance.ClawMode == EClawSelectionMode.SelectTargetCarrier)
            return CanBeClawTarget() && CanPassTutorialCarrierRule();
        if (!RuntimeState.IsIdle) return false;
        if (!_actionGateResolver.EvaluateInteract(this).IsAllowed)
        {
            PlayBlockedByFullConveyorFeedback();
            return false;
        }
        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(this)) return false;
        return true;
    }

    /// <summary>
    /// Thực hiện hành động unload khi click thành công.
    /// </summary>
    public override void OnObjectClicked()
    {
        if (BoosterSystem.Instance != null && BoosterSystem.Instance.UseClawBooster)
        {
            BoosterSystem.Instance.SelectCarrier(Pivot.position, this);
            return;
        }
        if (!_unloadPort.UnloadBlocks())
        {
            PlayBlockedByFullConveyorFeedback();
            return;
        }
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
    }

    /// <summary>
    /// Xử lý khi click bị chặn (có thể thêm hiệu ứng phản hồi tại đây).
    /// </summary>
    public override void OnClickBlocked()
    {
        if (TryPlayFinishedBlock4XActiveAnimation()) return;
    }

    #endregion

    #region Public Methods
    
    public override void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false)
    {
        EnsureRuntime();
        ResetRuntime();
        
        _mechanicContainer.Rebuild(carrierStack != null ? carrierStack.Mechanics : null);
        _mechanicContainer.Reset(this);
        
        _blockController.BuildBlocks(carrierStack != null ? carrierStack.Blocks : null, suppressProgressAnimation);
        
        RefreshMechanicVisualState();
        _linkedBlockVisualController?.Refresh(suppressProgressAnimation);
    }

    /// <summary>
    /// Lưu progress hiện tại trên spline để conveyor biết vị trí carrier.
    /// </summary>
    public override void SetSplineProgress(float progress)
    {
        SplineProgress = Mathf.Repeat(progress, 1f);
    }

    /// <summary>
    /// Kết thúc lượt unload hiện tại sau khi conveyor đã spawn đủ cube.
    /// </summary>
    public override void FinishUnloadCarrier()
    {
        EnsureRuntime();
        var revealedHiddenBlockIndex = _blockController.CleanupEmptyBlocks();
        _runtimeState.FinishUnloading();
        _linkedBlockVisualController?.RefreshAfterUnload(revealedHiddenBlockIndex);
    }

    /// <summary>
    /// Đặt chỗ một cell trong carrier để cube trên conveyor bay vào.
    /// </summary>
    public override bool TryReserveReceive(
        EBlockColorType blockColorType,
        out CarrierReceiveReservation reservation,
        int undoBatchId = 0)
    {
        EnsureRuntime();
        return _receivePort.TryReserveReceive(blockColorType, out reservation, undoBatchId);
    }

    /// <summary>
    /// Kiểm tra carrier có khả năng nhận màu cube truyền vào hay không.
    /// </summary>
    public override bool CanPotentiallyReceive(EBlockColorType color)
    {
        EnsureRuntime();
        return _receivePort.CanPotentiallyReceive(color);
    }
    
    /// <summary>
    /// Hoàn tất nhận cube vào block đã reserve trước đó.
    /// </summary>
    public override void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color)
    {
        EnsureRuntime();
        _receivePort.CompleteReceive(reservation, color);
        if (reservation.TargetBlock != null && reservation.TargetBlock.IsFull())
        {
            var targetBlockIndex = _blockController != null
                ? _blockController.GetBlockIndex(reservation.TargetBlock)
                : -1;

            if (targetBlockIndex >= 0 && _linkedBlockVisualController != null)
            {
                if (_linkedBlockVisualController.CanBlockMergeWithNeighbors(targetBlockIndex))
                {
                    reservation.TargetBlock.PlayMergeVfx();
                    SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_merge);
                }
            }

            _linkedBlockVisualController?.RefreshAfterReceive(targetBlockIndex);
        }
    }

    public override void EvaluateFinishState()
    {
        EnsureRuntime();
        _receivePort.EvaluateFinishCondition();
    }

    #endregion

    protected override void ResetRuntime()
    {
        EnsureRuntime();

        var member = GetComponent<CarrierContainerMember>();
        if (member != null)
        {
            member.Clear();
        }

        _runtimeState.Reset();
        _mechanicContainer.Reset(this);
        _linkedBlockVisualController.Reset();
        _visualController.Reset();
        _isWaitingHiddenCarrierReveal = false;
        SetBlockLayoutRootVisible(true);
    }

    public override void RefreshMechanicVisualState()
    {
        var visualRequest = _visualResolver.Resolve(this);
        UpdateBlockLayoutVisibility(visualRequest);
        _visualController?.ApplyVisualRequest(visualRequest);
        _visualController?.SetSpecialColorTint(GetSpecialReceiverTintColor());
    }

    public override bool CanUnloadByMechanic()
    {
        return _actionGateResolver.EvaluateUnload(this).IsAllowed;
    }

    public override bool CanReceiveByMechanic(EBlockColorType colorType)
    {
        return _actionGateResolver.EvaluateReceive(this, colorType).IsAllowed;
    }

    public override int GetClawTargetBlockCount()
    {
        if (blockLayout == null) return 0;
        var targetBlockCount = 0;
        var foundTargetRange = false;

        for (var i = 0; i < maxBlockCount; i++)
        {
            var block = blockLayout.GetBlockByIndex(i);
            if (!foundTargetRange)
            {
                if (!block || !block.IsEmptyAndStable()) continue;
                foundTargetRange = true;
            }
            else if (!block || !block.IsEmptyAndStable()) break;

            targetBlockCount++;
        }

        return targetBlockCount;
    }

    public override bool CanBeClawTarget()
    {
        EnsureRuntime();
        if (IsLockedByContainer()) return false;
        if (IsHiddenByColor()) return false;
        if (!RuntimeState.IsIdle && !RuntimeState.IsCompleted) return false;
        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(this)) return false;
        if (HasIncompleteContentBlock()) return false;
        return GetClawTargetBlockCount() > 0;
    }
    
    private bool IsHiddenByColor()
    {
        if (_mechanicContainer?.Mechanics == null) return false;
        foreach (var mechanic in _mechanicContainer.Mechanics)
        {
            if (mechanic is ICarrierVisualRequestProvider visualProvider
                && mechanic.Type == ECarrierMechanic.HiddenByColor
                && visualProvider.GetVisualRequest(this) != null)
                return true;
        }
        return false;
    }

    public override bool IsLockedByContainer()
    {
        var member = GetComponent<CarrierContainerMember>();
        return member != null && member.IsLocked;
    }

    public override List<LinkedBlockVisual> GetLinkedBlockVisuals(int size)
    {
        return _linkedBlockVisualController != null ? _linkedBlockVisualController.GetVisuals(size) : new List<LinkedBlockVisual>();
    }

    public bool TryPlayFinishedBlock4XActiveAnimation()
    {
        EnsureRuntime();
        return _runtimeState != null
               && _runtimeState.IsFinished
               && _linkedBlockVisualController != null
               && _linkedBlockVisualController.TryPlayFinishedBlock4XActiveAnimation();
    }

    public override bool TryGetSpecialReceiverTargetColor(out EBlockColorType colorType)
    {
        foreach (var mechanic in _mechanicContainer.Mechanics)
        {
            if (mechanic is not ISpecialColorReceiverMechanic specialReceiver) continue;
            colorType = specialReceiver.TargetColor;
            return colorType != EBlockColorType.None;
        }

        colorType = EBlockColorType.None;
        return false;
    }

    public override bool IsSpecialReceiverForColor(EBlockColorType colorType)
    {
        return TryGetSpecialReceiverTargetColor(out var targetColor)
               && targetColor == colorType;
    }

    private bool HasIncompleteContentBlock()
    {
        if (blockLayout?.Blocks == null) return false;
        foreach (var block in blockLayout.Blocks)
        {
            if (block == null || !block.HasContent) continue;
            if (!block.IsFull()) return true;
        }
        return false;
    }

    private void PlayBlockedByFullConveyorFeedback()
    {
        var topBlock = _blockController?.GetTopUnloadCandidateBlock();
        if (topBlock == null || !topBlock.IsFull()) return;
        if (_linkedBlockVisualController != null
            && _linkedBlockVisualController.TryPlayBlockedFullAnimation(topBlock))
            return;

        if (!topBlock.IsLinkedVisualSuppressed())
            topBlock.PlayFullRevealAnimation();
    }

    protected override void EnsureRuntime()
    {
        if (_runtimeState != null) return;
        maxBlockCount = GetConfiguredBlockCount();
        _runtimeState = new CarrierRuntimeState();
        var blockFactory = new CarrierBlockFactory(colorConfigSO);
        _blockController = new CarrierBlockController(this, blockLayout, blockFactory, _runtimeState, maxBlockCount);
        _visualController = new CarrierVisualController(
            transform,
            hiddenVisualRoot,
            mechanicVisualConfig,
            specialColorReceiverCarrierMeshRenderer,
            RevealBlockLayoutForHiddenCarrier);
        _linkedBlockVisualController = new CarrierLinkedBlockVisualController(this, linkedBlockVisualConfig);
        _unloadPort = new CarrierUnloadPort(this);
        _receivePort = new CarrierReceivePort(this);
    }

    protected virtual void UpdateBlockLayoutVisibility(CarrierVisualRequest visualRequest)
    {
        var isHiddenCarrierActive = visualRequest != null && visualRequest.Kind == ECarrierVisualKind.HiddenShell;
        if (isHiddenCarrierActive)
        {
            _isWaitingHiddenCarrierReveal = true;
            SetBlockLayoutRootVisible(false);
            return;
        }

        if (_isWaitingHiddenCarrierReveal) return;
        SetBlockLayoutRootVisible(true);
    }

    protected virtual void RevealBlockLayoutForHiddenCarrier()
    {
        _isWaitingHiddenCarrierReveal = false;
        SetBlockLayoutRootVisible(true);
    }

    protected virtual void SetBlockLayoutRootVisible(bool isVisible)
    {
        var root = blockLayout != null ? blockLayout.Root : null;
        if (root != null && root.gameObject.activeSelf != isVisible)
            root.gameObject.SetActive(isVisible);
    }

    private void HandleMechanicEvent(ICarrierMechanicEvent carrierEvent)
    {
        EnsureRuntime();
        _mechanicContainer.DispatchEvent(this, carrierEvent);
        RefreshMechanicVisualState();
    }

    private Color? GetSpecialReceiverTintColor()
    {
        if (!TryGetSpecialReceiverTargetColor(out var colorType)) return null;
        var colorEntry = colorConfigSO != null ? colorConfigSO.GetColorEntry(colorType) : null;
        return colorEntry?.Color;
    }

    private bool CanPassTutorialCarrierRule()
    {
        if (GameEventBus.CanSelectTutorialClawCarrier == null) return true;
        return GameEventBus.CanSelectTutorialClawCarrier.Invoke(this);
    }

    public override void SetLayer(int layer)
    {
        gameObject.layer = layer;
        if (hiddenVisualRoot != null)
        {
            ApplyLayer(hiddenVisualRoot.gameObject, layer);
        }
    }

    private static void ApplyLayer(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        var transform = target.transform;
        for (var i = 0; i < transform.childCount; i++)
            ApplyLayer(transform.GetChild(i).gameObject, layer);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && UnityEditor.Selection.activeGameObject != gameObject) return;

        // Tìm SplineContainer
        var splineContainer = ConveyorDeliverySystem.Instance != null 
            ? ConveyorDeliverySystem.Instance.GetComponent<SplineContainer>() 
            : FindObjectOfType<SplineContainer>();

        if (splineContainer == null) return;

        // 1. Evaluate vị trí trên Spline
        splineContainer.Evaluate(SplineProgress, out float3 localPos, out _, out float3 localUp);
        Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);
        Vector3 worldUp = splineContainer.transform.TransformDirection(localUp);

        // 2. Xác định màu sắc và text theo trạng thái
        Color dotColor = Color.cyan;
        string stateText = "Idle";
        if (IsDelivering) { dotColor = Color.yellow; stateText = "Unloading"; }
        if (_runtimeState != null && _runtimeState.IsFinished) { dotColor = Color.red; stateText = "Finished"; }
        
        // 3. Vẽ chấm tròn (Dot)
        Gizmos.color = dotColor;
        Gizmos.DrawSphere(worldPos, 0.12f);
        
        // 4. Vẽ nhãn thông tin
        var labelStyle = new GUIStyle
        {
            normal = { textColor = dotColor },
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            fontSize = 10
        };
        
        string info = $"Carrier [{stateText}] | {SplineProgress:P1}";
        UnityEditor.Handles.Label(worldPos + worldUp * 0.2f + transform.right * 0.15f, info, labelStyle);

        // 5. Vẽ điểm pickup offset nếu khác 0
        if (pickupDistanceOffset != 0f)
        {
            float actualPickup = GetActualPickupProgress();
            splineContainer.Evaluate(actualPickup, out float3 pickupLocal, out _, out float3 pickupUp);
            Vector3 pickupWorld = splineContainer.transform.TransformPoint(pickupLocal);
            Vector3 pickupWorldUp = splineContainer.transform.TransformDirection(pickupUp);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(pickupWorld, 0.08f);
            Gizmos.DrawLine(worldPos, pickupWorld);

            var pickupLabelStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = Color.magenta }
            };
            string pickupInfo = $"Pickup | {actualPickup:P1} (Offset: {pickupDistanceOffset:F2}m)";
            UnityEditor.Handles.Label(pickupWorld + pickupWorldUp * 0.15f + transform.right * 0.1f, pickupInfo, pickupLabelStyle);
        }
    }
#endif
}
