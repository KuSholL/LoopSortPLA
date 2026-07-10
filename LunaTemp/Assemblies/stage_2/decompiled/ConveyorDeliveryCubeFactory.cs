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
		AnimCube animCubePrefab = ((_cubeConfig != null) ? _cubeConfig.AnimCubePrefab : null);
		if (animCubePrefab == null)
		{
			Debug.LogError("[ConveyorDeliveryCubeFactory] AnimCube prefab is not assigned.");
			return null;
		}
		AnimCube animCube = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(animCubePrefab, parent);
		cacheList?.Add(animCube);
		MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(animCube);
		return animCube;
	}

	public Cube CreateCubeInstance()
	{
		Cube cube = ((_cubeConfig != null) ? _cubeConfig.CubePrefab : null);
		if (cube == null)
		{
			Debug.LogError("[ConveyorDeliveryCubeFactory] Cube prefab is not assigned.");
			return null;
		}
		Cube instance = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(cube);
		instance.Trans.localScale = _cubeConfig.CubeDefaultScale;
		MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(instance);
		return instance;
	}

	public void SetupCube(CubeBase cube, Vector3 startPos, EBlockColorType colorType, Transform parent)
	{
		if (!(cube == null))
		{
			cube.transform.SetParent(parent, false);
			cube.transform.position = startPos;
			cube.InitCube(colorType);
		}
	}
}
