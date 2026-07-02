using System.Collections.Generic;

public class CarrierSystem : MonoSingleton<CarrierSystem>
{
    [UnityEngine.SerializeField] private CarrierSpawner carrierSpawner;

    public CarrierSpawner CarrierSpawner
    {
        get { return carrierSpawner; }
    }

    public IReadOnlyList<CarrierBase> SpawnedCarriers
    {
        get { return carrierSpawner != null ? carrierSpawner.SpawnedCarriers : null; }
    }

    public void InitCarrier(LevelData levelData)
    {
        if (carrierSpawner != null)
        {
            carrierSpawner.SpawnCarriers(levelData);
        }
    }

    public bool TrySpawnCarrier(CarrierStackData carrierStack)
    {
        return carrierStack != null &&
               carrierSpawner != null &&
               carrierSpawner.TrySpawnCarrier(carrierStack);
    }

    public CarrierBase GetCarrier(int index)
    {
        var carriers = SpawnedCarriers;
        return carriers != null && index >= 0 && index < carriers.Count ? carriers[index] : null;
    }
}
