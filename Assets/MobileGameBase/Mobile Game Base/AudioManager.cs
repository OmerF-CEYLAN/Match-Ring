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

    [Header("Combo Sound")]
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private float comboBasePitch = 1f;
    [SerializeField] private float comboPitchStepUp = 0.05f;
    [SerializeField] private float comboMaxPitch = 1.8f;

    [Header("Default Volumes")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1.0f;

    [Header("Button Click Sound")]
    [SerializeField] private AudioClip clickSound;

    [SerializeField] AudioClip idleEnteranceSound;

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsMusicMuted { get; private set; }
    public bool IsSFXMuted { get; private set; }

    private AudioSource musicSource;
    private AudioSource[] sfxPool;
    private const int SFX_POOL_SIZE = 6;

    private EventBinding<GameOverEvent> gameOverBinding;
    private EventBinding<PlaySFXEvent> playSFXBinding;
    private EventBinding<PlayMusicEvent> playMusicBinding;
    private EventBinding<PerfectMatchSFXEvent> perfectMatchSFXBinding;
    private EventBinding<ComboIncreasedEvent> comboIncreasedBinding;
    private EventBinding<ButtonClickedEvent> buttonClickedBinding;
    private EventBinding<IdleEnteranceEvent> idleEnteranceBinding;

    private const string KeyMusicVolume = "Audio_MusicVol";
    private const string KeySFXVolume = "Audio_SFXVol";
    private const string KeyMusicMuted = "Audio_MusicMuted";
    private const string KeySFXMuted = "Audio_SFXMuted";

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
        comboIncreasedBinding = new EventBinding<ComboIncreasedEvent>(HandleComboIncreased);
        buttonClickedBinding = new EventBinding<ButtonClickedEvent>(HandleButtonClicked);
        idleEnteranceBinding = new EventBinding<IdleEnteranceEvent>(HandleIdleEnterance);

        EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Subscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Subscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Subscribe(perfectMatchSFXBinding);
        EventBus<ComboIncreasedEvent>.Subscribe(comboIncreasedBinding);
        EventBus<ButtonClickedEvent>.Subscribe(buttonClickedBinding);
        EventBus<IdleEnteranceEvent>.Subscribe(idleEnteranceBinding);
    }

    private void OnDisable()
    {
        EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Unsubscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Unsubscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Unsubscribe(perfectMatchSFXBinding);
        EventBus<ComboIncreasedEvent>.Unsubscribe(comboIncreasedBinding);
        EventBus<ButtonClickedEvent>.Unsubscribe(buttonClickedBinding);
        EventBus<IdleEnteranceEvent>.Unsubscribe(idleEnteranceBinding);
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

    private void HandleButtonClicked()
    {
        PlaySFX(clickSound);
    }
    
    private void HandleIdleEnterance()
    {
        PlaySFX(idleEnteranceSound);
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
        //PlaySFX(perfectMatchEffect);
    }

    private void HandleComboIncreased(ComboIncreasedEvent e)
    {
        if (e.combo < 2) return;

        float pitch = Mathf.Min(comboMaxPitch, comboBasePitch + (e.combo - 2) * comboPitchStepUp);
        PlaySFX(comboSound, 1f, 0f, pitch);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        StopAllCoroutines();

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = IsMusicMuted ? 0f : MusicVolume;
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

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f, float? fixedPitch = null)
    {
        if (clip == null || IsSFXMuted) return;

        AudioSource source = GetFreeSFXSource();

        source.clip = clip;
        source.loop = false;
        source.pitch = fixedPitch ?? (1f + Random.Range(-pitchVariance, pitchVariance));
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

        if (!IsMusicMuted)
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

    public void ToggleMusicMute()
    {
        SetMusicMute(!IsMusicMuted);
    }

    public void ToggleSFXMute()
    {
        SetSFXMute(!IsSFXMuted);
    }

    public void SetMusicMute(bool muted)
    {
        IsMusicMuted = muted;
        musicSource.volume = IsMusicMuted ? 0f : MusicVolume;

        PlayerPrefs.SetInt(KeyMusicMuted, IsMusicMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFXMute(bool muted)
    {
        IsSFXMuted = muted;

        if (IsSFXMuted)
            StopAllSFX();

        PlayerPrefs.SetInt(KeySFXMuted, IsSFXMuted ? 1 : 0);
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
        IsMusicMuted = PlayerPrefs.GetInt(KeyMusicMuted, 0) == 1;
        IsSFXMuted = PlayerPrefs.GetInt(KeySFXMuted, 0) == 1;

        musicSource.volume = IsMusicMuted ? 0f : MusicVolume;
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

        float target = IsMusicMuted ? 0f : MusicVolume;

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