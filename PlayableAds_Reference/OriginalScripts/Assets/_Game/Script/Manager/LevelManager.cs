using UnityEngine;
using Cysharp.Threading.Tasks;
using Alchemy.Inspector;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-100)]
public class LevelManager : MonoSingleton<LevelManager>
{
    static LevelManager()
    {
        AutoCreate = false;
    }

    [SerializeField] private LevelConfigSO levelConfigSo;
    [SerializeField] private LevelEntryAnimConfigSO levelEntryAnimConfig;

    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private CarrierSystem carrierSystem;
    [SerializeField] private CapacityManager capacityManager;

    public LevelEntryAnimConfigSO LevelEntryAnimConfig
    {
        get
        {
            if (levelEntryAnimConfig == null)
            {
                Debug.LogError("[LevelManager] levelEntryAnimConfig is not assigned! Please assign LevelEntryAnimConfigSO in the Inspector.");
            }
            return levelEntryAnimConfig;
        }
    }

    public int CurrentLevelIndex { get; private set; }
    public bool IsLevelLoaded { get; private set; }
    public bool IsReplay { get; private set; }
    public bool IsGameEnded { get; set; }
    private bool _isPreloseDelay;
    public bool IsPreloseDelay
    {
        get => _isPreloseDelay;
        set
        {
            if (_isPreloseDelay != value)
            {
                _isPreloseDelay = value;
                GameEventBus.OnPreloseDelayChanged?.Invoke(value);
            }
        }
    }
    public bool IsPreloseDelayPaused { get; set; }
    public int PreloseCount { get; set; }
    private LevelData CurrentLevelData { get; set; }
    public LevelData CurrentLevel => CurrentLevelData;

    public bool IsTutorial;

    private int _currentLevelIndex;
    private CancellationTokenSource _levelLoadCts;
    private bool _isFirstLoadAfterMainMenu = true;

