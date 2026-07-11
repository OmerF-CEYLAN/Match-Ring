using UnityEngine;

public class ComboManager : MonoBehaviour
{
    EventBinding<PerfectMatchEvent> perfectMatchBinding;
    EventBinding<ComboFinishedEvent> comboResetBinding;

    int currentCombo;

    private void OnEnable()
    {
        perfectMatchBinding = new EventBinding<PerfectMatchEvent>(OnPerfectMatch);
        comboResetBinding = new EventBinding<ComboFinishedEvent>(OnComboFinished);

        EventBus<PerfectMatchEvent>.Subscribe(perfectMatchBinding);
        EventBus<ComboFinishedEvent>.Subscribe(comboResetBinding);
    }

    private void OnDisable()
    {
        EventBus<PerfectMatchEvent>.Unsubscribe(perfectMatchBinding);
        EventBus<ComboFinishedEvent>.Unsubscribe(comboResetBinding);
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

}
