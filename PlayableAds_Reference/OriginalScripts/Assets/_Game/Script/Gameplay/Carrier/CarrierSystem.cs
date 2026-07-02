using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class CarrierSystem : MonoSingleton<CarrierSystem>
{
    private const string CarrierLayerName = "Carrier";
    private const string DefaultLayerName = "Default";
    private static readonly string[] SourceLayerNames = { "Slime1x", "Slime2x", "Slime3x", "Slime4x" };
    private static readonly string[] HighlightLayerNames = { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };
    
    [SerializeField] private CarrierSpawner carrierSpawner;
    [SerializeField] private ClawBoosterAnimator clawBoosterAnimator;
    private readonly ContainerRuntimeSpawner _containerSpawner = new();
    
    public CarrierSpawner CarrierSpawner => carrierSpawner;
    public ClawBoosterAnimator ClawBoosterAnimator => clawBoosterAnimator;
    
    public IReadOnlyList<CarrierBase> SpawnedCarriers => carrierSpawner != null ? carrierSpawner.SpawnedCarriers : null;

    public void InitCarrier(LevelData levelData)
    {
        _containerSpawner.ClearContainers();
        if (carrierSpawner == null) return;
        carrierSpawner.SpawnCarriers(levelData);
        RebuildRuntimeContainers(levelData);
    }

    public async UniTask PlayContainersScaleAnimation(System.Threading.CancellationToken token)
    {
        await _containerSpawner.PlayContainersScaleAnimation(token);
    }

    public bool TrySpawnCarrier(CarrierStackData carrierStack)
    {
        var currentLevel = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
        if (carrierStack == null || carrierSpawner == null || currentLevel == null) return false;
        if (!carrierSpawner.TrySpawnCarrier(carrierStack)) return false;
        return true;
    }

    public int GetMaxClawTargetBlockCount()
    {
        var carriers = SpawnedCarriers;
        if (carriers == null) return 0;
        var maxTargetBlockCount = 0;

        foreach (var carrier in carriers)
        {
            if (carrier == null) continue;
            maxTargetBlockCount = Mathf.Max(maxTargetBlockCount, carrier.GetClawTargetBlockCount());
        }

        return maxTargetBlockCount;
    }

    public int GetValidClawTargetCountExcluding(CarrierBase excludedCarrier)
    {
        return GetValidClawTargets(excludedCarrier).Count;
    }

    public void SetClawSourceSelectionLayers()
    {
        var carriers = SpawnedCarriers;
        if (carriers == null) return;
        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        foreach (var carrier in carriers)
        {
            if (carrier == null) continue;
            carrier.SetLayer(carrierLayer);
            SetSingleBlockSourceLayers(carrier);
            for (var size = 2; size <= HighlightLayerNames.Length; size++)
                SetLinkedBlockSourceLayers(carrier, size);
        }
    }

    public void SetClawTargetSelectionLayers(
        bool targetSelectionActive,
        CarrierBase excludedCarrier = null,
        int requiredBlockCount = 0,
        EBlockColorType pickedColor = EBlockColorType.None)
    {
        var carriers = SpawnedCarriers;
        if (carriers == null) return;
        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        var defaultLayer = LayerMask.NameToLayer(DefaultLayerName);
        foreach (var carrier in carriers)
        {
            if (carrier == null) continue;
            var canHighlightTarget = targetSelectionActive
                                     && carrier != excludedCarrier
                                     && CanHighlightClawTarget(carrier, excludedCarrier, requiredBlockCount, pickedColor);
            var layer = targetSelectionActive && !canHighlightTarget
                ? defaultLayer
                : carrierLayer;
            carrier.SetLayer(layer);
            var contentLayers = canHighlightTarget
                ? HighlightLayerNames
                : SourceLayerNames;
            SetCarrierContentLayers(carrier, contentLayers);
        }
    }

    private static bool CanHighlightClawTarget(CarrierBase carrier, CarrierBase sourceCarrier, int requiredBlockCount, EBlockColorType pickedColor)
    {
        if (carrier == null || !carrier.CanBeClawTarget()) return false;
        if (requiredBlockCount > 0 && carrier.GetClawTargetBlockCount() < requiredBlockCount) return false;
        return ClawTransferUtility.IsValidClawTargetColor(carrier, pickedColor);
    }

    public void SetTutorialSelectionLayers(bool highlightActive)
    {
        var carriers = SpawnedCarriers;
        if (carriers == null) return;

        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        var defaultLayer = LayerMask.NameToLayer(DefaultLayerName);
        foreach (var carrier in carriers)
        {
            if (carrier == null) continue;
            carrier.SetLayer(highlightActive ? carrierLayer : defaultLayer);
            SetCarrierContentLayers(carrier, highlightActive ? HighlightLayerNames : SourceLayerNames);
        }
    }
    
    public void SetTutorialSelectionLayers(int highlightIndex)
    {
        var carriers = SpawnedCarriers;
        if (carriers == null) return;

        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        var defaultLayer = LayerMask.NameToLayer(DefaultLayerName);
        for (var i = 0; i < carriers.Count; i++)
        {
            var carrier = carriers[i];
            if (carrier == null) continue;

            var shouldHighlight = i == highlightIndex;
            carrier.SetLayer(shouldHighlight ? carrierLayer : defaultLayer);
            SetCarrierContentLayers(carrier, shouldHighlight ? HighlightLayerNames : SourceLayerNames);
        }
    }

    public void EnableHighlightClickableLayers()
    {
        InputController.SetClickableLayerOverrideMask(BuildHighlightClickableMask());
    }

    public void DisableHighlightClickableLayers()
    {
        InputController.ResetClickableLayerOverrideMask();
    }

    private List<CarrierBase> GetValidClawTargets(CarrierBase excludedCarrier)
    {
        var result = new List<CarrierBase>();
        var carriers = SpawnedCarriers;
        if (carriers == null) return result;
        foreach (var carrier in carriers)
        {
            if (carrier == null || carrier == excludedCarrier) continue;
            if (!carrier.CanBeClawTarget()) continue;
            result.Add(carrier);
        }
        return result;
    }

    private static void SetCarrierContentLayers(CarrierBase carrier, IReadOnlyList<string> layerNames)
    {
        SetSingleBlockLayers(carrier, GetLayer(layerNames, 1));
        for (var size = 2; size <= layerNames.Count; size++)
            SetLinkedBlockLayers(carrier, size, GetLayer(layerNames, size));
    }

    private static void SetSingleBlockLayers(CarrierBase carrier, int layer)
    {
        if (carrier == null || layer < 0 || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) return;
        foreach (var block in carrier.BlockLayout.Blocks)
        {
            if (block == null || block.IsLinkedVisualSuppressed()) continue;
            block.SetLayer(layer);
        }
    }

    private static void SetSingleBlockSourceLayers(CarrierBase carrier)
    {
        var highlightLayer = GetLayer(HighlightLayerNames, 1);
        var sourceLayer = GetLayer(SourceLayerNames, 1);
        if (highlightLayer < 0 || sourceLayer < 0) return;
        if (carrier == null || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) return;
        foreach (var block in carrier.BlockLayout.Blocks)
        {
            if (block == null || block.IsLinkedVisualSuppressed()) continue;
            block.SetLayer(CanHighlightSourceBlock(block, carrier) ? highlightLayer : sourceLayer);
        }
    }

    private static void SetLinkedBlockLayers(CarrierBase carrier, int size, int layer)
    {
        if (carrier == null || layer < 0) return;
        foreach (var visual in carrier.GetLinkedBlockVisuals(size))
            visual?.SetLayer(layer);
    }

    private static void SetLinkedBlockSourceLayers(CarrierBase carrier, int size)
    {
        var highlightLayer = GetLayer(HighlightLayerNames, size);
        var sourceLayer = GetLayer(SourceLayerNames, size);
        if (carrier == null || highlightLayer < 0 || sourceLayer < 0) return;
        foreach (var visual in carrier.GetLinkedBlockVisuals(size))
        {
            if (visual == null) continue;
            visual.SetLayer(visual.CanSelectAsSource() ? highlightLayer : sourceLayer);
        }
    }

    private static bool CanHighlightSourceBlock(Block block, CarrierBase carrier)
    {
        if (!ClawTransferUtility.CanSelectSource(block, carrier)) return false;
        if (GameEventBus.CanSelectTutorialClawBlock == null) return true;
        return GameEventBus.CanSelectTutorialClawBlock.Invoke(block, carrier);
    }

    private static int GetLayer(IReadOnlyList<string> layerNames, int size)
    {
        if (size < 1 || size > layerNames.Count) return -1;
        return LayerMask.NameToLayer(layerNames[size - 1]);
    }

    private static int BuildHighlightClickableMask()
    {
        var mask = 0;
        AddLayerToMask(ref mask, CarrierLayerName);
        foreach (var layerName in HighlightLayerNames)
            AddLayerToMask(ref mask, layerName);
        return mask;
    }

    private static void AddLayerToMask(ref int mask, string layerName)
    {
        var layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            mask |= 1 << layer;
    }

    public CarrierBase GetCarrier(int index)
    {
        return SpawnedCarriers[index];
    }

    private void RebuildRuntimeContainers(LevelData levelData)
    {
        _containerSpawner.SpawnContainers(
            levelData,
            carrierSpawner.SpawnedCarriers,
            carrierSpawner.CarrierConfig,
            carrierSpawner.SplineContainer);
    }
}
