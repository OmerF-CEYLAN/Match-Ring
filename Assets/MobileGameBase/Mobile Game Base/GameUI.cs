using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public enum Toggle
{
    Sound,Music,Vibration
}

public class GameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject idlePanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI accuricyText;
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Game Over Screen")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverHighScoreText;
    [SerializeField] private GameObject newRecordObject;
    [SerializeField] private Button gameOverMainMenuButton;
    [SerializeField] private Button continueButton;

    [Header("Idle Screen")]
    [SerializeField] private TextMeshProUGUI idleHighScoreText;
    [SerializeField] private RectTransform titleRect;

    [Header("Input Blocker Panel")]
    [SerializeField] GameObject inputBlockerPanel;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button playButton;

    [Header("Item Colleciton")]
    [SerializeField] GameObject collectionPanel;
    [SerializeField] Button collectionButton;
    [SerializeField] Button closeCollectionButton;
    [SerializeField] CollectionManager collectionManager;

    [Header("Settings")]
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Button settingsButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button inGameSettingsButton;
    [SerializeField] Button closeSettingsButton;
    [SerializeField] Button musicToggleButton;
    [SerializeField] Button sfxToggleButton;
    [SerializeField] Button vibrationToggleButton;
    [SerializeField] Sprite toggleOn, toggleOff;

    [Header("Idle Buttons Entrance")]
    [SerializeField] private float idleEntranceDuration = 0.5f;
    [SerializeField] private Ease idleEntranceEase = Ease.OutBack;
    private Sequence idleEntranceSequence;

    private int cachedHighScore;
    private int displayedScore;

    private EventBinding<GameStartedEvent> gameStartedBinding;
    private EventBinding<GameOverEvent> gameOverBinding;
    private EventBinding<ScoreChangedEvent> scoreChangedBinding;
    private EventBinding<NewHighScoreEvent> newHighScoreBinding;
    private EventBinding<AccuricyTextEvent> accuricyTextBinding;
    private EventBinding<ComboIncreasedEvent> comboIncreasedBinding;
    private EventBinding<ComboFinishedEvent> comboFinishedBinding;
    private EventBinding<ReturnedToMainMenuEvent> returnedToMainMenuBinding;
    private EventBinding<TutorialStartedEvent> tutorialStartedBinding;
    private EventBinding<TutorialCompletedEvent> tutorialCompletedBinding;
    private EventBinding<RewardEarnedEvent> rewardEarnedBinding;

    private Coroutine idleEntranceRoutine;

    private void OnEnable()
    {
        gameStartedBinding = new EventBinding<GameStartedEvent>(HandleGameStarted);
        gameOverBinding = new EventBinding<GameOverEvent>(HandleGameOver);
        scoreChangedBinding = new EventBinding<ScoreChangedEvent>(HandleScoreChanged);
        newHighScoreBinding = new EventBinding<NewHighScoreEvent>(HandleNewHighScore);
        accuricyTextBinding = new EventBinding<AccuricyTextEvent>(HandleAccuricyText);
        comboIncreasedBinding = new EventBinding<ComboIncreasedEvent>(HandleComboIncreased);
        comboFinishedBinding = new EventBinding<ComboFinishedEvent>(HandleComboFinished);
        returnedToMainMenuBinding = new EventBinding<ReturnedToMainMenuEvent>(ShowIdle);
        tutorialStartedBinding = new EventBinding<TutorialStartedEvent>(HandleTutorialStarted);
        tutorialCompletedBinding = new EventBinding<TutorialCompletedEvent>(HandleTutorialCompleted);
        rewardEarnedBinding = new EventBinding<RewardEarnedEvent>(OnRewardEarned);

        EventBus<GameStartedEvent>.Subscribe(gameStartedBinding);
        EventBus<GameOverEvent>.Subscribe(gameOverBinding);
        EventBus<ScoreChangedEvent>.Subscribe(scoreChangedBinding);
        EventBus<NewHighScoreEvent>.Subscribe(newHighScoreBinding);
        EventBus<AccuricyTextEvent>.Subscribe(accuricyTextBinding);
        EventBus<ComboIncreasedEvent>.Subscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Subscribe(comboFinishedBinding);
        EventBus<ReturnedToMainMenuEvent>.Subscribe(returnedToMainMenuBinding);
        EventBus<TutorialStartedEvent>.Subscribe(tutorialStartedBinding);
        EventBus<TutorialCompletedEvent>.Subscribe(tutorialCompletedBinding);
        EventBus<RewardEarnedEvent>.Subscribe(rewardEarnedBinding);

    }

    private void OnDisable()
    {
        EventBus<GameStartedEvent>.Unsubscribe(gameStartedBinding);
        EventBus<GameOverEvent>.Unsubscribe(gameOverBinding);
        EventBus<ScoreChangedEvent>.Unsubscribe(scoreChangedBinding);
        EventBus<NewHighScoreEvent>.Unsubscribe(newHighScoreBinding);
        EventBus<AccuricyTextEvent>.Unsubscribe(accuricyTextBinding);
        EventBus<ComboIncreasedEvent>.Unsubscribe(comboIncreasedBinding);
        EventBus<ComboFinishedEvent>.Unsubscribe(comboFinishedBinding);
        EventBus<ReturnedToMainMenuEvent>.Unsubscribe(returnedToMainMenuBinding);
        EventBus<TutorialStartedEvent>.Unsubscribe(tutorialStartedBinding);
        EventBus<TutorialCompletedEvent>.Unsubscribe(tutorialCompletedBinding);
        EventBus<RewardEarnedEvent>.Unsubscribe(rewardEarnedBinding);

        idleEntranceSequence?.Kill();

        if (idleEntranceRoutine != null)
            StopCoroutine(idleEntranceRoutine);
    }

    private void Start()
    {
        restartButton?.onClick.AddListener(OnRestartClicked);
        continueButton?.onClick.AddListener(OnContinueClicked);
        playButton?.onClick.AddListener(OnPlayClicked);

        collectionButton.onClick.AddListener(OpenCollection);
        closeCollectionButton.onClick.AddListener(CloseCollection);

        settingsButton?.onClick.AddListener(OpenSettings);
        inGameSettingsButton?.onClick.AddListener(OpenSettings);
        closeSettingsButton?.onClick.AddListener(CloseSettings);
        musicToggleButton?.onClick.AddListener(OnMusicToggleClicked);
        sfxToggleButton?.onClick.AddListener(OnSFXToggleClicked);
        vibrationToggleButton?.onClick.AddListener(OnVibrationToggleClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        gameOverMainMenuButton?.onClick.AddListener(OnMainMenuClicked);

        if (GameManager.Instance != null)
            cachedHighScore = GameManager.Instance.HighScore;

        settingsPanel?.SetActive(false);
        collectionPanel?.SetActive(false);

        EventBus<HideBannerEvent>.Publish(new HideBannerEvent());

        SetUpAllButtonsClickSound();

        ShowIdle();
        PlayAnimation(titleRect);
    }

    private void PlayIdleButtonsEntrance()
    {
        if (idleEntranceRoutine != null)
            StopCoroutine(idleEntranceRoutine);

        idleEntranceSequence?.Kill();

        SetButtonScaleZero(playButton);
        SetButtonScaleZero(collectionButton);
        SetButtonScaleZero(settingsButton);

        idleEntranceRoutine = StartCoroutine(PlayIdleButtonsEntranceRoutine());
    }

    private IEnumerator PlayIdleButtonsEntranceRoutine()
    {
        yield return null;

        float stagger = idleEntranceDuration * 0.5f;

        idleEntranceSequence = DOTween.Sequence().SetUpdate(true);

        idleEntranceSequence.AppendCallback(() => TriggerButtonEntrance(playButton));
        idleEntranceSequence.AppendInterval(stagger);
        idleEntranceSequence.AppendCallback(() => TriggerButtonEntrance(collectionButton));
        idleEntranceSequence.AppendInterval(stagger);
        idleEntranceSequence.AppendCallback(() => TriggerButtonEntrance(settingsButton));
    }

    private void SetButtonScaleZero(Button button)
    {
        if (button == null) return;

        RectTransform rt = button.GetComponent<RectTransform>();
        rt.DOKill();
        rt.localScale = Vector3.zero;
    }

    private void TriggerButtonEntrance(Button button)
    {
        if (button == null) return;

        EventBus<IdleEnteranceEvent>.Publish(new IdleEnteranceEvent());

        ButtonAnimator animator = button.GetComponent<ButtonAnimator>();

        if (animator != null)
        {
            animator.PlayEntrance(idleEntranceDuration, idleEntranceEase);
        }
        else
        {
            RectTransform rt = button.GetComponent<RectTransform>();
            rt.DOKill();
            rt.localScale = Vector3.zero;
            rt.DOScale(1f, idleEntranceDuration).SetEase(idleEntranceEase);
        }
    }

    private void AddButtonSound(Button button)
    {
        button.onClick.AddListener(() =>
        {
            EventBus<ButtonClickedEvent>.Publish(new ButtonClickedEvent());
        });
    }

    private void HandleTutorialStarted()
    {
        mainMenuButton?.gameObject.SetActive(false);
        scoreText.enabled = false;
    }

    private void HandleTutorialCompleted()
    {
        mainMenuButton?.gameObject.SetActive(true);
        scoreText.enabled = true;
    }
    void SetUpAllButtonsClickSound()
    {
        AddButtonSound(restartButton);
        AddButtonSound(playButton);
        AddButtonSound(collectionButton);
        AddButtonSound(closeCollectionButton);
        AddButtonSound(settingsButton);
        AddButtonSound(inGameSettingsButton);
        AddButtonSound(closeSettingsButton);
        AddButtonSound(musicToggleButton);
        AddButtonSound(sfxToggleButton);
        AddButtonSound(vibrationToggleButton);
        AddButtonSound(mainMenuButton);
        AddButtonSound(gameOverMainMenuButton);
    }

    void OnPlayClicked()
    {
        UIClickGuard.LastUIClickTime = Time.unscaledTime;
        GameManager.Instance?.StartGame();
    }

    private void OnMainMenuClicked()
    {
        UIClickGuard.LastUIClickTime = Time.unscaledTime;
        GameManager.Instance?.ReturnToMainMenu();
    }

    void OpenCollection()
    {
        collectionManager.RefreshAll();
        collectionPanel.SetActive(true);
        EventBus<ShowBannerEvent>.Publish(new ShowBannerEvent());
    }

    void CloseCollection()
    {
        collectionPanel.SetActive(false);
        EventBus<HideBannerEvent>.Publish(new HideBannerEvent());
    }

    void OpenSettings()
    {
        RefreshSettingsUI();
        mainMenuButton.gameObject.SetActive(GameManager.Instance.IsPlaying);
        settingsPanel.SetActive(true);
    }

    void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    void OnMusicToggleClicked()
    {
        EventBus<ToggleSettingEvent>.Publish(new ToggleSettingEvent { toggleType = Toggle.Music });
        RefreshSettingsUI();
    }

    void OnSFXToggleClicked()
    {
        EventBus<ToggleSettingEvent>.Publish(new ToggleSettingEvent { toggleType = Toggle.Sound });
        RefreshSettingsUI();
    }

    void OnVibrationToggleClicked()
    {
        EventBus<ToggleSettingEvent>.Publish(new ToggleSettingEvent { toggleType = Toggle.Vibration });
        RefreshSettingsUI();
    }

    void RefreshSettingsUI()
    {
        if (AudioManager.Instance == null) return;

        musicToggleButton.GetComponent<Image>().sprite = AudioManager.Instance.IsMusicMuted ? toggleOff : toggleOn;
        sfxToggleButton.GetComponent<Image>().sprite = AudioManager.Instance.IsSFXMuted ? toggleOff : toggleOn;
        vibrationToggleButton.GetComponent<Image>().sprite = VibrationManager.Instance.IsVibrationDisabled ? toggleOff : toggleOn;
    }

    private void HandleGameStarted()
    {
        displayedScore = 0;
        accuricyText.DOKill();
        accuricyText.gameObject.SetActive(false);
        idlePanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        hudPanel?.SetActive(true);
        newRecordObject?.SetActive(false);
        SetText(scoreText, "0");
        comboText.DOKill();
        comboText.gameObject.SetActive(false);
        comboText.rectTransform.localScale = Vector3.one;
    }

    private void HandleGameOver(GameOverEvent e)
    {
        cachedHighScore = e.highScore;

        hudPanel?.SetActive(false);
        gameOverPanel?.SetActive(true);

        SetText(gameOverScoreText, Mathf.FloorToInt(e.finalScore).ToString());
        SetText(gameOverHighScoreText, e.highScore.ToString());

        continueButton.gameObject.SetActive(GameManager.Instance.CanUseRewardedContinue);
    }

    void HandleComboIncreased(ComboIncreasedEvent e)
    {
        if (e.combo < 2)
            return;

        ToggleScoreRainbowEffect(true);

        comboText.gameObject.SetActive(true);
        comboText.text = $"x{e.combo}";

        comboText.DOKill();
        comboText.rectTransform.DOKill();

        comboText.color = Color.white;
        comboText.rectTransform.localScale = Vector3.one;

        comboText.rectTransform
            .DOScale(1.4f, 0.08f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutBack);

        comboText
            .DOColor(Color.yellow, 0.08f)
            .SetLoops(2, LoopType.Yoyo);
    }

    void HandleComboFinished()
    {
        comboText.DOKill();
        comboText.rectTransform.DOKill();

        comboText
            .DOFade(0f, 0.15f)
            .OnComplete(() =>
            {
                comboText.gameObject.SetActive(false);
                comboText.alpha = 1f;
                comboText.color = Color.white;
                comboText.rectTransform.localScale = Vector3.one;
            });

        ToggleScoreRainbowEffect(false);
    }

    private void ToggleScoreRainbowEffect(bool isActive)
    {
        if (scoreText == null) return;

        RainbowWaveText effect = scoreText.GetComponent<RainbowWaveText>();
        if (effect != null)
        {
            effect.enabled = isActive;

            if (!isActive)
            {
                scoreText.color = Color.white;

                scoreText.rectTransform.DOKill();
                scoreText.rectTransform.localScale = Vector3.one;
            }
            else
            {
                scoreText.DOKill();
                scoreText.rectTransform.DOKill();

                scoreText.rectTransform.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();

                seq.Join(
                    scoreText.rectTransform
                    .DOScale(1.2f, 0.125f)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() =>
                        scoreText.rectTransform.DOScale(1.3f, 0.5f).SetLoops(-1, LoopType.Yoyo)
                    )
                );
            }
        }
    }

    private void HandleScoreChanged(ScoreChangedEvent e)
    {
        scoreText.DOKill();

        int targetScore = Mathf.FloorToInt(e.currentScore);

        Sequence seq = DOTween.Sequence();

        seq.Join(
            DOTween.To(
                () => displayedScore,
                x =>
                {
                    displayedScore = x;
                    scoreText.text = x.ToString();
                },
                targetScore,
                0.9f
            )
        );

    }

    private void HandleNewHighScore(NewHighScoreEvent e)
    {
        cachedHighScore = e.highScore;
        newRecordObject?.SetActive(true);
        SetText(gameOverHighScoreText, e.highScore.ToString());
    }

    void HandleAccuricyText(AccuricyTextEvent e)
    {
        accuricyText.gameObject.SetActive(true);
        RainbowWaveText effect = accuricyText.GetComponent<RainbowWaveText>();
        effect.enabled = false;

        if (e.score == 3)
        {
            accuricyText.text = "Perfect!";
            effect.enabled = true;
        }
        else if (e.score == 1)
        {
            accuricyText.text = "Good!";
        }
        else
        {
            accuricyText.text = "Miss";
        }

        accuricyText.rectTransform.DOKill();

        accuricyText.rectTransform.localScale = Vector3.one;
        accuricyText.rectTransform.gameObject.SetActive(true);

        accuricyText.rectTransform
            .DOScale(1.5f, 1f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                effect.enabled = false;
                accuricyText.gameObject.SetActive(false);
            });
    }

    private void OnRestartClicked()
    {
        UIClickGuard.LastUIClickTime = Time.unscaledTime;
        GameManager.Instance?.RestartGame();
    } 
    
    private void OnContinueClicked()
    {
        inputBlockerPanel.SetActive(true);
        EventBus<ShowRewardedAdEvent>.Publish(new ShowRewardedAdEvent());
    }

    void OnRewardEarned(RewardEarnedEvent e)
    {
        inputBlockerPanel.SetActive(false);

        if (e.isRewardGiven == false)
            return;

        GameManager.Instance?.RewardedContinueGame();
    }

    private void ShowIdle()
    {
        idlePanel?.SetActive(true);
        settingsPanel?.SetActive(false);
        hudPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        SetText(idleHighScoreText, $"{cachedHighScore}");
        PlayIdleButtonsEntrance();
    }

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    void PlayAnimation(RectTransform rectToAnimate)
    {
        rectToAnimate.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Append(
            rectToAnimate.DOScale(1.3f, 1f)
                .SetEase(Ease.InOutSine)
        );

        seq.SetLoops(-1, LoopType.Yoyo);
    }
}