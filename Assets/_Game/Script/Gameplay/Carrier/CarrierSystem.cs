using System.Collections.Generic;
using System.Collections;

public class CarrierSystem : MonoSingleton<CarrierSystem>
{
    [UnityEngine.SerializeField] private CarrierSpawner carrierSpawner;
    private readonly ContainerRuntimeSpawner _containerSpawner = new ContainerRuntimeSpawner();

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
            _containerSpawner.SpawnContainers(
                levelData,
                carrierSpawner.SpawnedCarriers,
                carrierSpawner.CarrierConfig,
                carrierSpawner.SplineContainer);
        }
    }

    public IEnumerator PlayContainersScaleAnimation(LevelEntryAnimConfigSO animationConfig)
    {
        yield return _containerSpawner.PlayScaleAnimation(animationConfig);
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
