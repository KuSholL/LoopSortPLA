using UnityEngine;

public static class RendererMaterialExtensions
{
    private const string RuntimeMaterialSuffix = "_PLA_Runtime";
    private static readonly int Color = Shader.PropertyToID("_Color");
    private static readonly int ShadowColor = Shader.PropertyToID("_SColor");
    private static readonly int SpecularColor = Shader.PropertyToID("_SpecularColor");
    private static readonly int RimColor = Shader.PropertyToID("_RimColor");
    private static readonly int MatCapColor = Shader.PropertyToID("_MatCapColor");
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColorVertex");
    private static readonly int StylizedShadowColor = Shader.PropertyToID("_ShadowColor");
    private static readonly int ReflectColor = Shader.PropertyToID("_ReflectColor");

    public static void ApplyColorEntry(this Renderer renderer, ColorEntry entry, int materialIndex = -1)
    {
        if (entry == null) return;
        renderer.ApplyColor(Color, entry.Color, materialIndex);
        renderer.ApplyColor(ShadowColor, entry.ShadowColor, materialIndex);
        renderer.ApplyColor(SpecularColor, entry.SpecularColor, materialIndex);
        renderer.ApplyColor(RimColor, entry.RimColor, materialIndex);
        renderer.ApplyColor(MatCapColor, entry.MatCapColor, materialIndex);
        renderer.ApplyColor(OutlineColor, entry.OutlineColor, materialIndex);
    }

    public static void ApplyColorWhite(this Renderer renderer, int materialIndex = -1)
    {
        renderer.ApplyColor(Color, UnityEngine.Color.white, materialIndex);
        renderer.ApplyColor(ShadowColor, UnityEngine.Color.white, materialIndex);
        renderer.ApplyColor(SpecularColor, UnityEngine.Color.white, materialIndex);
        renderer.ApplyColor(RimColor, UnityEngine.Color.white, materialIndex);
        renderer.ApplyColor(MatCapColor, UnityEngine.Color.white, materialIndex);
        renderer.ApplyColor(OutlineColor, UnityEngine.Color.white, materialIndex);
    }

    public static void ApplyColorEntry(this Renderer renderer, StylizedColorEntry entry, int materialIndex = -1)
    {
        if (entry == null) return;
        renderer.ApplyColor(Color, entry.Color, materialIndex);
        renderer.ApplyColor(StylizedShadowColor, entry.ShadowColor, materialIndex);
        renderer.ApplyColor(SpecularColor, entry.SpecularColor, materialIndex);
        renderer.ApplyColor(ReflectColor, entry.ReflectColor, materialIndex);
    }

    public static void ApplyColor(this Renderer renderer, int propertyId, UnityEngine.Color value, int materialIndex = -1)
    {
        if (renderer == null) return;
        var materials = EnsureRuntimeMaterials(renderer);
        if (materials == null || materials.Length == 0) return;

        if (materialIndex >= 0)
        {
            if (materialIndex >= materials.Length || materials[materialIndex] == null) return;
            materials[materialIndex].SetColor(propertyId, value);
            return;
        }

        for (var i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null) materials[i].SetColor(propertyId, value);
        }
    }

    public static void ApplyFloat(this Renderer renderer, int propertyId, float value, int materialIndex = -1)
    {
        if (renderer == null) return;
        var materials = EnsureRuntimeMaterials(renderer);
        if (materials == null || materials.Length == 0) return;

        if (materialIndex >= 0)
        {
            if (materialIndex >= materials.Length || materials[materialIndex] == null) return;
            materials[materialIndex].SetFloat(propertyId, value);
            return;
        }

        for (var i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null) materials[i].SetFloat(propertyId, value);
        }
    }

    private static Material[] EnsureRuntimeMaterials(Renderer renderer)
    {
        if (renderer == null) return null;
        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0) return materials;

        var changed = false;
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null) continue;
            if (material.name.EndsWith(RuntimeMaterialSuffix)) continue;

            materials[i] = LunaMaterialUtility.CreateRuntimeMaterial(material);
            changed = true;
        }

        if (changed) renderer.sharedMaterials = materials;
        return materials;
    }
}

