using System.Collections.Generic;
using UnityEngine;

public class ConveyorDeliveryCubeFactory
{
    public AnimCube CreateAnimCubeInstance(Transform parent, List<AnimCube> cacheList = null)
    {
        var animCubePrefab = ConfigManager.Instance.GetAnimCubePrefab();
        var animCube = PoolManagerNew.Instance.PopFromPool(animCubePrefab, parent);
        cacheList?.Add(animCube);
        CustomTimeScaleGroup.Instance.AddTarget(animCube);
        return animCube;
    }

    public Cube CreateCubeInstance()
    {
        var cube = ConfigManager.Instance.GetCubePrefab();
        var instance = PoolManagerNew.Instance.PopFromPool(cube);
        instance.Trans.localScale = ConfigManager.Instance.GetCubeDefaultScale();
        CustomTimeScaleGroup.Instance.AddTarget(instance);
        return instance;
    }

    public void SetupCube(
        CubeBase cube,
        Vector3 startPos,
        EBlockColorType colorType,
        Transform parent)
    {
        cube.transform.SetParent(parent, false);
        cube.transform.position = startPos;
        cube. InitCube(colorType);
    }
}
