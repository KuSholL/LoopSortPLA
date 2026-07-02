using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VibrationUtility;

/// <summary>
/// Chuyên trách việc điều phối cube bay từ Conveyor vào Carrier.
/// </summary>
public class ConveyorPickupHandler
{
    private readonly Dictionary<CarrierBase, PickupState> _activePickupStates = new();
    private readonly ConveyorDeliveryCubeFactory _factory;
    private readonly Transform _spawnRoot;
    
    private readonly Action<Cube> _onSplinePickupComplete;
    private readonly Action<AnimCube> _onPushAnimCube;
    private readonly Action CheckTutorial;

    public ConveyorPickupHandler(
        ConveyorDeliveryCubeFactory factory,
        Transform spawnRoot,
        Action<Cube> onSplinePickupComplete,
        Action<AnimCube> onPushAnimCube, Action checkTutorial)
    {
        _factory = factory;
        _spawnRoot = spawnRoot;
        _onSplinePickupComplete = onSplinePickupComplete;
        _onPushAnimCube = onPushAnimCube;
        CheckTutorial = checkTutorial;
    }


    /// <summary>
    /// Bắt đầu quá trình bay vào carrier.
    /// </summary>
    public void HandleReceiveCube(
        Cube cube,
        CarrierBase targetCarrier,
        Color color,
        AnimCube animCube,
        CarrierReceiveReservation reservation)
    {
        ReceiveCubeAsync(cube, targetCarrier, color, animCube, reservation).Forget();
    }

    private async UniTaskVoid ReceiveCubeAsync(
        Cube cube,
        CarrierBase targetCarrier,
        Color color,
        AnimCube animCube,
        CarrierReceiveReservation reservation)
    {
        if (VibrationManager.Instance != null)
        {
            VibrationManager.Instance.StartContinuousVibration(
                VibrationManager.Instance.ReceiveVibrationDuration,
                VibrationManager.Instance.ReceiveVibrationRate);
        }

        try
        {
            var startPos = cube.transform.position;
            _factory.SetupCube(
                animCube,
                startPos,
                reservation.BlockColorType,
                _spawnRoot);
            
            // Xóa cube thật trên spline
            _onSplinePickupComplete?.Invoke(cube);
            
            await animCube.FlyToTarget(
                reservation.TargetPosition,
                () => CompleteReceive(animCube, targetCarrier, reservation, color));
        }
        finally
        {
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.StopContinuousVibration();
            }
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
        return !_activePickupStates.TryGetValue(targetCarrier, out var activeState)
               || activeState.BlockColorType == blockColorType;
    }

    public void BeginPickup(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        var wasEmpty = _activePickupStates.Count == 0;
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
            ConveyorDeliverySystem.Instance?.RequestPlayLoopSound();
        }
    }

    private void ClearActivePickupColor(CarrierBase targetCarrier, EBlockColorType blockColorType)
    {
        if (!_activePickupStates.TryGetValue(targetCarrier, out var state)) return;
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

    public Dictionary<CarrierBase, PickupState> GetActivePickupStates() => _activePickupStates;
}
