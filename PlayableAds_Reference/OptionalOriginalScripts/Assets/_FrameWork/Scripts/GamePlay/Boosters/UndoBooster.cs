using AdMod;
using TMPro;
using UnityEngine;

public class UndoBooster : BaseBooster
{
    private int _levelOpen;
    private bool _canUndo;
    private float _cachedTimeScale = 1f;

    protected override void Awake()
    {
        base.Awake();
        _levelOpen = _config.LevelOpen;

        GameEventBus.OnUndoBoosterAvailabilityChanged += OnUndoBoosterAvailabilityChanged;
        GameEventBus.OnSelectBooster += RefreshVisual;
        GameEventBus.OnCancelSelectBooster += RefreshVisual;
    }

    private void OnUndoBoosterAvailabilityChanged(bool canUndo)
    {
        _canUndo = canUndo;
        RefreshVisual();
    }

    private void Start()
    {
        RefreshVisual();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GameEventBus.OnUndoBoosterAvailabilityChanged -= OnUndoBoosterAvailabilityChanged;
        GameEventBus.OnSelectBooster -= RefreshVisual;
        GameEventBus.OnCancelSelectBooster -= RefreshVisual;
    }

    #region Logic Implementations

    protected override BoosterState CalculateState()
    {
        if (TutorialUndoBooster_Step3.isActive)
        {
            return BoosterState.Disabled;
        }
        
        var currentLevel = DataManager.PlayerData.LevelDisplay;
        if (currentLevel < _levelOpen)
            return BoosterState.Locked;

        if (BoosterSystem.Instance != null && BoosterSystem.Instance.UseClawBooster)
            return BoosterState.Disabled;

        if (!_canUndo) return BoosterState.Disabled;

        int amount = GetAmount();
        return amount > 0 ? BoosterState.Available : BoosterState.Empty;
    }

    protected override int GetAmount()
    {
        var currentAmount = DataManager.PlayerData.BoosterData.UndoBooster;
        
        if (UIUndoBoosterTutorialLayer.IsTutorial && !BoosterSystem.Instance.UseClawBooster && !RewardItemFlyEffectController.IsPlayingAnimation)
        {
            currentAmount += ConfigHolder.Instance.TutorialConfigSO.BoosterFree;
        }
        
        return currentAmount;    }

    protected override string GetLockInfo()
    {
        var levelText = TextUtility.GetI2("level");
        if (levelText == "level") levelText = "Level";
        return $"{levelText} {_levelOpen}";
    }

    #endregion

    #region Events

    protected override void OnBoosterDataChanged(RewardType type)
    {
        if (type == RewardType.UndoBooster) RefreshVisual();
    }

    private void OnLanguageChanged(string lang) => RefreshVisual();

    #endregion

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
        
        if (CalculateState() == BoosterState.Locked)
        {
            LayerManager.Instance.ShowPopupFeedbackLayer(TextUtility.GetI2("ui_popup_locked_booster"));
            return;
        }
        
        if (CalculateState() == BoosterState.Disabled) return;

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
        // Visual feedback for clicking
        var usedBooster = BoosterSystem.Instance != null && BoosterSystem.Instance.TryUseUndoBooster();
        if (!usedBooster) return;
        base.ExecuteBooster();
        
        if (LevelManager.Instance.IsTutorial)
        {
            GameEventBus.FinishUndoBoosterStep4?.Invoke();
            RefreshVisual();
            return;
        }

        DataManager.ChangeBooster(RewardType.UndoBooster, -1);
        RefreshVisual();
    }

    public override void BuyBooster()
    {
        if (CustomTimeScaleGroup.Instance != null)
        {
            _cachedTimeScale = CustomTimeScaleGroup.Instance.CurrentTimeScale;
        }
        LayerManager.Instance.ShowPopupOfferLayer(new PopupOfferLayerData()
        {
            ItemType = RewardType.UndoBooster,
            Title = TextUtility.GetI2("ui_undo"),
            Content = TextUtility.GetI2("popup_get_undo"),
            GoldAction = OnBuyBoosterByGold,
            AdsAction = OnBuyBoosterByAds,
            CloseAction = OnCloseAction,
            Price = new PriceType()
            {
                Gold = _config.BoosterCost,
                Ads = 1
            },
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
            "booster_undo");

        GameEventBus.OnReceiveBooster?.Invoke(() =>
        {
            DataManager.ChangeBooster(RewardType.UndoBooster, _config.AmountBuy, source: "ingame_gold");
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
        AdmobManager.Instance.TryRequestShowRewardedVideo("ad_booster_undo", OnVideoRewarded, null);
    }

    private void OnVideoRewarded()
    {
        GameEventBus.OnReceiveBooster?.Invoke(() =>
        {
            DataManager.ChangeBooster(RewardType.UndoBooster, _config.WatchAds, source: "ingame_ads");
            LayerManager.Instance.CloseLastLayerGroup();
        });
        
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(GetRestoreTimeScale());
        GameEventBus.OnActiveCameraGameplay?.Invoke(ActiveCameraPlace.OfferPopup, true);
    }
}