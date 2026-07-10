using UnityEngine;

public class CubeVisual : MonoBehaviour
{
	[SerializeField]
	private Renderer cubeRenderer;

	private MaterialPropertyBlock _materialBlock;

	private void OnValidate()
	{
		if (cubeRenderer == null)
		{
			cubeRenderer = GetComponentInChildren<Renderer>();
		}
	}

	public void Setup(EBlockColorType colorType)
	{
		ColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCubeColorConfig() : null);
		ColorEntry entry = (config ? config.GetColorEntry(colorType) : null);
		ApplyEntry(entry);
	}

	private void ApplyEntry(ColorEntry entry)
	{
		if ((bool)cubeRenderer)
		{
			if (_materialBlock == null)
			{
				_materialBlock = new MaterialPropertyBlock();
			}
			cubeRenderer.GetPropertyBlock(_materialBlock);
			_materialBlock.SetColorEntry(entry);
			cubeRenderer.SetPropertyBlock(_materialBlock);
			cubeRenderer.enabled = true;
		}
	}
}
