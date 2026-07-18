using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarrierSpawner : MonoBehaviour
{
    [SerializeField] private CarrierConfigSO carrierConfig;
    [SerializeField] private Transform spawnRoot;

    private readonly List<CarrierBase> _spawnedCarriers = new List<CarrierBase>();
    public List<CarrierBase> SpawnedCarriers => _spawnedCarriers;
    public CarrierConfigSO CarrierConfig => carrierConfig;
    private ConveyorPathRuntime _runtimePath;
    public ConveyorPathRuntime Path => _runtimePath != null ? _runtimePath : ConveyorDeliverySystem.Instance != null ? ConveyorDeliverySystem.Instance.Path : null;

    public void SetPath(ConveyorPathRuntime path)
    {
        _runtimePath = path;
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
            if (carrier == null) continue;
            carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
            carrier.transform.localScale = Vector3.one;
        }
    }

    public bool TrySpawnCarrier(CarrierStackData carrierStack)
    {
        var carrier = CreateCarrier(carrierStack);
        if (carrier == null) return false;
        ConveyorDeliverySystem.Instance?.SetupCarrierPickup(_spawnedCarriers);

        carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
        carrier.transform.localScale = Vector3.one;
        return true;
    }

    private void ClearCarriers()
    {
        foreach (var carrier in _spawnedCarriers)
        {
            if (carrier == null) continue;
            carrier.SetClickCollidersEnabled(true);
            carrier.transform.localScale = Vector3.one; // reset scale trước khi pool
            Destroy(carrier.gameObject);
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
                var mechanic = carrierStack.Mechanics[i];
                if (mechanic != null && mechanic.Type == ECarrierMechanic.Spawner)
                {
                    isSpawner = true;
                    break;
                }
            }
        }
        var carrier = CreateCarrierInstance(isSpawner);
        if (carrier == null) return null;
        carrier.IncrementSessionId();
        ApplyCarrierTransform(carrier, carrierStack);
        carrier.SetSplineProgress(carrierStack.Progress);
        carrier.CreateBlocks(carrierStack, suppressProgressAnimation: true);
        LunaMaterialUtility.NormalizeRenderers(carrier.gameObject);
        _spawnedCarriers.Add(carrier);
        return carrier;
    }

    private CarrierBase CreateCarrierInstance(bool isSpawner)
    {
        var prefab = isSpawner && carrierConfig.Spawner != null
            ? carrierConfig.Spawner.gameObject
            : carrierConfig.Prefab != null
                ? carrierConfig.Prefab.gameObject
                : null;
        if (prefab == null) return null;

        var instance = Instantiate(prefab, spawnRoot);
        if (isSpawner)
        {
            var spawner = instance.GetComponent<Spawner>();
            if (spawner != null) return spawner;
        }

        var carrier = instance.GetComponent<Carrier>();
        if (carrier != null) return carrier;
        return instance.GetComponent<CarrierBase>();
    }

    private void ApplyCarrierTransform(CarrierBase carrier, CarrierStackData carrierStack)
    {
        if (carrier == null || Path == null || !Path.IsValid) return;
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
        var path = Path;
        return path != null && path.IsValid
            ? path.TransformPoint(GetCarrierPosition(carrierStack))
            : GetCarrierPosition(carrierStack);
    }

    private Quaternion GetCarrierWorldRotation(CarrierStackData carrierStack)
    {
        var path = Path;
        var rootRotation = path != null && path.Root != null ? path.Root.rotation : Quaternion.identity;
        var rotationY = NormalizeRotationY(carrierStack.RotationY);
        return rootRotation * Quaternion.Euler(0f, rotationY, 0f);
    }

    private static float NormalizeRotationY(float rotationY)
    {
        var normalized = rotationY % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }

    public IEnumerator PlayCarriersScaleAnimation()
    {
        EnsureCarriersVisibleAndClickable();
        yield break;
    }

    public void EnsureCarriersVisibleAndClickable()
    {
        for (var i = 0; i < _spawnedCarriers.Count; i++)
        {
            var carrier = _spawnedCarriers[i];
            if (carrier == null) continue;

            carrier.CancelScaleAnimation();
            carrier.transform.localScale = Vector3.one;
            carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
        }
    }
}
