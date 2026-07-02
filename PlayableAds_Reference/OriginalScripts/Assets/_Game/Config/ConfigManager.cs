using UnityEngine.Serialization;
using UnityEngine;

public class ConfigManager : MonoSingleton<ConfigManager>
{
    [FormerlySerializedAs("blockConfig")] [SerializeField]
    private ColorConfigSO colorConfig;

    [SerializeField] private ColorConfigSO cubeColorConfig;
    [SerializeField] private CubeConfigSO cubeConfig;
    [SerializeField] private ColorConfigSO specialColor;
    [SerializeField] private CarrierConfigSO carrierConfig;
    [SerializeField] private CatColorConfigSO catColorConfig;
    [SerializeField] private AnimBlockConfig animBlockConfig;
    [SerializeField] private CubeMovementConfigSO cubeMovementConfig;
    [SerializeField] private StylizedColorConfigSO stylizedColorConfig;
    [SerializeField] private RemainingColorConfigSO remainingColorConfig;

    public CubeMovementConfigSO GetCubeMovementConfig()
    {
        return cubeMovementConfig;
    }

    public RemainingColorConfigSO GetRemainingColorConfig()
    {
        return remainingColorConfig;
    }

    public StylizedColorConfigSO GetStylizedColorConfig()
    {
        return stylizedColorConfig;
    }

    public ColorConfigSO GetColorConfig()
    {
        return colorConfig;
    }

    public ColorConfigSO GetCubeColorConfig()
    {
        return cubeColorConfig;
    }

    public ColorEntry GetCubeColorEntryByType(EBlockColorType colorType)
    {
        return cubeColorConfig.GetColorEntry(colorType);
    }
    
    public ColorConfigSO GetSpecialColorConfig()
    {
        return specialColor;
    }

    public Cube GetCubePrefab()
    {
        return cubeConfig.CubePrefab;
    }

    public AnimCube GetAnimCubePrefab()
    {
        return cubeConfig.AnimCubePrefab;
    }

    public Vector3 GetCubeDefaultScale()
    {
        if (cubeConfig == null) return Vector3.one;
        return cubeConfig.CubeDefaultScale;
    }

    public CarrierConfigSO GetCarrierConfig()
    {
        return carrierConfig;
    }

    public AnimBlockConfig GetAnimBlockConfig()
    {
        return animBlockConfig;
    }

    public CatColorEntry GetCatColorEntryByType(EBlockColorType colorType)
    {
        if (!catColorConfig || catColorConfig.CatColorEntries == null) return null;
        return catColorConfig.CatColorEntries.Find(x=> x.BlockColorType == colorType);
    }
}
