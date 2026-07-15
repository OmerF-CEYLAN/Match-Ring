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

    [SerializeField] TMP_Text instructionText;
    [SerializeField] TMP_Text feedbackText;

    [SerializeField] string holdInstruction = "Basýlý tut, tam zamanýnda býrak!";
    [SerializeField] string tapInstruction = "Doðru zamanda dokun!";
    [SerializeField] string failInstruction = "You missed. Try again!";

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
        onComplete = onCompleteCallback;
        IsActive = true;
        EventBus<TutorialStartedEvent>.Publish(new TutorialStartedEvent());

        tapToContinuePanel?.SetActive(false);
        feedbackText?.gameObject.SetActive(false);

        BeginHoldPractice();
    }

    void BeginHoldPractice()
    {
        currentStep = Step.Hold;
        currentMiniGame = holdMiniGame;

        SetInstructionText(holdInstruction);

        tapMiniGameSource.gameObject.SetActive(false);
        holdMiniGameSource.gameObject.SetActive(true);
        holdMiniGame.BeginRound();
    }

    void BeginTapPractice()
    {
        currentStep = Step.Tap;
        currentMiniGame = tapMiniGame;

        SetInstructionText(tapInstruction);

        holdMiniGameSource.gameObject.SetActive(false);
        tapMiniGameSource.gameObject.SetActive(true);
        tapMiniGame.BeginRound();
    }

    void SetInstructionText(string message)
    {
        if (instructionText != null)
            instructionText.text = message;
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
        EventBus<MissMatchSFXEvent>.Publish(new MissMatchSFXEvent());
        SetInstructionText(failInstruction);
        yield return new WaitForSeconds(retryDelay);
        currentMiniGame.BeginRound();
        yield return new WaitForSeconds(0.5f);

        if (currentStep == Step.Hold)
            SetInstructionText(holdInstruction);
        else
            SetInstructionText(tapInstruction);
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

    IEnumerator ShowFeedback(string message, float duration)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(duration);

        feedbackText?.gameObject.SetActive(false);
    }

    IEnumerator ContinueToTapPractice()
    {
        yield return StartCoroutine(ShowFeedback("Perfect!", delayBeforeContinuePrompt));

        holdMiniGameSource.gameObject.SetActive(false);

        yield return StartCoroutine(WaitForContinueTap());

        BeginTapPractice();
    }

    IEnumerator WaitForContinueTap()
    {
        SetInstructionText("");
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
        yield return StartCoroutine(ShowFeedback("Perfect!", delayBeforeContinuePrompt));

        tapMiniGameSource.gameObject.SetActive(false);
        SetInstructionText("");

        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        yield return StartCoroutine(ShowFeedback("Tutorial Completed!", completedDisplayDuration));

        hudPanel?.SetActive(false);
        idlePanel?.SetActive(true);

        tapToContinuePanel?.SetActive(false);

        idlePanel?.SetActive(false);
        hudPanel?.SetActive(true);

        IsActive = false;
        EventBus<TutorialCompletedEvent>.Publish(new TutorialCompletedEvent());

        onComplete?.Invoke();
    }
}