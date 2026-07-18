using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MatchItemsGameManager : GameManager
{
    [Header("Hold Game Settings")]
    [SerializeField] private float currentDelayForNextRound, minDelayForNextRound, maxDelayForNextRound = 1.5f;

    [SerializeField] MonoBehaviour holdMiniGameSource;
    [SerializeField] MonoBehaviour tapMiniGameSource;

    IMiniGameMode holdMiniGame, tapMiniGame,currentMinigame;

    [SerializeField] ComboManager comboManager;

    EventBinding<MiniGameRoundResultEvent> miniGameRoundResultBinding;

    private void OnEnable()
    {
        miniGameRoundResultBinding = new EventBinding<MiniGameRoundResultEvent>(GetDifferenceRate);
        EventBus<MiniGameRoundResultEvent>.Subscribe(miniGameRoundResultBinding);
    }

    private void OnDisable()
    {
        EventBus<MiniGameRoundResultEvent>.Unsubscribe(miniGameRoundResultBinding);
    }

    protected override void Awake()
    {
        base.Awake();

        holdMiniGame = holdMiniGameSource as IMiniGameMode;
        tapMiniGame = tapMiniGameSource as IMiniGameMode;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        ResetRoundDelay();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (!IsPlaying) return;
    }

    void GetDifferenceRate(MiniGameRoundResultEvent e)
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive) return;
        EvaluateDifference(e.differenceRate);
    }

    public void EvaluateDifference(float differenceRate)
    {
        if (differenceRate <= 10f)
        {
            EventBus<ShakeEvent>.Publish(new ShakeEvent());
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 3 });
            EventBus<PerfectMatchSFXEvent>.Publish(new PerfectMatchSFXEvent());
            PerfectMatch();
            AddScoreWithComboMultiplier(3);
            DecreaseRoundDelay();
            OnSuccessfulMatch();
        }
        else if (differenceRate <= 20f)
        {
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 1 });
            EventBus<ComboFinishedEvent>.Publish(new ComboFinishedEvent());
            GoodMatch();
            AddScore(1);
            ResetRoundDelay();
            OnSuccessfulMatch();
        }
        else
        {
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 0 });
            EventBus<ComboFinishedEvent>.Publish(new ComboFinishedEvent());
            MissMatch();
            ResetRoundDelay();
            StartCoroutine(TriggerGameOverDelayed());
        }
    }

    public void AddScoreWithComboMultiplier(int score)
    {
        int combo = comboManager.GetComboMultiplier();
        AddScore(score * combo);
    }

    public void OnSuccessfulMatch()
    {
        EventBus<SuccessfulMatchEvent>.Publish(new SuccessfulMatchEvent());
        StartCoroutine(SelectRandomMinigameDelayed());
    }

    void MissMatch()
    {
        EventBus<MissMatchSFXEvent>.Publish(new MissMatchSFXEvent());
    }

    void GoodMatch()
    {
        EventBus<GoodMatchSFXEvent>.Publish(new GoodMatchSFXEvent());
    }

    void PerfectMatch()
    {
        EventBus<PerfectMatchEvent>.Publish(new PerfectMatchEvent());
        RequestVibration();
    }

    void RequestVibration()
    {
        EventBus<VibrationEvent>.Publish(new VibrationEvent());
    }

    void DecreaseRoundDelay()
    {
        float delay = maxDelayForNextRound - (0.1f * comboManager.GetComboMultiplier());

        if(delay < minDelayForNextRound)
            delay = minDelayForNextRound;

        currentDelayForNextRound = delay;
    }

    void ResetRoundDelay()
    {
        currentDelayForNextRound = maxDelayForNextRound;
    }

    void SelectRandomMinigame()
    {
        int randomIndex = UnityEngine.Random.Range(0, 2);

        if(randomIndex == 0)
        {
            tapMiniGameSource.gameObject.SetActive(true);
            currentMinigame = tapMiniGame;

            holdMiniGameSource.gameObject.SetActive(false);
        }
        else
        {
            holdMiniGameSource.gameObject.SetActive(true);
            currentMinigame = holdMiniGame;

            tapMiniGameSource.gameObject.SetActive(false);
        }

        currentMinigame.BeginRound();

    }

    IEnumerator SelectRandomMinigameDelayed()
    {
        yield return new WaitForSeconds(currentDelayForNextRound);

        SelectRandomMinigame();
    }

    protected override void OnGameStarted_Hook()
    {
        if (TutorialManager.Instance != null && !TutorialManager.IsTutorialCompleted)
            TutorialManager.Instance.StartTutorial(SelectRandomMinigame);
        else
            SelectRandomMinigame();
    }
    protected override void OnReturnToMainMenu_Hook()
    {
        StopAllCoroutines();

        tapMiniGameSource.gameObject.SetActive(false);
        holdMiniGameSource.gameObject.SetActive(false);
        currentMinigame = null;

        ResetRoundDelay();
    }

    IEnumerator TriggerGameOverDelayed()
    {
        yield return new WaitForSeconds(currentDelayForNextRound);
        TriggerGameOver();
    }

}