public static class LunaMaterialUtility
{
    private const string RuntimeMaterialSuffix = "_PLA_Runtime";
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int HighlightColorId = Shader.PropertyToID("_HColor");
    private static readonly int ShadowColorId = Shader.PropertyToID("_SColor");
    private static readonly int StylizedShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int SpecularColorId = Shader.PropertyToID("_SpecularColor");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int MatCapColorId = Shader.PropertyToID("_MatCapColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColorVertex");
    private static readonly int ReflectColorId = Shader.PropertyToID("_ReflectColor");
    private static readonly int ReflectionColorId = Shader.PropertyToID("_ReflectionColor");
    private static readonly int FollowerColorId = Shader.PropertyToID("_FollowerColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int RampThresholdId = Shader.PropertyToID("_RampThreshold");
    private static readonly int RampSmoothingId = Shader.PropertyToID("_RampSmoothing");
    private static readonly int SpecularToonSizeId = Shader.PropertyToID("_SpecularToonSize");
    private static readonly int SpecularToonSmoothnessId = Shader.PropertyToID("_SpecularToonSmoothness");
    private static readonly int SpecularIntensityId = Shader.PropertyToID("_SpecularIntensity");
    private static readonly int ReflectIntensityId = Shader.PropertyToID("_ReflectIntensity");

    public static void NormalizeRenderers(GameObject root)
    {
#if UNITY_LUNA
        if (root == null) return;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
            NormalizeRenderer(renderers[i]);
#endif
    }

    public static void NormalizeRenderer(Renderer renderer)
    {
#if UNITY_LUNA
        if (renderer == null) return;
        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0) return;

        var changed = false;
        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null) continue;
            if (material.name.EndsWith(RuntimeMaterialSuffix)) continue;
            materials[i] = CreateRuntimeMaterial(material);
            changed = true;
        }

        if (changed) renderer.sharedMaterials = materials;
