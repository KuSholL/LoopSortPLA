using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorPickupHandler
{
	private readonly Dictionary<CarrierBase, PickupState> _activePickupStates = new Dictionary<CarrierBase, PickupState>();

	private readonly ConveyorDeliveryCubeFactory _factory;

	private readonly Transform _spawnRoot;

	private readonly Action<Cube> _onSplinePickupComplete;

	private readonly Action<AnimCube> _onPushAnimCube;

	private readonly Action CheckTutorial;

	public ConveyorPickupHandler(ConveyorDeliveryCubeFactory factory, Transform spawnRoot, Action<Cube> onSplinePickupComplete, Action<AnimCube> onPushAnimCube, Action checkTutorial)
	{
		_factory = factory;
		_spawnRoot = spawnRoot;
		_onSplinePickupComplete = onSplinePickupComplete;
		_onPushAnimCube = onPushAnimCube;
		CheckTutorial = checkTutorial;
	}

	public void HandleReceiveCube(Cube cube, CarrierBase targetCarrier, Color color, AnimCube animCube, CarrierReceiveReservation reservation)
	{
		if (!(MonoSingleton<ConveyorDeliverySystem>.Instance == null))
		{
			MonoSingleton<ConveyorDeliverySystem>.Instance.StartCoroutine(ReceiveCubeRoutine(cube, targetCarrier, color, animCube, reservation));
		}
	}

	private IEnumerator ReceiveCubeRoutine(Cube cube, CarrierBase targetCarrier, Color color, AnimCube animCube, CarrierReceiveReservation reservation)
	{
		bool completed = false;
		Vector3 startPos = cube.transform.position;
		_factory.SetupCube(animCube, startPos, reservation.BlockColorType, _spawnRoot);
		_onSplinePickupComplete?.Invoke(cube);
		animCube.FlyToTarget(reservation.TargetPosition, delegate
		{
			CompleteReceive(animCube, targetCarrier, reservation, color);
			completed = true;
		});
		while (!completed)
		{
			yield return null;
		}
	}

	private void CompleteReceive(AnimCube animCube, CarrierBase targetCarrier, CarrierReceiveReservation reservation, Color color)
	{
		_onPushAnimCube?.Invoke(animCube);
		targetCarrier.CompleteReceiveCube(reservation, color);
		ClearActivePickupColor(targetCarrier, reservation.BlockColorType);
		CheckTutorial?.Invoke();
	}

	public bool CanPickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
	{
		PickupState activeState;
		return !_activePickupStates.TryGetValue(targetCarrier, out activeState) || activeState.BlockColorType == blockColorType;
	}

	public void BeginPickup(CarrierBase targetCarrier, EBlockColorType blockColorType)
	{
		bool wasEmpty = _activePickupStates.Count == 0;
		if (_activePickupStates.TryGetValue(targetCarrier, out var state))
		{
			state.InFlightCount++;
		}
		else
		{
			_activePickupStates[targetCarrier] = new PickupState(blockColorType);
		}
		if (wasEmpty)
		{
			MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestPlayLoopSound();
		}
	}

	private void ClearActivePickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
	{
		if (!_activePickupStates.TryGetValue(targetCarrier, out var state) || state.BlockColorType != blockColorType)
		{
			return;
		}
		state.InFlightCount--;
		if (state.InFlightCount <= 0)
		{
			_activePickupStates.Remove(targetCarrier);
			if (_activePickupStates.Count == 0)
			{
				MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
			}
		}
	}

	public bool IsReceivingCube(CarrierBase carrier)
	{
		return carrier != null && _activePickupStates.ContainsKey(carrier);
	}

	public void ClearStates()
	{
		_activePickupStates.Clear();
		MonoSingleton<ConveyorDeliverySystem>.Instance?.RequestStopLoopSound();
	}

	public Dictionary<CarrierBase, PickupState> GetActivePickupStates()
	{
		return _activePickupStates;
	}
}
