using UnityEngine;

public class KeyAnim : MonoBehaviour
{
    [SerializeField] private Renderer[] keyRenderers;

    public void SetKeyTexture(EBlockColorType colorType)
    {
        if (keyRenderers == null) return;
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetStylizedColorConfig() : null;
#if UNITY_EDITOR
        if (config == null)
        {
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<StylizedColorConfigSO>("Assets/_Game/Config/CoreGameConfig/StylizedColorConfigSO.asset");
        }
#endif
        if (config == null) return;
        var entry = config.GetColorEntry(colorType);
        var propertyBlock = new MaterialPropertyBlock();
        var colorId = Shader.PropertyToID("_Color");
        var shadowColorId = Shader.PropertyToID("_ShadowColor");
        var specularColorId = Shader.PropertyToID("_SpecularColor");
        var reflectColorId = Shader.PropertyToID("_ReflectColor");

        foreach (var r in keyRenderers)
        {
            if (!r) continue;
            int matCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 0;
            if (matCount >= 3)
            {
                int[] targetIndices = { 0, 2 };
                foreach (int i in targetIndices)
                {
                    r.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetColor(colorId, entry.Color);
                    propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                    propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                    propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                    r.SetPropertyBlock(propertyBlock, i);
                }
            }
            else
            {
                r.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor(colorId, entry.Color);
                propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                r.SetPropertyBlock(propertyBlock, 0);
            }
        }
    }
}
