using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGamePlayLayer : LayerBase
{
    [SerializeField] private CapacityUI capacityUI;
    [SerializeField] private LevelTextUI levelTextUI;
    [SerializeField] private GameObject returnButton;
    [SerializeField] private GameObject settingButton;
    [SerializeField] private GameObject levelUI;
    [SerializeField] private GameObject supporterGroup;
    [SerializeField] private RectTransform sthWrongPanel;
    [SerializeField] private CanvasGroup sthWrongCanvasGroup;
    [SerializeField] private TextMeshProUGUI sthWrongText;
    [SerializeField] private AnimShowPopupCustom notiPanel;
    private MotionHandle _sthWrongMotionHandle;
    private bool _isReplayButtonActiveByTutorial = true;
    private bool _isUsingClawBooster = false;

    private void Awake()
    {
        GameEventBus.OnInitLoadLevel += OnInitLoadLevel;
        GameEventBus.OnUpdateCapcityUI += UpdateCapacityUI;
        GameEventBus.OnSelectBooster += OnClawSelectBooster;
        GameEventBus.OnSelectStartBlock += OnClawSelectStartBlock;
        GameEventBus.OnCancelSelectBooster += OnClawCancel;
        GameEventBus.OnSelectTargetBlock += OnClawSelectTargetBlock;
        GameEventBus.SetActiveSettingReplayButton += SetActiveSettingReplayButton;
        GameEventBus.OnShowSthWrong += ShowSthWrongPanel;
        GameEventBus.OnPreloseDelayChanged += OnPreloseDelayChanged;
        HeartSystem.OnHasPlayedLevelChanged += UpdateReturnButtonInteractable;
        if (sthWrongCanvasGroup != null) sthWrongCanvasGroup.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEventBus.OnInitLoadLevel -= OnInitLoadLevel;
        GameEventBus.OnUpdateCapcityUI -= UpdateCapacityUI;
        GameEventBus.OnSelectBooster -= OnClawSelectBooster;
        GameEventBus.OnSelectStartBlock -= OnClawSelectStartBlock;
        GameEventBus.OnCancelSelectBooster -= OnClawCancel;
        GameEventBus.OnSelectTargetBlock -= OnClawSelectTargetBlock;
        GameEventBus.SetActiveSettingReplayButton -= SetActiveSettingReplayButton;
        GameEventBus.OnShowSthWrong -= ShowSthWrongPanel;
        GameEventBus.OnPreloseDelayChanged -= OnPreloseDelayChanged;
        HeartSystem.OnHasPlayedLevelChanged -= UpdateReturnButtonInteractable;
        _sthWrongMotionHandle.TryCancel();
    }

    private void HideUIForTutorial()
    {
        var levelDisplay = DataManager.PlayerData.LevelDisplay;
        var isHide = levelDisplay == 1;
        returnButton.SetActive(!isHide);
        capacityUI.gameObject.SetActive(!isHide);
        supporterGroup.SetActive(!isHide);
    }

    private void OnInitLoadLevel()
    {
        capacityUI.InitLevelLoaded();
        levelTextUI.SetupLevelText();
        HideUIForTutorial();
        _isReplayButtonActiveByTutorial = true;
        _isUsingClawBooster = false;
        UpdateReturnButtonInteractable();
    }

    private void UpdateCapacityUI(int capacity, int maxCapacity)
    {
        capacityUI.SetCapacity(capacity, maxCapacity);
    }

    private void OnClawSelectBooster()
    {
        notiPanel?.PlayAnim( "popup_choose_cube");
        _isUsingClawBooster = true;
        if (settingButton != null)
        {
            var btn = settingButton.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
        UpdateReturnButtonInteractable();
    }

    private void OnClawSelectStartBlock(Vector3 _)
    {
        notiPanel?.PlayAnim("popup_choose_box");
    }

    private void OnClawCancel()
    {
        notiPanel?.Close();
        _isUsingClawBooster = false;
        if (settingButton != null)
        {
            var btn = settingButton.GetComponent<Button>();
            if (btn != null) btn.interactable = _isReplayButtonActiveByTutorial;
        }
        UpdateReturnButtonInteractable();
    }

    private void OnClawSelectTargetBlock(Vector3 _)
    {
        notiPanel?.Close();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!GameStateManager.Instance.IsState(GameState.InGame)) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            LayerManager.Instance.ShowEndGameLayer(new UIEndGameData()
            {
                IsWinMatch = true
            });
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            LayerManager.Instance.ShowEndGameLayer(new UIEndGameData()
            {
                IsWinMatch = false
            });
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            GameEventBus.OnLoseTrigger?.Invoke(ELoseReason.CapacityFull);
        }
        
        if (Input.GetKeyDown(KeyCode.F5))
        {
            CustomTimeScaleGroup.Instance.ApplyTimeScale(2f);
        }
#endif
    }

    private void SetActiveSettingReplayButton(bool active)
    {
        _isReplayButtonActiveByTutorial = active;
        if (settingButton != null)
        {
            var btn = settingButton.GetComponent<Button>();
            if (btn != null) btn.interactable = active && !_isUsingClawBooster;
        }
        UpdateReturnButtonInteractable();
        if (levelTextUI != null) levelTextUI.SetDisableLevelText(!active);
        if (capacityUI != null) capacityUI.SetDisableCapacity(!active);
    }

    private void UpdateReturnButtonInteractable()
    {
        if (returnButton != null)
        {
            var btn = returnButton.GetComponent<Button>();
            if (btn != null)
            {
                bool hasPlayed = HeartSystem.Instance != null && HeartSystem.Instance.HasPlayedLevel;
                bool isPreloseDelay = LevelManager.Instance != null && LevelManager.Instance.IsPreloseDelay;
                btn.interactable = _isReplayButtonActiveByTutorial && hasPlayed && !isPreloseDelay && !_isUsingClawBooster;
            }
        }
    }

    private void OnPreloseDelayChanged(bool isPreloseDelay)
    {
        UpdateReturnButtonInteractable();
    }

    private void ShowSthWrongPanel(BoosterType type)
    {
        sthWrongText.text = type switch
        {
            BoosterType.UndoBooster => TextUtility.GetI2("popup_warrning_undo_disable"),
            BoosterType.ClawMachineBooster => TextUtility.GetI2("popup_warrning_claw_machine_disable"),
            BoosterType.ExtraSlotBooster => TextUtility.GetI2("popup_warning_extra_box_disable"),
            _ => string.Empty
        };
        ShowSthWrongPanel(1.5f);
    }

    private void ShowSthWrongPanel(float displayDuration)
    {
        if (sthWrongPanel == null) return;

        if (_sthWrongMotionHandle.IsActive())
        {
            _sthWrongMotionHandle.TryCancel();
        }

        sthWrongCanvasGroup.gameObject.SetActive(true);
        sthWrongPanel.localScale = Vector3.zero;

        var sequence = LSequence.Create();
        sequence.Append(LMotion.Create(0f, 1f, 0.35f).WithEase(Ease.OutBack).BindToAlpha(sthWrongCanvasGroup));
        sequence.Join(LMotion.Create(Vector3.zero, Vector3.one, 0.35f)
            .WithEase(Ease.OutBack)
            .BindToLocalScale(sthWrongPanel));
        sequence.Append(LMotion.Create(0f, 1f, displayDuration).RunWithoutBinding());
        sequence.Append(LMotion.Create(1f, 0f, 0.25f).WithEase(Ease.InBack).BindToAlpha(sthWrongCanvasGroup));
        sequence.Join(LMotion.Create(Vector3.one, Vector3.zero, 0.25f)
            .WithEase(Ease.InBack)
            .WithOnComplete(() => { sthWrongCanvasGroup.gameObject.SetActive(false); })
            .BindToLocalScale(sthWrongPanel));
    
        _sthWrongMotionHandle = sequence.Run();
    }
}