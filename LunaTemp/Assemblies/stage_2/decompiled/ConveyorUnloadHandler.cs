using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorUnloadHandler
{
	private readonly Dictionary<CarrierBase, Coroutine> _deliveryRoutines = new Dictionary<CarrierBase, Coroutine>();

	private readonly Dictionary<EBlockColorType, int> _inFlightToConveyor = new Dictionary<EBlockColorType, int>();

	private readonly HashSet<CarrierBase> _activeSpawningCarriers = new HashSet<CarrierBase>();

	private readonly List<AnimCube> _activeAnimCubes;

	private readonly ConveyorDeliveryCubeFactory _factory;

	private readonly ConveyorSpawnPointCalculator _calculator;

	private readonly Transform _spawnRoot;

	private readonly float _spawnInterval;

	private readonly Action<AnimCube, CarrierBase, CarrierCubePayload, float, Vector3, bool, int> _onComplete;

	public bool IsUnloadActive => _activeSpawningCarriers.Count > 0;

	public ConveyorUnloadHandler(ConveyorDeliveryCubeFactory factory, ConveyorSpawnPointCalculator calculator, List<AnimCube> activeAnimCubes, Transform spawnRoot, float spawnInterval, Action<AnimCube, CarrierBase, CarrierCubePayload, float, Vector3, bool, int> onComplete)
	{
		_factory = factory;
		_calculator = calculator;
		_activeAnimCubes = activeAnimCubes;
		_spawnRoot = spawnRoot;
		_spawnInterval = spawnInterval;
		_onComplete = onComplete;
	}

	public void HandleUnload(CarrierUnloadRequest unloadRequest)
	{
		if (unloadRequest != null && !(unloadRequest.SourceCarrier == null))
		{
			ConveyorDeliverySystem host = MonoSingleton<ConveyorDeliverySystem>.Instance;
			if (!(host == null))
			{
				CarrierBase carrier = unloadRequest.SourceCarrier;
				CancelDeliveryRoutine(carrier);
				_activeSpawningCarriers.Add(carrier);
				MonoSingleton<ConveyorDeliverySystem>.Instance.RequestPlayLoopSound();
				_deliveryRoutines[carrier] = host.StartCoroutine(SpawnCarrierUnloadRoutine(unloadRequest));
			}
		}
	}

	private IEnumerator SpawnCarrierUnloadRoutine(CarrierUnloadRequest unloadRequest)
	{
		CarrierBase carrier = unloadRequest.SourceCarrier;
		bool success = false;
		int pendingFlights = 0;
		try
		{
			for (int j = 0; j < unloadRequest.CubeCount; j++)
			{
				if (j > 0)
				{
					yield return DelayWithCustomTimeScale(_spawnInterval);
				}
				pendingFlights++;
				CreateDeliveryCube(unloadRequest, j, delegate
				{
					pendingFlights--;
				});
				if (j == unloadRequest.CubeCount - 1 && _activeSpawningCarriers.Remove(carrier))
				{
					MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
				}
			}
			while (pendingFlights > 0)
			{
				yield return null;
			}
			if (carrier != null && carrier.SessionId == unloadRequest.CarrierSessionId)
			{
				carrier.FinishUnloadCarrier();
				GameEventBus.OnCarrierUnloadDone?.Invoke();
			}
			if (MonoSingleton<ConveyorDeliverySystem>.Instance != null)
			{
				MonoSingleton<ConveyorDeliverySystem>.Instance.EvaluateLoseCondition();
			}
			success = true;
		}
		finally
		{
			if (!success)
			{
				for (int i = 0; i < unloadRequest.CubeCount; i++)
				{
					CleanupPendingHiddenCube(unloadRequest.CubePayloads[i], unloadRequest.CarrierSessionId);
				}
			}
			if (_activeSpawningCarriers.Remove(carrier))
			{
				MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
			}
			ClearDeliveryRoutine(carrier);
		}
	}

	private IEnumerator DelayWithCustomTimeScale(float duration)
	{
		float timeScale;
		for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime * timeScale)
		{
			yield return null;
			timeScale = ((MonoSingleton<CustomTimeScaleGroup>.Instance != null) ? MonoSingleton<CustomTimeScaleGroup>.Instance.CurrentTimeScale : 1f);
		}
	}

	private void CreateDeliveryCube(CarrierUnloadRequest unloadRequest, int index, Action onFlightComplete)
	{
		CarrierBase carrier = unloadRequest.SourceCarrier;
		CarrierCubePayload payload = unloadRequest.CubePayloads[index];
		if (carrier != null && carrier.SessionId != unloadRequest.CarrierSessionId)
		{
			onFlightComplete?.Invoke();
			return;
		}
		payload.SourceBlock?.TryConsumeUnloadCube();
		_inFlightToConveyor.TryGetValue(payload.BlockColorType, out var val);
		_inFlightToConveyor[payload.BlockColorType] = val + 1;
		AnimCube animCube = _factory.CreateAnimCubeInstance(_spawnRoot, _activeAnimCubes);
		_factory.SetupCube(animCube, payload.StartWorldPosition, payload.BlockColorType, _spawnRoot);
		float progress = ((carrier != null) ? carrier.SplineProgress : 0f);
		Vector3 deliveryTarget = _calculator.GetDeliverySpawnPosition(progress, index);
		animCube.FlyToTarget(deliveryTarget, delegate
		{
			NotifyCubeArrived(animCube, carrier, payload, progress, deliveryTarget, index, unloadRequest.UndoBatchId);
			onFlightComplete?.Invoke();
		});
	}

	private void NotifyCubeArrived(AnimCube animCube, CarrierBase carrier, CarrierCubePayload payload, float progress, Vector3 deliveryTarget, int index, int undoBatchId)
	{
		_inFlightToConveyor.TryGetValue(payload.BlockColorType, out var val);
		_inFlightToConveyor[payload.BlockColorType] = Mathf.Max(0, val - 1);
		_onComplete?.Invoke(animCube, carrier, payload, progress, deliveryTarget, index == 0, undoBatchId);
	}

	private static void CleanupPendingHiddenCube(CarrierCubePayload payload, int expectedSessionId)
	{
		if (!(payload.SourceBlock != null) || !(payload.SourceBlock.OwnerCarrier != null) || payload.SourceBlock.OwnerCarrier.SessionId == expectedSessionId)
		{
			payload.SourceBlock?.TryConsumeUnloadCube();
		}
	}

	private void CancelDeliveryRoutine(CarrierBase carrier)
	{
		if (!(carrier == null) && _deliveryRoutines.TryGetValue(carrier, out var routine))
		{
			if (routine != null && MonoSingleton<ConveyorDeliverySystem>.Instance != null)
			{
				MonoSingleton<ConveyorDeliverySystem>.Instance.StopCoroutine(routine);
			}
			_deliveryRoutines.Remove(carrier);
			_activeSpawningCarriers.Remove(carrier);
			MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
		}
	}

	public void CancelAll()
	{
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null)
		{
			foreach (KeyValuePair<CarrierBase, Coroutine> pair in _deliveryRoutines)
			{
				if (pair.Value != null)
				{
					MonoSingleton<ConveyorDeliverySystem>.Instance.StopCoroutine(pair.Value);
				}
			}
		}
		_deliveryRoutines.Clear();
		_inFlightToConveyor.Clear();
		_activeSpawningCarriers.Clear();
		MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
	}

	private void ClearDeliveryRoutine(CarrierBase carrier)
	{
		if (!(carrier == null))
		{
			_deliveryRoutines.Remove(carrier);
			MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
		}
	}

	public int GetInFlightToConveyorCount(EBlockColorType color)
	{
		int count;
		return _inFlightToConveyor.TryGetValue(color, out count) ? count : 0;
	}
}
