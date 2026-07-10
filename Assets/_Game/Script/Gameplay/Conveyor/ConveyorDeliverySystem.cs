using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối cube trên conveyor: spawn cube từ carrier, pickup cube vào carrier và kiểm tra deadlock.
/// </summary>
public class ConveyorDeliverySystem : MonoSingleton<ConveyorDeliverySystem>, IConveyorPickupHandler
{
    #region Inspector References

    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private ConveyorMeshBuilder conveyorMeshBuilder;
    [SerializeField] private CubeConfigSO cubeConfig;
    [SerializeField] private float spawnInterval = 0.01f;
    [SerializeField] private Transform spawnRoot;
   
    [SerializeField] private ConveyorSpawnPointConfigSO conveyorSpawnPointConfig;
    [SerializeField] private List<Cube> cachedMovers = new List<Cube>();
    [SerializeField] private ConveyorSpeedBoostConfigSO conveyorSpeedBoostConfig;
    [SerializeField] private ConveyorCornerDetector conveyorCornerDetector;
    
    [Header("Pickup Config")]
    [SerializeField] private float pickupThreshold = 0.02f;
    
    #endregion

    #region Runtime State

    private readonly List<AnimCube> _activeAnimCubes = new List<AnimCube>();
    private readonly List<DeliveryCubeState> _deliveryStates = new List<DeliveryCubeState>();
    private readonly List<CarrierBase> _activeCarriers = new List<CarrierBase>();
    private readonly List<float> _cachedCarrierPickupProgresses = new List<float>();

    private ConveyorSpawnPointCalculator _conveyorSpawnPointCalculator;
    private ConveyorDeliveryCubeFactory _deliveryCubeFactory;
    private ConveyorUnloadHandler _unloadHandler;
    private ConveyorPickupHandler _pickupHandler;
    private ForwardBestSlotPickupRule _pickupRule;
    private ConveyorCubeSpeedController _conveyorCubeSpeedController;
    private ConveyorLoseDetector _loseDetector;
    private ConveyorWinDetector _winDetector;
    
    public bool IsConveyorEmpty => _activeAnimCubes.Count == 0 && _deliveryStates.Count == 0;
    public bool HasActiveCubesOnConveyor => _deliveryStates.Count > 0;
    public ConveyorSpeedBoostConfigSO ConveyorSpeedBoostConfig => conveyorSpeedBoostConfig;
    public ConveyorPathRuntime Path => conveyorManager != null ? conveyorManager.Path : null;
    public ConveyorWinDetector WinDetector => _winDetector;

    public bool IsUnloadActive => _unloadHandler != null && _unloadHandler.IsUnloadActive;
    public bool IsPickupActive => _pickupHandler != null && _pickupHandler.GetActivePickupStates().Count > 0;

