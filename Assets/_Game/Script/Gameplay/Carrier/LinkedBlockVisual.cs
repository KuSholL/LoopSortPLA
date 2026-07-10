using UnityEngine;

public sealed class LinkedBlockVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private GameObject modelGO;
    [SerializeField] private EBlockShapeType shapeType;
    [SerializeField] private SpriteRenderer catFace;
    [SerializeField] private BlockSolidProgressAnimator progressAnimator;
    [SerializeField] private Renderer[] keyRenderers;

    [Header("Link Anchors")]
    [SerializeField] private Transform leftLinkAnchor;
    [SerializeField] private Transform rightLinkAnchor;

    public Transform LeftLinkAnchor => leftLinkAnchor;
    public Transform RightLinkAnchor => rightLinkAnchor;

    private bool IsBlock4X => shapeType == EBlockShapeType.Block4x;
    private CarrierBase _carrier;
    private Block _anchorBlock;
    private Collider[] _visualColliders;

    private void Awake()
    {
        DisableVisualColliders();
    }

    private void OnEnable()
    {
        DisableVisualColliders();
    }
    
    public void Apply(
        ColorEntry colorEntry,
        CatColorEntry catEntry,
        bool suppressProgressAnimation = false,
        bool forceFullAnimation = false,
        bool hasKey = false,
        EBlockColorType keyColorType = EBlockColorType.None)
    {
        SetupMaterial(colorEntry);
        SetProgress(1f, suppressProgressAnimation, forceFullAnimation);
        SetVisible(true);
        if (catFace)
        {
            catFace.color = catEntry != null
                ? catEntry.Color
                : colorEntry != null
                    ? colorEntry.Color
                    : Color.white;
        }

        SetKeyVisible(hasKey);
        if (hasKey && keyColorType != EBlockColorType.None)
        {
            SetKeyTexture(keyColorType);
        }
    }

    public void SetKeyVisible(bool isVisible)
    {
        if (keyRenderers == null) return;
        foreach (var r in keyRenderers)
        {
            if (!r) continue;
            r.gameObject.SetActive(isVisible);
            r.enabled = isVisible;
        }
    }

    private void SetKeyTexture(EBlockColorType colorType)
    {
        if (keyRenderers == null) return;
        var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetStylizedColorConfig() : null;
#if UNITY_EDITOR
        if (config == null)
        {
            config = UnityEditor.AssetDatabase.LoadAssetAtPath<StylizedColorConfigSO>("Assets/_Game/Config/CoreGameConfig/StylizedColorConfigSO.asset");
        }
#endif
        var entry = config != null
            ? config.GetColorEntry(colorType)
            : PlayableStylizedColorFallback.CreateColorEntry(colorType);
        if (entry == null) return;
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
                    r.ApplyColor(colorId, entry.Color, i);
                    r.ApplyColor(shadowColorId, entry.ShadowColor, i);
                    r.ApplyColor(specularColorId, entry.SpecularColor, i);
                    r.ApplyColor(reflectColorId, entry.ReflectColor, i);
                }
            }
            else
            {
                r.ApplyColor(colorId, entry.Color, 0);
                r.ApplyColor(shadowColorId, entry.ShadowColor, 0);
                r.ApplyColor(specularColorId, entry.SpecularColor, 0);
                r.ApplyColor(reflectColorId, entry.ReflectColor, 0);
            }
        }
    }

    public Renderer KeyRenderer => keyRenderers != null && keyRenderers.Length > 0 ? keyRenderers[0] : null;

    public Vector3 GetKeyVisualPosition()
    {
        return KeyRenderer != null ? KeyRenderer.transform.position : transform.position;
    }

    public void BindSelectionContext(CarrierBase carrier, Block anchorBlock)
    {
        _carrier = carrier;
        _anchorBlock = anchorBlock;
    }

    public bool MatchesSelection(CarrierBase carrier, Block anchorBlock)
    {
        return _carrier == carrier && _anchorBlock == anchorBlock;
    }

    public void SetVisible(bool isVisible)
    {
        DisableVisualColliders();
        if (!isVisible) SetProgress(1f, true);
        if (modelGO != null) modelGO.SetActive(isVisible);
        else gameObject.SetActive(isVisible);
        if (!isVisible) BindSelectionContext(null, null);
    }

    public void SetProgress(float progress, bool suppressAnimation = false, bool forceFullAnimation = false)
    {
        progress = Mathf.Clamp01(progress);
        var targetTransform = GetVisualTransform();
        if (targetTransform != null)
            targetTransform.localScale = new Vector3(progress <= 0 ? 0 : 1, progress <= 0 ? 0 : 1, progress);
        progressAnimator?.SetProgress(progress, suppressAnimation, forceFullAnimation);
        DisableVisualColliders();
    }

    public void SetLayer(int layer)
    {
        ApplyLayer(gameObject, layer);
    }

    public void PlayBlockedFullAnimation()
    {
        progressAnimator?.ReplayCurrentProgressAnimation(true);
    }

    public void PlayTriggerActiveAnimation()
    {
        progressAnimator?.PlayTriggerActiveAnimation();
    }

    private void SetupMaterial(ColorEntry colorEntry)
    {
        var targetRenderer = GetTargetRenderer();
        if (targetRenderer == null || colorEntry == null) return;

        targetRenderer.ApplyColorEntry(colorEntry);
    }
    
    private Renderer GetTargetRenderer()
    {
        if (IsBlock4X && skinnedMeshRenderer != null) return skinnedMeshRenderer;
        if (meshRenderer != null) return meshRenderer;
        return skinnedMeshRenderer;
    }

    private Transform GetVisualTransform()
    {
        var targetRenderer = GetTargetRenderer();
        return targetRenderer != null ? targetRenderer.transform : null;
    }

    private void DisableVisualColliders()
    {
        if (_visualColliders == null || _visualColliders.Length == 0)
            _visualColliders = GetComponentsInChildren<Collider>(true);

        if (_visualColliders == null) return;
        for (var i = 0; i < _visualColliders.Length; i++)
        {
            var target = _visualColliders[i];
            if (target != null) target.enabled = false;
        }
    }
    
    private static void ApplyLayer(GameObject target, int layer)
    {
        if (target == null) return;
        target.layer = layer;
        var transform = target.transform;
        for (var i = 0; i < transform.childCount; i++)
            ApplyLayer(transform.GetChild(i).gameObject, layer);
    }

}

public enum EBlockShapeType
{
    Block2x,
    Block3x,
    Block4x,
}
