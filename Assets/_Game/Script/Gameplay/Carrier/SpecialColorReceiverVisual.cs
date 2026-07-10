using System.Collections.Generic;
using UnityEngine;

public class SpecialColorReceiverVisual : CarrierMechanicVisual
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SColorId = Shader.PropertyToID("_SColor");
    private static readonly int SpecularColorId = Shader.PropertyToID("_SpecularColor");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColorVertex");

    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    private void OnValidate()
    {
        if (targetRenderers.Count == 0) targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
    }

    public override void ApplyVisualRequest(CarrierVisualRequest request)
    {
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetSpecialColorConfig() : null;
        var colorType = request != null ? request.ColorType : EBlockColorType.None;
        var colorEntry = config != null
            ? config.GetColorEntry(colorType)
            : PlayableColorFallback.CreateColorEntry(colorType);
        if (colorEntry == null) return;

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            var targetRenderer = targetRenderers[i];
            if (targetRenderer == null) continue;

            targetRenderer.ApplyColor(ColorId, colorEntry.Color, 0);
            targetRenderer.ApplyColor(SColorId, colorEntry.ShadowColor, 0);
            targetRenderer.ApplyColor(SpecularColorId, colorEntry.SpecularColor, 0);
            targetRenderer.ApplyColor(OutlineColorId, colorEntry.OutlineColor, 0);
        }
    }
}
