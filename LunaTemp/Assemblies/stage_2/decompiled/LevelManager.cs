using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LevelManager : MonoSingleton<LevelManager>
{
	[Header("Playable level sequence")]
	[Tooltip("Assign only the LevelData assets used by this playable, in play order.")]
	[SerializeField]
	private List<LevelData> playableLevels = new List<LevelData>();

	[SerializeField]
	[Min(0f)]
	private int startLevelIndex;

	[SerializeField]
	private bool loadOnStart = true;

	[SerializeField]
	private bool autoPlayNextLevel = true;

	[SerializeField]
	private bool loopLevelSequence;

	[Header("Animation")]
	[SerializeField]
	private LevelEntryAnimConfigSO levelEntryAnimConfig;

	[Header("Scene managers")]
	[SerializeField]
	private ConveyorManager conveyorManager;

	[SerializeField]
	private CarrierSystem carrierSystem;

	[SerializeField]
	private CapacityManager capacityManager;

	private Coroutine _loadRoutine;

	private bool _isPreloseDelay;

	public bool IsTutorial;

	public LevelEntryAnimConfigSO LevelEntryAnimConfig => levelEntryAnimConfig;

	public int CurrentLevelIndex { get; private set; }

	public bool IsLevelLoaded { get; private set; }

	public bool IsReplay { get; private set; }

	public bool IsGameEnded { get; set; }

	public bool IsPreloseDelayPaused { get; set; }

	public int PreloseCount { get; set; }

	public LevelData CurrentLevel { get; private set; }

	public bool IsPreloseDelay
	{
		get
		{
			return _isPreloseDelay;
		}
		set
		{
			if (_isPreloseDelay != value)
			{
				_isPreloseDelay = value;
				if (GameEventBus.OnPreloseDelayChanged != null)
				{
					GameEventBus.OnPreloseDelayChanged(value);
				}
			}
		}
	}

	static LevelManager()
	{
		MonoSingleton<LevelManager>.AutoCreate = false;
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
		GameEventBus.OnReloadCurrentLevel = (Action)Delegate.Combine(GameEventBus.OnReloadCurrentLevel, new Action(RestartLevel));
		GameEventBus.OnLevelWin = (Action)Delegate.Combine(GameEventBus.OnLevelWin, new Action(HandleLevelWin));
	}

	private void OnDisable()
	{
		GameEventBus.OnReloadCurrentLevel = (Action)Delegate.Remove(GameEventBus.OnReloadCurrentLevel, new Action(RestartLevel));
		GameEventBus.OnLevelWin = (Action)Delegate.Remove(GameEventBus.OnLevelWin, new Action(HandleLevelWin));
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
		if (TryGetNextLevelIndex(out var nextLevelIndex))
		{
			LoadLevel(nextLevelIndex);
		}
	}

	public void RestartLevel()
	{
		LoadLevel((CurrentLevelIndex <= 0) ? 1 : CurrentLevelIndex);
	}

	public void BackLevel()
	{
		if (playableLevels != null && playableLevels.Count != 0)
		{
			int previous = Mathf.Max(1, CurrentLevelIndex - 1);
			LoadLevel(previous);
		}
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
		if (GameEventBus.OnInitLoadLevel != null)
		{
			GameEventBus.OnInitLoadLevel();
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null)
		{
			MonoSingleton<ConveyorDeliverySystem>.Instance.ClearAllCubes();
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
		CustomTimeScaleGroup timeScale = MonoSingleton<CustomTimeScaleGroup>.Instance;
		if (timeScale != null)
		{
			timeScale.ApplyTimeScale(1f);
			timeScale.ClearTargets();
		}
		if (MonoSingleton<CameraManager>.Instance != null)
		{
			MonoSingleton<CameraManager>.Instance.SyncOrthographicCamera(CurrentLevel.OrthographicSize);
		}
		if (capacityManager != null)
		{
			capacityManager.Init(CurrentLevel);
		}
		if (conveyorManager != null)
		{
			yield return conveyorManager.InitConveyor(CurrentLevel.SplineLayout);
			conveyorManager.SetRevealProgress(1f);
		}
		if (carrierSystem != null)
		{
			carrierSystem.InitCarrier(CurrentLevel, (conveyorManager != null) ? conveyorManager.Path : null);
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null)
		{
			MonoSingleton<ConveyorDeliverySystem>.Instance.RefreshPreloseBlink();
		}
		if (conveyorManager != null)
		{
			conveyorManager.SetRevealProgress(1f);
		}
		if (carrierSystem != null)
		{
		}
		if (carrierSystem != null && carrierSystem.CarrierSpawner != null)
		{
			carrierSystem.CarrierSpawner.EnsureCarriersVisibleAndClickable();
		}
		if (MonoSingleton<BlockLinkVisualManager>.Instance != null)
		{
			MonoSingleton<BlockLinkVisualManager>.Instance.SetupLevelLinks();
		}
		MonoSingleton<SoundManager>.Instance.PlayInGameBgm(IsReplay);
		IsLevelLoaded = true;
		InputController.Enable();
		if (GameEventBus.OnLevelLoaded != null)
		{
			GameEventBus.OnLevelLoaded(CurrentLevel);
		}
		if (GameEventBus.OnLoadLevelDone != null)
		{
			GameEventBus.OnLoadLevelDone();
		}
		_loadRoutine = null;
	}

	private LevelData ResolvePlayableLevel(int levelIndex)
	{
		if (playableLevels == null || playableLevels.Count == 0)
		{
			return null;
		}
		int listIndex = levelIndex - 1;
		if (listIndex < 0 || listIndex >= playableLevels.Count)
		{
			return null;
		}
		return playableLevels[listIndex];
	}

	private void HandleLevelWin()
	{
		if (autoPlayNextLevel && TryGetNextLevelIndex(out var nextLevelIndex))
		{
			LoadLevel(nextLevelIndex);
		}
		else if (GameEventBus.OnPlayableWin != null)
		{
			GameEventBus.OnPlayableWin();
		}
	}

	private bool TryGetNextLevelIndex(out int nextLevelIndex)
	{
		nextLevelIndex = 0;
		if (playableLevels == null || playableLevels.Count == 0)
		{
			return false;
		}
		int candidate = CurrentLevelIndex + 1;
		if (candidate <= playableLevels.Count)
		{
			nextLevelIndex = candidate;
			return true;
		}
		if (!loopLevelSequence)
		{
			return false;
		}
		nextLevelIndex = 1;
		return true;
	}

	public void CancelPreloseDelay()
	{
		if (IsPreloseDelay)
		{
			IsGameEnded = false;
			IsPreloseDelay = false;
			IsPreloseDelayPaused = false;
			CustomTimeScaleGroup timeScale = MonoSingleton<CustomTimeScaleGroup>.Instance;
			if (timeScale != null)
			{
				timeScale.ApplyTimeScale(1f);
			}
		}
	}
}
