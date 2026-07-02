using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public enum ActiveCameraPlace
{
    Shop,
    SettingInGame,
    KeepPlaying,
    OfferPopup,
    RemoveAdsPopup,
    MainMenu,
}

public class CameraManager : MonoSingleton<CameraManager>
{
    private static readonly string[] HighlightLayerNames =
        { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };
    private static readonly string[] SelectedHighlightLayerNames =
        { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };
    private const string CarrierLayerName = "Carrier";
    private const string ClawMachineLayerName = "ClawMachine";
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera highlightCamera;
    private int _defaultHighlightMask;
    
    private Vector3 _originalMainCameraPos;
    private CancellationTokenSource _shakeCts;
    
    private List<ActiveCameraPlace> _activeCameraPlaces = new ();


    public Camera MainCamera => mainCamera;

    private void Awake()
    {
        _defaultHighlightMask = highlightCamera != null ? highlightCamera.cullingMask : 0;
        if (mainCamera != null) _originalMainCameraPos = mainCamera.transform.localPosition;
        
        GameEventBus.OnActiveCameraGameplay += SetActiveCameraPlace;
    }

    private void OnDestroy()
    {
        GameEventBus.OnActiveCameraGameplay -= SetActiveCameraPlace;
    }
    
    private void SetActiveCameraPlace(ActiveCameraPlace place, bool isActive)
    {
        if (!isActive)
        {
            if (!_activeCameraPlaces.Contains(place))
            {
                _activeCameraPlaces.Add(place);
            }
        }
        else
        {
            if (_activeCameraPlaces.Contains(place))
            {
                _activeCameraPlaces.Remove(place);
            }
        }
            
        ActiveCameraGameplay(_activeCameraPlaces.Count == 0);
    }
    
    private void ActiveCameraGameplay(bool isActive)
    {
        mainCamera.enabled = isActive;
    }

    public async UniTask ShakeCamera(float duration, float magnitude)
    {
        _shakeCts?.Cancel();
        _shakeCts?.Dispose();
        _shakeCts = new CancellationTokenSource();

        await ShakeCameraAsync(duration, magnitude, _shakeCts.Token);
    }

    private async UniTask ShakeCameraAsync(float duration, float magnitude, CancellationToken cancellationToken)
    {
        if (mainCamera == null) return;

        var mainTransform = mainCamera.transform;
        float elapsed = 0.0f;

        try
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                Vector3 randomOffset = Random.insideUnitSphere * magnitude;
                randomOffset.z = 0f;

                mainTransform.localPosition = _originalMainCameraPos + randomOffset;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        finally
        {
            if (mainTransform != null) mainTransform.localPosition = _originalMainCameraPos;
        }
    }

    public void SetHighlightCameraActive(bool active, params int[] sizes)
    {
        if (highlightCamera != null) highlightCamera.gameObject.SetActive(active);
        if (active) SetHighlightCameraMaskBySize(sizes);
        GameEventBus.OnHighlightCameraActiveChanged?.Invoke(active);
    }

    public void ResetHighlightCameraMask()
    {
        if (highlightCamera == null) return;
        highlightCamera.cullingMask = _defaultHighlightMask;
    }

    public void SetHighlightCameraToCarrier()
    {
        if (highlightCamera == null) return;
        var layer = LayerMask.NameToLayer(CarrierLayerName);
        var mask = layer < 0 ? 0 : 1 << layer;
        highlightCamera.cullingMask = AddClawMachineLayer(mask);
    }

    public void SetHighlightCameraToCarrierAndHighlightTargets()
    {
        if (highlightCamera == null) return;

        var mask = 0;
        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        if (carrierLayer >= 0) mask |= 1 << carrierLayer;

        foreach (var layerName in SelectedHighlightLayerNames)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) mask |= 1 << layer;
        }

        highlightCamera.cullingMask = AddClawMachineLayer(mask);
    }

    public void SetHighlightCameraToCarrierAndSelectedSource()
    {
        if (highlightCamera == null) return;
        var mask = 0;
        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        if (carrierLayer >= 0) mask |= 1 << carrierLayer;
        foreach (var layerName in SelectedHighlightLayerNames)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) mask |= 1 << layer;
        }
        highlightCamera.cullingMask = AddClawMachineLayer(mask);
    }

    private void SetHighlightCameraMaskBySize(params int[] sizes)
    {
        if (highlightCamera == null) return;
        var mask = 0;
        foreach (var size in sizes)
        {
            if (size < 1 || size > HighlightLayerNames.Length) continue;
            var layer = LayerMask.NameToLayer(HighlightLayerNames[size - 1]);
            if (layer < 0) continue;
            mask |= 1 << layer;
        }
        highlightCamera.cullingMask = AddClawMachineLayer(mask);
    }

    private static int AddClawMachineLayer(int mask)
    {
        var layer = LayerMask.NameToLayer(ClawMachineLayerName);
        return layer >= 0 ? mask | (1 << layer) : mask;
    }

    public void SyncOrthographicCamera(int orthographicSize)
    {
        if (!mainCamera || !highlightCamera) return;
        mainCamera.orthographicSize = orthographicSize;
        highlightCamera.orthographicSize = orthographicSize;
    }
}
