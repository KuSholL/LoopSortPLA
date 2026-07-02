using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColorEntry
{
    public EBlockColorType BlockColorType;
    public Color Color = Color.white;
    public Color ShadowColor = Color.white;
    public Color SpecularColor = Color.white;

    [Header("Custom Cat Cube")]
    public Color RimColor = new(0.8f, 0.8f, 0.8f, 0.5f);
    public Color MatCapColor = Color.white;
    public Color OutlineColor = Color.black;
}

[Serializable]
public class ColorMaterialSourceEntry
{
    public EBlockColorType BlockColorType;
    public Material SourceMaterial;
}

[CreateAssetMenu(fileName = "ColorConfigSO", menuName = "ScriptableObjects/ColorConfigSO")]
public class ColorConfigSO : ScriptableObject
{
    public List<ColorEntry> blockColors = new();
    public List<ColorMaterialSourceEntry> exportSources = new();

    public ColorEntry GetColorEntry(EBlockColorType blockColorType)
    {
        foreach (var entry in blockColors)
        {
            if (entry == null || entry.BlockColorType != blockColorType) continue;
            return entry;
        }

        return new ColorEntry();
    }
}
