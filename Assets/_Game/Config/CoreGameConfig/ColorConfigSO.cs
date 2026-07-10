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
    public Color RimColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
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
    public List<ColorEntry> blockColors = new List<ColorEntry>();
    public List<ColorMaterialSourceEntry> exportSources = new List<ColorMaterialSourceEntry>();

    public ColorEntry GetColorEntry(EBlockColorType blockColorType)
    {
        foreach (var entry in blockColors)
        {
            if (entry == null || entry.BlockColorType != blockColorType) continue;
            return entry;
        }

        return PlayableColorFallback.CreateColorEntry(blockColorType);
    }
}

public static class PlayableColorFallback
{
    public static ColorEntry CreateColorEntry(EBlockColorType colorType)
    {
        var color = GetColor(colorType);
        return new ColorEntry
        {
            BlockColorType = colorType,
            Color = color,
            ShadowColor = GetShadowColor(color),
            SpecularColor = Color.white,
            RimColor = new Color(0.8f, 0.8f, 0.8f, 0.5f),
            MatCapColor = Color.white,
            OutlineColor = new Color(0.18f, 0.18f, 0.3f, 1f)
        };
    }

    public static Color GetColor(EBlockColorType colorType)
    {
        switch (colorType)
        {
            case EBlockColorType.Red: return new Color(0.9f, 0.18f, 0.16f, 1f);
            case EBlockColorType.Pink: return new Color(1f, 0.22f, 0.58f, 1f);
            case EBlockColorType.Brown: return new Color(0.58f, 0.28f, 0.12f, 1f);
            case EBlockColorType.Peach: return new Color(1f, 0.7f, 0.53f, 1f);
            case EBlockColorType.Yellow: return new Color(1f, 0.78f, 0.08f, 1f);
            case EBlockColorType.Orange: return new Color(1f, 0.45f, 0.03f, 1f);
            case EBlockColorType.Purple: return new Color(0.58f, 0.18f, 0.95f, 1f);
            case EBlockColorType.DarkPink: return new Color(0.84f, 0.08f, 0.52f, 1f);
            case EBlockColorType.Green: return new Color(0.02f, 0.58f, 0.29f, 1f);
            case EBlockColorType.LimeGreen: return new Color(0.42f, 0.9f, 0.22f, 1f);
            case EBlockColorType.Blue: return new Color(0.1f, 0.36f, 1f, 1f);
            case EBlockColorType.Cyan: return new Color(0.02f, 0.78f, 1f, 1f);
            case EBlockColorType.DarkPurple: return new Color(0.28f, 0.2f, 0.78f, 1f);
            case EBlockColorType.Teal: return new Color(0.02f, 0.62f, 0.58f, 1f);
            default: return Color.white;
        }
    }

    private static Color GetShadowColor(Color color)
    {
        return new Color(color.r * 0.58f, color.g * 0.58f, color.b * 0.68f, color.a);
    }
}
