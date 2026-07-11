using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour
{
    RectTransform rect;
    Tween scaleTween, clickTween;
    Button btn;

    [SerializeField] bool isScaleAnimationEnabled,isClickAnimationEnabled;

    [Header("ScaleAnimation")]
    [SerializeField] Ease scaleEase;
    [SerializeField] float scaleAnimationSize;
    [SerializeField] float scaleAnimationDuration;

    [Header("ClickAnimation")]
    [SerializeField] Ease clickEase;
    [SerializeField] float clickAnimationSize;
    [SerializeField] float clickAnimationDuration;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        btn = GetComponent<Button>();

        if (isClickAnimationEnabled)
            btn?.onClick.AddListener(OnClickAnimation);
    }

    private void OnEnable()
    {
        if (isScaleAnimationEnabled)
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
            .DOScale(scaleAnimationSize, scaleAnimationDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(scaleEase);
    }

    void OnClickAnimation()
    {
        clickTween?.Kill();

        clickTween = rect
            .DOScale(scaleAnimationSize, scaleAnimationDuration)
            .SetLoops(1, LoopType.Restart)
            .SetEase(clickEase);
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