using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LevelManager : MonoSingleton<LevelManager>
{
    static LevelManager()
    {
        AutoCreate = false;
    }

    [Header("Playable level sequence")]
    [Tooltip("Assign only the LevelData assets used by this playable, in play order.")]
    [SerializeField] private List<LevelData> playableLevels = new List<LevelData>();
    [SerializeField, Min(0)] private int startLevelIndex;
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool autoPlayNextLevel = true;
    [SerializeField] private bool loopLevelSequence;

    [Header("Animation")]
    [SerializeField] private LevelEntryAnimConfigSO levelEntryAnimConfig;

    [Header("Scene managers")]
    [SerializeField] private ConveyorManager conveyorManager;
    [SerializeField] private CarrierSystem carrierSystem;
    [SerializeField] private CapacityManager capacityManager;

    private Coroutine _loadRoutine;
    private bool _isPreloseDelay;

    public LevelEntryAnimConfigSO LevelEntryAnimConfig
    {
        get { return levelEntryAnimConfig; }
    }

    public int CurrentLevelIndex { get; private set; }
    public bool IsLevelLoaded { get; private set; }
    public bool IsReplay { get; private set; }
    public bool IsGameEnded { get; set; }
    public bool IsPreloseDelayPaused { get; set; }
    public int PreloseCount { get; set; }
    public LevelData CurrentLevel { get; private set; }
    public bool IsTutorial;

    public bool IsPreloseDelay
    {
        get { return _isPreloseDelay; }
        set
        {
            if (_isPreloseDelay == value) return;
            _isPreloseDelay = value;
            if (GameEventBus.OnPreloseDelayChanged != null)
            {
                GameEventBus.OnPreloseDelayChanged(value);
            }
        }
    }

    private void Start()
    {
        if (loadOnStart)
        {
            LoadLevel(startLevelIndex + 1);
        }
    }

    private void OnEnable()
    {
        GameEventBus.OnReloadCurrentLevel += RestartLevel;
        GameEventBus.OnLevelWin += HandleLevelWin;
    }

    private void OnDisable()
    {
        GameEventBus.OnReloadCurrentLevel -= RestartLevel;
        GameEventBus.OnLevelWin -= HandleLevelWin;
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }
    }

    public void GeneratorLevel()
    {
        LoadLevel(startLevelIndex + 1);
    }

    public void LoadLevel(int levelIndex)
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
        }
        _loadRoutine = StartCoroutine(LoadLevelRoutine(levelIndex));
    }

    public void NextLevel()
    {
        int nextLevelIndex;
        if (TryGetNextLevelIndex(out nextLevelIndex))
        {
            LoadLevel(nextLevelIndex);
        }
    }

    public void RestartLevel()
    {
        LoadLevel(CurrentLevelIndex > 0 ? CurrentLevelIndex : 1);
    }

    public void BackLevel()
    {
        if (playableLevels == null || playableLevels.Count == 0) return;
        var previous = Mathf.Max(1, CurrentLevelIndex - 1);
        LoadLevel(previous);
    }

    public void LoadLevelByIndex(int levelIndex)
    {
        LoadLevel(levelIndex);
    }

    private IEnumerator LoadLevelRoutine(int levelIndex)
    {
        IsLevelLoaded = false;
        IsReplay = CurrentLevel != null;
        InputController.Disable();
        yield return null;

        if (GameEventBus.OnInitLoadLevel != null) GameEventBus.OnInitLoadLevel();
        if (ConveyorDeliverySystem.Instance != null)
        {
            ConveyorDeliverySystem.Instance.ClearAllCubes();
        }

        IsGameEnded = false;
        IsPreloseDelay = false;
        IsPreloseDelayPaused = false;
        PreloseCount = 0;

        CurrentLevel = ResolvePlayableLevel(levelIndex);
        if (CurrentLevel == null)
        {
            Debug.LogError("[LevelManager] No LevelData is assigned at sequence index " + levelIndex + ".");
            _loadRoutine = null;
            yield break;
        }
        CurrentLevelIndex = levelIndex;

        var timeScale = CustomTimeScaleGroup.Instance;
        if (timeScale != null)
        {
            timeScale.ApplyTimeScale(1f);
            timeScale.ClearTargets();
        }

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SyncOrthographicCamera(CurrentLevel.OrthographicSize);
        }

        if (capacityManager != null) capacityManager.Init(CurrentLevel);
        if (conveyorManager != null)
        {
            yield return conveyorManager.InitConveyor(CurrentLevel.SplineLayout);
#if UNITY_LUNA
            conveyorManager.SetRevealProgress(1f);
#else
            conveyorManager.SetRevealProgress(0f);
#endif
        }
        if (carrierSystem != null)
        {
            carrierSystem.InitCarrier(CurrentLevel, conveyorManager != null ? conveyorManager.Path : null);
        }

        if (ConveyorDeliverySystem.Instance != null)
        {
            ConveyorDeliverySystem.Instance.RefreshPreloseBlink();
        }

        if (conveyorManager != null)
        {
#if UNITY_LUNA
            conveyorManager.SetRevealProgress(1f);
#else
            yield return conveyorManager.PlayRevealAnimation();
            conveyorManager.SetRevealProgress(1f);
#endif
        }

        if (carrierSystem != null)
        {
#if !UNITY_LUNA
            yield return carrierSystem.PlayContainersScaleAnimation(levelEntryAnimConfig);
#endif
        }

        if (carrierSystem != null && carrierSystem.CarrierSpawner != null)
        {
#if UNITY_LUNA
            carrierSystem.CarrierSpawner.EnsureCarriersVisibleAndClickable();
#else
            yield return carrierSystem.CarrierSpawner.PlayCarriersScaleAnimation();
            carrierSystem.CarrierSpawner.EnsureCarriersVisibleAndClickable();
#endif
        }

        if (BlockLinkVisualManager.Instance != null)
        {
            BlockLinkVisualManager.Instance.SetupLevelLinks();
        }

        SoundManager.Instance.PlayInGameBgm(IsReplay);
        IsLevelLoaded = true;
        InputController.Enable();
        if (GameEventBus.OnLevelLoaded != null) GameEventBus.OnLevelLoaded(CurrentLevel);
        if (GameEventBus.OnLoadLevelDone != null) GameEventBus.OnLoadLevelDone();
        _loadRoutine = null;
    }

    private LevelData ResolvePlayableLevel(int levelIndex)
    {
        if (playableLevels == null || playableLevels.Count == 0) return null;
        var listIndex = levelIndex - 1;
        if (listIndex < 0 || listIndex >= playableLevels.Count) return null;
        return playableLevels[listIndex];
    }

    private void HandleLevelWin()
    {
        int nextLevelIndex;
        if (autoPlayNextLevel && TryGetNextLevelIndex(out nextLevelIndex))
        {
            LoadLevel(nextLevelIndex);
            return;
        }

        if (GameEventBus.OnPlayableWin != null)
        {
            GameEventBus.OnPlayableWin();
        }
    }

    private bool TryGetNextLevelIndex(out int nextLevelIndex)
    {
        nextLevelIndex = 0;
        if (playableLevels == null || playableLevels.Count == 0) return false;

        var candidate = CurrentLevelIndex + 1;
        if (candidate <= playableLevels.Count)
        {
            nextLevelIndex = candidate;
            return true;
        }

        if (!loopLevelSequence) return false;
        nextLevelIndex = 1;
        return true;
    }

    public void CancelPreloseDelay()
    {
        if (!IsPreloseDelay) return;
        IsGameEnded = false;
        IsPreloseDelay = false;
        IsPreloseDelayPaused = false;
        var timeScale = CustomTimeScaleGroup.Instance;
        if (timeScale != null) timeScale.ApplyTimeScale(1f);
    }
}