#endif
    }

    public static Material CreateRuntimeMaterial(Material source)
    {
        if (source == null) return null;
#if UNITY_LUNA
        var shader = GetFallbackShader();
        var material = shader != null ? new Material(shader) : new Material(source);
        material.name = source.name + RuntimeMaterialSuffix;
        CopyMainTexture(source, material);
        CopyVisualProperties(source, material);
        return material;
#else
        return new Material(source)
        {
            name = source.name + RuntimeMaterialSuffix
        };
#endif
    }

    public static Material CreateRuntimeMaterial(Material source, Color fallbackColor)
    {
        var material = CreateRuntimeMaterial(source);
        if (material != null) ApplyColor(material, fallbackColor);
        return material;
    }

    public static Material CreateRuntimeMaterial(Color color, string materialName)
    {
#if UNITY_LUNA
        var shader = GetFallbackShader();
        if (shader == null) return null;
        var material = new Material(shader);
        material.name = (string.IsNullOrEmpty(materialName) ? "PLA_RuntimeMaterial" : materialName) + RuntimeMaterialSuffix;
        ApplyColor(material, color);
        return material;
#else
        var shader = Shader.Find("Standard");
        if (shader == null) return null;
        var material = new Material(shader);
        material.name = string.IsNullOrEmpty(materialName) ? "PLA_RuntimeMaterial" : materialName;
        ApplyColor(material, color);
        return material;
#endif
    }

    private static Shader GetFallbackShader()
    {
        var shader = Shader.Find("PLA/Custom_Cube_Mechanic_Lite");
        if (shader != null) return shader;
        shader = Shader.Find("Custom_Cube");
        if (shader != null) return shader;
        shader = Shader.Find("Mobile/Diffuse");
        if (shader != null) return shader;
        shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;
        shader = Shader.Find("Unlit/Color");
        if (shader != null) return shader;
        return Shader.Find("Standard");
    }

    private static Color GetReadableColor(Material source)
    {
        var color = Color.white;
        if (source == null) return color;

        if (source.HasProperty(ColorId)) color = source.GetColor(ColorId);
        else if (source.HasProperty(BaseColorId)) color = source.GetColor(BaseColorId);
        else if (source.HasProperty(FollowerColorId)) color = source.GetColor(FollowerColorId);

        if (color.a < 0.05f) color.a = 1f;
        return color;
    }

    private static void ApplyColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
        if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
        if (material.HasProperty(HighlightColorId)) material.SetColor(HighlightColorId, Color.Lerp(color, Color.white, 0.55f));
        if (material.HasProperty(ShadowColorId)) material.SetColor(ShadowColorId, Color.Lerp(color, Color.black, 0.42f));
    }

    private static void CopyVisualProperties(Material source, Material target)
    {
        if (source == null || target == null) return;

        var readableColor = GetReadableColor(source);
        ApplyColor(target, readableColor);

        CopyColor(source, target, ColorId);
        CopyColor(source, target, BaseColorId);
        CopyColor(source, target, HighlightColorId);
        CopyColor(source, target, ShadowColorId);
        CopyColor(source, target, StylizedShadowColorId, ShadowColorId);
        CopyColor(source, target, SpecularColorId);
        CopyColor(source, target, RimColorId);
        CopyColor(source, target, MatCapColorId);
        CopyColor(source, target, OutlineColorId);
        CopyColor(source, target, ReflectColorId);
        CopyColor(source, target, ReflectionColorId, ReflectColorId);

        CopyFloat(source, target, RampThresholdId);
        CopyFloat(source, target, RampSmoothingId);
        CopyFloat(source, target, SpecularToonSizeId);
        CopyFloat(source, target, SpecularToonSmoothnessId);
        CopyFloat(source, target, SpecularIntensityId);
        CopyFloat(source, target, ReflectIntensityId);
        ClampMinimumFloat(target, SpecularToonSizeId, 0.22f);
        ClampMinimumFloat(target, SpecularToonSmoothnessId, 0.035f);
        ClampMinimumFloat(target, SpecularIntensityId, 0.65f);
        ClampMinimumFloat(target, ReflectIntensityId, 0.2f);
    }

    private static void CopyColor(Material source, Material target, int sourceId)
    {
        CopyColor(source, target, sourceId, sourceId);
    }

    private static void CopyColor(Material source, Material target, int sourceId, int targetId)
    {
        if (source == null || target == null) return;
        if (!source.HasProperty(sourceId) || !target.HasProperty(targetId)) return;
        target.SetColor(targetId, source.GetColor(sourceId));
    }

    private static void CopyFloat(Material source, Material target, int propertyId)
    {
        if (source == null || target == null) return;
        if (!source.HasProperty(propertyId) || !target.HasProperty(propertyId)) return;
        target.SetFloat(propertyId, source.GetFloat(propertyId));
    }

    private static void ClampMinimumFloat(Material target, int propertyId, float minimum)
    {
        if (target == null || !target.HasProperty(propertyId)) return;
        if (target.GetFloat(propertyId) < minimum) target.SetFloat(propertyId, minimum);
    }

    private static void CopyMainTexture(Material source, Material target)
    {
        if (source == null || target == null) return;
        if (!source.HasProperty(MainTexId) || !target.HasProperty(MainTexId)) return;

        var texture = source.GetTexture(MainTexId);
        if (texture == null) return;
        target.SetTexture(MainTexId, texture);
        target.SetTextureScale(MainTexId, source.GetTextureScale(MainTexId));
        target.SetTextureOffset(MainTexId, source.GetTextureOffset(MainTexId));
    }
}
