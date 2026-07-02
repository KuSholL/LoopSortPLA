using Alchemy.Inspector;
using UnityEngine;

public class BoosterSystem : MonoSingleton<BoosterSystem>
{
    private const string CarrierLayerName = "Carrier";
    private static readonly string[] HighlightLayerNames =
        { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };
    [SerializeField] private BoosterExtraSlot boosterExtraSlot;
    [SerializeField] private BoosterClaw boosterClaw;
    private bool _useClawBooster;
    public bool UseClawBooster => _useClawBooster;
    public bool IsClawAnimating => boosterClaw != null && boosterClaw.IsAnimating;
    public EClawSelectionMode ClawMode { get; set; }
    public CarrierBase CurrentClawSourceCarrier => boosterClaw != null ? boosterClaw.CurrentSourceCarrier : null;
    private float _cachedClawTimeScale = 1f;

    protected override void Awake()
    {
        base.Awake();
        GameStateManager.OnGameStateChange.Register(OnGameStateChange);
        GameEventBus.OnInitLoadLevel += CancelClawBooster;
    }

    private void OnDestroy()
    {
        GameStateManager.OnGameStateChange.UnRegister(OnGameStateChange);
        GameEventBus.OnInitLoadLevel -= CancelClawBooster;
    }

    private void OnGameStateChange(GameState state)
    {
        if (state == GameState.MainMenu)
        {
            CancelClawBooster();
        }
    }

    public void Init(LevelData levelData)
    {
        _useClawBooster = false;
        ClawMode = EClawSelectionMode.SelectBooster;
        boosterExtraSlot?.Init(levelData);
        BoosterUndoSystem.Instance.ResetState();
    }

    public bool CanUseBoosterExtraSlot()
    {
        return boosterExtraSlot.CanUse();
    }

    public bool CanUseBoosterClaw()
    {
        if (CarrierSystem.Instance == null) return false;
        var maxTargetBlockCount = CarrierSystem.Instance.GetMaxClawTargetBlockCount();
        if (maxTargetBlockCount <= 0) return false;
        if (!HasValidClawSource()) return false;
        if (LevelManager.Instance && LevelManager.Instance.IsGameEnded && !LevelManager.Instance.IsPreloseDelay) return false;
        return true;
    }

    public bool TryUseExtraSlotBooster()
    {
        if (LevelManager.Instance && LevelManager.Instance.IsGameEnded && !LevelManager.Instance.IsPreloseDelay) return false;
        bool success = boosterExtraSlot.TryUse();
        if (success && LevelManager.Instance)
        {
            LevelManager.Instance.CancelPreloseDelay();
        }
        return success;
    }

    public bool TryUseUndoBooster()
    {
        if (LevelManager.Instance && LevelManager.Instance.IsGameEnded && !LevelManager.Instance.IsPreloseDelay) return false;
        bool success = BoosterUndoSystem.Instance.TryUseUndoBooster();
        if (success && LevelManager.Instance)
        {
            LevelManager.Instance.CancelPreloseDelay();
        }
        return success;
    }

    public bool TryUseClawMachineBooster()
    {
        if (!CanUseBoosterClaw()) return false;
        bool success = SelectClawBooster();
        if (success && LevelManager.Instance)
        {
            LevelManager.Instance.CancelPreloseDelay();
        }
        return success;
    }

    public void CancelClawBooster()
    {
        if (!_useClawBooster) return;
        _useClawBooster = false;
        ClawMode = EClawSelectionMode.SelectBooster;
        UpdateClawInputLayers();
        boosterClaw.CancelSelection();
        CameraManager.Instance.SetHighlightCameraActive(false);
        
        float targetTimeScale = _cachedClawTimeScale;
        if (LevelManager.Instance != null && !LevelManager.Instance.IsPreloseDelay && targetTimeScale < 1f)
        {
            targetTimeScale = 1f;
        }
        CustomTimeScaleGroup.Instance.ApplyTimeScale(targetTimeScale);
        
        GameEventBus.OnCancelSelectBooster?.Invoke();
        ConveyorDeliverySystem.Instance?.EvaluateLoseCondition();
    }

    public void TryCancelClawBoosterOnMissedClick()
    {
        if (!_useClawBooster) return;
        if (LevelManager.Instance != null && LevelManager.Instance.IsTutorial) return;
        if (ClawMode != EClawSelectionMode.SelectStartBlock
            && ClawMode != EClawSelectionMode.SelectTargetCarrier) return;
        CancelClawBooster();
    }

    public void SelectBlock(Vector3 worldPosition, Block block, CarrierBase carrier)
    {
        if (!_useClawBooster || !block || !carrier) return;
        if (ClawMode == EClawSelectionMode.SelectStartBlock)
            boosterClaw.SelectStartBlock(worldPosition, block, carrier);
    }

    public void SelectCarrier(Vector3 worldPosition, CarrierBase carrier)
    {
        if (!_useClawBooster || !carrier) return;
        if (ClawMode == EClawSelectionMode.SelectTargetCarrier)
            boosterClaw.SelectTargetCarrier(worldPosition, carrier);
    }

    public void SetClawMode(EClawSelectionMode mode)
    {
        ClawMode = mode;
        UpdateClawInputLayers();
    }

    [Button]
    private void UseUndoBooster()
    {
        TryUseUndoBooster();
    }

    private bool SelectClawBooster()
    {
        if (_useClawBooster) return false;
        _useClawBooster = true;
        ClawMode = EClawSelectionMode.SelectStartBlock;
        UpdateClawInputLayers();
        if (CustomTimeScaleGroup.Instance != null)
        {
            _cachedClawTimeScale = CustomTimeScaleGroup.Instance.CurrentTimeScale;
        }
        boosterClaw.SelectBooster();
        GameEventBus.OnSelectBooster?.Invoke();
        return true;
    }

    private static bool HasValidClawSource()
    {
        var carriers = CarrierSystem.Instance != null ? CarrierSystem.Instance.SpawnedCarriers : null;
        if (carriers == null) return false;
        foreach (var carrier in carriers)
        {
            if (carrier == null || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) continue;
            foreach (var block in carrier.BlockLayout.Blocks)
                if (ClawTransferUtility.CanSelectSource(block, carrier))
                    return true;
        }
        return false;
    }

    private void UpdateClawInputLayers()
    {
        var carrierLayer = LayerMask.NameToLayer(CarrierLayerName);
        if (ClawMode == EClawSelectionMode.SelectStartBlock && _useClawBooster)
        {
            InputController.IgnoreLayer(carrierLayer);
            InputController.SetAdditionalClickableLayers(GetClawSourceLayers());
            return;
        }
        InputController.UnignoreLayer(carrierLayer);
        InputController.ResetAdditionalClickableLayers();
    }

    private static int[] GetClawSourceLayers()
    {
        var maxTargetBlockCount = CarrierSystem.Instance != null ? CarrierSystem.Instance.GetMaxClawTargetBlockCount() : 0;
        var layers = new System.Collections.Generic.List<int>();
        for (var size = 1; size <= maxTargetBlockCount && size <= HighlightLayerNames.Length; size++)
        {
            if (size == 4) continue;
            var layer = LayerMask.NameToLayer(HighlightLayerNames[size - 1]);
            if (layer >= 0) layers.Add(layer);
        }
        return layers.ToArray();
    }
}
