using UnityEngine;

public class CubeVisual : MonoBehaviour
{
    [SerializeField] private Renderer cubeRenderer;

    private void OnValidate()
    {
        if (cubeRenderer == null) cubeRenderer = GetComponentInChildren<Renderer>();
    }

    public void Setup(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetCubeColorConfig() : null;
        var entry = config ? config.GetColorEntry(colorType) : PlayableColorFallback.CreateColorEntry(colorType);
        ApplyEntry(entry);
    }

    private void ApplyEntry(ColorEntry entry)
    {
        if (!cubeRenderer) return;
        cubeRenderer.ApplyColorEntry(entry);
        cubeRenderer.enabled = true;
    }
}
