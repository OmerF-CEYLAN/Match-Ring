using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip gameOverStinger;
    [SerializeField] private AudioClip perfectMatchEffect;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Default Volumes")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1.0f;

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsMuted { get; private set; }

    private AudioSource musicSource;
    private AudioSource[] sfxPool;
    private const int SFX_POOL_SIZE = 6;

    private EventBinding<GameOverEvent> gameOverBinding;
    private EventBinding<PlaySFXEvent> playSFXBinding;
    private EventBinding<PlayMusicEvent> playMusicBinding;
    private EventBinding<PerfectMatchSFXEvent> perfectMatchSFXBinding;

    private const string KeyMusicVolume = "Audio_MusicVol";
    private const string KeySFXVolume = "Audio_SFXVol";
    private const string KeyMuted = "Audio_Muted";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
        LoadSettings();
    }

    private void Start()
    {
        if (backgroundMusic != null)
            PlayMusic(backgroundMusic);
    }

    private void OnEnable()
    {
        gameOverBinding = new EventBinding<GameOverEvent>(HandleGameOver);
        playSFXBinding = new EventBinding<PlaySFXEvent>(HandlePlaySFX);
        playMusicBinding = new EventBinding<PlayMusicEvent>(HandlePlayMusic);
        perfectMatchSFXBinding = new EventBinding<PerfectMatchSFXEvent>(HandlePerfectMatchSFX);

        EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Subscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Subscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Subscribe(perfectMatchSFXBinding);
    }

    private void OnDisable()
    {
        EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Unsubscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Unsubscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Unsubscribe(perfectMatchSFXBinding);
    }

    private void HandleGameOver(GameOverEvent e)
    {
        if (gameOverStinger != null)
        {
            StopMusic();
            PlaySFX(gameOverStinger);

            if (backgroundMusic != null)
                StartCoroutine(PlayMusicAfterDelay(backgroundMusic, gameOverStinger.length));
        }
        else if (backgroundMusic != null)
        {
            FadeToMusic(backgroundMusic);
        }
        else
        {
            FadeOutMusic();
        }
    }

    private void HandlePlaySFX(PlaySFXEvent e)
    {
        PlaySFX(e.clip, e.volumeScale, e.pitchVariance);
    }

    private void HandlePlayMusic(PlayMusicEvent e)
    {
        if (e.fade)
            FadeToMusic(e.clip);
        else
            PlayMusic(e.clip);
    }

    private void HandlePerfectMatchSFX()
    {
        PlaySFX(perfectMatchEffect);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        StopAllCoroutines();

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = IsMuted ? 0f : MusicVolume;
        musicSource.Play();
    }

    public void FadeToMusic(AudioClip clip, float? duration = null)
    {
        if (clip == null) return;

        StopAllCoroutines();
        StartCoroutine(FadeMusicRoutine(clip, duration ?? fadeDuration));
    }

    public void FadeOutMusic(float? duration = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(duration ?? fadeDuration));
    }

    public void StopMusic()
    {
        StopAllCoroutines();
        musicSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null || IsMuted) return;

        AudioSource source = GetFreeSFXSource();

        source.clip = clip;
        source.loop = false;
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.volume = SFXVolume * Mathf.Clamp01(volumeScale);
        source.Play();
    }

    public void StopAllSFX()
    {
        foreach (var s in sfxPool)
            s.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);

        if (!IsMuted)
            musicSource.volume = MusicVolume;

        PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(KeySFXVolume, SFXVolume);
        PlayerPrefs.Save();
    }

    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }

    public void SetMute(bool muted)
    {
        IsMuted = muted;

        musicSource.volume = IsMuted ? 0f : MusicVolume;

        foreach (var s in sfxPool)
            s.volume = IsMuted ? 0f : SFXVolume;

        PlayerPrefs.SetInt(KeyMuted, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void BuildAudioSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f;

        sfxPool = new AudioSource[SFX_POOL_SIZE];

        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].playOnAwake = false;
            sfxPool[i].loop = false;
        }
    }

    private AudioSource GetFreeSFXSource()
    {
        foreach (var s in sfxPool)
        {
            if (!s.isPlaying)
                return s;
        }

        AudioSource candidate = sfxPool[0];
        float shortestRemain = float.MaxValue;

        foreach (var s in sfxPool)
        {
            float remaining = s.clip != null ? s.clip.length - s.time : 0f;

            if (remaining < shortestRemain)
            {
                shortestRemain = remaining;
                candidate = s;
            }
        }

        return candidate;
    }

    private void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, defaultMusicVolume);
        SFXVolume = PlayerPrefs.GetFloat(KeySFXVolume, defaultSFXVolume);
        IsMuted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;

        musicSource.volume = IsMuted ? 0f : MusicVolume;
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
    {
        float start = musicSource.volume;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        float target = IsMuted ? 0f : MusicVolume;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, target, t / duration);
            yield return null;
        }

        musicSource.volume = target;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float start = musicSource.volume;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
    }

    private IEnumerator PlayMusicAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        FadeToMusic(clip);
    }
}