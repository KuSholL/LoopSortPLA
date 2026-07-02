using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialPropertyBlockExtensions
{
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int ShadowColor = Shader.PropertyToID("_SColor");
    private static readonly int SpecularColor = Shader.PropertyToID("_SpecularColor");
    private static readonly int RimColor = Shader.PropertyToID("_RimColor");
    private static readonly int MatCapColor = Shader.PropertyToID("_MatCapColor");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColorVertex");

    public static void SetColorEntry(this MaterialPropertyBlock propertyBlock, ColorEntry entry)
    {
        if (entry == null || propertyBlock == null) return;
        propertyBlock.SetColor(Color, entry.Color);
        propertyBlock.SetColor(ShadowColor, entry.ShadowColor);
        propertyBlock.SetColor(SpecularColor, entry.SpecularColor);
        propertyBlock.SetColor(RimColor, entry.RimColor);
        propertyBlock.SetColor(MatCapColor, entry.MatCapColor);
        propertyBlock.SetColor(OutlineColor, entry.OutlineColor);
    }

    public static void SetColorWhite(this MaterialPropertyBlock propertyBlock)
    {
        if (propertyBlock == null) return;
        propertyBlock.SetColor(Color, UnityEngine.Color.white);
        propertyBlock.SetColor(ShadowColor, UnityEngine.Color.white);
        propertyBlock.SetColor(SpecularColor, UnityEngine.Color.white);
        propertyBlock.SetColor(RimColor, UnityEngine.Color.white);
        propertyBlock.SetColor(MatCapColor, UnityEngine.Color.white);
        propertyBlock.SetColor(OutlineColor, UnityEngine.Color.white);
    }
}
