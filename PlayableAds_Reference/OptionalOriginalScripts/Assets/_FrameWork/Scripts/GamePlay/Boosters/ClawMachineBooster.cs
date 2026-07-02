using AdMod;
using TMPro;
using UnityEngine;

public class ClawMachineBooster : BaseBooster
{
    [SerializeField] private ParticleSystem boosterParticles;

    private int _levelOpen;
    private bool _isTutorialBoosterUsed;
    private float _cachedTimeScale = 1f;

    protected override void Awake()
    {
        base.Awake();
        _levelOpen = _config.LevelOpen;
        _isTutorialBoosterUsed = false;
        GameEventBus.OnCarrierUnloadDone  += RefreshVisual;
        GameEventBus.OnCarrierPickupDone  += RefreshVisual;
        GameEventBus.OnUndoSuccess        += RefreshVisual;
    }

    private void Start()
    {
        RefreshVisual();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEventBus.OnCarrierUnloadDone  -= RefreshVisual;
        GameEventBus.OnCarrierPickupDone  -= RefreshVisual;
        GameEventBus.OnUndoSuccess        -= RefreshVisual;
    }

    protected override BoosterState CalculateState()
    {
        var currentLevel = DataManager.PlayerData.LevelDisplay;
        if (currentLevel < _levelOpen) return BoosterState.Locked;

        if (BoosterSystem.Instance != null && !BoosterSystem.Instance.CanUseBoosterClaw())
            return BoosterState.Disabled;

        int amount = GetAmount();
        return amount > 0 ? BoosterState.Available : BoosterState.Empty;
    }

    protected override int GetAmount()
    {
        var currentAmount = DataManager.PlayerData.BoosterData.ClawMachineBooster;
        
        if (UIClawBoosterTutorial.IsTutorial && !RewardItemFlyEffectController.IsPlayingAnimation)
        {
            var freeAmount = ConfigHolder.Instance.TutorialConfigSO.BoosterFree;
            if (_isTutorialBoosterUsed)
            {
                freeAmount = Mathf.Max(0, freeAmount - 1);
            }
            currentAmount += freeAmount;
        }

        return currentAmount;
    }

    protected override string GetLockInfo()
    {
        var levelText = TextUtility.GetI2("level");
        if (levelText == "level") levelText = "Level";
        return $"{levelText} {_levelOpen}";
    }

    protected override void OnBoosterDataChanged(RewardType type)
    {
        if (type == RewardType.ClawMachineBooster || type == RewardType.ExtraSlotBooster) RefreshVisual();
    }

    public override void OnButtonClick()
    {
        if (Time.unscaledTime - _lastClickTime < 0.25f) return;
        _lastClickTime = Time.unscaledTime;

        // TUT mặc định được dùng
        if (LevelManager.Instance.IsTutorial)
        {
            ExecuteBooster();
            return;
        }
        
        var state = CalculateState();
        if (state == BoosterState.Locked)
        {
            LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_popup_locked_booster"));
            return;
        }

        if (state == BoosterState.Disabled) return;

        if (GetAmount() <= 0)
        {
            BuyBooster();
            return;
        }

        ExecuteBooster();
    }

    public override void OnLockButtonClick()
    {
        LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_popup_locked_booster"));
    }

    public override void ExecuteBooster()
    {
        var usedBooster = BoosterSystem.Instance != null && BoosterSystem.Instance.TryUseClawMachineBooster();
        if (!usedBooster) return;
        base.ExecuteBooster();

        if (LevelManager.Instance.IsTutorial)
        {
            _isTutorialBoosterUsed = true;
            GameEventBus.FinishClawTutSecondStep?.Invoke();
            RefreshTutVisual();
            return;
        }
    }
    
    private void RefreshTutVisual()
    {
        if (visual == null) return;
        var state = CalculateState();
        var amount = Mathf.Max(DataManager.PlayerData.BoosterData.ClawMachineBooster + ConfigHolder.Instance.TutorialConfigSO.BoosterFree - 1, 0);
        var lockInfo = GetLockInfo();
        visual.ApplyState(state, amount, lockInfo);
    }

    public override void BuyBooster()
    {
        if (CustomTimeScaleGroup.Instance != null)
        {
            _cachedTimeScale = CustomTimeScaleGroup.Instance.CurrentTimeScale;
        }
        LayerManager.Instance.ShowPopupOfferLayer(new PopupOfferLayerData()
        {
            ItemType = RewardType.ClawMachineBooster,
            Title = TextUtility.GetI2("ui_claw_machine"),
            Content = TextUtility.GetI2("popup_get_clawmachine"),
            GoldAction = OnBuyBoosterByGold,
            AdsAction = OnBuyBoosterByAds,
            CloseAction = OnCloseAction,
            Price = new PriceType() { Gold = _config.BoosterCost, Ads = 1 },
        });
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(0f);
        GameEventBus.OnShowPopupOffer?.Invoke();
    }
    
    private float GetRestoreTimeScale()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.IsPreloseDelay && _cachedTimeScale < 1f)
        {
            return 1f;
        }
        return _cachedTimeScale;
    }
    
    private void OnCloseAction()
    {
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(GetRestoreTimeScale());
    }

    protected override void OnBuyBoosterByGold()
    {
        if (DataManager.PlayerData.Gold < _config.BoosterCost)
        {
            LayerManager.Instance.ShowShopInGameLayer();
            return;
        }
        DataManager.ChangeGold(-_config.BoosterCost);
        
        UserBehaviorTracker.SendSpendVirtualCurrencyTracking("Gold", _config.BoosterCost, 
            DataManager.PlayerData.Gold + _config.BoosterCost, DataManager.PlayerData.Gold, 
            "booster_clawmachine");
            
        GameEventBus.OnReceiveBooster?.Invoke(() =>
        {
            DataManager.ChangeBooster(RewardType.ClawMachineBooster, _config.AmountBuy, source: "ingame_gold");
            LayerManager.Instance.CloseLastLayerGroup();
        });
        
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(GetRestoreTimeScale());
        GameEventBus.OnActiveCameraGameplay?.Invoke(ActiveCameraPlace.OfferPopup, true);
    }

    protected override void OnBuyBoosterByAds()
    {
#if NO_ADS
        OnVideoRewarded();
        return;
#endif
        AdmobManager.Instance.TryRequestShowRewardedVideo("ad_booster_clawmachine", OnVideoRewarded, null);
    }

    private void OnVideoRewarded()
    {
        GameEventBus.OnReceiveBooster?.Invoke(() =>
        {
            DataManager.ChangeBooster(RewardType.ClawMachineBooster, _config.WatchAds, source: "ingame_ads");
            LayerManager.Instance.CloseLastLayerGroup();
        });
        
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(GetRestoreTimeScale());
        GameEventBus.OnActiveCameraGameplay?.Invoke(ActiveCameraPlace.OfferPopup, true);
    }
}
