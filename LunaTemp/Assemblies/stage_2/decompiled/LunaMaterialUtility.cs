using UnityEngine;

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
		if (!(root == null))
		{
			Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
			{
				NormalizeRenderer(renderers[i]);
			}
		}
	}

	public static void NormalizeRenderer(Renderer renderer)
	{
		if (renderer == null)
		{
			return;
		}
		Material[] materials = renderer.sharedMaterials;
		if (materials == null || materials.Length == 0)
		{
			return;
		}
		bool changed = false;
		for (int i = 0; i < materials.Length; i++)
		{
			Material material = materials[i];
			if (!(material == null) && !material.name.EndsWith("_PLA_Runtime"))
			{
				materials[i] = CreateRuntimeMaterial(material);
				changed = true;
			}
		}
		if (changed)
		{
			renderer.sharedMaterials = materials;
		}
	}

	public static Material CreateRuntimeMaterial(Material source)
	{
		if (source == null)
		{
			return null;
		}
		Material material = new Material(source);
		material.name = source.name + "_PLA_Runtime";
		CopyMainTexture(source, material);
		CopyVisualProperties(source, material);
		return material;
	}

	public static Material CreateRuntimeMaterial(Material source, Color fallbackColor)
	{
		Material material = CreateRuntimeMaterial(source);
		if (material != null)
		{
			ApplyColor(material, fallbackColor);
		}
		return material;
	}

	public static Material CreateRuntimeMaterial(Color color, string materialName)
	{
		Shader shader = GetFallbackShader();
		if (shader == null)
		{
			return null;
		}
		Material material = new Material(shader);
		material.name = (string.IsNullOrEmpty(materialName) ? "PLA_RuntimeMaterial" : materialName) + "_PLA_Runtime";
		ApplyColor(material, color);
		return material;
	}

	private static Shader GetFallbackShader()
	{
		Shader shader = Shader.Find("PLA/Custom_Cube_Mechanic_Lite");
		if (shader != null)
		{
			return shader;
		}
		shader = Shader.Find("Custom_Cube");
		if (shader != null)
		{
			return shader;
		}
		shader = Shader.Find("Mobile/Diffuse");
		if (shader != null)
		{
			return shader;
		}
		shader = Shader.Find("Universal Render Pipeline/Unlit");
		if (shader != null)
		{
			return shader;
		}
		shader = Shader.Find("Unlit/Color");
		if (shader != null)
		{
			return shader;
		}
		return Shader.Find("Standard");
	}

	private static Color GetReadableColor(Material source)
	{
		Color color = Color.white;
		if (source == null)
		{
			return color;
		}
		if (source.HasProperty(ColorId))
		{
			color = source.GetColor(ColorId);
		}
		else if (source.HasProperty(BaseColorId))
		{
			color = source.GetColor(BaseColorId);
		}
		else if (source.HasProperty(FollowerColorId))
		{
			color = source.GetColor(FollowerColorId);
		}
		if (color.a < 0.05f)
		{
			color.a = 1f;
		}
		return color;
	}

	private static void ApplyColor(Material material, Color color)
	{
		if (!(material == null))
		{
			color = TuneColorForLuna(color);
			if (material.HasProperty(BaseColorId))
			{
				material.SetColor(BaseColorId, color);
			}
			if (material.HasProperty(ColorId))
			{
				material.SetColor(ColorId, color);
			}
			if (material.HasProperty(HighlightColorId))
			{
				material.SetColor(HighlightColorId, Color.Lerp(color, Color.white, 0.38f));
			}
			if (material.HasProperty(ShadowColorId))
			{
				material.SetColor(ShadowColorId, Color.Lerp(color, Color.black, 0.42f));
			}
		}
	}

	private static void CopyVisualProperties(Material source, Material target)
	{
		if (!(source == null) && !(target == null))
		{
			Color readableColor = GetReadableColor(source);
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
			ClampMinimumFloat(target, SpecularIntensityId, 0.35f);
			ClampMinimumFloat(target, ReflectIntensityId, 0.08f);
		}
	}

	private static void CopyColor(Material source, Material target, int sourceId)
	{
		CopyColor(source, target, sourceId, sourceId);
	}

	private static void CopyColor(Material source, Material target, int sourceId, int targetId)
	{
		if (!(source == null) && !(target == null) && source.HasProperty(sourceId) && target.HasProperty(targetId))
		{
			Color color = source.GetColor(sourceId);
			color = TuneColorForLuna(color);
			target.SetColor(targetId, color);
		}
	}

	private static void CopyFloat(Material source, Material target, int propertyId)
	{
		if (!(source == null) && !(target == null) && source.HasProperty(propertyId) && target.HasProperty(propertyId))
		{
			target.SetFloat(propertyId, source.GetFloat(propertyId));
		}
	}

	private static void ClampMinimumFloat(Material target, int propertyId, float minimum)
	{
		if (!(target == null) && target.HasProperty(propertyId) && target.GetFloat(propertyId) < minimum)
		{
			target.SetFloat(propertyId, minimum);
		}
	}

	private static void CopyMainTexture(Material source, Material target)
	{
		if (!(source == null) && !(target == null) && source.HasProperty(MainTexId) && target.HasProperty(MainTexId))
		{
			Texture texture = source.GetTexture(MainTexId);
			if (!(texture == null))
			{
				target.SetTexture(MainTexId, texture);
				target.SetTextureScale(MainTexId, source.GetTextureScale(MainTexId));
				target.SetTextureOffset(MainTexId, source.GetTextureOffset(MainTexId));
			}
		}
	}

	public static Color TuneColorForLuna(Color color)
	{
		float alpha = color.a;
		float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
		if (maxChannel > 0.72f)
		{
			float t = Mathf.InverseLerp(0.72f, 1f, maxChannel);
			float scale = Mathf.Lerp(1f, 0.84f, t);
			color.r *= scale;
			color.g *= scale;
			color.b *= scale;
		}
		color = Color.Lerp(color, Color.black, 0.06f);
		color.a = alpha;
		return color;
	}
}
