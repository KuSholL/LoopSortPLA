using System;
using DG.Tweening;
using UnityEngine;

public class BlockSolidVisual : MonoBehaviour
{
    [SerializeField] private Block parentBlock;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SpriteRenderer questionMarkRenderer;
    [SerializeField] private Renderer[] keyRenderers;
    [SerializeField] private Renderer swapArrowRenderer;
    [SerializeField] private BlockSolidProgressAnimator progressAnimator;
    [SerializeField] private float progressTweenDuration = 0.2f;

    [Header("First Cube Scale Config")]
    [SerializeField] private bool useFirstCubeScaleConfig = true;
    [SerializeField] [Range(0f, 1f)] private float firstCubeScalePercent = 0.05f;

    public Renderer KeyRenderer => keyRenderers != null && keyRenderers.Length > 0 ? keyRenderers[0] : null;
    private MaterialPropertyBlock _materialBlock;
    private Tween _progressTween;
    private float _currentProgress;

    private void OnValidate()
    {
        parentBlock = GetComponentInParent<Block>();
    }

    private void Awake()
    {
        if (parentBlock == null) parentBlock = GetComponentInParent<Block>();
        if (meshRenderer)
        {
            _currentProgress = Mathf.Clamp01(meshRenderer.transform.localScale.z);
        }
    }

    private void OnDisable()
    {
        CancelProgressTween();
    }

    private void Start()
    {
        questionMarkRenderer.transform.rotation = Quaternion.Euler(90, 0, 0f);
    }

    private void SetupMaterial(ColorEntry colorEntry)
    {
        if (colorEntry == null) return;
        if (_materialBlock == null) _materialBlock = new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_materialBlock);
        _materialBlock.SetColorEntry(colorEntry);
        meshRenderer.SetPropertyBlock(_materialBlock);
    }

    public void ApplyNormalVisual(Material material, ColorEntry colorEntry)
    {
        SetSharedMaterial(material);
        SetupMaterial(colorEntry);
    }

    public void ApplyMechanicVisual(Material material)
    {
        SetSharedMaterial(material);
        ClearPropertyBlocks();
    }

    public void ResetVisual(Material defaultMaterial)
    {
        SetSharedMaterial(defaultMaterial);
        ClearPropertyBlocks();
    }

    public void SetVisible(bool isVisible)
    {
        meshRenderer.enabled = isVisible;
    }

    public void SetQuestionMark(bool isHiddenBlock)
    {
        questionMarkRenderer.enabled = isHiddenBlock;
        questionMarkRenderer.transform.rotation = Quaternion.Euler(90, 0, 0f);
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

    public void SetSwapArrowVisible(bool isVisible)
    {
        if (swapArrowRenderer != null)
        {
            swapArrowRenderer.gameObject.SetActive(isVisible);
            swapArrowRenderer.enabled = isVisible;
        }
    }

    public void SetSwapArrowColor(EBlockColorType colorType, ColorConfigSO colorConfig)
    {
        if (swapArrowRenderer == null || colorConfig == null) return;
        var entry = colorConfig.GetColorEntry(colorType);
        if (entry != null)
        {
            var propBlock = new MaterialPropertyBlock();
            swapArrowRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColorEntry(entry);
            swapArrowRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void PlaySwapRotateAnimation()
    {
        if (swapArrowRenderer == null) return;
        var arrowTransform = swapArrowRenderer.transform;
        var currentRotation = arrowTransform.localEulerAngles;
        arrowTransform.DOKill();
        DOTween.To(
                () => currentRotation.y,
                y => arrowTransform.localRotation = Quaternion.Euler(90f, y, 0f),
                currentRotation.y + 180f,
                0.3f)
            .SetEase(Ease.OutQuad)
            .SetTarget(arrowTransform);
    }

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

    public void SetProgress(float progress, bool suppressAnimation = false, bool forceFullAnimation = false)
    {
        if (!meshRenderer) return;
        progress = Mathf.Clamp01(progress);
        if (suppressAnimation || !Application.isPlaying || progressTweenDuration <= 0f || progress <= 0f)
        {
            CancelProgressTween();
            ApplyScaleProgress(progress);
        }
        else
        {
            StartScaleProgressTween(progress);
        }
        if (suppressAnimation)
        {
            progressAnimator?.SetProgress(progress, true);
            return;
        }

        progressAnimator?.SetProgress(progress, false, forceFullAnimation);
    }

    private void StartScaleProgressTween(float targetProgress)
    {
        CancelProgressTween();
        var currentProgress = _currentProgress;
        _progressTween = DOTween.To(
                () => currentProgress,
                ApplyScaleProgress,
                targetProgress,
                progressTweenDuration)
            .SetEase(Ease.OutQuad)
            .SetTarget(this);
    }

    private void ApplyScaleProgress(float progress)
    {
        if (!meshRenderer) return;
        _currentProgress = progress;
        var mappedProgress = GetMappedProgress(progress);
        meshRenderer.transform.localScale = new Vector3(progress <= 0f ? 0f : 1f, progress <= 0f ? 0f : 1f, mappedProgress);
    }

    private float GetMappedProgress(float progress)
    {
        if (!useFirstCubeScaleConfig) return progress;
        
        if (parentBlock == null)
        {
            parentBlock = GetComponentInParent<Block>();
        }

        if (parentBlock == null) return progress;

        int maxCubes = parentBlock.GetMaxCubes();
        if (maxCubes <= 1) return progress;

        float p1 = 1f / maxCubes;
        if (progress <= p1)
        {
            return p1 > 0f ? (progress / p1) * firstCubeScalePercent : 0f;
        }
        else
        {
            float denominator = 1f - p1;
            return denominator > 0f 
                ? firstCubeScalePercent + ((progress - p1) / denominator) * (1f - firstCubeScalePercent)
                : progress;
        }
    }

    private void CancelProgressTween()
    {
        if (_progressTween != null)
        {
            _progressTween.Kill();
            _progressTween = null;
        }
    }

    private void SetSharedMaterial(Material material)
    {
        if (!meshRenderer) return;
        var sharedMaterials = meshRenderer.sharedMaterials;
        if (sharedMaterials == null || sharedMaterials.Length == 0)
        {
            meshRenderer.sharedMaterial = material;
            return;
        }

        for (var i = 0; i < sharedMaterials.Length; i++)
            sharedMaterials[i] = material;
        meshRenderer.sharedMaterials = sharedMaterials;
    }

    private void ClearPropertyBlocks()
    {
        if (meshRenderer) meshRenderer.SetPropertyBlock(null);
    }

}