    protected override void Awake()
    {
        base.Awake();
        if (levelEntryAnimConfig == null)
        {
            Debug.LogError("[LevelManager] levelEntryAnimConfig is not assigned! Please assign LevelEntryAnimConfigSO in the Inspector.");
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        // Nếu chơi thẳng từ Scene GamePlay trên Editor, tự động nạp Level 1 để test
        if (SceneLoader.GetCurrentActiveScene() != SCENE_NAME.S_Main.ToString())
        {
            LoadLevel(1).Forget();
        }
#endif
    }

    public void GeneratorLevel()
    {
        int levelIndex = DataManager.PlayerData.LevelProgress;
        _currentLevelIndex = levelIndex;

        CheckShowNewFeature.IsShowNewFeature = CheckShowNewFeature.CanShowNewFeature();
        if (CheckShowNewFeature.IsShowNewFeature)
        {
            CheckShowNewFeature.ShowPopupNewFeature();
        }

        LoadLevel(levelIndex).Forget();
    }

    public async UniTask LoadLevel(int levelIndex)
    {
        _levelLoadCts?.Cancel();
        _levelLoadCts?.Dispose();
        _levelLoadCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = _levelLoadCts.Token;

        try
        {
            IsLevelLoaded = false;
            IsReplay = !_isFirstLoadAfterMainMenu && levelIndex == CurrentLevelIndex;
            await UniTask.DelayFrame(1, cancellationToken: token);

            GameEventBus.OnInitLoadLevel?.Invoke();

            // todo : xu ly unload level
            ConveyorDeliverySystem.Instance?.ClearAllCubes();
            IsGameEnded = false;
            IsPreloseDelay = false;
            IsPreloseDelayPaused = false;
            PreloseCount = 0;
            InputController.Disable();

            int targetLevelIndex = levelIndex;
            if (levelConfigSo != null)
            {
                targetLevelIndex = levelConfigSo.GetLevelToLoad(levelIndex);
            }

            LevelData levelData = null;
#if DEVELOPER_LOAD_LEVEL_REMOTE
            levelData = await LevelDataLoaderHelper.LoadLevelAsync(targetLevelIndex - 1);
            if (levelData == null)
            {
                Debug.LogWarning($"[LevelManager] Failed to load level {levelIndex} (mapped: {targetLevelIndex}) from remote server, fallback to LevelConfigSO");
                if (levelConfigSo != null)
                {
                    levelData = levelConfigSo.GetLevelData(levelIndex);
                }
            }
#else
            if (levelConfigSo != null)
            {
                levelData = levelConfigSo.GetLevelData(levelIndex);
            }
#endif

            CurrentLevelData = levelData;
            CurrentLevelIndex = levelIndex;
            
            if (_isFirstLoadAfterMainMenu)
            {
                _isFirstLoadAfterMainMenu = false;
                SoundManager.Instance?.PlayInGameBgm(forceReset: false);
            }
            else
            {
                SoundManager.Instance?.PlayInGameBgm(forceReset: true);
            }

            if (CustomTimeScaleGroup.Instance)
            {
                CustomTimeScaleGroup.Instance.ApplyTimeScale(1f);
                CustomTimeScaleGroup.Instance.ClearTargets();
            }
            await HandleLevelDataAsync(CurrentLevelData, token);
            if (BoosterSystem.Instance) BoosterSystem.Instance.TryCancelClawBoosterOnMissedClick();
            GameEventBus.OnLevelLoaded?.Invoke(levelData);
            GameEventBus.OnLoadLevelDone?.Invoke();
            if (HeartSystem.Instance) HeartSystem.Instance.HasPlayedLevel = false;
            
            var gameMode = CurrentLevelData != null ? CurrentLevelData.LevelType.ToString() : "Normal";
            UserBehaviorTracker.SendStartGameTracking(gameMode);
            CustomTimeScaleGroup.Instance.ApplyTimeScale(1f);
           
            
            IsLevelLoaded = true;
            InputController.Enable();
            GameEventBus.PlayAnimButton?.Invoke();
        }
        catch (System.OperationCanceledException)
        {
            // Cancelled due to loading another level
        }
    }

    [Button]
    public void NextLevel()
    {
        LoadLevel(CurrentLevelIndex + 1).Forget();
    }

    [Button]
    public void RestartLevel()
    {
        LoadLevel(CurrentLevelIndex).Forget();
    }

    [Button]
    public void BackLevel()
    {
        LoadLevel(CurrentLevelIndex - 1).Forget();
    }
    [Button]
    public void LoadLevelByIndex(int levelIndex)
    {
        LoadLevel(levelIndex).Forget();
    }
    private async UniTask HandleLevelDataAsync(LevelData levelData, CancellationToken token)
    {
        ApplyCameraSettings(levelData);
        capacityManager.Init(levelData);
        carrierSystem.InitCarrier(levelData);
        if (SwappingBlockManager.Instance != null)
        {
            SwappingBlockManager.Instance.InitializeLevel();
        }
        await conveyorManager.InitConveyorAsync(levelData.SplineLayout);
        token.ThrowIfCancellationRequested();

        if (ConveyorDeliverySystem.Instance != null)
        {
            ConveyorDeliverySystem.Instance.RefreshPreloseBlink();
        }

        conveyorManager.SetRevealProgress(0f);

        if (UIGeneratorLevelProcess.Instance != null)
        {
            UIGeneratorLevelProcess.Instance.HideAsync().Forget();
        }
        
        await UniTask.WaitUntil(() => !CheckShowNewFeature.IsShowNewFeature, cancellationToken: token);
        
        await conveyorManager.PlayRevealAnimation();
        token.ThrowIfCancellationRequested();

        if (carrierSystem != null)
        {
            await carrierSystem.PlayContainersScaleAnimation(token);
        }
        token.ThrowIfCancellationRequested();

        if (carrierSystem != null && carrierSystem.CarrierSpawner != null)
        {
            await carrierSystem.CarrierSpawner.PlayCarriersScaleAnimation();
        }
        token.ThrowIfCancellationRequested();

        BoosterSystem.Instance.Init(levelData);
        BlockLinkVisualManager.Instance.SetupLevelLinks();
    }

    private void ApplyCameraSettings(LevelData levelData)
    {
        if (!levelData) return;
        CameraManager.Instance.SyncOrthographicCamera(levelData.OrthographicSize);
    }

    public void CancelPreloseDelay()
    {
        if (IsPreloseDelay)
        {
            IsGameEnded = false;
            IsPreloseDelay = false;
            IsPreloseDelayPaused = false;
            CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnReloadCurrentLevel += RestartLevel;
        GameStateManager.OnGameStateChange.Register(OnGameStateChange);
    }

    private void OnDisable()
    {
        GameEventBus.OnReloadCurrentLevel -= RestartLevel;
        GameStateManager.OnGameStateChange.UnRegister(OnGameStateChange);
    }

    private void OnGameStateChange(GameState gameState)
    {
        if (gameState == GameState.MainMenu)
        {
            _isFirstLoadAfterMainMenu = true;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && HeartSystem.Instance != null && HeartSystem.Instance.HasPlayedLevel)
        {
            DataManager.NoteConsumeHeart();
        }
    }

    private void OnApplicationQuit()
    {
        if (HeartSystem.Instance != null && HeartSystem.Instance.HasPlayedLevel)
        {
            DataManager.NoteConsumeHeart();
        }
    }
}
