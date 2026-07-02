using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dispatches cubes flying from the conveyor into a carrier.
/// </summary>
public class ConveyorPickupHandler
{
    private readonly Dictionary<CarrierBase, PickupState> _activePickupStates = new Dictionary<CarrierBase, PickupState>();
    private readonly ConveyorDeliveryCubeFactory _factory;
    private readonly Transform _spawnRoot;

    private readonly Action<Cube> _onSplinePickupComplete;
    private readonly Action<AnimCube> _onPushAnimCube;
    private readonly Action CheckTutorial;

    public ConveyorPickupHandler(
        ConveyorDeliveryCubeFactory factory,
        Transform spawnRoot,
        Action<Cube> onSplinePickupComplete,
        Action<AnimCube> onPushAnimCube,
        Action checkTutorial)
    {
        _factory = factory;
        _spawnRoot = spawnRoot;
        _onSplinePickupComplete = onSplinePickupComplete;
        _onPushAnimCube = onPushAnimCube;
        CheckTutorial = checkTutorial;
    }

    public void HandleReceiveCube(
        Cube cube,
        CarrierBase targetCarrier,
        Color color,
        AnimCube animCube,
        CarrierReceiveReservation reservation)
    {
        if (ConveyorDeliverySystem.Instance == null) return;
        ConveyorDeliverySystem.Instance.StartCoroutine(
            ReceiveCubeRoutine(cube, targetCarrier, color, animCube, reservation));
    }

    private IEnumerator ReceiveCubeRoutine(
        Cube cube,
        CarrierBase targetCarrier,
        Color color,
        AnimCube animCube,
        CarrierReceiveReservation reservation)
    {
        var completed = false;
        var startPos = cube.transform.position;
        _factory.SetupCube(
            animCube,
            startPos,
            reservation.BlockColorType,
            _spawnRoot);

        _onSplinePickupComplete?.Invoke(cube);

        animCube.FlyToTarget(
            reservation.TargetPosition,
            () =>
            {
                CompleteReceive(animCube, targetCarrier, reservation, color);
                completed = true;
            });

        while (!completed)
        {
            yield return null;
        }
    }

    private void CompleteReceive(
        AnimCube animCube,
        CarrierBase targetCarrier,
        CarrierReceiveReservation reservation,
        Color color)
    {
        _onPushAnimCube?.Invoke(animCube);
        targetCarrier.CompleteReceiveCube(reservation, color);
        ClearActivePickupColor(targetCarrier, reservation.BlockColorType);
        CheckTutorial?.Invoke();
    }

    public bool CanPickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        PickupState activeState;
        return !_activePickupStates.TryGetValue(targetCarrier, out activeState)
               || activeState.BlockColorType == blockColorType;
    }

    public void BeginPickup(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        var wasEmpty = _activePickupStates.Count == 0;
        PickupState state;
        if (_activePickupStates.TryGetValue(targetCarrier, out state))
        {
            state.InFlightCount++;
        }
        else
        {
            _activePickupStates[targetCarrier] = new PickupState(blockColorType);
        }

        if (wasEmpty)
        {
            ConveyorDeliverySystem.Instance?.RequestPlayLoopSound();
        }
    }

    private void ClearActivePickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        PickupState state;
        if (!_activePickupStates.TryGetValue(targetCarrier, out state)) return;
        if (state.BlockColorType != blockColorType) return;
        state.InFlightCount--;
        if (state.InFlightCount > 0) return;
        _activePickupStates.Remove(targetCarrier);

        if (_activePickupStates.Count == 0)
        {
            ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
        }
    }

    public bool IsReceivingCube(CarrierBase carrier)
    {
        return carrier != null && _activePickupStates.ContainsKey(carrier);
    }

    public void ClearStates()
    {
        _activePickupStates.Clear();
        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    public Dictionary<CarrierBase, PickupState> GetActivePickupStates()
    {
        return _activePickupStates;
    }
}
