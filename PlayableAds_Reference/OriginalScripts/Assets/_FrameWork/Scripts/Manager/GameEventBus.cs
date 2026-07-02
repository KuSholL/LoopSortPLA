using System;
using System.Collections.Generic;
using UnityEngine;

public enum ELoseReason
{
    None = 0,
    CapacityFull = 1,
    Deadlock = 2,
}

public static class GameEventBus
{
    #region Profile

    public static Action<int> OnSelectAvatarCell;
    public static Action<int> OnSelectFrameCell;
    public static Action OnAvatarChange;
    public static Action OnFrameChange;
    public static Action OnChangeNameSuccess;
    public static Action UpdateProfileNotify;
    #endregion
    
    #region BottomBarLayer

    public static Action<BottomBarTabType, bool> SetBottomBarTabActive;

    #endregion

    #region IAP

    public static Action OnRestorePurchases;
    public static Action<IAPProduct> OnPurchaseSuccess;
    public static Action<string> OnBuyProduct;
    public static Func<IAPCategory, IAPProduct> GetIAPProductByCategory;
    public static Func<string, IAPProduct> GetIAPProductById;
    public static Func<List<IAPProduct>> GetAllIAPProducts;
    public static Func<IAPCategory, string> GetProductPriceByProductCategory;
    public static Func<IAPCategory, string[]> GetProductPriceArrByCategory;
    public static Action<string> OnReceivePackInfoFromStore;

    #endregion

    #region Heart

    public static Action OnHeartDataChange;
    public static Action UpdateOffHeartListen;
    public static Action OnRefillSuccess;


    #endregion

    #region LayerManager

    public static ActionSealed<ShowLayerGroupData> OnCloseLayerGroup = new();

    #endregion

    #region Setting

    public static ActionSealed<string> OnLanguageChanged = new();
    public static Action<string> OnLanguageSelected;
    #endregion

    #region Shop

    public static Action CancelIgnoreLastPackShop;
    public static Action<bool> ReloadFreeGoldPack;
    public static Action<bool> ShowShopLayer;

    #endregion

    #region Offer Pack

    public static Action OnOfferPackClosed;
    public static Action<IAPCategory> OnOfferPackOpened;
    public static Action IgnoreLastPack;
    public static Action OnShowPopupOffer;
    #endregion

    #region Reward

    public static Action OnGainGold; // Hiệu ứng tiền bay
    public static Action OnGoldChange; // Hiệu ứng tiền thay đổi
    public static Action<RewardType> OnBoosterDataChanged;
    #endregion

    #region New Level

    public static Action OnNewLevel;

    #endregion

    #region SoundManager

    public static Action<float> OnChangeSound;
    public static Action<float> OnChangeSoundFx;

    #endregion

    #region HelperPack

    public static Action ActiveHelperItem;

    #endregion

    #region Game Play
    public static Action OnLoadLevelDone;
    public static Action<Action> OnReceiveBooster;
    public static Action<RewardType, Vector3> OnReceiveBoosterFromBundle;
    public static Action<RewardType, Vector3, Action> OnReceiveBoosterFromPopUpBuyMore;
    public static Action<bool> OnUndoBoosterAvailabilityChanged;
    public static Action OnUndoSuccess;
    public static Action OnReloadCurrentLevel;
    public static Action OnLoopConfigUpdated;
    public static Action<float> OnCustomTimeScaleChanged;
    #endregion

    #region UIGamePlay

    public static Action OnInitLoadLevel;
    public static Action<int, int> OnUpdateCapcityUI;
    public static Action<LevelData> OnLevelLoaded;
    public static Action<BoosterType> OnShowSthWrong;
    public static Action PlayAnimButton;

    #endregion

    #region Carrier

    public static Action<EBlockColorType> OnCarrierFinished;
    public static Action<CarrierBase> OnCarrierComplimentTrigger;
    public static Action OnWinTrigger;
    public static Action<ELoseReason> OnLoseTrigger;
    public static Action OnEndGame;
    public static Action OnCarrierUnloadDone;
    public static Action OnCarrierUnload;
    public static Action OnCarrierPickupDone;
    public static Action OnContainerUnlocked;


    #endregion

    #region PreLose

    public static Action OnHoldToSeeBoard;
    public static Action OnReleaseToHideBoard;
    public static Action<SupportPackLayerData> OnReloadSupportPackLayer;
    public static Action<bool> OnPreloseDelayChanged;

    #endregion

    #region Claw Booster

    public static Action OnSelectBooster;
    public static Action OnCancelSelectBooster;
    public static Action<Vector3> OnSelectStartBlock;
    public static Action<Vector3> OnSelectTargetBlock;

    #endregion

    #region Tutorial Booster

    public static Action<BoosterType, bool> SetTutorialVisualBooster;
    public static Func<BoosterType, Transform> GetHandBoosterTarget;
    public static Func<BoosterType, RectTransform> GetTransBoosterTarget;
    public static Action AdvanceStepTutorial;
    public static Func<Block, CarrierBase, bool> CanSelectTutorialClawBlock;
    public static Func<CarrierBase, bool> CanSelectTutorialClawCarrier;
    
    public static Action FinishUndoBoosterStep4;
    public static Action FinishExtraSlotBoosterStep2;
    public static Action FinishClawTutSecondStep;
    public static Action<bool> SetActiveSettingReplayButton;

    #endregion

    public static Action<bool> OnRemoveAdsActive;
    
    #region Camera

    public static Action<ActiveCameraPlace, bool> OnActiveCameraGameplay;
    public static Action<bool> OnHighlightCameraActiveChanged;

    #endregion
    
    #region DailyReward

    public static Action ReloadDailyReward;
    public static Action SetStateButtonReloadDaily;

    #endregion
}
