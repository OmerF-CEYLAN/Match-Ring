using DG.Tweening;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    [SerializeField]
    RectTransform rect;

    EventBinding<ShakeEvent> shakeEventBinding;

    private void OnEnable()
    {
        shakeEventBinding = new EventBinding<ShakeEvent>(OnShake);
        EventBus<ShakeEvent>.Subscribe(shakeEventBinding);
    }

    private void OnDisable()
    {
        EventBus<ShakeEvent>.Unsubscribe(shakeEventBinding);
    }

    void OnShake()
    {
        rect.DOPunchScale(
    Vector3.one * 0.08f, // %8 büyüsün
    0.2f,                // süre
    8,                   // titreþim sayýsý
    0.8f                 // elastiklik
);
    }
}
