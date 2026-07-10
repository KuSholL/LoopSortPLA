using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CarrierSpawner : MonoBehaviour
{
    [SerializeField] private CarrierConfigSO carrierConfig;
    [SerializeField] private Transform spawnRoot;

    private LevelEntryAnimConfigSO EntryConfig =>
        LevelManager.Instance != null ? LevelManager.Instance.LevelEntryAnimConfig : null;
    private float CarrierScaleDuration => EntryConfig != null ? EntryConfig.CarrierScaleDuration : 0.3f;
    private float CarrierScaleStagger => EntryConfig != null ? EntryConfig.CarrierScaleStagger : 0.1f;
    private DG.Tweening.Ease CarrierScaleEase => EntryConfig != null ? EntryConfig.CarrierScaleEase : DG.Tweening.Ease.OutBack;

    private readonly List<CarrierBase> _spawnedCarriers = new List<CarrierBase>();
    public List<CarrierBase> SpawnedCarriers => _spawnedCarriers;
    public CarrierConfigSO CarrierConfig => carrierConfig;
    private ConveyorPathRuntime _runtimePath;
    public ConveyorPathRuntime Path => _runtimePath != null ? _runtimePath : ConveyorDeliverySystem.Instance != null ? ConveyorDeliverySystem.Instance.Path : null;

    private Sequence _carrierSequence;

    public void SetPath(ConveyorPathRuntime path)
    {
        _runtimePath = path;
    }

    private void OnDestroy()
    {
        if (_carrierSequence != null) _carrierSequence.Kill();
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
#if UNITY_LUNA
            carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
            carrier.transform.localScale = Vector3.one;
#else
            carrier.SetClickCollidersEnabled(false);
            carrier.transform.localScale = Vector3.zero;
#endif
        }
    }

    public bool TrySpawnCarrier(CarrierStackData carrierStack)
    {
        var carrier = CreateCarrier(carrierStack);
        if (carrier == null) return false;
        ConveyorDeliverySystem.Instance?.SetupCarrierPickup(_spawnedCarriers);

#if UNITY_LUNA
        carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
        carrier.transform.localScale = Vector3.one;
        return true;
#else
        carrier.SetClickCollidersEnabled(false);
        carrier.transform.localScale = Vector3.zero;
        var spawnedCarrier = carrier;
        var scaleTween = carrier.transform.DOScale(Vector3.one, CarrierScaleDuration)
            .SetEase(CarrierScaleEase)
            .SetUpdate(true)
            .OnComplete(() => spawnedCarrier.SetClickCollidersEnabled(true));
        carrier.SetScaleMotionHandle(scaleTween);

        return true;
#endif
    }

    private void ClearCarriers()
    {
        // Cancel anim đang chạy trước khi trả carrier về pool
        if (_carrierSequence != null)
        {
            _carrierSequence.Kill();
            _carrierSequence = null;
        }

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
        return rootRotation * Quaternion.Euler(0f, carrierStack.RotationY, 0f);
    }

    public IEnumerator PlayCarriersScaleAnimation()
    {
        if (_spawnedCarriers.Count == 0) yield break;
#if UNITY_LUNA
        EnsureCarriersVisibleAndClickable();
        yield break;
#endif
        if (_carrierSequence != null)
        {
            _carrierSequence.Kill();
            _carrierSequence = null;
        }

        for (var i = 0; i < _spawnedCarriers.Count; i++)
        {
            var carrier = _spawnedCarriers[i];
            if (carrier != null)
            {
                var isLocked = carrier.IsLockedByContainer();
                carrier.SetClickCollidersEnabled(isLocked);
                carrier.transform.localScale = isLocked ? Vector3.one : Vector3.zero;
            }
        }

        _carrierSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
        for (var i = 0; i < _spawnedCarriers.Count; i++)
        {
            var carrier = _spawnedCarriers[i];
            if (carrier == null || carrier.IsLockedByContainer()) continue;
            var scaleTween = carrier.transform.DOScale(Vector3.one, CarrierScaleDuration)
                .SetEase(CarrierScaleEase)
                .SetUpdate(true)
                .OnComplete(() => carrier.SetClickCollidersEnabled(true));
            carrier.SetScaleMotionHandle(scaleTween);
            _carrierSequence.Insert(i * CarrierScaleStagger, scaleTween);
        }

        var elapsed = 0f;
        var timeout = Mathf.Max(0.05f, CarrierScaleDuration + CarrierScaleStagger * Mathf.Max(0, _spawnedCarriers.Count - 1) + 0.25f);
        while (_carrierSequence != null
               && _carrierSequence.IsActive()
               && !_carrierSequence.IsComplete()
               && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_carrierSequence != null && _carrierSequence.IsActive() && !_carrierSequence.IsComplete())
        {
            _carrierSequence.Kill();
        }

        _carrierSequence = null;
        EnsureCarriersVisibleAndClickable();
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
