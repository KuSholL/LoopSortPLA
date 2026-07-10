using UnityEngine;
using System;

public sealed class CarrierVisualController
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly Transform _carrierRoot;
    private readonly Renderer _carrierRenderer;
    private readonly Transform _hiddenVisualRoot;
    private readonly CarrierMechanicVisualConfigSO _mechanicVisualConfig;
    private readonly MeshRenderer[] _carrierMeshRenderers;
    private readonly Action _onHiddenVisualFlyOutStarted;
    private readonly Action _onHiddenVisualDisappearCompleted;
    private CarrierMechanicVisual _spawnedVisual;
    private ECarrierVisualKind _spawnedVisualKind = ECarrierVisualKind.None;

    public CarrierVisualController(
        Transform carrierRoot,
        Transform hiddenVisualRoot,
        CarrierMechanicVisualConfigSO mechanicVisualConfig,
        MeshRenderer[] carrierMeshRenderers,
        Action onHiddenVisualFlyOutStarted,
        Action onHiddenVisualDisappearCompleted)
    {
        _carrierRoot = carrierRoot;
        _carrierRenderer = carrierRoot != null ? carrierRoot.GetComponent<Renderer>() : null;
        _hiddenVisualRoot = hiddenVisualRoot;
        _mechanicVisualConfig = mechanicVisualConfig;
        _carrierMeshRenderers = carrierMeshRenderers;
        _onHiddenVisualFlyOutStarted = onHiddenVisualFlyOutStarted;
        _onHiddenVisualDisappearCompleted = onHiddenVisualDisappearCompleted;
    }

    public void Reset()
    {
        ClearVisual(animate: false);
        ClearSpecialColorTint();
        SetCarrierRenderersVisible(true);
    }

    public void ApplyVisualRequest(CarrierVisualRequest request)
    {
        var hasVisual = request != null && request.Kind != ECarrierVisualKind.None;
        if (hasVisual)
        {
            EnsureVisual(request.Kind);
            _spawnedVisual?.ApplyVisualRequest(request);
            SetCarrierRenderersVisible(request.HideCarrierRenderers == false);
            return;
        }

        ClearVisual(animate: true);
        SetCarrierRenderersVisible(true);
    }

    public void SetSpecialColorTint(Color? tintColor)
    {
        if (!tintColor.HasValue)
        {
            ClearSpecialColorTint();
            return;
        }

        if (_carrierRenderer == null) return;

        _carrierRenderer.ApplyColor(ColorId, tintColor.Value);
        _carrierRenderer.ApplyColor(BaseColorId, tintColor.Value);
    }

    private void EnsureVisual(ECarrierVisualKind kind)
    {
        if (_spawnedVisual != null && _spawnedVisualKind == kind) return;
        ClearVisual(animate: false);
        var prefab = _mechanicVisualConfig != null ? _mechanicVisualConfig.GetVisualPrefab(kind) : null;
        if (prefab == null) return;

        var instanceObject = UnityEngine.Object.Instantiate(prefab.gameObject, GetHiddenVisualRoot());
        _spawnedVisual = instanceObject.GetComponent<CarrierMechanicVisual>();
        if (_spawnedVisual == null)
        {
            UnityEngine.Object.Destroy(instanceObject);
            return;
        }
        _spawnedVisualKind = kind;
        _spawnedVisual.transform.localPosition = Vector3.zero;
        _spawnedVisual.transform.localRotation = Quaternion.identity;
        _spawnedVisual.transform.localScale = Vector3.one;
        LunaMaterialUtility.NormalizeRenderers(_spawnedVisual.gameObject);
    }

    private void ClearVisual(bool animate = true)
    {
        if (_spawnedVisual == null) return;

        var visualToClear = _spawnedVisual;
        var visualKindToClear = _spawnedVisualKind;
        _spawnedVisual = null;
        _spawnedVisualKind = ECarrierVisualKind.None;

        if (Application.isPlaying)
        {
            if (animate)
            {
                if (visualKindToClear == ECarrierVisualKind.HiddenShell)
                    visualToClear.SetBeforeDisappearCallback(_onHiddenVisualFlyOutStarted);
                PlayAndPool(visualToClear, visualKindToClear);
            }
            else
            {
                UnityEngine.Object.Destroy(visualToClear.gameObject);
            }
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(visualToClear.gameObject);
        }
    }

    private void PlayAndPool(CarrierMechanicVisual visual, ECarrierVisualKind visualKind)
    {
        if (visual == null) return;
        visual.PlayDisappearAnimation(() =>
        {
            if (visualKind == ECarrierVisualKind.HiddenShell)
                _onHiddenVisualDisappearCompleted?.Invoke();

            if (visual != null)
                UnityEngine.Object.Destroy(visual.gameObject);
        });
    }

    private void SetCarrierRenderersVisible(bool isVisible)
    {
        if (_carrierMeshRenderers != null)
        {
            for (int i = 0; i < _carrierMeshRenderers.Length; i++)
            {
                var renderer = _carrierMeshRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }
    }

    private Transform GetHiddenVisualRoot()
    {
        return _hiddenVisualRoot != null ? _hiddenVisualRoot : _carrierRoot;
    }

    private void ClearSpecialColorTint()
    {
        // Normal carrier material is restored by the next visual setup/tint update.
    }
}
