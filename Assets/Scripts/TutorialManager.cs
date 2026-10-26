using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    const string TutorialCompletedKey = "TutorialCompleted";
    public static bool IsTutorialCompleted => PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

    public bool IsActive { get; private set; }

    [SerializeField] MonoBehaviour holdMiniGameSource;
    [SerializeField] MonoBehaviour tapMiniGameSource;

    [SerializeField] GameObject tapToContinuePanel;
    [SerializeField] GameObject idlePanel;
    [SerializeField] GameObject hudPanel;

    [SerializeField] TMP_Text holdInstructionText;
    [SerializeField] TMP_Text tapInstructionText;
    [SerializeField] TMP_Text failInstructionText;

    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text perfectFeedbackText;
    [SerializeField] TMP_Text tutorialCompletedFeedbackText;

    [SerializeField] float perfectThreshold = 10f;
    [SerializeField] float retryDelay = 1f;
    [SerializeField] float delayBeforeContinuePrompt = 1.5f;
    [SerializeField] float completedDisplayDuration = 2f;

    IMiniGameMode holdMiniGame, tapMiniGame, currentMiniGame;

    enum Step { Hold, Tap }
    Step currentStep;

    Action onComplete;
    EventBinding<MiniGameRoundResultEvent> resultBinding;

    void Awake()
    {
        Instance = this;
        holdMiniGame = holdMiniGameSource as IMiniGameMode;
        tapMiniGame = tapMiniGameSource as IMiniGameMode;
    }

    void OnEnable()
    {
        resultBinding = new EventBinding<MiniGameRoundResultEvent>(HandleResult);
        EventBus<MiniGameRoundResultEvent>.Subscribe(resultBinding);
    }

    void OnDisable()
    {
        EventBus<MiniGameRoundResultEvent>.Unsubscribe(resultBinding);
    }

    public void StartTutorial(Action onCompleteCallback)
    {
        DisableInstructionTexts();
        onComplete = onCompleteCallback;
        IsActive = true;
        EventBus<TutorialStartedEvent>.Publish(new TutorialStartedEvent());

        tapToContinuePanel?.SetActive(false);
        feedbackText?.gameObject.SetActive(false);

        BeginHoldPractice();
    }

    void BeginHoldPractice()
    {
        DisableInstructionTexts();

        currentStep = Step.Hold;
        currentMiniGame = holdMiniGame;

        holdInstructionText.gameObject.SetActive(true);

        tapMiniGameSource.gameObject.SetActive(false);
        holdMiniGameSource.gameObject.SetActive(true);
        holdMiniGame.BeginRound();
    }

    void BeginTapPractice()
    {
        DisableInstructionTexts();
     
        currentStep = Step.Tap;
        currentMiniGame = tapMiniGame;

        tapInstructionText.gameObject.SetActive(true);

        holdMiniGameSource.gameObject.SetActive(false);
        tapMiniGameSource.gameObject.SetActive(true);
        tapMiniGame.BeginRound();
    }

    void HandleResult(MiniGameRoundResultEvent e)
    {
        if (!IsActive) return;

        if (e.differenceRate <= perfectThreshold)
            StartCoroutine(OnPerfectSuccess());
        else
            StartCoroutine(RetryCurrentStep());
    }

    IEnumerator RetryCurrentStep()
    {
        DisableInstructionTexts();

        EventBus<MissMatchSFXEvent>.Publish(new MissMatchSFXEvent());

        failInstructionText.gameObject.SetActive(true);
        yield return new WaitForSeconds(retryDelay);
        currentMiniGame.BeginRound();
        yield return new WaitForSeconds(0.5f);
        DisableInstructionTexts();

        if (currentStep == Step.Hold)
            holdInstructionText.gameObject.SetActive(true);
        else
            tapInstructionText.gameObject.SetActive(true);
    }

    void OnStepSuccess()
    {
        if (currentStep == Step.Hold)
            StartCoroutine(ContinueToTapPractice());
        else
            StartCoroutine(FinishTutorial());
    }

    IEnumerator OnPerfectSuccess()
    {
        EventBus<PerfectMatchEvent>.Publish(new PerfectMatchEvent());
        EventBus<PerfectMatchSFXEvent>.Publish(new PerfectMatchSFXEvent());

        yield return new WaitForSeconds(0.25f);

        OnStepSuccess();
    }

    IEnumerator ShowFeedback(TMP_Text feedbackText ,float duration)
    {
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(duration);

        feedbackText?.gameObject.SetActive(false);
    }

    IEnumerator ContinueToTapPractice()
    {
        yield return StartCoroutine(ShowFeedback(perfectFeedbackText, delayBeforeContinuePrompt));

        holdMiniGameSource.gameObject.SetActive(false);

        yield return StartCoroutine(WaitForContinueTap());

        BeginTapPractice();
    }

    IEnumerator WaitForContinueTap()
    {
        DisableInstructionTexts();
        tapToContinuePanel?.SetActive(true);

        bool tapped = false;
        Action handler = () => tapped = true;
        InputManager.OnTap += handler;

        yield return new WaitUntil(() => tapped);

        InputManager.OnTap -= handler;
        tapToContinuePanel?.SetActive(false);
    }

    IEnumerator FinishTutorial()
    {
        yield return StartCoroutine(ShowFeedback(perfectFeedbackText, delayBeforeContinuePrompt));

        tapMiniGameSource.gameObject.SetActive(false);
        DisableInstructionTexts();

        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        yield return StartCoroutine(ShowFeedback(tutorialCompletedFeedbackText, completedDisplayDuration));

        hudPanel?.SetActive(false);
        idlePanel?.SetActive(true);

        tapToContinuePanel?.SetActive(false);

        idlePanel?.SetActive(false);
        hudPanel?.SetActive(true);

        IsActive = false;
        EventBus<TutorialCompletedEvent>.Publish(new TutorialCompletedEvent());

        onComplete?.Invoke();
    }

    void DisableInstructionTexts()
    {
        tapInstructionText.gameObject.SetActive(false);
        holdInstructionText.gameObject.SetActive(false);
        failInstructionText.gameObject.SetActive(false);
    }
}