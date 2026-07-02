using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Splines;

public class RoadAreaCalculator : MonoBehaviour
{
    private const int BaselineCubePerBlock = 8;

    [SerializeField] private SplineContainer centerRoad;
    [SerializeField] private ConveyorMeshBuilder conveyorMeshBuilder;
    [SerializeField] private float objectRadius = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float packingEfficiency = 0.9f;
    [SerializeField, ReadOnly] private float roadArea;
    [SerializeField, ReadOnly] private float objectFootprintArea;
    [SerializeField, ReadOnly] private int recommendedObjectCount;
    [SerializeField, ReadOnly] private int cubePerBlock;
    [SerializeField, ReadOnly] private int blockPerCarrier;
    [SerializeField, ReadOnly] private int requiredBlockCount;
    [SerializeField, ReadOnly] private int requiredCarrierCount;

    private void OnValidate()
    {
        CalculateArea();
    }

    [Button]
    private void CalculateArea()
    {
        roadArea = GetRoadArea(centerRoad);
        UpdateCapacity(roadArea);
    }

    public int GetRecommendedBlockCount(SplineContainer road)
    {
        var area = GetRoadArea(road);
        return GetRequiredBlockCount(area);
    }

    public int GetBaselineBlockCount(SplineContainer road)
    {
        var area = GetRoadArea(road);
        return GetRequiredBlockCount(area, BaselineCubePerBlock);
    }

    private float GetRoadArea(SplineContainer road)
    {
        if (conveyorMeshBuilder == null || road == null) return 0f;
        return conveyorMeshBuilder.GetProjectedRoadArea(road);
    }

    private void UpdateCapacity(float area)
    {
        if (objectRadius <= 0f)
        {
            objectFootprintArea = 0f;
            recommendedObjectCount = 0;
            ResetFillCounts();
            return;
        }

        objectFootprintArea = Mathf.PI * objectRadius * objectRadius;
        var maxObjectCount = Mathf.FloorToInt(area / objectFootprintArea);
        recommendedObjectCount = Mathf.FloorToInt(maxObjectCount * packingEfficiency);
        UpdateFillCounts();
    }

    private void UpdateFillCounts()
    {
        var config = GetCarrierConfig();
        cubePerBlock = CarrierGridUtility.GetBaselineCubePerBlock(config);
        blockPerCarrier = GetBlockPerCarrier(config);
        requiredBlockCount = Mathf.RoundToInt(GetFillObjectCount() / (float)cubePerBlock);
        requiredCarrierCount = blockPerCarrier > 0
            ? Mathf.RoundToInt(requiredBlockCount / (float)blockPerCarrier)
            : 0;
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
        return ConfigManager.Instance != null ? ConfigManager.Instance.GetCarrierConfig() : null;
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
        if (objectRadius <= 0f) return 0;
        var footprintArea = Mathf.PI * objectRadius * objectRadius;
        var objectCount = Mathf.FloorToInt(area / footprintArea);
        var fillCount = Mathf.FloorToInt(objectCount * packingEfficiency);
        return Mathf.RoundToInt(fillCount / (float)Mathf.Max(1, cubePerBlock));
    }

    private static int GetBlockPerCarrier(CarrierConfigSO config)
    {
        return config != null ? config.GetBlockCount() : 0;
    }
}
