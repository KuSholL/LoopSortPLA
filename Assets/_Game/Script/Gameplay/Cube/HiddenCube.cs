using UnityEngine;

public class HiddenCube : MonoBehaviour
{
    [SerializeField] private BoxCollider cubeCollider;
    [SerializeField] private Renderer cubeRenderer;
    private MaterialPropertyBlock _materialBlock;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ShadowColorId = Shader.PropertyToID("_SColor");

    private void OnValidate()
    {
        if (cubeCollider == null) cubeCollider = GetComponentInChildren<BoxCollider>();
        if (cubeRenderer == null) cubeRenderer = GetComponentInChildren<Renderer>();
    }

    public void SetupColor(Color color)
    {
        SetupColor(color, color);
    }

    private void SetupColor(Color color, Color shadowColor)
    {
        if (cubeRenderer == null) return;
        if (_materialBlock == null) _materialBlock = new MaterialPropertyBlock();
        cubeRenderer.GetPropertyBlock(_materialBlock);
        _materialBlock.SetColor(ColorId, color);
        _materialBlock.SetColor(ShadowColorId, shadowColor);
        cubeRenderer.SetPropertyBlock(_materialBlock);
        cubeRenderer.enabled = true;
    }
}
