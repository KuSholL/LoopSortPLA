using UnityEngine;

public class CubeVisual : MonoBehaviour
{
	[SerializeField]
	private Renderer cubeRenderer;

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
		ColorEntry entry = (config ? config.GetColorEntry(colorType) : PlayableColorFallback.CreateColorEntry(colorType));
		ApplyEntry(entry);
	}

	private void ApplyEntry(ColorEntry entry)
	{
		if ((bool)cubeRenderer)
		{
			cubeRenderer.ApplyColorEntry(entry);
			cubeRenderer.enabled = true;
		}
	}
}
