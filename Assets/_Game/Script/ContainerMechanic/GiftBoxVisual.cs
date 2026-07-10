using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class GiftBoxVisual : MonoBehaviour
{
    [SerializeField] private Transform giftBoxRenderer;
    [SerializeField] private Transform lidRenderer;
    [SerializeField] private List<MeshRenderer> ribbonRenderers = new List<MeshRenderer>();
    [SerializeField] private float lidDuration = 0.25f;
    [SerializeField] private DG.Tweening.Ease lidEase = DG.Tweening.Ease.OutQuad;
    [SerializeField] private float giftBoxDuration = 0.25f;
    [SerializeField] private DG.Tweening.Ease giftBoxEase = DG.Tweening.Ease.OutQuad;

    private Vector3 _initialLidScale;
    private Vector3 _initialGiftBoxScale;
    private readonly List<Vector3> _initialRibbonScales = new List<Vector3>();
    private Sequence _sequence;
    private Tween _tieTween;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        KillAnimation();
    }

    public void ResetVisual()
    {
        Initialize();
        KillAnimation();
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
        for (var i = 0; i < ribbonRenderers.Count; i++)
        {
            var ribbon = ribbonRenderers[i];
            if (ribbon == null) continue;
            ribbon.gameObject.SetActive(true);
            ribbon.transform.localScale = i < _initialRibbonScales.Count
                ? _initialRibbonScales[i]
                : Vector3.one;
        }
        AlignRibbonRotation();
    }

    public void PlayOpenAnimation(Action onComplete)
    {
        KillAnimation();
        _sequence = DOTween.Sequence();
        for (var i = 0; i < ribbonRenderers.Count; i++)
        {
            if (ribbonRenderers[i] != null) ribbonRenderers[i].gameObject.SetActive(false);
        }

        if (lidRenderer != null)
        {
            _sequence.Append(lidRenderer
                .DOScale(new Vector3(lidRenderer.localScale.x, lidRenderer.localScale.y, 0f), lidDuration)
                .SetEase(lidEase));
            _sequence.AppendCallback(() => lidRenderer.gameObject.SetActive(false));
        }

        if (giftBoxRenderer != null)
        {
            _sequence.Append(giftBoxRenderer
                .DOScale(new Vector3(giftBoxRenderer.localScale.x, 0f, giftBoxRenderer.localScale.z), giftBoxDuration)
                .SetEase(giftBoxEase));
            _sequence.Append(giftBoxRenderer.DOScale(Vector3.zero, giftBoxDuration).SetEase(giftBoxEase));
            _sequence.AppendCallback(() => giftBoxRenderer.gameObject.SetActive(false));
        }

        _sequence.OnComplete(() =>
        {
            _sequence = null;
            onComplete?.Invoke();
        });
    }

    public void PlayRibbonDisappearAnimation(Action onComplete)
    {
        KillAnimation();
        var hasRibbon = false;
        for (var i = 0; i < ribbonRenderers.Count; i++)
        {
            var ribbon = ribbonRenderers[i];
            if (ribbon == null) continue;
            hasRibbon = true;
        }
        if (!hasRibbon)
        {
            onComplete?.Invoke();
            return;
        }
        _sequence = DOTween.Sequence();
        for (var i = 0; i < ribbonRenderers.Count; i++)
        {
            var ribbon = ribbonRenderers[i];
            if (ribbon == null) continue;
            _sequence.Join(ribbon.transform.DOScale(Vector3.zero, 0.2f).SetEase(DG.Tweening.Ease.InQuad));
        }
        _sequence.OnComplete(() =>
        {
            for (var i = 0; i < ribbonRenderers.Count; i++)
            {
                if (ribbonRenderers[i] != null) ribbonRenderers[i].gameObject.SetActive(false);
            }
            _sequence = null;
            onComplete?.Invoke();
        });
    }

    public void PlayTieScaleAnimation()
    {
        Initialize();
        if (ribbonRenderers.Count == 0 || ribbonRenderers[0] == null) return;
        var target = ribbonRenderers[0].transform;
        var initial = _initialRibbonScales.Count > 0
            ? _initialRibbonScales[0]
            : target.localScale;
        if (_tieTween != null) _tieTween.Kill();
        target.localScale = initial;
        _tieTween = DOTween.Sequence()
            .Append(target.DOScale(initial * 1.13f, 0.08f).SetEase(DG.Tweening.Ease.OutQuad))
            .Append(target.DOScale(initial, 0.08f).SetEase(DG.Tweening.Ease.OutQuad))
            .OnComplete(() => _tieTween = null);
    }

    public void ApplyColor(EBlockColorType colorType)
    {
        var config = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetStylizedColorConfig()
            : null;
        if (config == null) return;
        var entry = config.GetColorEntry(colorType);
        if (entry == null) return;

        var propertyBlock = new MaterialPropertyBlock();
        for (var rendererIndex = 0; rendererIndex < ribbonRenderers.Count; rendererIndex++)
        {
            var target = ribbonRenderers[rendererIndex];
            if (target == null) continue;
            for (var materialIndex = 0; materialIndex < target.sharedMaterials.Length; materialIndex++)
            {
                target.GetPropertyBlock(propertyBlock, materialIndex);
                propertyBlock.SetColorEntry(entry);
                target.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (lidRenderer != null) _initialLidScale = lidRenderer.localScale;
        if (giftBoxRenderer != null) _initialGiftBoxScale = giftBoxRenderer.localScale;
        _initialRibbonScales.Clear();
        for (var i = 0; i < ribbonRenderers.Count; i++)
        {
            _initialRibbonScales.Add(
                ribbonRenderers[i] != null
                    ? ribbonRenderers[i].transform.localScale
                    : Vector3.one);
        }
    }

    private void AlignRibbonRotation()
    {
        if (ribbonRenderers.Count > 0 && ribbonRenderers[0] != null)
            ribbonRenderers[0].transform.rotation = Quaternion.identity;
    }

    private void KillAnimation()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
            _sequence = null;
        }
        if (_tieTween != null)
        {
            _tieTween.Kill();
            _tieTween = null;
        }
    }
}
