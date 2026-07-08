using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MatchItemsGameManager : GameManager
{
    [Header("Hold Game Settings")]
    [SerializeField] private float delayForNextRound = 1.5f;

    [SerializeField] MonoBehaviour holdMiniGameSource;
    [SerializeField] MonoBehaviour tapMiniGameSource;

    IMiniGameMode holdMiniGame, tapMiniGame,currentMinigame;

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
            AddScore(3);
            EventBus<ShakeEvent>.Publish(new ShakeEvent());
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 3 });
            EventBus<PerfectMatchSFXEvent>.Publish(new PerfectMatchSFXEvent());
            OnSuccessfulMatch();
            PerfectMatch();
        }
        else if (differenceRate <= 20f)
        {
            AddScore(1);
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 1 });
            OnSuccessfulMatch();
        }
        else
        {
            EventBus<AccuricyTextEvent>.Publish(new AccuricyTextEvent { score = 0 });
            StartCoroutine(TriggerGameOverDelayed());
        }
    }

    public void OnSuccessfulMatch()
    {
        EventBus<SuccessfulMatchEvent>.Publish(new SuccessfulMatchEvent());
        StartCoroutine(SelectRandomMinigameDelayed());
    }

    void PerfectMatch()
    {
        EventBus<PerfectMatchEvent>.Publish(new PerfectMatchEvent());
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
        yield return new WaitForSeconds(delayForNextRound);

        SelectRandomMinigame();
    }

    protected override void OnGameStarted_Hook()
    {
        if (TutorialManager.Instance != null && !TutorialManager.IsTutorialCompleted)
            TutorialManager.Instance.StartTutorial(SelectRandomMinigame);
        else
            SelectRandomMinigame();
    }

    IEnumerator TriggerGameOverDelayed()
    {
        yield return new WaitForSeconds(delayForNextRound);
        TriggerGameOver();
    }

}
