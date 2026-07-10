using UnityEngine;

public class HiddenCube : MonoBehaviour
{
	[SerializeField]
	private BoxCollider cubeCollider;

	[SerializeField]
	private Renderer cubeRenderer;

	private static readonly int ColorId = Shader.PropertyToID("_Color");

	private static readonly int ShadowColorId = Shader.PropertyToID("_SColor");

	private void OnValidate()
	{
		if (cubeCollider == null)
		{
			cubeCollider = GetComponentInChildren<BoxCollider>();
		}
		if (cubeRenderer == null)
		{
			cubeRenderer = GetComponentInChildren<Renderer>();
		}
	}

	public void SetupColor(Color color)
	{
		SetupColor(color, color);
	}

	private void SetupColor(Color color, Color shadowColor)
	{
		if (!(cubeRenderer == null))
		{
			cubeRenderer.ApplyColor(ColorId, color);
			cubeRenderer.ApplyColor(ShadowColorId, shadowColor);
			cubeRenderer.enabled = true;
		}
	}
}
