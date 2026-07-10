using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    #region Inspector

    [SerializeField] private BoxCollider blockCollider;
    [SerializeField] private BlockVisual blockVisual;
    [SerializeField] private ContainerKey containerKey;
    [SerializeField] private Transform animationPivot;

    [Header("Link Anchors")]
    [SerializeField] private Transform leftLinkAnchor;
    [SerializeField] private Transform rightLinkAnchor;

    public Transform LeftLinkAnchor => leftLinkAnchor;
    public Transform RightLinkAnchor => rightLinkAnchor;

    [SerializeField] private EBlockColorType blockColorType;
    [SerializeField] private Color blockColor = Color.white;
    [SerializeField] private Color blockShadowColor = Color.white;

    [SerializeField] private bool hasContent;

    [SerializeField] private int maxCubes;
    [SerializeField] private int currentCubes;
    [SerializeField] private int visualCubes;

    #endregion

    #region Runtime State

    [SerializeField] EBlockState state = EBlockState.Idle;

    private int _reservedReceiveCount;

    private BlockOpenHandler _openHandler;
    private BlockReceiveHandler _receiveHandler;

    private BlockOpenHandler OpenHandler
    {
        get
        {
            if (_openHandler == null) _openHandler = new BlockOpenHandler();
            return _openHandler;
        }
    }

    private BlockReceiveHandler ReceiveHandler
    {
        get
        {
            if (_receiveHandler == null) _receiveHandler = new BlockReceiveHandler();
            return _receiveHandler;
        }
    }

    private BlockVisual BlockVisualComponent
    {
        get
        {
            if (blockVisual == null) blockVisual = GetComponent<BlockVisual>();
            return blockVisual;
        }
    }

    private ContainerKey ContainerKeyComponent
    {
        get
        {
            if (containerKey == null) containerKey = GetComponent<ContainerKey>();
            if (containerKey == null && Application.isPlaying)
                containerKey = gameObject.AddComponent<ContainerKey>();
            return containerKey;
        }
    }
    private CarrierBase _ownerCarrier;
    private List<BlockMechanicData> _runtimeMechanics = new List<BlockMechanicData>();
    private Collider[] _cachedPhysicsColliders;

    private bool _hasHiddenMechanic;
    private bool _isHiddenRevealed;
    private bool _isLinkedVisualSuppressed;
    private bool _isHiddenForClawBooster;
    private bool _preserveRuntimeStateOnDisable;
    private bool _isSwappingActive = true;
    private int _swapGroupId = -1;
    private EBlockColorType _swapArrowColorType = EBlockColorType.None;
    public Transform AnimationPivot => animationPivot;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        SetPhysicsCollidersEnabled(false);
    }

    private void OnDisable()
    {
        if (_preserveRuntimeStateOnDisable) return;

        state = EBlockState.Idle;
        _ownerCarrier = null;
        _runtimeMechanics.Clear();
        hasContent = false;
        blockShadowColor = Color.white;
        _hasHiddenMechanic = false;
        _isHiddenRevealed = false;
        _isLinkedVisualSuppressed = false;
        _swapArrowColorType = EBlockColorType.None;
        InternalClearCubes();
    }

    #endregion

    #region Setup
    private void ConfigureMechanics(BlockData blockData)
    {
        _runtimeMechanics = CloneMechanics(blockData?.Mechanics);
        _hasHiddenMechanic = HasHiddenMechanic(blockData?.Mechanics);
        _isHiddenRevealed = !_hasHiddenMechanic;
        ConfigureContainerKey(GetResolvedKeyColor(blockData != null
            ? blockData.BlockColor
            : EBlockColorType.None), false);
        // Configure swap mechanic
        var swapMechanic = GetBlockMechanic(EBlockMechanic.SwappingBlock);
        if (swapMechanic != null)
        {
            ConfigureSwapMechanic(swapMechanic.SwapGroupId, true);
        }
        else
        {
            ConfigureSwapMechanic(-1, false);
        }
    }

    public void SetOwnerCarrier(CarrierBase carrier)
    {
        _ownerCarrier = carrier;
    }

    public void SetLayer(int layer)
    {
        ApplyLayer(gameObject, layer);
    }

    public void SetPhysicsCollidersEnabled(bool isEnabled)
    {
        if (_cachedPhysicsColliders == null || _cachedPhysicsColliders.Length == 0)
            _cachedPhysicsColliders = GetComponentsInChildren<Collider>(true);

        for (var i = 0; i < _cachedPhysicsColliders.Length; i++)
        {
            var target = _cachedPhysicsColliders[i];
            if (target != null) target.enabled = false;
        }
    }

    public void ApplyBlockData(BlockData blockData, ColorConfigSO colorConfig, float visualProgress, bool suppressProgressAnimation)
    {
        var colorEntry = GetColorEntry(colorConfig, blockData);
        InternalClearCubes();

        hasContent = blockData.BlockColor != EBlockColorType.None;

        blockColorType = hasContent ? blockData.BlockColor : EBlockColorType.None;
        blockColor = colorEntry?.Color ?? Color.white;
        blockShadowColor = colorEntry?.ShadowColor ?? blockColor;

        state = EBlockState.Idle;

        maxCubes = CapacityManager.Instance.CubePerBlock;
        currentCubes = hasContent ? maxCubes : 0;
        visualCubes = currentCubes;

        _swapArrowColorType = EBlockColorType.None;
        ConfigureMechanics(blockData);

        RefreshVisualState(visualProgress, suppressProgressAnimation);
    }

    private static ColorEntry GetColorEntry(ColorConfigSO colorConfig, BlockData blockData)
    {
        if (colorConfig == null || blockData == null) return null;
        return colorConfig.GetColorEntry(blockData.BlockColor);
    }

    public void ClearContent()
    {
        maxCubes = CapacityManager.Instance.CubePerBlock;
        InternalClearCubes();
        _runtimeMechanics.Clear();
        hasContent = false;
        blockColorType = EBlockColorType.None;
        blockColor = Color.white;
        blockShadowColor = Color.white;
        state = EBlockState.Idle;
        _hasHiddenMechanic = false;
        _isHiddenRevealed = false;
        _isLinkedVisualSuppressed = false;
        _isHiddenForClawBooster = false;
        ConfigureContainerKey(EBlockColorType.None, true);
        ConfigureSwapMechanic(-1, false);
        _swapArrowColorType = EBlockColorType.None;
        SetSwapArrowColor(EBlockColorType.None);
        currentCubes = 0;
        visualCubes = 0;
        RefreshVisualState();
    }

    private void InternalClearCubes()
    {
        currentCubes = 0;
        visualCubes = 0;
        _reservedReceiveCount = 0;
    }

    public void PreserveRuntimeStateWhileHidden(bool preserve)
    {
        _preserveRuntimeStateOnDisable = preserve;
    }

    #endregion

    #region Query

    public Vector3 GetBoundSize() => blockCollider.size;
    public CarrierBase OwnerCarrier => _ownerCarrier;
    public EBlockColorType GetBlockColorType() => blockColorType;
    public int GetCurrentCubes() => currentCubes;
    public int GetMaxCubes() => maxCubes;
    public Color GetBlockColor() => blockColor;
    public Color GetBlockShadowColor() => blockShadowColor;
    public bool IsReceiving() => state == EBlockState.Receiving;
    public bool HasContent => hasContent;
    public bool IsLinkedVisualSuppressed() => _isLinkedVisualSuppressed;
    public bool IsHiddenForClawBooster() => _isHiddenForClawBooster;
    public bool IsEmptyIdleSlot()
    {
        return blockColorType == EBlockColorType.None && state == EBlockState.Idle;
    }

    public bool IsEmptyAndStable()
    {
        return !hasContent
               && currentCubes == 0
               && _reservedReceiveCount == 0
               && state == EBlockState.Idle;
    }

    public bool IsHiddenVisualActive() => IsHiddenMechanicVisualActive();
    public int GetKeyTargetContainerId()
    {
        var mechanic = GetBlockMechanic(EBlockMechanic.KeyUnlockContainer);
        return mechanic != null ? mechanic.ContainerId : -1;
    }

    public bool HasVisibleContainerKey()
    {
        return HasContainerKeyMechanic()
               && (ContainerKeyComponent == null || !ContainerKeyComponent.IsConsumed);
    }
    public bool IsReadyForFinish()
    {
        return hasContent
               && IsFull()
               && !IsHiddenMechanicVisualActive();
    }

    public BlockRuntimeData CaptureRuntimeData()
    {
        return new BlockRuntimeData
        {
            HasContent = hasContent,
            BlockColorType = blockColorType,
            Color = blockColor,
            ShadowColor = blockShadowColor,
            CubeCount = currentCubes,
            IsHiddenRevealed = _isHiddenRevealed,
            IsContainerKeyConsumed = ContainerKeyComponent != null && ContainerKeyComponent.IsConsumed,
            Mechanics = CloneMechanics(_runtimeMechanics),
            IsSwappingActive = _isSwappingActive,
            SwapGroupId = _swapGroupId,
            SwapArrowColorType = _swapArrowColorType
        };
    }

    public void ApplyRuntimeData(BlockRuntimeData runtimeData, bool suppressProgressAnimation = false)
    {
        if (runtimeData == null || !runtimeData.HasContent)
        {
            ClearContent();
            return;
        }

        maxCubes = CapacityManager.Instance.CubePerBlock;
        hasContent = true;
        blockColorType = runtimeData.BlockColorType;
        blockColor = runtimeData.Color;
        blockShadowColor = runtimeData.ShadowColor;
        currentCubes = Mathf.Clamp(runtimeData.CubeCount, 0, maxCubes);
        visualCubes = currentCubes;
        _reservedReceiveCount = 0;
        state = EBlockState.Idle;
        _runtimeMechanics = CloneMechanics(runtimeData.Mechanics);
        _hasHiddenMechanic = HasHiddenMechanic(_runtimeMechanics);
        _isHiddenRevealed = !_hasHiddenMechanic || runtimeData.IsHiddenRevealed;
        _isLinkedVisualSuppressed = false;
        ConfigureContainerKey(GetResolvedKeyColor(blockColorType), runtimeData.IsContainerKeyConsumed);
        ConfigureSwapMechanic(runtimeData.SwapGroupId, runtimeData.IsSwappingActive);
        
        _swapArrowColorType = runtimeData.SwapArrowColorType;
        SetSwapArrowColor(_swapArrowColorType);

        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    public bool CanBeginUnload()
    {
        return hasContent
               && CanOpenByMechanic()
               && state != EBlockState.Unloading
               && _reservedReceiveCount == 0;
    }

    public bool IsFull()
    {
        return hasContent && state == EBlockState.Idle && currentCubes == maxCubes;
    }

    public bool IsOpened => state == EBlockState.Unloading;

    public int GetExpectedUnloadCount()
    {
        if (!hasContent) return 0;
        return currentCubes;
    }

    #endregion

    #region Open Flow

    public bool TryBeginUnload()
    {
        if (!CanOpenByMechanic()) return false;
        var didOpen = OpenHandler.TryOpen(currentCubes, ref state, _reservedReceiveCount);
        if (didOpen) RefreshVisualState();
        return didOpen;
    }

    public IReadOnlyList<BlockCubePayload> GetUnloadCubePayloadSnapshot()
    {
        var worldPos = animationPivot != null ? animationPivot.position : transform.position;
        return OpenHandler.CreateHiddenCubePayloadSnapshot(currentCubes, blockColorType, blockColor, worldPos, transform, maxCubes);
    }

    public bool TryConsumeUnloadCube()
    {
        if (currentCubes <= 0) return false;
        currentCubes--;
        visualCubes = Mathf.Max(0, visualCubes - 1);
        RefreshVisualState();
        _ownerCarrier?.LinkedBlockVisualController?.NotifyBlockUnloadProgress(this);
        return true;
    }

    #endregion

    #region Receive Flow

    public bool IsAvailableForReceive(EBlockColorType colorType)
    {
        if (!CanReceiveByMechanic()) return false;
        return ReceiveHandler.IsAvailableForReceive(
            hasContent, currentCubes, _reservedReceiveCount, state, blockColorType, colorType, maxCubes);
    }

    public bool TryReserveReceive(EBlockColorType colorType, out Vector3 worldPosition)
    {
        var basePos = animationPivot != null ? animationPivot.position : transform.position;
        worldPosition = basePos;
        if (!CanReceiveByMechanic()) return false;

        var cubeIndex = maxCubes - 1 - currentCubes;
        var wasEmpty = !hasContent || state == EBlockState.Idle;
        var didReserve = ReceiveHandler.TryReserveReceive(
            ref hasContent, ref blockColorType, ref blockColor, ref blockShadowColor, ref state,
            ref currentCubes, ref _reservedReceiveCount, colorType, maxCubes);

        if (didReserve)
        {
            worldPosition = BlockZigzagOffsetCalculator.GetZigzagWorldPosition(
                cubeIndex, maxCubes, basePos, transform);
            RefreshVisualState();
        }

        return didReserve;
    }

    public bool TryReceiveCube(EBlockColorType colorType, Color color)
    {
        if (!CanReceiveByMechanic()) return false;
        var didReceive = ReceiveHandler.TryReceiveCube(
            ref currentCubes, ref visualCubes, ref _reservedReceiveCount,
            ref hasContent, ref blockColorType, ref blockColor, ref blockShadowColor,
            ref state,
            colorType, color, maxCubes);
        if (didReceive) RefreshVisualState();
        return didReceive;
    }

    public void RestoreFullBlock(
        EBlockColorType colorType,
        Color color,
        Color shadowColor,
        int cubeCount,
        bool suppressProgressAnimation = false)
    {
        maxCubes = CapacityManager.Instance != null ? CapacityManager.Instance.CubePerBlock : maxCubes;
        hasContent = colorType != EBlockColorType.None;
        blockColorType = colorType;
        blockColor = color;
        blockShadowColor = shadowColor;
        currentCubes = Mathf.Clamp(cubeCount, 0, maxCubes);
        visualCubes = currentCubes;
        _reservedReceiveCount = 0;
        state = EBlockState.Idle;
        _hasHiddenMechanic = false;
        _isHiddenRevealed = true;
        _isLinkedVisualSuppressed = false;
        ConfigureContainerKey(colorType, true);
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    public void SetVisualCubes(int count, bool suppressProgressAnimation = false)
    {
        visualCubes = Mathf.Clamp(count, 0, maxCubes);
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    #endregion

    #region Linked Visual

    public void SetLinkedVisualSuppressed(bool isSuppressed)
    {
        SetLinkedVisualSuppressed(isSuppressed, false);
    }

    public void SetLinkedVisualSuppressed(bool isSuppressed, bool suppressProgressAnimation)
    {
        if (_isLinkedVisualSuppressed == isSuppressed) return;
        _isLinkedVisualSuppressed = isSuppressed;
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    public void SetHiddenForClawBooster(bool isHidden, bool suppressProgressAnimation = false)
    {
        if (_isHiddenForClawBooster == isHidden) return;
        _isHiddenForClawBooster = isHidden;
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    #endregion

    #region Mechanics

    public void NotifyMechanicEvent(BlockMechanicEvent blockEvent, bool suppressProgressAnimation = false)
    {
        HandleMechanicEvent(blockEvent);
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    public void PlayFullRevealAnimation()
    {
        RefreshVisualState(GetCurrentVisualProgress(), false, true);
    }

    public void PlayMergeVfx()
    {
        if (BlockVisualComponent != null)
        {
            BlockVisualComponent.PlayMergeVfx();
        }
    }

    private void RefreshVisualState()
    {
        RefreshVisualState(GetCurrentVisualProgress());
    }

    private void RefreshVisualState(float progress)
    {
        RefreshVisualState(progress, false);
    }

    private void RefreshVisualState(float progress, bool suppressProgressAnimation)
    {
        RefreshVisualState(progress, suppressProgressAnimation, false);
    }

    private void RefreshVisualState(float progress, bool suppressProgressAnimation, bool forceFullAnimation)
    {
        if (BlockVisualComponent == null) return;
        if (_isHiddenForClawBooster)
        {
            BlockVisualComponent.ApplyVisualState(false, false, blockColorType, false, false, progress, suppressProgressAnimation);
            return;
        }
        BlockVisualComponent.ApplyVisualState(
            hasContent, !_isLinkedVisualSuppressed, blockColorType, IsHiddenMechanicVisualActive(),
            HasVisibleContainerKey(),
            progress, suppressProgressAnimation, forceFullAnimation, GetResolvedKeyColor(blockColorType));
    }

    private float GetCurrentVisualProgress()
    {
        return maxCubes > 0 ? (float)visualCubes / maxCubes : 0f;
    }

    private bool IsHiddenMechanicVisualActive()
    {
        return _hasHiddenMechanic && !_isHiddenRevealed;
    }

    private bool CanOpenByMechanic()
    {
        if (!_hasHiddenMechanic) return true;
        return _isHiddenRevealed;
    }

    private bool CanReceiveByMechanic()
    {
        if (!_hasHiddenMechanic) return true;
        return _isHiddenRevealed;
    }

    private void HandleMechanicEvent(BlockMechanicEvent blockEvent)
    {
        if (!(blockEvent is PreviousBlockReleasedEvent)) return;
        RevealHiddenMechanicIfNeeded();
    }

    private void RevealHiddenMechanicIfNeeded()
    {
        if (!_hasHiddenMechanic || _isHiddenRevealed) return;
        _isHiddenRevealed = true;
    }

    public void RevealContainerKeyIfNeeded(bool suppressProgressAnimation = false)
    {
        if (!HasContainerKeyMechanic()) return;
        var startPosition = transform.position;
        var startRotation = transform.rotation;
        if (BlockVisualComponent != null
            && BlockVisualComponent.FixedVisual != null
            && BlockVisualComponent.FixedVisual.KeyRenderer != null)
        {
            startPosition = BlockVisualComponent.FixedVisual.KeyRenderer.transform.position;
            startRotation = BlockVisualComponent.FixedVisual.KeyRenderer.transform.rotation;
        }

        if (ContainerKeyComponent != null)
            ContainerKeyComponent.RevealAndUnlock(startPosition, startRotation);
        RefreshVisualState(GetCurrentVisualProgress(), suppressProgressAnimation);
    }

    private void ConfigureContainerKey(EBlockColorType colorType, bool isConsumed)
    {
        var component = ContainerKeyComponent;
        if (component == null) return;
        var mechanic = GetBlockMechanic(EBlockMechanic.KeyUnlockContainer);
        component.Configure(
            mechanic != null,
            colorType,
            mechanic != null ? mechanic.ContainerId : -1,
            isConsumed);
    }

    private EBlockColorType GetResolvedKeyColor(EBlockColorType fallbackColor)
    {
        var mechanic = GetBlockMechanic(EBlockMechanic.KeyUnlockContainer);
        return mechanic != null && mechanic.KeyColor != EBlockColorType.None
            ? mechanic.KeyColor
            : fallbackColor;
    }

    private bool HasContainerKeyMechanic()
    {
        return HasMechanic(_runtimeMechanics, EBlockMechanic.KeyUnlockContainer);
    }
    public bool HasLinkGroupId() => GetLinkGroupId() >= 0;

    public int GetLinkGroupId()
    {
        var linkMechanic = GetBlockMechanic(EBlockMechanic.BlockLink);
        return linkMechanic != null ? linkMechanic.LinkGroupId : -1;
    }

    public void RemoveLinkMechanic()
    {
        _runtimeMechanics.RemoveAll(m => m.Type == EBlockMechanic.BlockLink);
    }

    private BlockMechanicData GetBlockMechanic(EBlockMechanic type)
    {
        if (_runtimeMechanics == null) return null;
        foreach (var mechanic in _runtimeMechanics)
        {
            if (mechanic == null || mechanic.Type != type) continue;
            return mechanic;
        }

        return null;
    }

    private static bool HasHiddenMechanic(List<BlockMechanicData> mechanics)
    {
        return HasMechanic(mechanics, EBlockMechanic.HiddenBlock);
    }

    private static bool HasMechanic(List<BlockMechanicData> mechanics, EBlockMechanic type)
    {
        if (mechanics == null) return false;
        foreach (var mechanic in mechanics)
        {
            if (mechanic == null || mechanic.Type != type) continue;
            return true;
        }

        return false;
    }

    public static List<BlockMechanicData> CloneMechanics(List<BlockMechanicData> mechanics)
    {
        var result = new List<BlockMechanicData>();
        if (mechanics == null) return result;
        foreach (var mechanic in mechanics)
        {
            if (mechanic == null) continue;
            result.Add(new BlockMechanicData
            {
                Type = mechanic.Type,
                ContainerId = mechanic.ContainerId,
                KeyColor = mechanic.KeyColor,
                LinkGroupId = mechanic.LinkGroupId,
                SwapGroupId = mechanic.SwapGroupId
            });
        }
        return result;
    }

    private static void ApplyLayer(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        var transform = target.transform;
        for (var i = 0; i < transform.childCount; i++)
            ApplyLayer(transform.GetChild(i).gameObject, layer);
    }

    public bool HasSwappingMechanic()
    {
        return _isSwappingActive && HasMechanic(_runtimeMechanics, EBlockMechanic.SwappingBlock);
    }

    public int GetSwapGroupId()
    {
        return _swapGroupId;
    }

    public void ConfigureSwapMechanic(int swapGroupId, bool isActive)
    {
        _swapGroupId = swapGroupId;
        _isSwappingActive = isActive;
        var visual = BlockVisualComponent;
        if (visual != null && visual.FixedVisual != null)
        {
            visual.FixedVisual.SetSwapArrowVisible(isActive && HasMechanic(_runtimeMechanics, EBlockMechanic.SwappingBlock));
        }
    }

    public void DisableSwappingMechanic()
    {
        if (!_isSwappingActive) return;
        _isSwappingActive = false;
        var visual = BlockVisualComponent;
        if (visual != null && visual.FixedVisual != null)
        {
            visual.FixedVisual.SetSwapArrowVisible(false);
        }
    }

    public void UpdateColorType(EBlockColorType newColorType, ColorConfigSO colorConfig)
    {
        if (!hasContent || !_isSwappingActive) return;
        blockColorType = newColorType;
        var entry = colorConfig.GetColorEntry(newColorType);
        blockColor = entry?.Color ?? Color.white;
        blockShadowColor = entry?.ShadowColor ?? blockColor;
        RefreshVisualState();
    }

    public void PlaySwapRotateAnimation()
    {
        var visual = BlockVisualComponent;
        if (visual != null && visual.FixedVisual != null)
        {
            visual.FixedVisual.PlaySwapRotateAnimation();
        }
    }

    public void SetSwapArrowColor(EBlockColorType colorType, ColorConfigSO colorConfig = null)
    {
        _swapArrowColorType = colorType;
        if (colorConfig == null && ConfigManager.Instance != null)
        {
            colorConfig = ConfigManager.Instance.GetColorConfig();
        }
        if (BlockVisualComponent != null)
        {
            BlockVisualComponent.SetSwapArrowColor(colorType, colorConfig);
        }
    }

    #endregion
}

public sealed class BlockRuntimeData
{
    public bool HasContent;
    public EBlockColorType BlockColorType;
    public Color Color;
    public Color ShadowColor;
    public int CubeCount;
    public bool IsHiddenRevealed;
    public bool IsContainerKeyConsumed;
    public List<BlockMechanicData> Mechanics = new List<BlockMechanicData>();
    public bool IsSwappingActive = true;
    public int SwapGroupId = -1;
    public EBlockColorType SwapArrowColorType = EBlockColorType.None;
}
