using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Splines;

public class CarrierSpawner : MonoBehaviour
{
	[SerializeField]
	private CarrierConfigSO carrierConfig;

	[SerializeField]
	private SplineContainer splineContainer;

	[SerializeField]
	private Transform spawnRoot;

	private readonly List<CarrierBase> _spawnedCarriers = new List<CarrierBase>();

	private Sequence _carrierSequence;

	private LevelEntryAnimConfigSO EntryConfig => (MonoSingleton<LevelManager>.Instance != null) ? MonoSingleton<LevelManager>.Instance.LevelEntryAnimConfig : null;

	private float CarrierScaleDuration => (EntryConfig != null) ? EntryConfig.CarrierScaleDuration : 0.3f;

	private float CarrierScaleStagger => (EntryConfig != null) ? EntryConfig.CarrierScaleStagger : 0.1f;

	private Ease CarrierScaleEase => (EntryConfig != null) ? EntryConfig.CarrierScaleEase : Ease.OutBack;

	public List<CarrierBase> SpawnedCarriers => _spawnedCarriers;

	public CarrierConfigSO CarrierConfig => carrierConfig;

	public SplineContainer SplineContainer => splineContainer;

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
				carrier.SetClickCollidersEnabled(false);
				carrier.transform.localScale = Vector3.zero;
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
		carrier.SetClickCollidersEnabled(false);
		carrier.transform.localScale = Vector3.zero;
		CarrierBase spawnedCarrier = carrier;
		TweenerCore<Vector3, Vector3, VectorOptions> scaleTween = carrier.transform.DOScale(Vector3.one, CarrierScaleDuration).SetEase(CarrierScaleEase).SetUpdate(true)
			.OnComplete(delegate
			{
				spawnedCarrier.SetClickCollidersEnabled(true);
			});
		carrier.SetScaleMotionHandle(scaleTween);
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
				MonoSingleton<PoolManagerNew>.Instance.PushToPool(carrier);
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
		CarrierBase prefab = ((isSpawner && carrierConfig.Spawner != null) ? ((CarrierBase)carrierConfig.Spawner) : ((CarrierBase)carrierConfig.Prefab));
		CarrierBase carrier = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(prefab, spawnRoot);
		carrier.IncrementSessionId();
		ApplyCarrierTransform(carrier, carrierStack);
		carrier.SetSplineProgress(carrierStack.Progress);
		carrier.CreateBlocks(carrierStack, true);
		_spawnedCarriers.Add(carrier);
		return carrier;
	}

	private void ApplyCarrierTransform(CarrierBase carrier, CarrierStackData carrierStack)
	{
		if (!(carrier == null) && !(splineContainer == null))
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
		return splineContainer.transform.TransformPoint(GetCarrierPosition(carrierStack));
	}

	private Quaternion GetCarrierWorldRotation(CarrierStackData carrierStack)
	{
		return splineContainer.transform.rotation * Quaternion.Euler(0f, carrierStack.RotationY, 0f);
	}

	public IEnumerator PlayCarriersScaleAnimation()
	{
		if (_spawnedCarriers.Count == 0)
		{
			yield break;
		}
		if (_carrierSequence != null)
		{
			_carrierSequence.Kill();
			_carrierSequence = null;
		}
		for (int i = 0; i < _spawnedCarriers.Count; i++)
		{
			CarrierBase carrier = _spawnedCarriers[i];
			if (carrier != null)
			{
				bool isLocked = carrier.IsLockedByContainer();
				carrier.SetClickCollidersEnabled(isLocked);
				carrier.transform.localScale = (isLocked ? Vector3.one : Vector3.zero);
			}
		}
		_carrierSequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
		for (int j = 0; j < _spawnedCarriers.Count; j++)
		{
			CarrierBase carrier2 = _spawnedCarriers[j];
			if (!(carrier2 == null) && !carrier2.IsLockedByContainer())
			{
				TweenerCore<Vector3, Vector3, VectorOptions> scaleTween = carrier2.transform.DOScale(Vector3.one, CarrierScaleDuration).SetEase(CarrierScaleEase).SetUpdate(true)
					.OnComplete(delegate
					{
						carrier2.SetClickCollidersEnabled(true);
					});
				carrier2.SetScaleMotionHandle(scaleTween);
				_carrierSequence.Insert((float)j * CarrierScaleStagger, scaleTween);
			}
		}
		float elapsed = 0f;
		float timeout = Mathf.Max(0.05f, CarrierScaleDuration + CarrierScaleStagger * (float)Mathf.Max(0, _spawnedCarriers.Count - 1) + 0.25f);
		while (_carrierSequence != null && _carrierSequence.IsActive() && !_carrierSequence.IsComplete() && elapsed < timeout)
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
