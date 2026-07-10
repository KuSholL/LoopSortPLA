using UnityEngine;

public sealed class KeyAnim : MonoBehaviour
{
	[SerializeField]
	private Renderer[] keyRenderers;

	public void SetKeyTexture(EBlockColorType colorType)
	{
		StylizedColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetStylizedColorConfig() : null);
		if (config == null || keyRenderers == null)
		{
			return;
		}
		StylizedColorEntry entry = config.GetColorEntry(colorType);
		if (entry == null)
		{
			return;
		}
		MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		for (int rendererIndex = 0; rendererIndex < keyRenderers.Length; rendererIndex++)
		{
			Renderer target = keyRenderers[rendererIndex];
			if (!(target == null))
			{
				for (int materialIndex = 0; materialIndex < target.sharedMaterials.Length; materialIndex++)
				{
					target.GetPropertyBlock(propertyBlock, materialIndex);
					propertyBlock.SetColorEntry(entry);
					target.SetPropertyBlock(propertyBlock, materialIndex);
				}
			}
		}
	}
}
