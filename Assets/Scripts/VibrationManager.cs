using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private EventBinding<ToggleSettingEvent> toggleSettingBinding;
    private EventBinding<VibrationEvent> vibrationBinding;

    public bool IsVibrationDisabled { get; private set; }

    private const string KeyVibrationDisabled = "Vibration_Disabled";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsVibrationDisabled = PlayerPrefs.GetInt(KeyVibrationDisabled, 0) == 1;
    }

    private void OnEnable()
    {
        toggleSettingBinding = new EventBinding<ToggleSettingEvent>(ToggleSettings);
        vibrationBinding = new EventBinding<VibrationEvent>(Vibrate);

        EventBus<ToggleSettingEvent>.Subscribe(toggleSettingBinding);
        EventBus<VibrationEvent>.Subscribe(vibrationBinding);
    }

    private void OnDisable()
    {
        EventBus<ToggleSettingEvent>.Unsubscribe(toggleSettingBinding);
        EventBus<VibrationEvent>.Unsubscribe(vibrationBinding);
    }

    private void ToggleSettings(ToggleSettingEvent e)
    {
        if (e.toggleType == Toggle.Vibration)
        {
            SetVibration(!IsVibrationDisabled);
        }
    }

    public void SetVibration(bool disabled)
    {
        IsVibrationDisabled = disabled;

        PlayerPrefs.SetInt(KeyVibrationDisabled, disabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Vibrate()
    {
        if (IsVibrationDisabled)
            return;

#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }
}