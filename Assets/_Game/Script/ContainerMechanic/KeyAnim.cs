using UnityEngine;

public sealed class KeyAnim : MonoBehaviour
{
    [SerializeField] private Renderer[] keyRenderers;

    public void SetKeyTexture(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetStylizedColorConfig()
            : null;
        if (config == null || keyRenderers == null) return;
        var entry = config.GetColorEntry(colorType);
        if (entry == null) return;

        for (var rendererIndex = 0; rendererIndex < keyRenderers.Length; rendererIndex++)
        {
            var target = keyRenderers[rendererIndex];
            if (target == null) continue;
            for (var materialIndex = 0; materialIndex < target.sharedMaterials.Length; materialIndex++)
            {
                target.ApplyColorEntry(entry, materialIndex);
            }
        }
    }
}