    public void RequestPlayLoopSound()
    {
        int activeCount = (IsPickupActive ? 1 : 0) + (IsUnloadActive ? 1 : 0);
        if (activeCount == 1)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayLoop(AudioClipName.sfx_merge_loop);
        }
    }

    public void RequestStopLoopSound()
    {
        if (!IsPickupActive && !IsUnloadActive)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.StopLoop();
        }
    }
    private const string InstancesRootPrefix = "root-";
    private const float MaxPickupProgressEpsilon = 0.002f;
    private const float BaseMaxPickupTravelDeltaPerStep = 0.1f;
    private float MaxPickupTravelDeltaPerStep
    {
        get
        {
            float timeScale = CustomTimeScaleGroup.Instance != null 
                ? CustomTimeScaleGroup.Instance.CurrentTimeScale 
                : 1f;
            float speedScale = timeScale;
            if (timeScale > 1f)
            {
                float multiplier = 0.2f;
                if (ConfigManager.Instance != null)
                {
                    var movementConfig = ConfigManager.Instance.GetCubeMovementConfig();
                    if (movementConfig != null)
                    {
                        multiplier = movementConfig.ScaleSpeedMultiplier;
                    }
                }
                speedScale = 1f + (timeScale - 1f) * multiplier;
            }
            return BaseMaxPickupTravelDeltaPerStep * Mathf.Max(1f, speedScale);
        }
    }
    private const float WrapPreviousProgressThreshold = 0.8f;
    private const float WrapCurrentProgressThreshold = 0.2f;
    private static readonly int BlinkEnabledId = Shader.PropertyToID("_BlinkEnabled");
    private static readonly int BlinkStartTimeId = Shader.PropertyToID("_BlinkStartTime");

    #region Prelose Shader  
    
    private void ApplyBlinkToFirstRenderer(bool active)
    {
        var root = GetInstancesRoot();
        if (root == null) return;
        var conveyorRenderer = root.GetComponentInChildren<Renderer>(true);
        if (!conveyorRenderer) return;
        SetBlinkEnabled(conveyorRenderer.sharedMaterial, active);
    }

    private void SetBlinkEnabled(Material material, bool active)
    {
        if (!material) return;
        if (material.GetFloat(BlinkEnabledId) > 0.5f == active) return;
        if (active) material.SetFloat(BlinkStartTimeId, Time.time);
        material.SetFloat(BlinkEnabledId, active ? 1f : 0f);
    }


    private Transform GetInstancesRoot()
    {
        foreach (Transform child in transform)
            if (child.name.StartsWith(InstancesRootPrefix)) return child;
        return null;
    }
    
    #endregion
    
    /// <summary>
    /// Khởi tạo các service phụ trợ của conveyor khi scene bắt đầu.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if (conveyorManager == null) conveyorManager = GetComponent<ConveyorManager>();
        if (conveyorCornerDetector == null) conveyorCornerDetector = GetComponent<ConveyorCornerDetector>();
        if (_conveyorSpawnPointCalculator == null)
        {
            _conveyorSpawnPointCalculator = new ConveyorSpawnPointCalculator(
                conveyorSpawnPointConfig,
                conveyorMeshBuilder,
                Path,
                GetSpawnRoot(),
                transform);
        }

        if (_deliveryCubeFactory == null) _deliveryCubeFactory = new ConveyorDeliveryCubeFactory(cubeConfig);
        if (_conveyorCubeSpeedController == null)
        {
            _conveyorCubeSpeedController = new ConveyorCubeSpeedController(
                _deliveryStates,
                conveyorSpeedBoostConfig);
        }
        
        _pickupHandler = new ConveyorPickupHandler(
            _deliveryCubeFactory,
            GetSpawnRoot(),
            CompleteSplinePickup,
            PushAnimCubeToPool,
            CheckTutorial);
        _pickupRule = new ForwardBestSlotPickupRule(_pickupHandler);


        _unloadHandler = new ConveyorUnloadHandler(
            _deliveryCubeFactory,
            _conveyorSpawnPointCalculator,
            _activeAnimCubes,
            GetSpawnRoot(),
            spawnInterval,
            CompleteUnload);
            
        _loseDetector = new ConveyorLoseDetector(
            _deliveryStates,
            _activeCarriers,
            _pickupHandler.GetActivePickupStates(),
            _activeAnimCubes,
            _pickupRule);
        _winDetector = new ConveyorWinDetector();

        GameEventBus.OnLevelLoaded += OnLevelLoaded;
        GameEventBus.OnCarrierFinished += OnCarrierFinished;
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnLevelLoaded -= OnLevelLoaded;
        GameEventBus.OnCarrierFinished -= OnCarrierFinished;
        base.OnDestroy();
    }

    #endregion
    
    #region Win - Lose
    
    private void OnLevelLoaded(LevelData levelData)
    {
        _winDetector.Init(levelData);
    }

    private void OnCarrierFinished(EBlockColorType colorType)
    {
        _winDetector.OnCarrierFinished(colorType);
    }

    private void Update()
    {
#if UNITY_LUNA
        UpdateLunaManualCubeMovement(Time.unscaledDeltaTime);
#endif
        var cornerDetector = GetCornerDetector();
        if (_conveyorCubeSpeedController == null || cornerDetector == null) return;
        _conveyorCubeSpeedController.BoostCubesPassingCorners(cornerDetector.CornerProgresses);
        UpdatePickupDetection();
    }

#if UNITY_LUNA
    private void UpdateLunaManualCubeMovement(float deltaTime)
    {
        if (_deliveryStates == null || _deliveryStates.Count == 0) return;
        for (var i = _deliveryStates.Count - 1; i >= 0; i--)
        {
            var state = _deliveryStates[i];
            if (state == null || state.IsPickedUp || state.Cube == null) continue;
            state.Cube.ManualUpdate(deltaTime);
        }
    }
