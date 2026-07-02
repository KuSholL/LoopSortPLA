using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StylizedColorEntry
{
    public EBlockColorType BlockColorType;
    public Color Color = Color.white;
    public Color ShadowColor = Color.white;
    public Color SpecularColor = Color.white;
    public Color ReflectColor = Color.white;
}

[Serializable]
public class StylizedColorMaterialSourceEntry
{
    public EBlockColorType BlockColorType;
    public Material SourceMaterial;
}

[CreateAssetMenu(fileName = "StylizedColorConfigSO", menuName = "ScriptableObjects/StylizedColorConfigSO")]
public class StylizedColorConfigSO : ScriptableObject
{
    public List<StylizedColorEntry> blockColors = new();
    public List<StylizedColorMaterialSourceEntry> exportSources = new();

    public StylizedColorEntry GetColorEntry(EBlockColorType blockColorType)
    {
        foreach (var entry in blockColors)
        {
            if (entry == null || entry.BlockColorType != blockColorType) continue;
            return entry;
        }

        return new StylizedColorEntry();
    }
}
