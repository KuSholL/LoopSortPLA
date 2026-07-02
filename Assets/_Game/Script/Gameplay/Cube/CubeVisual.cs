using UnityEngine;

public class CubeVisual : MonoBehaviour
{
    [SerializeField] private Renderer cubeRenderer;
    private MaterialPropertyBlock _materialBlock;

    private void OnValidate()
    {
        if (cubeRenderer == null) cubeRenderer = GetComponentInChildren<Renderer>();
    }

    public void Setup(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetCubeColorConfig() : null;
        var entry = config ? config.GetColorEntry(colorType) : null;
        ApplyEntry(entry);
    }

    private void ApplyEntry(ColorEntry entry)
    {
        if (!cubeRenderer) return;
        if (_materialBlock == null) _materialBlock = new MaterialPropertyBlock();
        cubeRenderer.GetPropertyBlock(_materialBlock);
        _materialBlock.SetColorEntry(entry);
        cubeRenderer.SetPropertyBlock(_materialBlock);
        cubeRenderer.enabled = true;
    }
}
