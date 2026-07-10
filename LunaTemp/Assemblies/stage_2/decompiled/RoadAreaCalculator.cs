using System;
using UnityEngine;

public class RoadAreaCalculator : MonoBehaviour
{
	private const int BaselineCubePerBlock = 8;

	[SerializeField]
	private ConveyorManager conveyorManager;

	[SerializeField]
	private ConveyorMeshBuilder conveyorMeshBuilder;

	[SerializeField]
	private float objectRadius = 0.2f;

	[SerializeField]
	[Range(0.1f, 1f)]
	private float packingEfficiency = 0.9f;

	[SerializeField]
	private float roadArea;

	[SerializeField]
	private float objectFootprintArea;

	[SerializeField]
	private int recommendedObjectCount;

	[SerializeField]
	private int cubePerBlock;

	[SerializeField]
	private int blockPerCarrier;

	[SerializeField]
	private int requiredBlockCount;

	[SerializeField]
	private int requiredCarrierCount;

	private void OnValidate()
	{
		CalculateArea();
	}

	private void CalculateArea()
	{
		roadArea = GetRoadArea((conveyorManager != null) ? conveyorManager.Path : null);
		UpdateCapacity(roadArea);
	}

	public int GetRecommendedBlockCount(ConveyorPathRuntime road)
	{
		float area = GetRoadArea(road);
		return GetRequiredBlockCount(area);
	}

	public int GetBaselineBlockCount(ConveyorPathRuntime road)
	{
		float area = GetRoadArea(road);
		return GetRequiredBlockCount(area, 8);
	}

	private float GetRoadArea(ConveyorPathRuntime road)
	{
		if (conveyorMeshBuilder == null || road == null)
		{
			return 0f;
		}
		return conveyorMeshBuilder.GetProjectedRoadArea(road);
	}

	private void UpdateCapacity(float area)
	{
		if (objectRadius <= 0f)
		{
			objectFootprintArea = 0f;
			recommendedObjectCount = 0;
			ResetFillCounts();
		}
		else
		{
			objectFootprintArea = 3.14159265f * objectRadius * objectRadius;
			int maxObjectCount = Mathf.FloorToInt(area / objectFootprintArea);
			recommendedObjectCount = Mathf.FloorToInt((float)maxObjectCount * packingEfficiency);
			UpdateFillCounts();
		}
	}

	private void UpdateFillCounts()
	{
		CarrierConfigSO config = GetCarrierConfig();
		cubePerBlock = CarrierGridUtility.GetBaselineCubePerBlock(config);
		blockPerCarrier = GetBlockPerCarrier(config);
		requiredBlockCount = Mathf.RoundToInt((float)GetFillObjectCount() / (float)cubePerBlock);
		requiredCarrierCount = ((blockPerCarrier > 0) ? Mathf.RoundToInt((float)requiredBlockCount / (float)blockPerCarrier) : 0);
	}

	private void ResetFillCounts()
	{
		cubePerBlock = 0;
		blockPerCarrier = 0;
		requiredBlockCount = 0;
		requiredCarrierCount = 0;
	}

	private CarrierConfigSO GetCarrierConfig()
	{
		return (MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCarrierConfig() : null;
	}

	private int GetFillObjectCount()
	{
		return Mathf.Max(0, recommendedObjectCount);
	}

	private int GetRequiredBlockCount(float area)
	{
		return GetRequiredBlockCount(area, CarrierGridUtility.GetBaselineCubePerBlock(GetCarrierConfig()));
	}

	private int GetRequiredBlockCount(float area, int cubePerBlock)
	{
		if (objectRadius <= 0f)
		{
			return 0;
		}
		float footprintArea = 3.14159265f * objectRadius * objectRadius;
		int objectCount = Mathf.FloorToInt(area / footprintArea);
		int fillCount = Mathf.FloorToInt((float)objectCount * packingEfficiency);
		return Mathf.RoundToInt((float)fillCount / (float)Mathf.Max(1, cubePerBlock));
	}

	private static int GetBlockPerCarrier(CarrierConfigSO config)
	{
		return (config != null) ? config.GetBlockCount() : 0;
	}
}
