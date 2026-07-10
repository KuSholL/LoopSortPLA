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

    public void InitCarrier(LevelData levelData, ConveyorPathRuntime path)
    {
        if (carrierSpawner != null)
        {
            carrierSpawner.SetPath(path);
            carrierSpawner.SpawnCarriers(levelData);
            _containerSpawner.SpawnContainers(
                levelData,
                carrierSpawner.SpawnedCarriers,
                carrierSpawner.CarrierConfig,
                path);
#if UNITY_LUNA
            carrierSpawner.EnsureCarriersVisibleAndClickable();
            _containerSpawner.EnsureContainersAtFinalState();
#endif
        }
    }

    public void InitCarrier(LevelData levelData)
    {
        InitCarrier(levelData, carrierSpawner != null ? carrierSpawner.Path : null);
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
