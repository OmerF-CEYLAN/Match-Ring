// ═══════════════════════════════════════════════════════
//  AudioManager.cs  —  REUSABLE. NEVER MODIFY.
//  Subscribes to game state and audio events via EventBus.
//  Any script can trigger audio without an AudioManager
//  reference by publishing PlaySFXEvent or PlayMusicEvent.
// ═══════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ───────────────────────────────────────────────────
    //  Singleton
    // ───────────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ───────────────────────────────────────────────────
    //  Inspector — Assign your clips here
    // ───────────────────────────────────────────────────
    [Header("Game State Music")]
    [SerializeField] private AudioClip idleMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip gameOverStinger;
    [SerializeField] private AudioClip perfectMatchEffect;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Default Volumes")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 1.0f;

    // ───────────────────────────────────────────────────
    //  Public State
    // ───────────────────────────────────────────────────
    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsMuted { get; private set; }

    // ───────────────────────────────────────────────────
    //  Audio Sources
    // ───────────────────────────────────────────────────
    private AudioSource musicSource;
    private AudioSource[] sfxPool;
    private const int SFX_POOL_SIZE = 6;

    // ───────────────────────────────────────────────────
    //  Event Bindings
    // ───────────────────────────────────────────────────
    private EventBinding<GameStartedEvent> gameStartedBinding;
    private EventBinding<GameOverEvent> gameOverBinding;
    private EventBinding<PlaySFXEvent> playSFXBinding;
    private EventBinding<PlayMusicEvent> playMusicBinding;
    private EventBinding<PerfectMatchSFXEvent> perfectMatchSFXBinding;

    // ───────────────────────────────────────────────────
    //  PlayerPrefs Keys
    // ───────────────────────────────────────────────────
    private const string KeyMusicVolume = "Audio_MusicVol";
    private const string KeySFXVolume = "Audio_SFXVol";
    private const string KeyMuted = "Audio_Muted";

    // ───────────────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
        LoadSettings();
    }

    private void Start()
    {
        if (idleMusic != null) PlayMusic(idleMusic);
    }

    private void OnEnable()
    {
        gameStartedBinding = new EventBinding<GameStartedEvent>(HandleGameStarted);
        gameOverBinding = new EventBinding<GameOverEvent>(HandleGameOver);
        playSFXBinding = new EventBinding<PlaySFXEvent>(HandlePlaySFX);
        playMusicBinding = new EventBinding<PlayMusicEvent>(HandlePlayMusic);
        perfectMatchSFXBinding = new EventBinding<PerfectMatchSFXEvent>(HandlePerfectMatchSFX);

        EventBus<GameStartedEvent>.Subscribe(gameStartedBinding);
        EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Subscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Subscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Subscribe(perfectMatchSFXBinding);
    }

    private void OnDisable()
    {
        EventBus<GameStartedEvent>.Unsubscribe(gameStartedBinding);
        EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
        EventBus<PlaySFXEvent>.Unsubscribe(playSFXBinding);
        EventBus<PlayMusicEvent>.Unsubscribe(playMusicBinding);
        EventBus<PerfectMatchSFXEvent>.Unsubscribe(perfectMatchSFXBinding);
    }

    // ───────────────────────────────────────────────────
    //  Event Handlers
    // ───────────────────────────────────────────────────
    private void HandleGameStarted()
    {
        if (gameplayMusic != null) FadeToMusic(gameplayMusic);
    }

    private void HandleGameOver(GameOverEvent e)
    {
        if (gameOverStinger != null)
        {
            StopMusic();
            PlaySFX(gameOverStinger);
            if (idleMusic != null)
                StartCoroutine(PlayMusicAfterDelay(idleMusic, gameOverStinger.length));
        }
        else if (idleMusic != null)
            FadeToMusic(idleMusic);
        else
            FadeOutMusic();
    }

    private void HandlePlaySFX(PlaySFXEvent e) => PlaySFX(e.clip, e.volumeScale, e.pitchVariance);
    private void HandlePlayMusic(PlayMusicEvent e)
    {
        if (e.fade) FadeToMusic(e.clip);
        else PlayMusic(e.clip);
    }

    void HandlePerfectMatchSFX()
    {
        PlaySFX(perfectMatchEffect);
    }

    // ───────────────────────────────────────────────────
    //  Public API — Music
    // ───────────────────────────────────────────────────
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

    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    // ───────────────────────────────────────────────────
    //  Public API — SFX
    // ───────────────────────────────────────────────────

    /// <summary>
    /// Play a sound effect directly.
    /// pitchVariance: random ±shift per play — great for coins, footsteps.
    /// </summary>
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
        foreach (var s in sfxPool) s.Stop();
    }

    // ───────────────────────────────────────────────────
    //  Public API — Volume & Mute
    // ───────────────────────────────────────────────────
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        if (!IsMuted) musicSource.volume = MusicVolume;
        PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(KeySFXVolume, SFXVolume);
        PlayerPrefs.Save();
    }

    public void ToggleMute() => SetMute(!IsMuted);

    public void SetMute(bool muted)
    {
        IsMuted = muted;
        musicSource.volume = IsMuted ? 0f : MusicVolume;
        foreach (var s in sfxPool)
            s.volume = IsMuted ? 0f : SFXVolume;
        PlayerPrefs.SetInt(KeyMuted, IsMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ───────────────────────────────────────────────────
    //  Internal
    // ───────────────────────────────────────────────────
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
            if (!s.isPlaying) return s;

        // All busy — steal the one closest to finishing
        AudioSource candidate = sfxPool[0];
        float shortestRemain = float.MaxValue;
        foreach (var s in sfxPool)
        {
            float remaining = s.clip != null ? s.clip.length - s.time : 0f;
            if (remaining < shortestRemain) { shortestRemain = remaining; candidate = s; }
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

    // ───────────────────────────────────────────────────
    //  Coroutines
    // ───────────────────────────────────────────────────
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