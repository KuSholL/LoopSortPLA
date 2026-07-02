using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dispatches cubes flying from a carrier back onto the conveyor.
/// </summary>
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

    public ConveyorUnloadHandler(
        ConveyorDeliveryCubeFactory factory,
        ConveyorSpawnPointCalculator calculator,
        List<AnimCube> activeAnimCubes,
        Transform spawnRoot,
        float spawnInterval,
        Action<AnimCube, CarrierBase, CarrierCubePayload, float, Vector3, bool, int> onComplete)
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
        if (unloadRequest == null || unloadRequest.SourceCarrier == null) return;
        var host = ConveyorDeliverySystem.Instance;
        if (host == null) return;

        var carrier = unloadRequest.SourceCarrier;
        CancelDeliveryRoutine(carrier);
        _activeSpawningCarriers.Add(carrier);

        ConveyorDeliverySystem.Instance.RequestPlayLoopSound();
        _deliveryRoutines[carrier] = host.StartCoroutine(SpawnCarrierUnloadRoutine(unloadRequest));
    }

    private IEnumerator SpawnCarrierUnloadRoutine(CarrierUnloadRequest unloadRequest)
    {
        var carrier = unloadRequest.SourceCarrier;
        var success = false;
        var pendingFlights = 0;

        try
        {
            for (var i = 0; i < unloadRequest.CubeCount; i++)
            {
                if (i > 0)
                {
                    yield return DelayWithCustomTimeScale(_spawnInterval);
                }

                pendingFlights++;
                CreateDeliveryCube(unloadRequest, i, () => pendingFlights--);

                if (i == unloadRequest.CubeCount - 1)
                {
                    if (_activeSpawningCarriers.Remove(carrier))
                    {
                        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
                    }
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

            if (ConveyorDeliverySystem.Instance != null)
            {
                ConveyorDeliverySystem.Instance.EvaluateLoseCondition();
            }

            success = true;
        }
        finally
        {
            if (!success)
            {
                for (var i = 0; i < unloadRequest.CubeCount; i++)
                    CleanupPendingHiddenCube(unloadRequest.CubePayloads[i], unloadRequest.CarrierSessionId);
            }

            if (_activeSpawningCarriers.Remove(carrier))
            {
                ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
            }

            ClearDeliveryRoutine(carrier);
        }
    }

    private IEnumerator DelayWithCustomTimeScale(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            float timeScale = CustomTimeScaleGroup.Instance != null
                ? CustomTimeScaleGroup.Instance.CurrentTimeScale
                : 1f;
            elapsed += Time.unscaledDeltaTime * timeScale;
        }
    }

    private void CreateDeliveryCube(CarrierUnloadRequest unloadRequest, int index, Action onFlightComplete)
    {
        var carrier = unloadRequest.SourceCarrier;
        var payload = unloadRequest.CubePayloads[index];

        if (carrier != null && carrier.SessionId != unloadRequest.CarrierSessionId)
        {
            onFlightComplete?.Invoke();
            return;
        }

        payload.SourceBlock?.TryConsumeUnloadCube();

        int val;
        _inFlightToConveyor.TryGetValue(payload.BlockColorType, out val);
        _inFlightToConveyor[payload.BlockColorType] = val + 1;

        var animCube = _factory.CreateAnimCubeInstance(_spawnRoot, _activeAnimCubes);
        _factory.SetupCube(
            animCube,
            payload.StartWorldPosition,
            payload.BlockColorType,
            _spawnRoot);

        var progress = carrier != null ? carrier.SplineProgress : 0f;
        var deliveryTarget = _calculator.GetDeliverySpawnPosition(progress, index);

        animCube.FlyToTarget(
            deliveryTarget,
            () =>
            {
                NotifyCubeArrived(
                    animCube,
                    carrier,
                    payload,
                    progress,
                    deliveryTarget,
                    index,
                    unloadRequest.UndoBatchId);
                onFlightComplete?.Invoke();
            });
    }

    private void NotifyCubeArrived(
        AnimCube animCube,
        CarrierBase carrier,
        CarrierCubePayload payload,
        float progress,
        Vector3 deliveryTarget,
        int index,
        int undoBatchId)
    {
        int val;
        _inFlightToConveyor.TryGetValue(payload.BlockColorType, out val);
        _inFlightToConveyor[payload.BlockColorType] = Mathf.Max(0, val - 1);

        _onComplete?.Invoke(animCube, carrier, payload, progress, deliveryTarget, index == 0, undoBatchId);
    }

    private static void CleanupPendingHiddenCube(CarrierCubePayload payload, int expectedSessionId)
    {
        if (payload.SourceBlock != null && payload.SourceBlock.OwnerCarrier != null)
        {
            if (payload.SourceBlock.OwnerCarrier.SessionId != expectedSessionId)
            {
                return;
            }
        }
        payload.SourceBlock?.TryConsumeUnloadCube();
    }

    private void CancelDeliveryRoutine(CarrierBase carrier)
    {
        if (carrier == null) return;
        Coroutine routine;
        if (!_deliveryRoutines.TryGetValue(carrier, out routine)) return;
        if (routine != null && ConveyorDeliverySystem.Instance != null)
            ConveyorDeliverySystem.Instance.StopCoroutine(routine);
        _deliveryRoutines.Remove(carrier);
        _activeSpawningCarriers.Remove(carrier);
        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    public void CancelAll()
    {
        if (ConveyorDeliverySystem.Instance != null)
        {
            foreach (var pair in _deliveryRoutines)
            {
                if (pair.Value != null)
                    ConveyorDeliverySystem.Instance.StopCoroutine(pair.Value);
            }
        }

        _deliveryRoutines.Clear();
        _inFlightToConveyor.Clear();
        _activeSpawningCarriers.Clear();
        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    private void ClearDeliveryRoutine(CarrierBase carrier)
    {
        if (carrier == null) return;
        _deliveryRoutines.Remove(carrier);
        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    public bool IsUnloadActive
    {
        get { return _activeSpawningCarriers.Count > 0; }
    }

    public int GetInFlightToConveyorCount(EBlockColorType color)
    {
        int count;
        return _inFlightToConveyor.TryGetValue(color, out count) ? count : 0;
    }
}
