using System.Collections.Generic;
using UnityEngine;

public class ConveyorDeliveryCubeFactory
{
    private const int MaxCubePoolSize = 128;
    private const int MaxAnimCubePoolSize = 32;

    private readonly CubeConfigSO _cubeConfig;
    private readonly Stack<Cube> _cubePool = new Stack<Cube>();
    private readonly Stack<AnimCube> _animCubePool = new Stack<AnimCube>();

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

        var animCube = TakeAnimCube();
        if (animCube == null)
        {
            animCube = Object.Instantiate(animCubePrefab, parent);
            LunaMaterialUtility.NormalizeRenderers(animCube.gameObject);
        }
        else
        {
            animCube.transform.SetParent(parent, false);
            animCube.gameObject.SetActive(true);
        }
        if (animCube.Trans == null) animCube.Trans = animCube.transform;
        ResetCubeTransform(animCube, GetAnimCubeScale());
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

        var instance = TakeCube();
        if (instance == null)
        {
            instance = Object.Instantiate(cube);
            LunaMaterialUtility.NormalizeRenderers(instance.gameObject);
        }
        else
        {
            instance.gameObject.SetActive(true);
        }
        if (instance.Trans == null) instance.Trans = instance.transform;
        ResetCubeTransform(instance, GetMovingCubeScale());
        if (CustomTimeScaleGroup.Instance != null) CustomTimeScaleGroup.Instance.AddTarget(instance);
        return instance;
    }

    public void ReleaseCube(Cube cube)
    {
        if (cube == null || !cube.gameObject.activeSelf) return;
        cube.gameObject.SetActive(false);
        if (_cubePool.Count >= MaxCubePoolSize)
        {
            Object.Destroy(cube.gameObject);
            return;
        }
        _cubePool.Push(cube);
    }

    public void ReleaseAnimCube(AnimCube animCube)
    {
        if (animCube == null || !animCube.gameObject.activeSelf) return;
        animCube.gameObject.SetActive(false);
        if (_animCubePool.Count >= MaxAnimCubePoolSize)
        {
            Object.Destroy(animCube.gameObject);
            return;
        }
        _animCubePool.Push(animCube);
    }

    private Cube TakeCube()
    {
        while (_cubePool.Count > 0)
        {
            var cube = _cubePool.Pop();
            if (cube != null) return cube;
        }
        return null;
    }

    private AnimCube TakeAnimCube()
    {
        while (_animCubePool.Count > 0)
        {
            var animCube = _animCubePool.Pop();
            if (animCube != null) return animCube;
        }
        return null;
    }

    public void SetupCube(
        Cube cube,
        Vector3 startPos,
        EBlockColorType colorType,
        Transform parent)
    {
        SetupCubeInternal(cube, startPos, colorType, parent, GetMovingCubeScale());
    }

    public void SetupCube(
        AnimCube cube,
        Vector3 startPos,
        EBlockColorType colorType,
        Transform parent)
    {
        SetupCubeInternal(cube, startPos, colorType, parent, GetAnimCubeScale());
    }

    private static void SetupCubeInternal(
        CubeBase cube,
        Vector3 startPos,
        EBlockColorType colorType,
        Transform parent,
        Vector3 defaultScale)
    {
        if (cube == null) return;
        cube.transform.SetParent(parent, false);
        cube.transform.position = startPos;
        ResetCubeTransform(cube, defaultScale);
        cube.InitCube(colorType);
    }

    private static void ResetCubeTransform(CubeBase cube, Vector3 defaultScale)
    {
        if (cube == null) return;
        var trans = cube.Trans != null ? cube.Trans : cube.transform;
        trans.localRotation = Quaternion.identity;
        trans.localScale = defaultScale;
    }

    private Vector3 GetMovingCubeScale()
    {
        return _cubeConfig != null ? _cubeConfig.CubeDefaultScale : Vector3.one;
    }

    private Vector3 GetAnimCubeScale()
    {
        var prefab = _cubeConfig != null ? _cubeConfig.AnimCubePrefab : null;
        if (prefab == null) return Vector3.one;
        var prefabTransform = prefab.Trans != null ? prefab.Trans : prefab.transform;
        return prefabTransform.localScale;
    }
}
