using System;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorDeliverySystem : MonoSingleton<ConveyorDeliverySystem>, IConveyorPickupHandler
{
	[SerializeField]
	private ConveyorManager conveyorManager;

	[SerializeField]
	private ConveyorMeshBuilder conveyorMeshBuilder;

	[SerializeField]
	private CubeConfigSO cubeConfig;

	[SerializeField]
	private float spawnInterval = 0.01f;

	[SerializeField]
	private Transform spawnRoot;

	[SerializeField]
	private ConveyorSpawnPointConfigSO conveyorSpawnPointConfig;

	[SerializeField]
	private List<Cube> cachedMovers = new List<Cube>();

	[SerializeField]
	private ConveyorSpeedBoostConfigSO conveyorSpeedBoostConfig;

	[SerializeField]
	private ConveyorCornerDetector conveyorCornerDetector;

	[Header("Pickup Config")]
	[SerializeField]
	private float pickupThreshold = 0.02f;

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

	private const string InstancesRootPrefix = "root-";

	private const float MaxPickupProgressEpsilon = 0.002f;

	private const float BaseMaxPickupTravelDeltaPerStep = 0.1f;

	private const float WrapPreviousProgressThreshold = 0.8f;

	private const float WrapCurrentProgressThreshold = 0.2f;

	private static readonly int BlinkEnabledId = Shader.PropertyToID("_BlinkEnabled");

	private static readonly int BlinkStartTimeId = Shader.PropertyToID("_BlinkStartTime");

	public bool IsConveyorEmpty => _activeAnimCubes.Count == 0 && _deliveryStates.Count == 0;

	public bool HasActiveCubesOnConveyor => _deliveryStates.Count > 0;

	public ConveyorSpeedBoostConfigSO ConveyorSpeedBoostConfig => conveyorSpeedBoostConfig;

	public ConveyorPathRuntime Path => (conveyorManager != null) ? conveyorManager.Path : null;

	public ConveyorWinDetector WinDetector => _winDetector;

	public bool IsUnloadActive => _unloadHandler != null && _unloadHandler.IsUnloadActive;

	public bool IsPickupActive => _pickupHandler != null && _pickupHandler.GetActivePickupStates().Count > 0;

	private float MaxPickupTravelDeltaPerStep
	{
		get
		{
			float timeScale = ((MonoSingleton<CustomTimeScaleGroup>.Instance != null) ? MonoSingleton<CustomTimeScaleGroup>.Instance.CurrentTimeScale : 1f);
			float speedScale = timeScale;
			if (timeScale > 1f)
			{
				float multiplier = 0.2f;
				if (MonoSingleton<ConfigManager>.Instance != null)
				{
					CubeMovementConfigSO movementConfig = MonoSingleton<ConfigManager>.Instance.GetCubeMovementConfig();
					if (movementConfig != null)
					{
						multiplier = movementConfig.ScaleSpeedMultiplier;
					}
				}
				speedScale = 1f + (timeScale - 1f) * multiplier;
			}
			return 0.1f * Mathf.Max(1f, speedScale);
		}
	}

	public void RequestPlayLoopSound()
	{
		int activeCount = (IsPickupActive ? 1 : 0) + (IsUnloadActive ? 1 : 0);
		if (activeCount == 1 && MonoSingleton<SoundManager>.Instance != null)
		{
			MonoSingleton<SoundManager>.Instance.PlayLoop(AudioClipName.sfx_merge_loop);
		}
	}

	public void RequestStopLoopSound()
	{
		if (!IsPickupActive && !IsUnloadActive && MonoSingleton<SoundManager>.Instance != null)
		{
			MonoSingleton<SoundManager>.Instance.StopLoop();
		}
	}

	private void ApplyBlinkToFirstRenderer(bool active)
	{
		Transform root = GetInstancesRoot();
		if (!(root == null))
		{
			Renderer conveyorRenderer = root.GetComponentInChildren<Renderer>(true);
			if ((bool)conveyorRenderer)
			{
				SetBlinkEnabled(conveyorRenderer.sharedMaterial, active);
			}
		}
	}

	private void SetBlinkEnabled(Material material, bool active)
	{
		if ((bool)material && material.GetFloat(BlinkEnabledId) > 0.5f != active)
		{
			if (active)
			{
				material.SetFloat(BlinkStartTimeId, Time.time);
			}
			material.SetFloat(BlinkEnabledId, active ? 1f : 0f);
		}
	}

	private Transform GetInstancesRoot()
	{
		foreach (Transform child in base.transform)
		{
			if (child.name.StartsWith("root-"))
			{
				return child;
			}
		}
		return null;
	}

	protected override void Awake()
	{
		base.Awake();
		if (conveyorManager == null)
		{
			conveyorManager = GetComponent<ConveyorManager>();
		}
		if (conveyorCornerDetector == null)
		{
			conveyorCornerDetector = GetComponent<ConveyorCornerDetector>();
		}
		if (_conveyorSpawnPointCalculator == null)
		{
			_conveyorSpawnPointCalculator = new ConveyorSpawnPointCalculator(conveyorSpawnPointConfig, conveyorMeshBuilder, Path, GetSpawnRoot(), base.transform);
		}
		if (_deliveryCubeFactory == null)
		{
			_deliveryCubeFactory = new ConveyorDeliveryCubeFactory(cubeConfig);
		}
		if (_conveyorCubeSpeedController == null)
		{
			_conveyorCubeSpeedController = new ConveyorCubeSpeedController(_deliveryStates, conveyorSpeedBoostConfig);
		}
		_pickupHandler = new ConveyorPickupHandler(_deliveryCubeFactory, GetSpawnRoot(), CompleteSplinePickup, PushAnimCubeToPool, CheckTutorial);
		_pickupRule = new ForwardBestSlotPickupRule(_pickupHandler);
		_unloadHandler = new ConveyorUnloadHandler(_deliveryCubeFactory, _conveyorSpawnPointCalculator, _activeAnimCubes, GetSpawnRoot(), spawnInterval, CompleteUnload);
		_loseDetector = new ConveyorLoseDetector(_deliveryStates, _activeCarriers, _pickupHandler.GetActivePickupStates(), _activeAnimCubes, _pickupRule);
		_winDetector = new ConveyorWinDetector();
		GameEventBus.OnLevelLoaded = (Action<LevelData>)Delegate.Combine(GameEventBus.OnLevelLoaded, new Action<LevelData>(OnLevelLoaded));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Combine(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
	}

	protected override void OnDestroy()
	{
		GameEventBus.OnLevelLoaded = (Action<LevelData>)Delegate.Remove(GameEventBus.OnLevelLoaded, new Action<LevelData>(OnLevelLoaded));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Remove(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
		base.OnDestroy();
	}

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
		UpdateLunaManualCubeMovement(Time.unscaledDeltaTime);
		ConveyorCornerDetector cornerDetector = GetCornerDetector();
		if (_conveyorCubeSpeedController != null && !(cornerDetector == null))
		{
			_conveyorCubeSpeedController.BoostCubesPassingCorners(cornerDetector.CornerProgresses);
			UpdatePickupDetection();
		}
	}

	private void UpdateLunaManualCubeMovement(float deltaTime)
	{
		if (_deliveryStates == null || _deliveryStates.Count == 0)
		{
			return;
		}
		for (int i = _deliveryStates.Count - 1; i >= 0; i--)
		{
			DeliveryCubeState state = _deliveryStates[i];
			if (state != null && !state.IsPickedUp && !(state.Cube == null))
			{
				state.Cube.ManualUpdate(deltaTime);
			}
		}
	}

	private void UpdatePickupDetection()
	{
		if (_activeCarriers == null || _activeCarriers.Count == 0 || _deliveryStates.Count == 0)
		{
			return;
		}
		int carrierCount = _activeCarriers.Count;
		while (_cachedCarrierPickupProgresses.Count < carrierCount)
		{
			_cachedCarrierPickupProgresses.Add(0f);
		}
		for (int j = 0; j < carrierCount; j++)
		{
			CarrierBase carrier = _activeCarriers[j];
			_cachedCarrierPickupProgresses[j] = ((carrier != null && carrier.Interactable) ? carrier.GetActualPickupProgress() : 0f);
		}
		for (int i = _deliveryStates.Count - 1; i >= 0; i--)
		{
			DeliveryCubeState state = _deliveryStates[i];
			if (state != null && !state.IsPickedUp && !(state.Cube == null))
			{
				Cube cube = state.Cube;
				float cubeProgress = cube.GetProgress();
				float previousProgress = state.PreviousProgress;
				bool pickedUp = false;
				for (int k = 0; k < carrierCount; k++)
				{
					CarrierBase carrier2 = _activeCarriers[k];
					if (!(carrier2 == null) && carrier2.Interactable)
					{
						float pickupProgress = _cachedCarrierPickupProgresses[k];
						if (HasReachedPickupProgress(previousProgress, cubeProgress, pickupProgress) && TryPickupState(state, carrier2, pickupProgress))
						{
							pickedUp = true;
							break;
						}
					}
				}
				if (!pickedUp && !state.IsPickedUp)
				{
					state.PreviousProgress = cubeProgress;
				}
			}
		}
	}

	private bool HasReachedPickupProgress(float previousProgress, float currentProgress, float pickupProgress)
	{
		if (!TryGetForwardPickupTravelDelta(previousProgress, currentProgress, out var travelDelta))
		{
			return false;
		}
		float pickupDelta = GetForwardProgressDelta(previousProgress, pickupProgress);
		return pickupDelta <= travelDelta + GetPickupProgressEpsilon();
	}

	private bool TryGetForwardPickupTravelDelta(float previousProgress, float currentProgress, out float travelDelta)
	{
		previousProgress = Mathf.Repeat(previousProgress, 1f);
		currentProgress = Mathf.Repeat(currentProgress, 1f);
		if (currentProgress >= previousProgress)
		{
			travelDelta = currentProgress - previousProgress;
			return travelDelta > GetPickupProgressEpsilon() && travelDelta <= MaxPickupTravelDeltaPerStep;
		}
		if (previousProgress > 0.8f && currentProgress < 0.2f)
		{
			travelDelta = 1f - previousProgress + currentProgress;
			return travelDelta > GetPickupProgressEpsilon() && travelDelta <= MaxPickupTravelDeltaPerStep;
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
		return Mathf.Clamp(pickupThreshold, 1E-05f, 0.002f);
	}

	public void ClearAllCubes()
	{
		ClearFollowers();
		ClearActiveAnimCubes();
	}

	public bool TrySpawnCarrierUnload(CarrierUnloadRequest unloadRequest)
	{
		if (!CanSpawnCarrierUnload(unloadRequest))
		{
			return false;
		}
		_conveyorSpawnPointCalculator.CacheDeliverySpreadSettings();
		_unloadHandler.HandleUnload(unloadRequest);
		return true;
	}

	private bool TryPickupState(DeliveryCubeState state, CarrierBase targetCarrier, float? pickupProgress)
	{
		if (targetCarrier.LockPick)
		{
			return false;
		}
		if (state == null || state.Cube == null)
		{
			return false;
		}
		if (!TryBeginReceive(state, targetCarrier, out var reservation))
		{
			return false;
		}
		if (pickupProgress.HasValue)
		{
			SnapCubeToSplineProgress(state.Cube, pickupProgress.Value);
		}
		AnimCube animCube = _deliveryCubeFactory.CreateAnimCubeInstance(GetSpawnRoot(), _activeAnimCubes);
		_pickupHandler.HandleReceiveCube(state.Cube, targetCarrier, state.Color, animCube, reservation);
		return true;
	}

	private void SnapCubeToSplineProgress(Cube cube, float progress)
	{
		ConveyorPathRuntime path = Path;
		if (!(cube == null) && path != null && path.IsValid)
		{
			float normalizedProgress = Mathf.Repeat(progress, 1f);
			Vector3 worldPosition = path.EvaluateWorldPosition(normalizedProgress);
			cube.transform.position = worldPosition;
			cube.SyncProgress(normalizedProgress);
		}
	}

	public bool IsReceivingCube(CarrierBase carrier)
	{
		return _pickupHandler.IsReceivingCube(carrier);
	}

	public bool IsConveyorStable()
	{
		if (_activeAnimCubes != null && _activeAnimCubes.Count > 0)
		{
			return false;
		}
		if (_pickupHandler != null && _pickupHandler.GetActivePickupStates() != null && _pickupHandler.GetActivePickupStates().Count > 0)
		{
			return false;
		}
		if (MonoSingleton<CarrierSystem>.Instance != null && MonoSingleton<CarrierSystem>.Instance.CarrierSpawner != null)
		{
			List<CarrierBase> spawnedCarriers = MonoSingleton<CarrierSystem>.Instance.CarrierSpawner.SpawnedCarriers;
			if (spawnedCarriers != null)
			{
				foreach (CarrierBase carrier in spawnedCarriers)
				{
					if (carrier != null && carrier.IsDelivering)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public bool ContainsDeliveryCube(Cube cube)
	{
		DeliveryCubeState state;
		return TryGetDeliveryState(cube, out state) && state != null && !state.IsPickedUp;
	}

	public bool TryRemoveDeliveryCube(Cube cube)
	{
		if (!ContainsDeliveryCube(cube))
		{
			return false;
		}
		RemoveActiveDeliveryCube(cube);
		return true;
	}

	public void SetupCarrierPickup(List<CarrierBase> carriers)
	{
		_activeCarriers.Clear();
		if (carriers != null)
		{
			_activeCarriers.AddRange(carriers);
		}
		_cachedCarrierPickupProgresses.Clear();
		ClearPickupStates();
	}

	public void ClearPickupStates()
	{
		if (_pickupHandler != null)
		{
			_pickupHandler.ClearStates();
		}
	}

	public void TryPickupCube(Cube cube, CarrierBase targetCarrier)
	{
		if (TryGetDeliveryState(cube, out var state))
		{
			TryPickupState(state, targetCarrier, null);
		}
	}

	private bool CanSpawnCarrierUnload(CarrierUnloadRequest unloadRequest)
	{
		return unloadRequest != null && unloadRequest.SourceCarrier != null && unloadRequest.CubeCount > 0;
	}

	private void CompleteUnload(AnimCube animCube, CarrierBase carrier, CarrierCubePayload payload, float progress, Vector3 deliveryTarget, bool isFirstCube, int undoBatchId)
	{
		Cube cube = _deliveryCubeFactory.CreateCubeInstance();
		_deliveryCubeFactory.SetupCube(cube, payload.StartWorldPosition, payload.BlockColorType, GetSpawnRoot());
		float progressOffset = 0f;
		if (carrier != null)
		{
			float pickupProgress = carrier.GetActualPickupProgress();
			float spawnProgress = carrier.SplineProgress;
			progressOffset = pickupProgress - spawnProgress;
			if (progressOffset > 0.5f)
			{
				progressOffset -= 1f;
			}
			else if (progressOffset < -0.5f)
			{
				progressOffset += 1f;
			}
		}
		cube.Setup(Path, progress, progressOffset, deliveryTarget);
		DeliveryCubeState state = new DeliveryCubeState(cube, carrier, payload.BlockColorType, payload.Color, undoBatchId);
		state.PreviousProgress = Mathf.Repeat(progress, 1f);
		state.PreviousProgressCorner = state.PreviousProgress;
		_deliveryStates.Add(state);
		cachedMovers.Add(cube);
		TryApplySpawnAreaEffects(progress, isFirstCube, undoBatchId);
		MonoSingleton<CapacityManager>.Instance.AddCube();
		PushAnimCubeToPool(animCube);
	}

	private void TryApplySpawnAreaEffects(float progress, bool isFirstCube, int undoBatchId)
	{
		if (isFirstCube)
		{
			_conveyorCubeSpeedController?.BoostCubesAheadFromProgress(progress);
			_conveyorCubeSpeedController?.LockCubesAroundSpawnTemporarily(progress, undoBatchId);
		}
	}

	private bool TryGetDeliveryState(Cube cube, out DeliveryCubeState state)
	{
		state = null;
		for (int i = 0; i < _deliveryStates.Count; i++)
		{
			DeliveryCubeState currentState = _deliveryStates[i];
			if (!(currentState.Cube != cube))
			{
				state = currentState;
				return true;
			}
		}
		return false;
	}

	private bool TryBeginReceive(DeliveryCubeState state, CarrierBase targetCarrier, out CarrierReceiveReservation reservation)
	{
		reservation = default(CarrierReceiveReservation);
		if (_pickupRule == null)
		{
			_pickupRule = new ForwardBestSlotPickupRule(_pickupHandler);
		}
		if (!_pickupRule.CanPickupTarget(state, targetCarrier, _activeCarriers))
		{
			return false;
		}
		if (!targetCarrier.TryReserveReceive(state.BlockColorType, out reservation, state.UndoBatchId))
		{
			return false;
		}
		state.IsPickedUp = true;
		_pickupHandler.BeginPickup(targetCarrier, state.BlockColorType);
		return true;
	}

	private bool CanPickupCube(DeliveryCubeState state, CarrierBase targetCarrier)
	{
		bool canReturnToSourceCarrier = CanReturnToSourceCarrier(state, targetCarrier);
		bool canPickupColor = CanPickupColor(targetCarrier, state.BlockColorType);
		bool isDelivering = targetCarrier != null && targetCarrier.IsDelivering;
		return !state.IsPickedUp && targetCarrier != null && !isDelivering && canReturnToSourceCarrier && canPickupColor;
	}

	private bool CanPickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
	{
		return _pickupHandler.CanPickupColor(targetCarrier, blockColorType);
	}

	private bool CanReturnToSourceCarrier(DeliveryCubeState state, CarrierBase targetCarrier)
	{
		if (state.SourceCarrier != targetCarrier)
		{
			return true;
		}
		return state.Cube != null && state.Cube.HasCompletedLap();
	}

	private void ClearFollowers()
	{
		if (_unloadHandler != null)
		{
			_unloadHandler.CancelAll();
		}
		foreach (Cube follower in cachedMovers)
		{
			if (!(follower == null))
			{
				UnityEngine.Object.Destroy(follower.gameObject);
			}
		}
		cachedMovers.Clear();
		_deliveryStates.Clear();
	}

	private void ClearActiveAnimCubes()
	{
		for (int i = _activeAnimCubes.Count - 1; i >= 0; i--)
		{
			AnimCube animCube = _activeAnimCubes[i];
			if (!(animCube == null))
			{
				UnityEngine.Object.Destroy(animCube.gameObject);
			}
		}
		_activeAnimCubes.Clear();
	}

	private void PushAnimCubeToPool(AnimCube animCube)
	{
		if (!(animCube == null))
		{
			_activeAnimCubes.Remove(animCube);
			UnityEngine.Object.Destroy(animCube.gameObject);
			EvaluateLoseCondition();
		}
	}

	public void EvaluateLoseCondition()
	{
		if (_activeAnimCubes.Count == 0 && _pickupHandler.GetActivePickupStates().Count == 0)
		{
			_loseDetector.OnCheckLose();
		}
	}

	public bool CanAnyConveyorCubeBeReceived()
	{
		if (_deliveryStates == null || _deliveryStates.Count == 0)
		{
			return false;
		}
		if (_activeCarriers == null || _activeCarriers.Count == 0 || _pickupRule == null)
		{
			return false;
		}
		for (int i = 0; i < _deliveryStates.Count; i++)
		{
			DeliveryCubeState state = _deliveryStates[i];
			if (state == null || state.IsPickedUp || state.Cube == null)
			{
				continue;
			}
			for (int j = 0; j < _activeCarriers.Count; j++)
			{
				CarrierBase carrier = _activeCarriers[j];
				if (!(carrier == null) && carrier.Interactable && !carrier.IsLockedByContainer() && _pickupRule.CanPickupTarget(state, carrier, _activeCarriers))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsWinGuaranteed()
	{
		HashSet<EBlockColorType> remainingRequiredColors = new HashSet<EBlockColorType>();
		if (_winDetector != null)
		{
			HashSet<EBlockColorType> req = _winDetector.RequiredColors;
			HashSet<EBlockColorType> fin = _winDetector.FinishedColors;
			if (req != null)
			{
				foreach (EBlockColorType color2 in req)
				{
					if (fin == null || !fin.Contains(color2))
					{
						remainingRequiredColors.Add(color2);
					}
				}
			}
		}
		Dictionary<EBlockColorType, int> conveyorCubes = new Dictionary<EBlockColorType, int>();
		if (_deliveryStates != null)
		{
			for (int i = 0; i < _deliveryStates.Count; i++)
			{
				DeliveryCubeState state = _deliveryStates[i];
				if (state != null && state.Cube != null && !state.IsPickedUp && state.BlockColorType != EBlockColorType.None)
				{
					conveyorCubes.TryGetValue(state.BlockColorType, out var val);
					conveyorCubes[state.BlockColorType] = val + 1;
				}
			}
		}
		if (_unloadHandler != null)
		{
			for (int colorVal = 0; colorVal <= 13; colorVal++)
			{
				EBlockColorType colorType = (EBlockColorType)colorVal;
				int inFlightToConveyor = _unloadHandler.GetInFlightToConveyorCount(colorType);
				if (inFlightToConveyor > 0)
				{
					conveyorCubes.TryGetValue(colorType, out var val2);
					conveyorCubes[colorType] = val2 + inFlightToConveyor;
				}
			}
		}
		if (_activeCarriers != null)
		{
			for (int k = 0; k < _activeCarriers.Count; k++)
			{
				CarrierBase carrier2 = _activeCarriers[k];
				if (carrier2 == null || carrier2.RuntimeState == null || carrier2.RuntimeState.IsFinished || !(carrier2.BlockLayout != null) || carrier2.BlockLayout.Blocks == null)
				{
					continue;
				}
				List<Block> blocks3 = carrier2.BlockLayout.Blocks;
				for (int j2 = 0; j2 < blocks3.Count; j2++)
				{
					Block block3 = blocks3[j2];
					if (!(block3 != null) || !block3.HasContent || !block3.IsOpened)
					{
						continue;
					}
					EBlockColorType blockColor2 = block3.GetBlockColorType();
					if (blockColor2 != EBlockColorType.None)
					{
						int currentCubes2 = block3.GetCurrentCubes();
						if (currentCubes2 > 0)
						{
							conveyorCubes.TryGetValue(blockColor2, out var val3);
							conveyorCubes[blockColor2] = val3 + currentCubes2;
						}
					}
				}
			}
		}
		int emptyCarriersCount = 0;
		Dictionary<EBlockColorType, List<CarrierBase>> occupiedCarriersByColor = new Dictionary<EBlockColorType, List<CarrierBase>>();
		int maxCubesPerBlock = ((MonoSingleton<CapacityManager>.Instance != null) ? MonoSingleton<CapacityManager>.Instance.CubePerBlock : 4);
		if (_activeCarriers != null)
		{
			for (int l = 0; l < _activeCarriers.Count; l++)
			{
				CarrierBase carrier3 = _activeCarriers[l];
				if (carrier3 == null || (carrier3.RuntimeState != null && carrier3.RuntimeState.IsFinished))
				{
					continue;
				}
				EBlockColorType carrierColor = EBlockColorType.None;
				bool isOccupied = false;
				bool isMixed = false;
				if (carrier3.TryGetSpecialReceiverTargetColor(out var specialColor) && specialColor != EBlockColorType.None)
				{
					isOccupied = true;
					carrierColor = specialColor;
				}
				if (carrier3.BlockLayout != null && carrier3.BlockLayout.Blocks != null)
				{
					List<Block> blocks2 = carrier3.BlockLayout.Blocks;
					for (int n = 0; n < blocks2.Count; n++)
					{
						Block block2 = blocks2[n];
						if (!(block2 != null) || !block2.HasContent || block2.IsOpened)
						{
							continue;
						}
						EBlockColorType blockColor = block2.GetBlockColorType();
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
				if (isMixed)
				{
					return false;
				}
				if (!isOccupied && _pickupHandler != null)
				{
					Dictionary<CarrierBase, PickupState> activePickupStates2 = _pickupHandler.GetActivePickupStates();
					if (activePickupStates2 != null && activePickupStates2.TryGetValue(carrier3, out var pickupState2) && pickupState2 != null && pickupState2.InFlightCount > 0 && pickupState2.BlockColorType != EBlockColorType.None)
					{
						isOccupied = true;
						carrierColor = pickupState2.BlockColorType;
					}
				}
				if (!isOccupied)
				{
					emptyCarriersCount++;
					continue;
				}
				if (!occupiedCarriersByColor.ContainsKey(carrierColor))
				{
					occupiedCarriersByColor[carrierColor] = new List<CarrierBase>();
				}
				occupiedCarriersByColor[carrierColor].Add(carrier3);
			}
		}
		int emptyCarriersUsed = 0;
		int carrierMaxBlockCount = 4;
		if (_activeCarriers != null && _activeCarriers.Count > 0 && _activeCarriers[0] != null)
		{
			carrierMaxBlockCount = _activeCarriers[0].MaxBlockCount;
		}
		int carrierCapacity = carrierMaxBlockCount * maxCubesPerBlock;
		HashSet<EBlockColorType> simulatedCompletedColors = new HashSet<EBlockColorType>();
		foreach (KeyValuePair<EBlockColorType, int> pair in conveyorCubes)
		{
			EBlockColorType color3 = pair.Key;
			int remainingCubes = pair.Value;
			if (remainingCubes <= 0)
			{
				continue;
			}
			bool colorCompleted = false;
			if (occupiedCarriersByColor.TryGetValue(color3, out var carriersOfColor))
			{
				for (int j = 0; j < carriersOfColor.Count; j++)
				{
					CarrierBase carrier = carriersOfColor[j];
					if (carrier == null)
					{
						continue;
					}
					int totalCapacity = carrier.MaxBlockCount * maxCubesPerBlock;
					int currentCubes = 0;
					if (carrier.BlockLayout != null && carrier.BlockLayout.Blocks != null)
					{
						List<Block> blocks = carrier.BlockLayout.Blocks;
						for (int m = 0; m < blocks.Count; m++)
						{
							Block block = blocks[m];
							if (block != null && !block.IsOpened)
							{
								currentCubes += block.GetCurrentCubes();
							}
						}
					}
					int inFlight = 0;
					if (_pickupHandler != null)
					{
						Dictionary<CarrierBase, PickupState> activePickupStates = _pickupHandler.GetActivePickupStates();
						if (activePickupStates != null && activePickupStates.TryGetValue(carrier, out var pickupState) && pickupState != null && pickupState.BlockColorType == color3)
						{
							inFlight = pickupState.InFlightCount;
						}
					}
					int emptySlots = totalCapacity - (currentCubes + inFlight);
					if (emptySlots < 0)
					{
						emptySlots = 0;
					}
					int placed = Mathf.Min(remainingCubes, emptySlots);
					remainingCubes -= placed;
					if (currentCubes + inFlight + placed == totalCapacity)
					{
						colorCompleted = true;
						emptyCarriersCount++;
					}
				}
			}
			if (remainingCubes > 0)
			{
				if (remainingRequiredColors.Contains(color3) && !colorCompleted)
				{
					if (carrierCapacity <= 0 || remainingCubes < carrierCapacity)
					{
						return false;
					}
					remainingCubes -= carrierCapacity;
					colorCompleted = true;
					emptyCarriersUsed++;
					emptyCarriersCount++;
				}
				if (remainingCubes > 0)
				{
					if (carrierCapacity <= 0)
					{
						return false;
					}
					int fullCarriersCount = remainingCubes / carrierCapacity;
					emptyCarriersUsed += fullCarriersCount;
					emptyCarriersCount += fullCarriersCount;
					int remainder = remainingCubes % carrierCapacity;
					if (remainder > 0)
					{
						emptyCarriersUsed++;
					}
					remainingCubes = 0;
				}
			}
			if (colorCompleted)
			{
				simulatedCompletedColors.Add(color3);
			}
		}
		foreach (EBlockColorType color in remainingRequiredColors)
		{
			if (!simulatedCompletedColors.Contains(color))
			{
				return false;
			}
		}
		return emptyCarriersUsed <= emptyCarriersCount;
	}

	public int GetInFlightToConveyorCount(EBlockColorType color)
	{
		return (_unloadHandler != null) ? _unloadHandler.GetInFlightToConveyorCount(color) : 0;
	}

	public HashSet<EBlockColorType> GetActiveConveyorColors()
	{
		HashSet<EBlockColorType> colors = new HashSet<EBlockColorType>();
		if (_deliveryStates == null)
		{
			return colors;
		}
		for (int i = 0; i < _deliveryStates.Count; i++)
		{
			DeliveryCubeState state = _deliveryStates[i];
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
		bool isPrelose = (bool)MonoSingleton<CapacityManager>.Instance && MonoSingleton<CapacityManager>.Instance.IsPrelose;
		ApplyBlinkToFirstRenderer(isPrelose);
	}

	private void RemoveDeliveryState(Cube cube)
	{
		for (int i = _deliveryStates.Count - 1; i >= 0; i--)
		{
			if (_deliveryStates[i].Cube == cube)
			{
				_deliveryStates.RemoveAt(i);
			}
		}
	}

	private void CompleteSplinePickup(Cube cube)
	{
		RemoveActiveDeliveryCube(cube);
	}

	private void RemoveActiveDeliveryCube(Cube cube)
	{
		if (!(cube == null))
		{
			cachedMovers.Remove(cube);
			RemoveDeliveryState(cube);
			UnityEngine.Object.Destroy(cube.gameObject);
			MonoSingleton<CapacityManager>.Instance.RemoveCube();
		}
	}

	private Transform GetSpawnRoot()
	{
		return (spawnRoot != null) ? spawnRoot : base.transform;
	}

	private ConveyorCornerDetector GetCornerDetector()
	{
		return (conveyorCornerDetector != null) ? conveyorCornerDetector : GetComponent<ConveyorCornerDetector>();
	}

	public float GetProgressLevel()
	{
		return _winDetector.GetProgressLevel();
	}

	private void OnDrawGizmos()
	{
		if (_conveyorCubeSpeedController != null)
		{
			List<CarrierBase> carriers = MonoSingleton<CarrierSystem>.Instance?.CarrierSpawner?.SpawnedCarriers;
			ConveyorPathRuntime path = Path;
			if (path == null && conveyorManager == null)
			{
				conveyorManager = GetComponent<ConveyorManager>();
			}
			path = Path;
			_conveyorCubeSpeedController.DrawPickupRanges(path, carriers);
		}
	}
}
