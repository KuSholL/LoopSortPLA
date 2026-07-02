using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Splines;
using LitMotion;
using LitMotion.Extensions;
using Cysharp.Threading.Tasks;

public class CarrierSpawner : MonoBehaviour
{
    [SerializeField] private CarrierConfigSO carrierConfig;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnRoot;

    private float CarrierScaleDuration => LevelManager.Instance.LevelEntryAnimConfig.CarrierScaleDuration;
    private float CarrierScaleStagger => LevelManager.Instance.LevelEntryAnimConfig.CarrierScaleStagger;
    private Ease CarrierScaleEase => LevelManager.Instance.LevelEntryAnimConfig.CarrierScaleEase;

    private readonly List<CarrierBase> _spawnedCarriers = new();
    public List<CarrierBase> SpawnedCarriers => _spawnedCarriers;
    public CarrierConfigSO CarrierConfig => carrierConfig;
    public SplineContainer SplineContainer => splineContainer;

    private CancellationTokenSource _carrierAnimCts;

    private void OnDestroy()
    {
        _carrierAnimCts?.Cancel();
        _carrierAnimCts?.Dispose();
        _carrierAnimCts = null;
    }

    public void SpawnCarriers(LevelData levelData)
    {
        ClearCarriers();
        var carrierStacks = levelData?.CarrierLayout?.Carriers;
        if (carrierStacks != null)
            foreach (var carrier in carrierStacks) CreateCarrier(carrier);
        ConveyorDeliverySystem.Instance?.SetupCarrierPickup(_spawnedCarriers);

        foreach (var carrier in _spawnedCarriers)
        {
            if (carrier != null) carrier.transform.localScale = Vector3.zero;
        }
    }

    public bool TrySpawnCarrier(CarrierStackData carrierStack)
    {
        var carrier = CreateCarrier(carrierStack);
        if (carrier == null) return false;
        ConveyorDeliverySystem.Instance?.SetupCarrierPickup(_spawnedCarriers);

        carrier.transform.localScale = Vector3.zero;
        var handle = LMotion.Create(Vector3.zero, Vector3.one, CarrierScaleDuration)
            .WithEase(CarrierScaleEase)
            .BindToLocalScale(carrier.transform);
        carrier.SetScaleMotionHandle(handle);

        return true;
    }

    private void ClearCarriers()
    {
        // Cancel anim đang chạy trước khi trả carrier về pool
        _carrierAnimCts?.Cancel();
        _carrierAnimCts?.Dispose();
        _carrierAnimCts = null;

        foreach (var carrier in _spawnedCarriers)
        {
            if (carrier == null) continue;
            carrier.transform.localScale = Vector3.one; // reset scale trước khi pool
            PoolManagerNew.Instance.PushToPool(carrier);
        }

        _spawnedCarriers.Clear();
    }

    private CarrierBase CreateCarrier(CarrierStackData carrierStack)
    {
        if (carrierStack == null) return null;
        var isSpawner = false;
        if (carrierStack.Mechanics != null)
        {
            for (var i = 0; i < carrierStack.Mechanics.Count; i++)
            {
                if (carrierStack.Mechanics[i] != null && carrierStack.Mechanics[i].Type == ECarrierMechanic.Spawner)
                {
                    isSpawner = true;
                    break;
                }
            }
        }
        var prefab = isSpawner && carrierConfig.Spawner != null ? (CarrierBase)carrierConfig.Spawner : (CarrierBase)carrierConfig.Prefab;
        var carrier = PoolManagerNew.Instance.PopFromPool(prefab, spawnRoot);
        carrier.IncrementSessionId();
        ApplyCarrierTransform(carrier, carrierStack);
        carrier.SetSplineProgress(carrierStack.Progress);
        carrier.CreateBlocks(carrierStack, suppressProgressAnimation: true);
        _spawnedCarriers.Add(carrier);
        return carrier;
    }

    private void ApplyCarrierTransform(CarrierBase carrier, CarrierStackData carrierStack)
    {
        if (carrier == null || splineContainer == null) return;
        carrier.transform.position = GetCarrierWorldPosition(carrierStack);
        carrier.transform.rotation = GetCarrierWorldRotation(carrierStack);
    }

    private static Vector3 GetCarrierPosition(CarrierStackData carrierStack)
    {
        if (carrierStack == null) return Vector3.zero;
        return carrierStack.Position;
    }

    public Vector3 GetCarrierWorldPosition(CarrierStackData carrierStack)
    {
        return splineContainer.transform.TransformPoint(GetCarrierPosition(carrierStack));
    }

    private Quaternion GetCarrierWorldRotation(CarrierStackData carrierStack)
    {
        return splineContainer.transform.rotation * Quaternion.Euler(0f, carrierStack.RotationY, 0f);
    }

    public async UniTask PlayCarriersScaleAnimation()
    {
        if (_spawnedCarriers == null || _spawnedCarriers.Count == 0) return;

        // Cancel anim cũ nếu đang chạy
        _carrierAnimCts?.Cancel();
        _carrierAnimCts?.Dispose();
        _carrierAnimCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());
        var token = _carrierAnimCts.Token;

        foreach (var carrier in _spawnedCarriers)
        {
            if (carrier != null)
            {
                if (carrier.IsLockedByContainer())
                {
                    carrier.transform.localScale = Vector3.one;
                }
                else
                {
                    carrier.transform.localScale = Vector3.zero;
                }
            }
        }

        try
        {
            var tasks = new List<UniTask>();
            for (var i = 0; i < _spawnedCarriers.Count; i++)
            {
                var carrier = _spawnedCarriers[i];
                if (carrier == null || carrier.IsLockedByContainer()) continue;

                if (i > 0 && CarrierScaleStagger > 0f)
                {
                    await UniTask.Delay(
                        System.TimeSpan.FromSeconds(CarrierScaleStagger),
                        delayType: Cysharp.Threading.Tasks.DelayType.DeltaTime,
                        cancellationToken: token);
                }

                if (carrier != null)
                {
                    var handle = LMotion.Create(Vector3.zero, Vector3.one, CarrierScaleDuration)
                        .WithEase(CarrierScaleEase)
                        .BindToLocalScale(carrier.transform);
                    carrier.SetScaleMotionHandle(handle);
                    tasks.Add(handle.ToUniTask(token));
                }
            }

            await UniTask.WhenAll(tasks);
        }
        catch (System.OperationCanceledException)
        {
            // Bị cancel do next level — reset về trạng thái sạch
            foreach (var carrier in _spawnedCarriers)
            {
                if (carrier != null)
                {
                    if (carrier.IsLockedByContainer())
                    {
                        carrier.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        carrier.transform.localScale = Vector3.zero;
                    }
                }
            }
        }
    }
}
