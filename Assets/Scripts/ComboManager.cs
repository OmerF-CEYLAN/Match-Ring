using UnityEngine;

public class ComboManager : MonoBehaviour
{
    EventBinding<PerfectMatchEvent> perfectMatchBinding;
    EventBinding<ComboFinishedEvent> comboResetBinding;
    EventBinding<ReturnedToMainMenuEvent> returnedToMainMenuBinding;

    int currentCombo;

    private void OnEnable()
    {
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(OnPerfectMatch);
        comboResetBinding = new EventBinding<ComboFinishedEvent>(OnComboFinished);
        returnedToMainMenuBinding = new EventBinding<ReturnedToMainMenuEvent>(OnComboFinished);

        EventBus<PerfectMatchEvent>.Subscribe(perfectMatchBinding);
        EventBus<ComboFinishedEvent>.Subscribe(comboResetBinding);
        EventBus<ReturnedToMainMenuEvent>.Subscribe(returnedToMainMenuBinding);
    }

    private void OnDisable()
    {
        EventBus<PerfectMatchEvent>.Unsubscribe(perfectMatchBinding);
        EventBus<ComboFinishedEvent>.Unsubscribe(comboResetBinding);
        EventBus<ReturnedToMainMenuEvent>.Unsubscribe(returnedToMainMenuBinding);
    }

    void OnPerfectMatch()
    {
        IncreaseCombo();
    }

    void OnComboFinished()
    {
        ResetCombo();
    }

    void IncreaseCombo()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive) return;

        currentCombo++;

        EventBus<ComboIncreasedEvent>.Publish(new ComboIncreasedEvent { combo = currentCombo });
    }

    void ResetCombo()
    {
        currentCombo = 0;
    }

    public int GetComboMultiplier()
    {
        if (currentCombo <= 0)
            return 1;

        return currentCombo;
    }

    public int GetCombo()
    {
        return currentCombo;
    }

}
