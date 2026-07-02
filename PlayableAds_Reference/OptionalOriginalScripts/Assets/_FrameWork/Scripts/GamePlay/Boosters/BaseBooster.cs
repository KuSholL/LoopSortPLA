using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseBooster : MonoBehaviour
{
    public BoosterType             BoosterType;
    
    public BoosterVisualController visual;
    public Button                  actionButton;
    public Button                  lockButton;
    
    public Transform              hand;
    public RectTransform          icon;
    
    protected BoosterConfig _config;
    protected float         _lastClickTime = -1f;

    protected virtual void Awake()
    {
        actionButton.onClick.AddListener(HandleButtonClick);
        lockButton.onClick.AddListener(OnLockButtonClick);
        _config = ConfigHolder.Instance.BoosterDataSO.GetBoosterConfig(BoosterType);
        GameEventBus.OnLoadLevelDone      += RefreshVisual;
        GameEventBus.OnBoosterDataChanged += OnBoosterDataChanged;
        GameEventBus.OnLanguageChanged.Register(OnLanguageChanged);
    }

    protected virtual void OnDestroy()
    {
        actionButton.onClick.RemoveListener(HandleButtonClick);
        lockButton.onClick.RemoveListener(OnLockButtonClick);
        GameEventBus.OnBoosterDataChanged -= OnBoosterDataChanged;
        GameEventBus.OnLoadLevelDone      -= RefreshVisual;
        GameEventBus.OnLanguageChanged.UnRegister(OnLanguageChanged);
    }

    public void SetTutorial(bool isOn)
    {
        visual.HandleTutorial(isOn);
    }

    public virtual void RefreshVisual()
    {
        if (visual == null) return;
        
        BoosterState state = CalculateState();
        if (RewardItemFlyEffectController.IsPlayingAnimation && state != BoosterState.Locked)
        {
            state = BoosterState.Disabled;
        }
        int amount = GetAmount();
        string lockInfo = GetLockInfo();
        
        visual.ApplyState(state, amount, lockInfo);
    }

    protected abstract BoosterState CalculateState();
    protected abstract int          GetAmount();
    protected abstract string       GetLockInfo();

    public abstract void OnButtonClick();
    public abstract void OnLockButtonClick();

    public virtual void ExecuteBooster()
    {
        HeartSystem.Instance.HasPlayedLevel = true;
        switch (BoosterType)
        {
            case BoosterType.UndoBooster:
                SoundManager.Instance.PlayOneShot(AudioClipName.sfx_booster_undo);
                break;
        }
    }

    protected abstract void OnBoosterDataChanged(RewardType type);

    protected void OnLanguageChanged(string lang) => RefreshVisual();
    
    public abstract void BuyBooster();

    protected abstract void OnBuyBoosterByGold();
    protected abstract void OnBuyBoosterByAds();

    private void HandleButtonClick()
    {
        if (RewardItemFlyEffectController.IsPlayingAnimation) return;
        OnButtonClick();
    }
}
