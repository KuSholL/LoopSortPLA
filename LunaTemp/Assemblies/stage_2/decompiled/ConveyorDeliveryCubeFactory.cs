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
		AnimCube animCube = Object.Instantiate(animCubePrefab, parent);
		if (animCube.Trans == null)
		{
			animCube.Trans = animCube.transform;
		}
		LunaMaterialUtility.NormalizeRenderers(animCube.gameObject);
		cacheList?.Add(animCube);
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(animCube);
		}
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
		Cube instance = Object.Instantiate(cube);
		if (instance.Trans == null)
		{
			instance.Trans = instance.transform;
		}
		LunaMaterialUtility.NormalizeRenderers(instance.gameObject);
		instance.Trans.localScale = _cubeConfig.CubeDefaultScale;
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(instance);
		}
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