#endif

    private void UpdatePickupDetection()
    {
        if (_activeCarriers == null || _activeCarriers.Count == 0 || _deliveryStates.Count == 0) return;

        int carrierCount = _activeCarriers.Count;
        while (_cachedCarrierPickupProgresses.Count < carrierCount)
        {
            _cachedCarrierPickupProgresses.Add(0f);
        }

        for (var j = 0; j < carrierCount; j++)
        {
            var carrier = _activeCarriers[j];
            _cachedCarrierPickupProgresses[j] = (carrier != null && carrier.Interactable) 
                ? carrier.GetActualPickupProgress() 
                : 0f;
        }

        for (var i = _deliveryStates.Count - 1; i >= 0; i--)
        {
            var state = _deliveryStates[i];
            if (state == null || state.IsPickedUp || state.Cube == null) continue;

            var cube = state.Cube;
            var cubeProgress = cube.GetProgress();
            var previousProgress = state.PreviousProgress;
            var pickedUp = false;
            
            for (var j = 0; j < carrierCount; j++)
            {
                var carrier = _activeCarriers[j];
                if (carrier == null || !carrier.Interactable) continue;
                
                var pickupProgress = _cachedCarrierPickupProgresses[j];
                if (!HasReachedPickupProgress(previousProgress, cubeProgress, pickupProgress)) continue;
                if (!TryPickupState(state, carrier, pickupProgress)) continue;

                pickedUp = true;
                break;
            }

            if (!pickedUp && !state.IsPickedUp) state.PreviousProgress = cubeProgress;
        }
    }

    private bool HasReachedPickupProgress(float previousProgress, float currentProgress, float pickupProgress)
    {
        if (!TryGetForwardPickupTravelDelta(previousProgress, currentProgress, out var travelDelta)) return false;
        var pickupDelta = GetForwardProgressDelta(previousProgress, pickupProgress);
        return pickupDelta <= travelDelta + GetPickupProgressEpsilon();
    }

    private bool TryGetForwardPickupTravelDelta(float previousProgress, float currentProgress, out float travelDelta)
    {
        previousProgress = Mathf.Repeat(previousProgress, 1f);
        currentProgress = Mathf.Repeat(currentProgress, 1f);

        if (currentProgress >= previousProgress)
        {
            travelDelta = currentProgress - previousProgress;
            return travelDelta > GetPickupProgressEpsilon()
                   && travelDelta <= MaxPickupTravelDeltaPerStep;
        }

        if (previousProgress > WrapPreviousProgressThreshold && currentProgress < WrapCurrentProgressThreshold)
        {
            travelDelta = 1f - previousProgress + currentProgress;
            return travelDelta > GetPickupProgressEpsilon()
                   && travelDelta <= MaxPickupTravelDeltaPerStep;
        }

        travelDelta = 0f;
        return false;
    }

    private float GetForwardProgressDelta(float fromProgress, float toProgress)
    {
        return Mathf.Repeat(Mathf.Repeat(toProgress, 1f) - Mathf.Repeat(fromProgress, 1f), 1f);
    }

    private float GetPickupProgressEpsilon()
    {
        return Mathf.Clamp(pickupThreshold, 0.00001f, MaxPickupProgressEpsilon);
    }
    #endregion

    #region Public API

    /// <summary>
    /// Xóa toàn bộ cube đang chạy và cube animation khỏi conveyor.
    /// </summary>
    public void ClearAllCubes()
    {
        ClearFollowers();
        ClearActiveAnimCubes();
    }

    /// <summary>
    /// Nhận request unload từ carrier và bắt đầu spawn các cube ra conveyor.
    /// </summary>
    public bool TrySpawnCarrierUnload(CarrierUnloadRequest unloadRequest)
    {
        if (!CanSpawnCarrierUnload(unloadRequest)) return false;
        _conveyorSpawnPointCalculator.CacheDeliverySpreadSettings();
        _unloadHandler.HandleUnload(unloadRequest);
        return true;
    }

    private bool TryPickupState(
        DeliveryCubeState state,
        CarrierBase targetCarrier,
        float? pickupProgress)
    {
        if(targetCarrier.LockPick) return false;
        if (state == null || state.Cube == null) return false;
        if (!TryBeginReceive(state, targetCarrier, out var reservation)) return false;

        if (pickupProgress.HasValue) SnapCubeToSplineProgress(state.Cube, pickupProgress.Value);

        var animCube = _deliveryCubeFactory.CreateAnimCubeInstance(GetSpawnRoot(), _activeAnimCubes);
        _pickupHandler.HandleReceiveCube(state.Cube, targetCarrier, state.Color, animCube, reservation);
        return true;
    }

    private void SnapCubeToSplineProgress(Cube cube, float progress)
    {
        var path = Path;
        if (cube == null || path == null || !path.IsValid) return;

        var normalizedProgress = Mathf.Repeat(progress, 1f);
        var worldPosition = path.EvaluateWorldPosition(normalizedProgress);
        cube.transform.position = worldPosition;
        cube.SyncProgress(normalizedProgress);

#if UNITY_LUNA
        return;
#else
        if (!cube.TryGetComponent<Rigidbody>(out var rb)) return;
        rb.position = worldPosition;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
#endif
    }

    /// <summary>
    /// Kiểm tra carrier đang có cube bay vào hay không.
    /// </summary>
    public bool IsReceivingCube(CarrierBase carrier)
    {
        return _pickupHandler.IsReceivingCube(carrier);
    }

    /// <summary>
    /// Kiểm tra xem Conveyor và tất cả Carrier có đang ở trạng thái ổn định (Stable) hay không.
    /// </summary>
    public bool IsConveyorStable()
    {
        if (_activeAnimCubes != null && _activeAnimCubes.Count > 0) return false;
        if (_pickupHandler != null && _pickupHandler.GetActivePickupStates() != null && _pickupHandler.GetActivePickupStates().Count > 0) return false;
        if (CarrierSystem.Instance != null && CarrierSystem.Instance.CarrierSpawner != null)
        {
            var spawnedCarriers = CarrierSystem.Instance.CarrierSpawner.SpawnedCarriers;
            if (spawnedCarriers != null)
            {
                foreach (var carrier in spawnedCarriers)
                {
                    if (carrier != null && carrier.IsDelivering) return false;
                }
            }
        }
        return true;
    }

    public bool ContainsDeliveryCube(Cube cube)
    {
        return TryGetDeliveryState(cube, out var state)
               && state != null
               && !state.IsPickedUp;
    }

    public bool TryRemoveDeliveryCube(Cube cube)
    {
        if (!ContainsDeliveryCube(cube)) return false;
        RemoveActiveDeliveryCube(cube);
        return true;
    }

    /// <summary>
    /// Đồng bộ các collider pickup theo danh sách carrier hiện tại.
    /// </summary>
    public void SetupCarrierPickup(List<CarrierBase> carriers)
    {
        _activeCarriers.Clear();
        if (carriers != null) _activeCarriers.AddRange(carriers);
        _cachedCarrierPickupProgresses.Clear();
        ClearPickupStates();
    }

    /// <summary>
    /// Xóa trạng thái pickup đang active khi rebuild collider hoặc reset conveyor.
    /// </summary>
    public void ClearPickupStates()
    {
        if (_pickupHandler != null) _pickupHandler.ClearStates();
    }

    /// <summary>
    /// Thử pickup một cube đang chạy trên conveyor vào carrier mục tiêu.
    /// </summary>
    public void TryPickupCube(Cube cube, CarrierBase targetCarrier)
    {
        if (!TryGetDeliveryState(cube, out var state)) return;
        TryPickupState(state, targetCarrier, null);
    }
    
    #endregion

    #region Delivery Spawn Flow

    /// <summary>
    /// Kiểm tra request unload có đủ dữ liệu để spawn cube không.
    /// </summary>
    private bool CanSpawnCarrierUnload(CarrierUnloadRequest unloadRequest)
    {
        return unloadRequest != null
               && unloadRequest.SourceCarrier != null
               && unloadRequest.CubeCount > 0;
    }

    /// <summary>
    /// Hoàn tất cube bay ra conveyor và tạo cube thật chạy theo spline.
    /// </summary>
    private void CompleteUnload(
        AnimCube animCube,
        CarrierBase carrier,
        CarrierCubePayload payload,
        float progress,
        Vector3 deliveryTarget,
        bool isFirstCube,
        int undoBatchId)
    {
        var cube = _deliveryCubeFactory.CreateCubeInstance();
        _deliveryCubeFactory.SetupCube(
            cube,
            payload.StartWorldPosition,
            payload.BlockColorType,
            GetSpawnRoot());
        float progressOffset = 0f;
        if (carrier != null)
        {
            float pickupProgress = carrier.GetActualPickupProgress();
            float spawnProgress = carrier.SplineProgress;
            progressOffset = pickupProgress - spawnProgress;
            if (progressOffset > 0.5f) progressOffset -= 1f;
            else if (progressOffset < -0.5f) progressOffset += 1f;
        }
        cube.Setup(Path, progress, progressOffset, deliveryTarget);
        var state = new DeliveryCubeState(cube, carrier, payload.BlockColorType, payload.Color, undoBatchId);
        state.PreviousProgress = Mathf.Repeat(progress, 1f);
        state.PreviousProgressCorner = state.PreviousProgress;
        _deliveryStates.Add(state);
        cachedMovers.Add(cube);
        TryApplySpawnAreaEffects(progress, isFirstCube, undoBatchId);
        CapacityManager.Instance.AddCube();
        
        PushAnimCubeToPool(animCube);
    }

    private void TryApplySpawnAreaEffects(float progress, bool isFirstCube, int undoBatchId)
    {
        if (!isFirstCube) return;
        _conveyorCubeSpeedController?.BoostCubesAheadFromProgress(progress);
        _conveyorCubeSpeedController?.LockCubesAroundSpawnTemporarily(progress, undoBatchId);
    }

    #endregion

    #region Pickup Rules

    /// <summary>
    /// Tìm state tương ứng với cube đang chạy trên conveyor.
    /// </summary>
    private bool TryGetDeliveryState(Cube cube, out DeliveryCubeState state)
    {
        state = null;
        for (var i = 0; i < _deliveryStates.Count; i++)
        {
            var currentState = _deliveryStates[i];
            if (currentState.Cube != cube) continue;
            state = currentState;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Bắt đầu một lượt receive nếu carrier mục tiêu còn khả năng nhận cube.
    /// </summary>
    private bool TryBeginReceive(
        DeliveryCubeState state,
        CarrierBase targetCarrier,
        out CarrierReceiveReservation reservation)
    {
        reservation = default;
        
        // logic check điều kiện xét carrier hiện tại
        //if (!CanPickupCube(state, targetCarrier)) return false;
        // --------------------------
        
        // logic check điều kiện quét tất cả các carrier phía trước
         if (_pickupRule == null) _pickupRule = new ForwardBestSlotPickupRule(_pickupHandler);
         if (!_pickupRule.CanPickupTarget(state, targetCarrier, _activeCarriers)) return false;
        // --------------------------
        
        if (!targetCarrier.TryReserveReceive(state.BlockColorType, out reservation, state.UndoBatchId)) return false;

        state.IsPickedUp = true;
        _pickupHandler.BeginPickup(targetCarrier, state.BlockColorType);
        return true;
    }

    /// <summary>
    /// Kiểm tra cube có thể được pickup vào carrier mục tiêu không.
    /// </summary>
    private bool CanPickupCube(DeliveryCubeState state, CarrierBase targetCarrier)
    {
        var canReturnToSourceCarrier = CanReturnToSourceCarrier(state, targetCarrier);
        var canPickupColor = CanPickupColor(targetCarrier, state.BlockColorType);
        var isDelivering = targetCarrier != null && targetCarrier.IsDelivering;
        return !state.IsPickedUp
               && targetCarrier != null
               && !isDelivering
               && canReturnToSourceCarrier
               && canPickupColor;
    }

    /// <summary>
    /// Kiểm tra carrier có đang nhận đúng màu cube này hoặc chưa nhận màu nào không.
    /// </summary>
    private bool CanPickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        return _pickupHandler.CanPickupColor(targetCarrier, blockColorType);
    }

    /// <summary>
    /// Chỉ cho cube quay về carrier nguồn sau khi đã đi hết một vòng.
    /// </summary>
    private bool CanReturnToSourceCarrier(DeliveryCubeState state, CarrierBase targetCarrier)
    {
        if (state.SourceCarrier != targetCarrier) return true;
        return state.Cube != null && state.Cube.HasCompletedLap();
    }

    #endregion

    #region Cleanup And Cancellation

    /// <summary>
    /// Xóa toàn bộ cube đang chạy trên spline.
    /// </summary>
    private void ClearFollowers()
    {
        if (_unloadHandler != null) _unloadHandler.CancelAll();
        foreach (var follower in cachedMovers)
        {
            if (follower == null) continue;
#if UNITY_LUNA
            Destroy(follower.gameObject);
#else
            PoolManagerNew.Instance.PushToPool(follower);
#endif
        }

        cachedMovers.Clear();
        _deliveryStates.Clear();
    }

    /// <summary>
    /// Xóa toàn bộ anim cube đang bay.
    /// </summary>
    private void ClearActiveAnimCubes()
    {
        for (var i = _activeAnimCubes.Count - 1; i >= 0; i--)
        {
            var animCube = _activeAnimCubes[i];
            if (animCube == null) continue;
#if UNITY_LUNA
            Destroy(animCube.gameObject);
#else
            PoolManagerNew.Instance.PushToPool(animCube);
#endif
        }
        _activeAnimCubes.Clear();
    }

    /// <summary>
    /// Đẩy anim cube về pool và xóa khỏi danh sách active.
    /// </summary>
    private void PushAnimCubeToPool(AnimCube animCube)
    {
        if (animCube == null) return;
        _activeAnimCubes.Remove(animCube);
#if UNITY_LUNA
        Destroy(animCube.gameObject);
#else
        PoolManagerNew.Instance.PushToPool(animCube);
#endif
        EvaluateLoseCondition();
    }
    
    public void EvaluateLoseCondition()
    {
        if (_activeAnimCubes.Count == 0 && _pickupHandler.GetActivePickupStates().Count == 0)
        {
            _loseDetector.OnCheckLose();
        }
    }

    /// <summary>
    /// Kiểm tra xem có bất kỳ cube nào đang chạy trên conveyor có thể được nhận bởi bất kỳ Carrier nào đang hoạt động hay không.
    /// </summary>
    public bool CanAnyConveyorCubeBeReceived()
    {
        if (_deliveryStates == null || _deliveryStates.Count == 0) return false;
        if (_activeCarriers == null || _activeCarriers.Count == 0 || _pickupRule == null) return false;

        for (var i = 0; i < _deliveryStates.Count; i++)
        {
            var state = _deliveryStates[i];
            if (state == null || state.IsPickedUp || state.Cube == null) continue;

            for (var j = 0; j < _activeCarriers.Count; j++)
            {
                var carrier = _activeCarriers[j];
                if (carrier == null || !carrier.Interactable || carrier.IsLockedByContainer()) continue;

                if (_pickupRule.CanPickupTarget(state, carrier, _activeCarriers, isCanReturnToSourceCarrier: true))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Kiểm tra xem người chơi có chắc chắn thắng hay không dựa trên số lượng cube trên băng chuyền và ô trống trong các carrier.
    /// </summary>
    public bool IsWinGuaranteed()
    {
        // 1. Xác định các màu sắc còn lại cần hoàn thành để thắng
        var remainingRequiredColors = new HashSet<EBlockColorType>();
        if (_winDetector != null)
        {
            var req = _winDetector.RequiredColors;
            var fin = _winDetector.FinishedColors;
            if (req != null)
            {
                foreach (var color in req)
                {
                    if (fin == null || !fin.Contains(color))
                    {
                        remainingRequiredColors.Add(color);
                    }
                }
            }
        }

        // 2. Thống kê số lượng cube của từng màu đang chạy trên băng chuyền (chưa được pickup).
        var conveyorCubes = new Dictionary<EBlockColorType, int>();
        if (_deliveryStates != null)
        {
            for (var i = 0; i < _deliveryStates.Count; i++)
            {
                var state = _deliveryStates[i];
                if (state != null && state.Cube != null && !state.IsPickedUp && state.BlockColorType != EBlockColorType.None)
                {
                    conveyorCubes.TryGetValue(state.BlockColorType, out int val);
                    conveyorCubes[state.BlockColorType] = val + 1;
                }
            }
        }


        // 4. Thống kê số lượng cube đang bay từ carrier ra conveyor (in-flight to conveyor).
        if (_unloadHandler != null)
        {
            for (int colorVal = 0; colorVal <= 13; colorVal++)
            {
                var colorType = (EBlockColorType)colorVal;
                int inFlightToConveyor = _unloadHandler.GetInFlightToConveyorCount(colorType);
                if (inFlightToConveyor > 0)
                {
                    conveyorCubes.TryGetValue(colorType, out int val);
                    conveyorCubes[colorType] = val + inFlightToConveyor;
                }
            }
        }

        // 5. Thống kê số lượng cube còn lại trong các block đang thực hiện dỡ hàng (unloading blocks).
        if (_activeCarriers != null)
        {
            for (var i = 0; i < _activeCarriers.Count; i++)
            {
                var carrier = _activeCarriers[i];
                if (carrier == null || carrier.RuntimeState == null || carrier.RuntimeState.IsFinished) continue;

                if (carrier.BlockLayout != null && carrier.BlockLayout.Blocks != null)
                {
                    var blocks = carrier.BlockLayout.Blocks;
                    for (var j = 0; j < blocks.Count; j++)
                    {
                        var block = blocks[j];
                        if (block != null && block.HasContent && block.IsOpened)
                        {
                            var blockColor = block.GetBlockColorType();
                            if (blockColor != EBlockColorType.None)
                            {
                                int currentCubes = block.GetCurrentCubes();
                                if (currentCubes > 0)
                                {
                                    conveyorCubes.TryGetValue(blockColor, out int val);
                                    conveyorCubes[blockColor] = val + currentCubes;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 6. Phân tích nhu cầu của các Carrier chưa hoàn thành và số lượng carrier trống
        int emptyCarriersCount = 0;
        var occupiedCarriersByColor = new Dictionary<EBlockColorType, List<CarrierBase>>();
        int maxCubesPerBlock = CapacityManager.Instance != null ? CapacityManager.Instance.CubePerBlock : 4;

        if (_activeCarriers != null)
        {
            for (var i = 0; i < _activeCarriers.Count; i++)
            {
                var carrier = _activeCarriers[i];
                if (carrier == null) continue;

                // Nếu carrier là Spawner, bỏ qua vì Spawner không thể nhận cube

                // Nếu carrier đã hoàn thành, bỏ qua
                if (carrier.RuntimeState != null && carrier.RuntimeState.IsFinished) continue;

                // Nếu carrier bị khóa và không thể mở trong lượt này, bỏ qua

                EBlockColorType carrierColor = EBlockColorType.None;
                bool isOccupied = false;
                bool isMixed = false;

                // Nếu carrier là SpecialColorReceiver cho một màu cụ thể
                if (carrier.TryGetSpecialReceiverTargetColor(out var specialColor) && specialColor != EBlockColorType.None)
                {
                    isOccupied = true;
                    carrierColor = specialColor;
                }

                // Quét qua các block ổn định (không đang Unload) trong layout để xác định màu mục tiêu của carrier
                if (carrier.BlockLayout != null && carrier.BlockLayout.Blocks != null)
                {
                    var blocks = carrier.BlockLayout.Blocks;
                    for (var j = 0; j < blocks.Count; j++)
                    {
                        var block = blocks[j];
                        if (block != null && block.HasContent && !block.IsOpened)
                        {
                            var blockColor = block.GetBlockColorType();
                            if (blockColor != EBlockColorType.None)
                            {
                                if (carrierColor == EBlockColorType.None)
                                {
                                    carrierColor = blockColor;
                                    isOccupied = true;
                                }
                                else if (carrierColor != blockColor)
                                {
                                    isMixed = true;
                                }
                            }
                        }
                    }
                }

                // Nếu carrier chứa các màu block khác nhau ổn định (chưa được sắp xếp), không thể tự động hoàn thành trực tiếp
                if (isMixed) return false;

                // Kiểm tra xem có cube nào đang bay vào carrier này không để xác định màu (nếu các block ổn định của carrier đều trống)
                if (!isOccupied && _pickupHandler != null)
                {
                    var activePickupStates = _pickupHandler.GetActivePickupStates();
                    if (activePickupStates != null && activePickupStates.TryGetValue(carrier, out var pickupState))
                    {
                        if (pickupState != null && pickupState.InFlightCount > 0 && pickupState.BlockColorType != EBlockColorType.None)
                        {
                            isOccupied = true;
                            carrierColor = pickupState.BlockColorType;
                        }
                    }
                }

                if (!isOccupied)
                {
                    emptyCarriersCount++;
                }
                else
                {
                    if (!occupiedCarriersByColor.ContainsKey(carrierColor))
                    {
                        occupiedCarriersByColor[carrierColor] = new List<CarrierBase>();
                    }
                    occupiedCarriersByColor[carrierColor].Add(carrier);
                }
            }
        }

        // 7. Giả lập xếp cube vào carrier và kiểm tra điều kiện hoàn thành màu
        int emptyCarriersUsed = 0;
        int carrierMaxBlockCount = 4;
        if (_activeCarriers != null && _activeCarriers.Count > 0 && _activeCarriers[0] != null)
        {
            carrierMaxBlockCount = _activeCarriers[0].MaxBlockCount;
        }
        int carrierCapacity = carrierMaxBlockCount * maxCubesPerBlock;

        var simulatedCompletedColors = new HashSet<EBlockColorType>();

        foreach (var pair in conveyorCubes)
        {
            var color = pair.Key;
            var remainingCubes = pair.Value;
            if (remainingCubes <= 0) continue;

            bool colorCompleted = false;

            // 7.1 Phân bổ vào các carrier đã có sẵn màu này
            if (occupiedCarriersByColor.TryGetValue(color, out var carriersOfColor))
            {
                for (var i = 0; i < carriersOfColor.Count; i++)
                {
                    var carrier = carriersOfColor[i];
                    if (carrier == null) continue;

                    int totalCapacity = carrier.MaxBlockCount * maxCubesPerBlock;
                    int currentCubes = 0;

                    if (carrier.BlockLayout != null && carrier.BlockLayout.Blocks != null)
                    {
                        var blocks = carrier.BlockLayout.Blocks;
                        for (var j = 0; j < blocks.Count; j++)
                        {
                            var block = blocks[j];
                            if (block != null && !block.IsOpened)
                            {
                                currentCubes += block.GetCurrentCubes();
                            }
                        }
                    }

                    int inFlight = 0;
                    if (_pickupHandler != null)
                    {
                        var activePickupStates = _pickupHandler.GetActivePickupStates();
                        if (activePickupStates != null && activePickupStates.TryGetValue(carrier, out var pickupState))
                        {
                            if (pickupState != null && pickupState.BlockColorType == color)
                            {
                                inFlight = pickupState.InFlightCount;
                            }
                        }
                    }

                    int emptySlots = totalCapacity - (currentCubes + inFlight);
                    if (emptySlots < 0) emptySlots = 0;

                    int placed = Mathf.Min(remainingCubes, emptySlots);
                    remainingCubes -= placed;

                    if (currentCubes + inFlight + placed == totalCapacity)
                    {
                        colorCompleted = true;
                        emptyCarriersCount++; // Completed carrier leaves and spawns a new empty carrier
                    }
                }
            }

            // 7.2 Phân bổ vào các carrier trống
            if (remainingCubes > 0)
            {
                // Nếu màu này là bắt buộc để thắng nhưng chưa được hoàn thành
                if (remainingRequiredColors.Contains(color) && !colorCompleted)
                {
                    if (carrierCapacity <= 0 || remainingCubes < carrierCapacity)
                    {
                        return false;
                    }

                    remainingCubes -= carrierCapacity;
                    colorCompleted = true;
                    emptyCarriersUsed += 1;
                    emptyCarriersCount++; // Completed empty carrier leaves and spawns a new empty carrier
                }

                // Phân bổ phần dư còn lại
                if (remainingCubes > 0)
                {
                    if (carrierCapacity <= 0) return false;
                    int fullCarriersCount = remainingCubes / carrierCapacity;
                    emptyCarriersUsed += fullCarriersCount;
                    emptyCarriersCount += fullCarriersCount;

                    int remainder = remainingCubes % carrierCapacity;
                    if (remainder > 0)
                    {
                        emptyCarriersUsed += 1;
                    }
                    remainingCubes = 0;
                }
            }

            if (colorCompleted)
            {
                simulatedCompletedColors.Add(color);
            }
        }

        // 8. Đảm bảo tất cả các màu yêu cầu còn lại đã được hoàn thành trong giả lập
        foreach (var color in remainingRequiredColors)
        {
            if (!simulatedCompletedColors.Contains(color))
            {
                return false;
            }
        }

        // 9. Đảm bảo số lượng carrier trống được sử dụng không vượt quá thực tế
        return emptyCarriersUsed <= emptyCarriersCount;
    }

    public int GetInFlightToConveyorCount(EBlockColorType color)
    {
        return _unloadHandler != null ? _unloadHandler.GetInFlightToConveyorCount(color) : 0;
    }

    /// <summary>
    /// Lấy tập hợp các màu cube đang chạy thực tế trên băng chuyền (chưa được pickup).
    /// </summary>
    public HashSet<EBlockColorType> GetActiveConveyorColors()
    {
        var colors = new HashSet<EBlockColorType>();
        if (_deliveryStates == null) return colors;
        for (var i = 0; i < _deliveryStates.Count; i++)
        {
            var state = _deliveryStates[i];
            if (state != null && !state.IsPickedUp && state.Cube != null)
            {
                colors.Add(state.BlockColorType);
            }
        }
        return colors;
    }

    private void CheckTutorial()
    {
        if (_activeAnimCubes.Count == 0 && _pickupHandler.GetActivePickupStates().Count == 0)
        {
            GameEventBus.OnCarrierPickupDone?.Invoke();
        }
    }

    public void RefreshPreloseBlink()
    {
        var isPrelose = CapacityManager.Instance && CapacityManager.Instance.IsPrelose;
        ApplyBlinkToFirstRenderer(isPrelose);
    }

    /// <summary>
    /// Xóa delivery state của cube đã được pickup.
    /// </summary>
    private void RemoveDeliveryState(Cube cube)
    {
        for (var i = _deliveryStates.Count - 1; i >= 0; i--)
        {
            if (_deliveryStates[i].Cube == cube) _deliveryStates.RemoveAt(i);
        }
    }

    /// <summary>
    /// Hoàn tất pickup khỏi spline và trả cube chạy conveyor về pool.
    /// </summary>
    private void CompleteSplinePickup(Cube cube)
    {
        RemoveActiveDeliveryCube(cube);
    }

    private void RemoveActiveDeliveryCube(Cube cube)
    {
        if (cube == null) return;
        cachedMovers.Remove(cube);
        RemoveDeliveryState(cube);
#if UNITY_LUNA
        Destroy(cube.gameObject);
#else
        PoolManagerNew.Instance.PushToPool(cube);
#endif
        CapacityManager.Instance.RemoveCube();
    }

    #endregion

    /// <summary>
    /// Lấy root dùng để chứa cube được spawn.
    /// </summary>
    private Transform GetSpawnRoot()
    {
        return spawnRoot != null ? spawnRoot : transform;
    }

    private ConveyorCornerDetector GetCornerDetector()
    {
        return conveyorCornerDetector != null
            ? conveyorCornerDetector
            : GetComponent<ConveyorCornerDetector>();
    }

    public float GetProgressLevel()
    {
        return _winDetector.GetProgressLevel();
    }

    private void OnDrawGizmos()
    {
        if (_conveyorCubeSpeedController == null) return;
        var carriers = CarrierSystem.Instance?.CarrierSpawner?.SpawnedCarriers;
        var path = Path;
        if (path == null && conveyorManager == null) conveyorManager = GetComponent<ConveyorManager>();
        path = Path;
        _conveyorCubeSpeedController.DrawPickupRanges(path, carriers);
    }
}
