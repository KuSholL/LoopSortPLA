using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

public class GiftBoxVisual : MonoBehaviour
{
    [SerializeField] private Transform giftBoxRenderer;
    [SerializeField] private Transform lidRenderer;
    [SerializeField] private List<MeshRenderer> ribbonRenderers;

    [Header("Lid Animation Settings")]
    [SerializeField] private float lidDuration = 0.25f;
    [SerializeField] private Ease lidEase = Ease.OutQuad;

    [Header("Gift Box Animation Settings")]
    [SerializeField] private float giftBoxDuration = 0.25f;
    [SerializeField] private Ease giftBoxEase = Ease.OutQuad;

    private Vector3 _initialLidScale;
    private Vector3 _initialGiftBoxScale;
    private List<Vector3> _initialRibbonScales = new();
    private bool _hasInitialized;

    private MotionHandle _lidMotionHandle;
    private MotionHandle _giftBoxMotionHandle;
    private List<MotionHandle> _activeRibbonHandles = new();
    private MotionHandle _tieScaleHandle;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        if (_lidMotionHandle.IsActive()) _lidMotionHandle.Cancel();
        if (_giftBoxMotionHandle.IsActive()) _giftBoxMotionHandle.Cancel();
        if (_tieScaleHandle.IsActive()) _tieScaleHandle.Cancel();
        ClearRibbonHandles();
    }

    private void Initialize()
    {
        if (_hasInitialized) return;
        if (lidRenderer != null) _initialLidScale = lidRenderer.localScale;
        if (giftBoxRenderer != null) _initialGiftBoxScale = giftBoxRenderer.localScale;
        
        _initialRibbonScales.Clear();
        if (ribbonRenderers != null)
        {
            foreach (var sr in ribbonRenderers)
            {
                if (sr != null)
                {
                    _initialRibbonScales.Add(sr.transform.localScale);
                    sr.gameObject.SetActive(true);
                }
                else
                {
                    _initialRibbonScales.Add(Vector3.one);
                }
            }
        }
        _hasInitialized = true;
    }

    private void ClearRibbonHandles()
    {
        foreach (var handle in _activeRibbonHandles)
        {
            if (handle.IsActive()) handle.Cancel();
        }
        _activeRibbonHandles.Clear();
    }

    public void ResetVisual()
    {
        Initialize();
        if (_lidMotionHandle.IsActive()) _lidMotionHandle.Cancel();
        if (_giftBoxMotionHandle.IsActive()) _giftBoxMotionHandle.Cancel();
        if (_tieScaleHandle.IsActive()) _tieScaleHandle.Cancel();
        ClearRibbonHandles();
        AlignRibbonRotation();
        if (lidRenderer != null)
        {
            lidRenderer.gameObject.SetActive(true);
            lidRenderer.localScale = _initialLidScale;
        }
        if (giftBoxRenderer != null)
        {
            giftBoxRenderer.gameObject.SetActive(true);
            giftBoxRenderer.localScale = _initialGiftBoxScale;
        }

        if (ribbonRenderers != null)
        {
            for (int i = 0; i < ribbonRenderers.Count; i++)
            {
                if (ribbonRenderers[i] != null)
                {
                    ribbonRenderers[i].gameObject.SetActive(true);
                    if (i < _initialRibbonScales.Count)
                    {
                        ribbonRenderers[i].transform.localScale = _initialRibbonScales[i];
                    }
                }
            }
        }
    }

    public async UniTask PlayOpenAnimationAsync()
    {
        Initialize();
        
        if (_lidMotionHandle.IsActive()) _lidMotionHandle.Cancel();
        if (_giftBoxMotionHandle.IsActive()) _giftBoxMotionHandle.Cancel();
        ClearRibbonHandles();

        if (ribbonRenderers != null)
        {
            for (int i = 0; i <= ribbonRenderers.Count; i++)
            {
                if (i < ribbonRenderers.Count && ribbonRenderers[i] != null)
                {
                    ribbonRenderers[i].gameObject.SetActive(false);
                }
            }
        }
        
        if (lidRenderer != null)
        {
            var startLidScale = lidRenderer.localScale;
            _lidMotionHandle = LMotion.Create(startLidScale.z, 0f, lidDuration)
                .WithEase(lidEase)
                .Bind(z =>
                {
                    if (lidRenderer != null)
                    {
                        var scale = lidRenderer.localScale;
                        scale.z = z;
                        lidRenderer.localScale = scale;
                    }
                });
            await _lidMotionHandle.ToUniTask();
            if (lidRenderer != null)
            {
                lidRenderer.gameObject.SetActive(false);
            }
        }

        if (giftBoxRenderer != null)
        {
            var startBoxScale = giftBoxRenderer.localScale;
            _giftBoxMotionHandle = LMotion.Create(startBoxScale.y, 0f, giftBoxDuration)
                .WithEase(giftBoxEase)
                .Bind(y =>
                {
                    if (giftBoxRenderer != null)
                    {
                        var scale = giftBoxRenderer.localScale;
                        scale.y = y;
                        giftBoxRenderer.localScale = scale;
                    }
                });
            await _giftBoxMotionHandle.ToUniTask();

            if (giftBoxRenderer != null)
            {
                var currentBoxScale = giftBoxRenderer.localScale;
                _giftBoxMotionHandle = LMotion.Create(new Vector2(currentBoxScale.x, currentBoxScale.z), Vector2.zero, giftBoxDuration)
                    .WithEase(giftBoxEase)
                    .Bind(xz =>
                    {
                        if (giftBoxRenderer != null)
                        {
                            var scale = giftBoxRenderer.localScale;
                            scale.x = xz.x;
                            scale.z = xz.y;
                            giftBoxRenderer.localScale = scale;
                        }
                    });
                await _giftBoxMotionHandle.ToUniTask();
            }

            if (giftBoxRenderer != null)
            {
                giftBoxRenderer.gameObject.SetActive(false);
            }
        }
    }

    public void ApplyColor(EBlockColorType colorType)
    {
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

        foreach (var sr in ribbonRenderers)
        {
            if (sr == null) continue;
            int matCount = sr.sharedMaterials != null ? sr.sharedMaterials.Length : 0;
            if (matCount >= 3)
            {
                int[] targetIndices = { 0, 2 };
                foreach (int i in targetIndices)
                {
                    sr.GetPropertyBlock(propertyBlock, i);
                    propertyBlock.SetColor(colorId, entry.Color);
                    propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                    propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                    propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                    sr.SetPropertyBlock(propertyBlock, i);
                }
            }
            else
            {
                sr.GetPropertyBlock(propertyBlock, 0);
                propertyBlock.SetColor(colorId, entry.Color);
                propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
                propertyBlock.SetColor(specularColorId, entry.SpecularColor);
                propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
                sr.SetPropertyBlock(propertyBlock, 0);
            }
        }
    }

    private void Start()
    {
        AlignRibbonRotation();
    }

    private void AlignRibbonRotation()
    {
        if (ribbonRenderers != null && ribbonRenderers.Count > 0 && ribbonRenderers[0] != null)
        {
            ribbonRenderers[0].transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    public void PlayTieScaleAnimation()
    {
        Initialize();
        if (ribbonRenderers == null || ribbonRenderers.Count == 0 || ribbonRenderers[0] == null) return;
        if (_initialRibbonScales.Count == 0) return;

        var targetTransform = ribbonRenderers[0].transform;
        if (_tieScaleHandle.IsActive()) _tieScaleHandle.Cancel();

        // TieScale curve keyframes:
        // 0.0s -> 0.9f
        // 0.0667s -> 1.0217736f
        // 0.2s -> 0.81141126f
        // 0.3333s -> 0.9f
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.06666667f, 1.0217736f),
            new Keyframe(0.2f, 0.81141126f),
            new Keyframe(0.33333334f, 0.9f)
        );
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }

        Vector3 initialScale = _initialRibbonScales[0];

        _tieScaleHandle = LMotion.Create(0f, 0.33333334f, 0.33333334f)
            .Bind(t =>
            {
                if (targetTransform != null)
                {
                    float factor = curve.Evaluate(t) / 0.9f;
                    targetTransform.localScale = initialScale * factor;
                }
            });
    }

    public async UniTask PlayRibbonDisappearAnimation()
    {
        Initialize();
        ClearRibbonHandles();
        if (ribbonRenderers != null)
        {
            var disappearTasks = new List<UniTask>();
            for (int i = 0; i < ribbonRenderers.Count; i++)
            {
                if (ribbonRenderers[i] == null) continue;
                var transformToScale = ribbonRenderers[i].transform;
                var startScale = transformToScale.localScale;
                var handle = LMotion.Create(startScale, Vector3.zero, 0.2f)
                    .WithEase(Ease.InQuad)
                    .Bind(s => {
                        if (transformToScale != null) transformToScale.localScale = s;
                    });
                disappearTasks.Add(handle.ToUniTask());
            }
            if (disappearTasks.Count > 0)
            {
                await UniTask.WhenAll(disappearTasks);
            }
            for (int i = 0; i < ribbonRenderers.Count; i++)
            {
                if (ribbonRenderers[i] != null) ribbonRenderers[i].gameObject.SetActive(false);
            }
        }
    }
}

