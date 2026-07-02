using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource fxMusicSource;
    [SerializeField] private AudioSource loopFxSource;

    [SerializeField] public SoundDataSO soundDataSO;
    private SoundData BgMusicMainMenu => soundDataSO?.GetSoundData(AudioClipName.bg_music1);
    private SoundData BgMusicInGame => soundDataSO?.GetSoundData(AudioClipName.bg_music2);
    private float _originalBgMusicVolume = 1f;
    private AudioClipName? _currentLoopClipName = null;
    private AudioClipName? _pausedLoopClipName = null;

    private void OnValidate()
    {
        if (audioMixer == null)
        {
            var audioMixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
            if (audioMixers.Length > 0) audioMixer = audioMixers[0];
        }

        if (soundDataSO == null)
        {
            var soundDataSOs = Resources.FindObjectsOfTypeAll<SoundDataSO>();
            if(soundDataSOs.Length > 0) soundDataSO = soundDataSOs[0];
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (bgMusicSource != null)
        {
            _originalBgMusicVolume = bgMusicSource.volume;
        }
        GameEventBus.OnChangeSound = (OnSoundChange);
        GameEventBus.OnChangeSoundFx = (OnSoundFxChange);
        GameEventBus.OnInitLoadLevel += OnInitLoadLevel;
        GameEventBus.OnCustomTimeScaleChanged += OnCustomTimeScaleChanged;
    }

    private void OnDestroy()
    {
        GameEventBus.OnChangeSound = null;
        GameEventBus.OnChangeSoundFx = null;
        GameEventBus.OnInitLoadLevel -= OnInitLoadLevel;
        GameEventBus.OnCustomTimeScaleChanged -= OnCustomTimeScaleChanged;
    }

    private void Start()
    {
        var playMusic = PlayerPrefs.GetInt(GameSettingData.IsPlayMusicKey, 1);
        var playSound = PlayerPrefs.GetInt(GameSettingData.IsPlaySoundKey, 1);
        OnSoundChange(playMusic);
        OnSoundFxChange(playSound);
    }

    private bool _isBgmDeferred = false;

    private bool IsBgmDeferredForHardLevel()
    {
        if (ConfigHolder.Instance == null || ConfigHolder.Instance.LevelConfigSO == null)
            return false;
            
        if (DataManager.PlayerData == null)
            return false;
            
        var level = DataManager.PlayerData.LevelProgress;
        var levelData = ConfigHolder.Instance.LevelConfigSO.GetLevelData(level);
        return levelData != null && levelData.LevelType >= LevelType.Hard;
    }

    private bool IsBgmDeferredForBoosterTut()
    {
        if (ConfigHolder.Instance == null || ConfigHolder.Instance.BoosterDataSO == null)
            return false;
            
        if (DataManager.PlayerData == null)
            return false;
            
        var playerData = DataManager.PlayerData;
        playerData.TutorialDataList ??= new();

        // Check UndoBooster tutorial
        var undoConfig = ConfigHolder.Instance.BoosterDataSO.GetBoosterConfig(BoosterType.UndoBooster);
        if (undoConfig != null && playerData.LevelDisplay == undoConfig.LevelOpen && !playerData.TutorialDataList.ContainsKey("UndoBooster"))
        {
            return true;
        }

        // Check ClawMachineBooster tutorial
        var clawConfig = ConfigHolder.Instance.BoosterDataSO.GetBoosterConfig(BoosterType.ClawMachineBooster);
        if (clawConfig != null && playerData.LevelDisplay == clawConfig.LevelOpen && !playerData.TutorialDataList.ContainsKey("ClawMachineBooster"))
        {
            return true;
        }

        // Check ExtraSlotBooster tutorial
        var extraConfig = ConfigHolder.Instance.BoosterDataSO.GetBoosterConfig(BoosterType.ExtraSlotBooster);
        if (extraConfig != null && playerData.LevelDisplay == extraConfig.LevelOpen && !playerData.TutorialDataList.ContainsKey("ExtraSlotBooster"))
        {
            return true;
        }

        return false;
    }

    private bool IsBgmDeferredForNewFeature()
    {
        return CheckShowNewFeature.IsShowNewFeature;
    }

    private bool IsBgmDeferred()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsReplay)
            return false;

        return IsBgmDeferredForHardLevel() || IsBgmDeferredForBoosterTut() || IsBgmDeferredForNewFeature();
    }

    private void OnInitLoadLevel()
    {
        _currentLoopClipName = null;
        _pausedLoopClipName = null;
        if (IsBgmDeferred())
        {
            _isBgmDeferred = true;
            StopBackGroundMusic();
        }
        else
        {
            _isBgmDeferred = false;
        }
    }

    public void ResumeDeferredBgm()
    {
        _isBgmDeferred = false;
        PlayInGameBgm(forceReset: true);
    }

    public void StopBackGroundMusic()
    {
        if (bgMusicSource == null) return;
        bgMusicSource.loop = false;
        bgMusicSource.Stop();
    }

    public void SetBgmVolumeLow()
    {
        if (bgMusicSource == null) return;
        bgMusicSource.volume = 0f;
    }

    private void RestoreBgmVolume(float volumeDefault = -1f)
    {
        if (bgMusicSource == null) return;
        bgMusicSource.volume = volumeDefault >= 0f ? volumeDefault : _originalBgMusicVolume;
    }

    public void ChangeBgMusic(bool isBGMInGame)
    {
        if (isBGMInGame && IsBgmDeferred())
        {
            _isBgmDeferred = true;
            StopBackGroundMusic();
            return;
        }
        _isBgmDeferred = false;

        var soundData = isBGMInGame ? BgMusicInGame : BgMusicMainMenu;
        if (bgMusicSource == null || soundData?.Clip == null) return;
        RestoreBgmVolume(soundData.VolumeDefault);
        if (bgMusicSource.clip == soundData.Clip && bgMusicSource.isPlaying) return;
        bgMusicSource.clip = soundData.Clip;
        bgMusicSource.loop = true;
        bgMusicSource.Play();
    }

    public void PlayInGameBgm(bool forceReset = false)
    {
        if (bgMusicSource == null) return;
        if (_isBgmDeferred && IsBgmDeferred())
        {
            StopBackGroundMusic();
            return;
        }

        var soundData = BgMusicInGame;
        if (soundData?.Clip == null) return;
        
        RestoreBgmVolume(soundData.VolumeDefault);
        if (!forceReset && bgMusicSource.clip == soundData.Clip && bgMusicSource.isPlaying) return;
        
        bgMusicSource.clip = soundData.Clip;
        bgMusicSource.loop = true;
        bgMusicSource.time = 0f;
        bgMusicSource.Play();
    }

    public void PlayBackGroundMusic()
    {
        if (bgMusicSource == null || bgMusicSource.clip == null) return;
        if (_isBgmDeferred && IsBgmDeferred())
        {
            StopBackGroundMusic();
            return;
        }

        bgMusicSource.loop = true;
        RestoreBgmVolume();
        if (bgMusicSource.isPlaying) return;
        bgMusicSource.Play();
    }

    private void OnSoundChange(float currentValue)
    {
        if (audioMixer == null) return;
        float maxRangeDesign = 1f;
        currentValue = Math_Utility.Remap(currentValue, 0, 1, 0, maxRangeDesign);
        var soundValue = currentValue == 0 ? -100 : Mathf.Log10(currentValue) * 20;
        var parameterName = Enum.GetName(typeof(SoundMixerGroup), SoundMixerGroup.BGMusic);
        var checkSet = audioMixer.SetFloat(parameterName, soundValue);
#if UNITY_EDITOR
        if (!checkSet) Debug.LogError($"không set được giá trị audio mixer với parameter {parameterName}");
#endif
    }
    private void OnSoundFxChange(float currentValue)
    {
        if (audioMixer == null) return;
        currentValue *= 2; // sound fx có âm lượng gấp đôi
        var soundValue = currentValue == 0 ? -100 : Mathf.Log10(currentValue) * 20;
        var parameterName = Enum.GetName(typeof(SoundMixerGroup), SoundMixerGroup.SoundFx);
        var checkSet = audioMixer.SetFloat(parameterName, soundValue);
#if UNITY_EDITOR
        if (!checkSet) Debug.LogError($"không set được giá trị audio mixer với parameter {parameterName}");
#endif
    }

    /// <summary>
    /// Hàm phát một âm thanh với Mixer là Sound
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="volume"></param>
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        fxMusicSource.PlayOneShot(clip, volume);
    }

    public void PlayOneShot(AudioClipName clipName)
    {
        var soundData = soundDataSO.GetSoundData(clipName);
        if (soundData == null) return;
        fxMusicSource.PlayOneShot(soundData.Clip, soundData.VolumeDefault);
    }

    public float GetClipLength(AudioClipName clipName)
    {
        var soundData = soundDataSO.GetSoundData(clipName);
        if (soundData == null || soundData.Clip == null) return 0f;
        return soundData.Clip.length;
    }

    public void StopOneShot()
    {
        fxMusicSource.Stop();
    }

    /// <summary>
    /// Phát âm thanh lặp vô hạn trên loopFxSource (chỉ một clip tại một thời điểm)
    /// </summary>
    public void PlayLoop(AudioClipName clipName)
    {
        if (loopFxSource == null) return;
        var soundData = soundDataSO.GetSoundData(clipName);
        if (soundData == null || soundData.Clip == null) return;
        _currentLoopClipName = clipName;
        loopFxSource.clip = soundData.Clip;
        loopFxSource.volume = soundData.VolumeDefault;
        loopFxSource.loop = true;
        loopFxSource.Play();
    }

    /// <summary>
    /// Dừng âm thanh đang loop
    /// </summary>
    public void StopLoop()
    {
        _currentLoopClipName = null;
        _pausedLoopClipName = null;
        if (loopFxSource == null) return;
        loopFxSource.loop = false;
        loopFxSource.Stop();
    }

    private void OnCustomTimeScaleChanged(float timeScale)
    {
        if (timeScale == 0f)
        {
            if (loopFxSource != null && loopFxSource.isPlaying && _currentLoopClipName.HasValue)
            {
                _pausedLoopClipName = _currentLoopClipName;
                loopFxSource.loop = false;
                loopFxSource.Stop();
            }
        }
        else
        {
            if (_pausedLoopClipName.HasValue)
            {
                PlayLoop(_pausedLoopClipName.Value);
                _pausedLoopClipName = null;
            }
        }
    }
}
// đặt theo exposed Parameters trong audio mixer
public enum SoundMixerGroup
{
    BGMusic,
    SoundFx,
}
