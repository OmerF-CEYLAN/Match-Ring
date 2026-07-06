using DG.Tweening;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    [SerializeField]
    RectTransform rect;

    [SerializeField]
    float shakeScale,shakeElasticity;

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
    Vector3.one * shakeScale, // %8 büyüsün
    0.2f,                // süre
    8,                   // titreþim sayýsý
    shakeElasticity                 // elastiklik
);
    }
}
