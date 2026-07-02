using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VibrationUtility;

/// <summary>
/// ChuyÃªn trÃ¡ch viá»‡c Ä‘iá»u phá»‘i cube bay tá»« Carrier ra Conveyor.
/// </summary>
public class ConveyorUnloadHandler
{
    private readonly Dictionary<CarrierBase, CancellationTokenSource> _deliveryTokens = new();
    private readonly Dictionary<EBlockColorType, int> _inFlightToConveyor = new();
    private readonly HashSet<CarrierBase> _activeSpawningCarriers = new();
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

    /// <summary>
    /// Báº¯t Ä‘áº§u quÃ¡ trÃ¬nh unload cho má»™t carrier.
    /// </summary>
    public void HandleUnload(CarrierUnloadRequest unloadRequest)
    {
        var carrier = unloadRequest.SourceCarrier;
        var token = CreateDeliveryToken(carrier);

        _activeSpawningCarriers.Add(carrier);

        ConveyorDeliverySystem.Instance?.RequestPlayLoopSound();

        SpawnCarrierUnload(unloadRequest, token).Forget();
    }

    private async UniTask SpawnCarrierUnload(CarrierUnloadRequest unloadRequest, CancellationToken token)
    {
        var carrier = unloadRequest.SourceCarrier;
        var success = false;
        try
        {
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.StartContinuousVibration(
                    VibrationManager.Instance.UnloadVibrationDuration,
                    VibrationManager.Instance.UnloadVibrationRate);
            }

            var tasks = new List<UniTask>();
            for (var i = 0; i < unloadRequest.CubeCount; i++)
            {
                if (i > 0)
                {
                    await DelayWithCustomTimeScale(_spawnInterval, token);
                }
                tasks.Add(CreateDeliveryCube(unloadRequest, i));

                if (i == unloadRequest.CubeCount - 1)
                {
                    if (_activeSpawningCarriers.Remove(carrier))
                    {
                        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
                    }
                }
            }

            await UniTask.WhenAll(tasks);
            carrier.FinishUnloadCarrier();
            GameEventBus.OnCarrierUnloadDone?.Invoke();
            BoosterUndoSystem.Instance.NotifyUnloadAnimationsCompleted(unloadRequest.UndoBatchId);
            
            if (ConveyorDeliverySystem.Instance != null)
            {
                ConveyorDeliverySystem.Instance.EvaluateLoseCondition();
            }
            success = true;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.StopContinuousVibration();
            }

            if (!success)
            {
                // Pool nốt các hidden cube chưa kịp spawn nếu bị cancel hoặc kết thúc lỗi
                for (var i = 0; i < unloadRequest.CubeCount; i++)
                    CleanupPendingHiddenCube(unloadRequest.CubePayloads[i], unloadRequest.CarrierSessionId);
            }

            if (_activeSpawningCarriers.Remove(carrier))
            {
                ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
            }

            ClearDeliveryToken(carrier);
        }
    }

    private async UniTask DelayWithCustomTimeScale(float duration, CancellationToken token)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            float timeScale = CustomTimeScaleGroup.Instance != null 
                ? CustomTimeScaleGroup.Instance.CurrentTimeScale 
                : 1f;
            elapsed += Time.unscaledDeltaTime * timeScale;
        }
    }

    private async UniTask CreateDeliveryCube(CarrierUnloadRequest unloadRequest, int index)
    {
        var carrier = unloadRequest.SourceCarrier;
        var payload = unloadRequest.CubePayloads[index];

        if (carrier != null && carrier.SessionId != unloadRequest.CarrierSessionId)
        {
            return;
        }

        payload.SourceBlock?.TryConsumeUnloadCube();

        _inFlightToConveyor.TryGetValue(payload.BlockColorType, out int val);
        _inFlightToConveyor[payload.BlockColorType] = val + 1;

        var animCube = _factory.CreateAnimCubeInstance(_spawnRoot, _activeAnimCubes);
            _factory.SetupCube(
                animCube,
                payload.StartWorldPosition,
                payload.BlockColorType,
                _spawnRoot);

        var progress = carrier.SplineProgress;
        var deliveryTarget = _calculator.GetDeliverySpawnPosition(progress, index);

        await animCube.FlyToTarget(
            deliveryTarget,
            () => NotifyCubeArrived(
                animCube,
                carrier,
                payload,
                progress,
                deliveryTarget,
                index,
                unloadRequest.UndoBatchId));
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
        _inFlightToConveyor.TryGetValue(payload.BlockColorType, out int val);
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

    private CancellationToken CreateDeliveryToken(CarrierBase carrier)
    {
        CancelDeliveryToken(carrier);
        var tokenSource = new CancellationTokenSource();
        _deliveryTokens[carrier] = tokenSource;
        return tokenSource.Token;
    }

    private void CancelDeliveryToken(CarrierBase carrier)
    {
        if (carrier == null) return;
        if (!_deliveryTokens.TryGetValue(carrier, out var tokenSource)) return;
        tokenSource.Cancel();
        tokenSource.Dispose();
        _deliveryTokens.Remove(carrier);

        _activeSpawningCarriers.Remove(carrier);

        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    public void CancelAll()
    {
        foreach (var pair in _deliveryTokens)
        {
            pair.Value.Cancel();
            pair.Value.Dispose();
        }
        _deliveryTokens.Clear();
        _inFlightToConveyor.Clear();
        _activeSpawningCarriers.Clear();

        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    private void ClearDeliveryToken(CarrierBase carrier)
    {
        if (carrier == null) return;
        if (!_deliveryTokens.TryGetValue(carrier, out var tokenSource)) return;
        tokenSource.Cancel();
        tokenSource.Dispose();
        _deliveryTokens.Remove(carrier);

        ConveyorDeliverySystem.Instance?.RequestStopLoopSound();
    }

    public bool IsUnloadActive => _activeSpawningCarriers.Count > 0;

    public int GetInFlightToConveyorCount(EBlockColorType color)
    {
        return _inFlightToConveyor.TryGetValue(color, out var count) ? count : 0;
    }
}
