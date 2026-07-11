using DG.Tweening;
using UnityEngine;

public class ButtonAnimator : MonoBehaviour
{
    RectTransform rect;
    Tween scaleTween;

    [SerializeField] Ease ease;
    [SerializeField] float animationSize;
    [SerializeField] float animationDuration;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        RestartTween();
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
    }

    public void RestartTween()
    {
        scaleTween?.Kill();

        rect.localScale = Vector3.one;

        scaleTween = rect
            .DOScale(animationSize, animationDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(ease);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
            return;

        RestartTween();
    }
#endif
}