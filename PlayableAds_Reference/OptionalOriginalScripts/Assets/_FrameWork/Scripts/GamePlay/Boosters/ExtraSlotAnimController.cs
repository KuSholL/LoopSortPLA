using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ExtraSlotAnimController : MonoSingleton<ExtraSlotAnimController>
{
    public ExtraSlotItemAnim prefab;

    public async UniTask PlayAnimAddCarrierFlow(Vector3 startWorldPos, Vector3 targetWorldPos,
        CarrierStackData carrierStack)
    {
        var iconCarrier = prefab;
        iconCarrier.gameObject.SetActive(true);
        await iconCarrier.PlayAnimPopup(startWorldPos);
        await iconCarrier.PlayAnimFly(startWorldPos, targetWorldPos);
        iconCarrier.gameObject.SetActive(false);
        PoolManagerNew.Instance.PushToPool(iconCarrier);
        if (CarrierSystem.Instance.TrySpawnCarrier(carrierStack))
        {
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_booster_extrabox);
            var spawnedCarriers = CarrierSystem.Instance.SpawnedCarriers;
            if (spawnedCarriers != null && spawnedCarriers.Count > 0)
            {
                var targetCarrier = spawnedCarriers[^1];
                targetCarrier.LockPick = true;
                // Play spawn animation using CarrierSpawnEffect script on the newly spawned carrier
                var spawnEffect = targetCarrier.GetComponent<CarrierSpawnEffect>();
                if (spawnEffect != null)
                {
                    await spawnEffect.PlaySpawnEffect();
                }
                targetCarrier.LockPick = false;
            }
        }
    }
}
