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
		if (entry != null)
		{
			renderer.ApplyColor(Color, entry.Color, materialIndex);
			renderer.ApplyColor(ShadowColor, entry.ShadowColor, materialIndex);
			renderer.ApplyColor(SpecularColor, entry.SpecularColor, materialIndex);
			renderer.ApplyColor(RimColor, entry.RimColor, materialIndex);
			renderer.ApplyColor(MatCapColor, entry.MatCapColor, materialIndex);
			renderer.ApplyColor(OutlineColor, entry.OutlineColor, materialIndex);
		}
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
		if (entry != null)
		{
			renderer.ApplyColor(Color, entry.Color, materialIndex);
			renderer.ApplyColor(StylizedShadowColor, entry.ShadowColor, materialIndex);
			renderer.ApplyColor(SpecularColor, entry.SpecularColor, materialIndex);
			renderer.ApplyColor(ReflectColor, entry.ReflectColor, materialIndex);
		}
	}

	public static void ApplyColor(this Renderer renderer, int propertyId, Color value, int materialIndex = -1)
	{
		if (renderer == null)
		{
			return;
		}
		value = LunaMaterialUtility.TuneColorForLuna(value);
		Material[] materials = EnsureRuntimeMaterials(renderer);
		if (materials == null || materials.Length == 0)
		{
			return;
		}
		if (materialIndex >= 0)
		{
			if (materialIndex < materials.Length && !(materials[materialIndex] == null))
			{
				materials[materialIndex].SetColor(propertyId, value);
			}
			return;
		}
		for (int i = 0; i < materials.Length; i++)
		{
			if (materials[i] != null)
			{
				materials[i].SetColor(propertyId, value);
			}
		}
	}

	public static void ApplyFloat(this Renderer renderer, int propertyId, float value, int materialIndex = -1)
	{
		if (renderer == null)
		{
			return;
		}
		Material[] materials = EnsureRuntimeMaterials(renderer);
		if (materials == null || materials.Length == 0)
		{
			return;
		}
		if (materialIndex >= 0)
		{
			if (materialIndex < materials.Length && !(materials[materialIndex] == null))
			{
				materials[materialIndex].SetFloat(propertyId, value);
			}
			return;
		}
		for (int i = 0; i < materials.Length; i++)
		{
			if (materials[i] != null)
			{
				materials[i].SetFloat(propertyId, value);
			}
		}
	}

	private static Material[] EnsureRuntimeMaterials(Renderer renderer)
	{
		if (renderer == null)
		{
			return null;
		}
		Material[] materials = renderer.sharedMaterials;
		if (materials == null || materials.Length == 0)
		{
			return materials;
		}
		bool changed = false;
		for (int i = 0; i < materials.Length; i++)
		{
			Material material = materials[i];
			if (!(material == null) && !material.name.EndsWith("_PLA_Runtime"))
			{
				materials[i] = LunaMaterialUtility.CreateRuntimeMaterial(material);
				changed = true;
			}
		}
		if (changed)
		{
			renderer.sharedMaterials = materials;
		}
		return materials;
	}
}
