using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CarrierSpawner : MonoBehaviour
{
	[SerializeField]
	private CarrierConfigSO carrierConfig;

	[SerializeField]
	private Transform spawnRoot;

	private readonly List<CarrierBase> _spawnedCarriers = new List<CarrierBase>();

	private ConveyorPathRuntime _runtimePath;

	private Sequence _carrierSequence;

	private LevelEntryAnimConfigSO EntryConfig => (MonoSingleton<LevelManager>.Instance != null) ? MonoSingleton<LevelManager>.Instance.LevelEntryAnimConfig : null;

	private float CarrierScaleDuration => (EntryConfig != null) ? EntryConfig.CarrierScaleDuration : 0.3f;

	private float CarrierScaleStagger => (EntryConfig != null) ? EntryConfig.CarrierScaleStagger : 0.1f;

	private Ease CarrierScaleEase => (EntryConfig != null) ? EntryConfig.CarrierScaleEase : Ease.OutBack;

	public List<CarrierBase> SpawnedCarriers => _spawnedCarriers;

	public CarrierConfigSO CarrierConfig => carrierConfig;

	public ConveyorPathRuntime Path => (_runtimePath != null) ? _runtimePath : ((MonoSingleton<ConveyorDeliverySystem>.Instance != null) ? MonoSingleton<ConveyorDeliverySystem>.Instance.Path : null);

	public void SetPath(ConveyorPathRuntime path)
	{
		_runtimePath = path;
	}

	private void OnDestroy()
	{
		if (_carrierSequence != null)
		{
			_carrierSequence.Kill();
		}
	}

	public void SpawnCarriers(LevelData levelData)
	{
		ClearCarriers();
		List<CarrierStackData> carrierStacks = levelData?.CarrierLayout?.Carriers;
		if (carrierStacks != null)
		{
			foreach (CarrierStackData carrier2 in carrierStacks)
			{
				CreateCarrier(carrier2);
			}
		}
		MonoSingleton<ConveyorDeliverySystem>.Instance?.SetupCarrierPickup(_spawnedCarriers);
		foreach (CarrierBase carrier in _spawnedCarriers)
		{
			if (!(carrier == null))
			{
				carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
				carrier.transform.localScale = Vector3.one;
			}
		}
	}

	public bool TrySpawnCarrier(CarrierStackData carrierStack)
	{
		CarrierBase carrier = CreateCarrier(carrierStack);
		if (carrier == null)
		{
			return false;
		}
		MonoSingleton<ConveyorDeliverySystem>.Instance?.SetupCarrierPickup(_spawnedCarriers);
		carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
		carrier.transform.localScale = Vector3.one;
		return true;
	}

	private void ClearCarriers()
	{
		if (_carrierSequence != null)
		{
			_carrierSequence.Kill();
			_carrierSequence = null;
		}
		foreach (CarrierBase carrier in _spawnedCarriers)
		{
			if (!(carrier == null))
			{
				carrier.SetClickCollidersEnabled(true);
				carrier.transform.localScale = Vector3.one;
				Object.Destroy(carrier.gameObject);
			}
		}
		_spawnedCarriers.Clear();
	}

	private CarrierBase CreateCarrier(CarrierStackData carrierStack)
	{
		if (carrierStack == null)
		{
			return null;
		}
		bool isSpawner = false;
		if (carrierStack.Mechanics != null)
		{
			for (int i = 0; i < carrierStack.Mechanics.Count; i++)
			{
				CarrierMechanicData mechanic = carrierStack.Mechanics[i];
				if (mechanic != null && mechanic.Type == ECarrierMechanic.Spawner)
				{
					isSpawner = true;
					break;
				}
			}
		}
		CarrierBase carrier = CreateCarrierInstance(isSpawner);
		if (carrier == null)
		{
			return null;
		}
		carrier.IncrementSessionId();
		ApplyCarrierTransform(carrier, carrierStack);
		carrier.SetSplineProgress(carrierStack.Progress);
		carrier.CreateBlocks(carrierStack, true);
		LunaMaterialUtility.NormalizeRenderers(carrier.gameObject);
		_spawnedCarriers.Add(carrier);
		return carrier;
	}

	private CarrierBase CreateCarrierInstance(bool isSpawner)
	{
		GameObject prefab = ((isSpawner && carrierConfig.Spawner != null) ? carrierConfig.Spawner.gameObject : ((carrierConfig.Prefab != null) ? carrierConfig.Prefab.gameObject : null));
		if (prefab == null)
		{
			return null;
		}
		GameObject instance = Object.Instantiate(prefab, spawnRoot);
		if (isSpawner)
		{
			Spawner spawner = instance.GetComponent<Spawner>();
			if (spawner != null)
			{
				return spawner;
			}
		}
		Carrier carrier = instance.GetComponent<Carrier>();
		if (carrier != null)
		{
			return carrier;
		}
		return instance.GetComponent<CarrierBase>();
	}

	private void ApplyCarrierTransform(CarrierBase carrier, CarrierStackData carrierStack)
	{
		if (!(carrier == null) && Path != null && Path.IsValid)
		{
			carrier.transform.position = GetCarrierWorldPosition(carrierStack);
			carrier.transform.rotation = GetCarrierWorldRotation(carrierStack);
		}
	}

	private static Vector3 GetCarrierPosition(CarrierStackData carrierStack)
	{
		return carrierStack?.Position ?? Vector3.zero;
	}

	public Vector3 GetCarrierWorldPosition(CarrierStackData carrierStack)
	{
		ConveyorPathRuntime path = Path;
		return (path != null && path.IsValid) ? path.TransformPoint(GetCarrierPosition(carrierStack)) : GetCarrierPosition(carrierStack);
	}

	private Quaternion GetCarrierWorldRotation(CarrierStackData carrierStack)
	{
		ConveyorPathRuntime path = Path;
		Quaternion rootRotation = ((path != null && path.Root != null) ? path.Root.rotation : Quaternion.identity);
		return rootRotation * Quaternion.Euler(0f, carrierStack.RotationY, 0f);
	}

	public IEnumerator PlayCarriersScaleAnimation()
	{
		if (_spawnedCarriers.Count != 0)
		{
			EnsureCarriersVisibleAndClickable();
		}
		yield break;
	}

	public void EnsureCarriersVisibleAndClickable()
	{
		for (int i = 0; i < _spawnedCarriers.Count; i++)
		{
			CarrierBase carrier = _spawnedCarriers[i];
			if (!(carrier == null))
			{
				carrier.CancelScaleAnimation();
				carrier.transform.localScale = Vector3.one;
				carrier.SetClickCollidersEnabled(!carrier.IsLockedByContainer());
			}
		}
	}
}
