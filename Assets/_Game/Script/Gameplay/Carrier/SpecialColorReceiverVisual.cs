using System.Collections.Generic;
using UnityEngine;

public class SpecialColorReceiverVisual : CarrierMechanicVisual
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SColorId = Shader.PropertyToID("_SColor");
    private static readonly int SpecularColorId = Shader.PropertyToID("_SpecularColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColorVertex");

    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    private MaterialPropertyBlock _materialBlock;

    private void OnValidate()
    {
        if (targetRenderers.Count == 0) targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }

    public override void ApplyVisualRequest(CarrierVisualRequest request)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetSpecialColorConfig() : null;
        var colorType = request != null ? request.ColorType : EBlockColorType.None;
        var colorEntry = config != null ? config.GetColorEntry(colorType) : null;
        if (colorEntry == null) return;

        if (_materialBlock == null) _materialBlock = new MaterialPropertyBlock();

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            var targetRenderer = targetRenderers[i];
            if (targetRenderer == null) continue;

            targetRenderer.GetPropertyBlock(_materialBlock, 0);
            _materialBlock.SetColor(ColorId, colorEntry.Color);
            _materialBlock.SetColor(SColorId, colorEntry.ShadowColor);
            _materialBlock.SetColor(SpecularColorId, colorEntry.SpecularColor);
            _materialBlock.SetColor(OutlineColorId, colorEntry.OutlineColor);
            targetRenderer.SetPropertyBlock(_materialBlock, 0);
        }
    }
}
