using System;
using UnityEngine;

public enum ELoseReason
{
    None = 0,
    CapacityFull = 1,
    Deadlock = 2
}

public static class GameEventBus
{
    public static Action OnInitLoadLevel;
    public static Action OnLoadLevelDone;
    public static Action<LevelData> OnLevelLoaded;
    public static Action OnReloadCurrentLevel;

    public static Action<int, int> OnUpdateCapcityUI;
    public static Action<float> OnCustomTimeScaleChanged;
    public static Action<bool> OnPreloseDelayChanged;

    public static Action<EBlockColorType> OnCarrierFinished;
    public static Action OnContainerUnlocked;
    public static Action OnCarrierUnload;
    public static Action OnCarrierUnloadDone;
    public static Action OnCarrierPickupDone;

    public static Action OnWinTrigger;
    public static Action<ELoseReason> OnLoseTrigger;
    public static Action OnEndGame;
    public static Action OnLevelWin;
    public static Action OnPlayableWin;
    public static Action<ELoseReason> OnPlayableLose;

    public static Action<float> OnChangeSound;
    public static Action<float> OnChangeSoundFx;
    public static Action<ActiveCameraPlace, bool> OnActiveCameraGameplay;
    public static Action<bool> OnHighlightCameraActiveChanged;
}
