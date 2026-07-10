using System.Collections.Generic;
using UnityEngine;

public class ConveyorDeliveryCubeFactory
{
    private readonly CubeConfigSO _cubeConfig;

    public ConveyorDeliveryCubeFactory(CubeConfigSO cubeConfig)
    {
        _cubeConfig = cubeConfig;
    }

    public AnimCube CreateAnimCubeInstance(Transform parent, List<AnimCube> cacheList = null)
    {
        var animCubePrefab = _cubeConfig != null ? _cubeConfig.AnimCubePrefab : null;
        if (animCubePrefab == null)
        {
            Debug.LogError("[ConveyorDeliveryCubeFactory] AnimCube prefab is not assigned.");
            return null;
        }

        AnimCube animCube;
#if UNITY_LUNA
        animCube = Object.Instantiate(animCubePrefab, parent);
#else
        animCube = PoolManagerNew.Instance.PopFromPool(animCubePrefab, parent);
#endif
        if (animCube.Trans == null) animCube.Trans = animCube.transform;
        LunaMaterialUtility.NormalizeRenderers(animCube.gameObject);
        cacheList?.Add(animCube);
        if (CustomTimeScaleGroup.Instance != null) CustomTimeScaleGroup.Instance.AddTarget(animCube);
        return animCube;
    }

    public Cube CreateCubeInstance()
    {
        var cube = _cubeConfig != null ? _cubeConfig.CubePrefab : null;
        if (cube == null)
        {
            Debug.LogError("[ConveyorDeliveryCubeFactory] Cube prefab is not assigned.");
            return null;
        }

        Cube instance;
#if UNITY_LUNA
        instance = Object.Instantiate(cube);
#else
        instance = PoolManagerNew.Instance.PopFromPool(cube);
#endif
        if (instance.Trans == null) instance.Trans = instance.transform;
        LunaMaterialUtility.NormalizeRenderers(instance.gameObject);
        instance.Trans.localScale = _cubeConfig.CubeDefaultScale;
        if (CustomTimeScaleGroup.Instance != null) CustomTimeScaleGroup.Instance.AddTarget(instance);
        return instance;
    }

    public void SetupCube(
        CubeBase cube,
        Vector3 startPos,
        EBlockColorType colorType,
        Transform parent)
    {
        if (cube == null) return;
        cube.transform.SetParent(parent, false);
        cube.transform.position = startPos;
        cube. InitCube(colorType);
    }
}
