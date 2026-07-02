using UnityEngine;

public class SoundManager : MonoSingleton<SoundManager>
{
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private SoundDataSO soundData;
    [SerializeField] private bool playMusicOnStart;

    private AudioClipName? _loopClip;

    protected override void Awake()
    {
        base.Awake();
        EnsureAudioSources();
        GameEventBus.OnChangeSound += SetMusicVolume;
        GameEventBus.OnChangeSoundFx += SetEffectsVolume;
        GameEventBus.OnCustomTimeScaleChanged += HandleTimeScaleChanged;
        GameEventBus.OnInitLoadLevel += StopLoop;
    }

    private void Start()
    {
        if (playMusicOnStart)
        {
            PlayInGameBgm(false);
        }
    }

    protected override void OnDestroy()
    {
        GameEventBus.OnChangeSound -= SetMusicVolume;
        GameEventBus.OnChangeSoundFx -= SetEffectsVolume;
        GameEventBus.OnCustomTimeScaleChanged -= HandleTimeScaleChanged;
        GameEventBus.OnInitLoadLevel -= StopLoop;
        base.OnDestroy();
    }

    private void EnsureAudioSources()
    {
        if (bgMusicSource == null)
        {
            bgMusicSource = gameObject.AddComponent<AudioSource>();
        }

        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
        }

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
        }

        bgMusicSource.playOnAwake = false;
        oneShotSource.playOnAwake = false;
        loopSource.playOnAwake = false;
    }

    public void PlayInGameBgm(bool forceReset)
    {
        PlayMusic(AudioClipName.bg_music2, forceReset);
    }

    public void PlayBackGroundMusic()
    {
        if (bgMusicSource != null && bgMusicSource.clip != null && !bgMusicSource.isPlaying)
        {
            bgMusicSource.Play();
        }
    }

    public void StopBackGroundMusic()
    {
        if (bgMusicSource != null)
        {
            bgMusicSource.Stop();
        }
    }

    private void PlayMusic(AudioClipName clipName, bool forceReset)
    {
        var entry = GetSound(clipName);
        if (entry == null || entry.Clip == null || bgMusicSource == null)
        {
            return;
        }

        if (!forceReset && bgMusicSource.clip == entry.Clip && bgMusicSource.isPlaying)
        {
            return;
        }

        bgMusicSource.clip = entry.Clip;
        bgMusicSource.volume = entry.VolumeDefault;
        bgMusicSource.loop = true;
        bgMusicSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip != null && oneShotSource != null)
        {
            oneShotSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayOneShot(AudioClip clip)
    {
        PlayOneShot(clip, 1f);
    }

    public void PlayOneShot(AudioClipName clipName)
    {
        var entry = GetSound(clipName);
        if (entry != null)
        {
            PlayOneShot(entry.Clip, entry.VolumeDefault);
        }
    }

    public float GetClipLength(AudioClipName clipName)
    {
        var entry = GetSound(clipName);
        return entry != null && entry.Clip != null ? entry.Clip.length : 0f;
    }

    public void StopOneShot()
    {
        if (oneShotSource != null)
        {
            oneShotSource.Stop();
        }
    }

    public void PlayLoop(AudioClipName clipName)
    {
        var entry = GetSound(clipName);
        if (entry == null || entry.Clip == null || loopSource == null)
        {
            return;
        }

        _loopClip = clipName;
        loopSource.clip = entry.Clip;
        loopSource.volume = entry.VolumeDefault;
        loopSource.loop = true;
        loopSource.Play();
    }

    public void StopLoop()
    {
        _loopClip = null;
        if (loopSource != null)
        {
            loopSource.Stop();
            loopSource.clip = null;
        }
    }

    private void HandleTimeScaleChanged(float scale)
    {
        if (loopSource == null || !_loopClip.HasValue)
        {
            return;
        }

        if (scale <= 0f)
        {
            loopSource.Pause();
        }
        else if (!loopSource.isPlaying)
        {
            loopSource.UnPause();
        }
    }

    private void SetMusicVolume(float volume)
    {
        if (bgMusicSource != null)
        {
            bgMusicSource.volume = Mathf.Clamp01(volume);
        }
    }

    private void SetEffectsVolume(float volume)
    {
        var clamped = Mathf.Clamp01(volume);
        if (oneShotSource != null) oneShotSource.volume = clamped;
        if (loopSource != null) loopSource.volume = clamped;
    }

    private SoundData GetSound(AudioClipName clipName)
    {
        return soundData != null ? soundData.GetSoundData(clipName) : null;
    }
}
