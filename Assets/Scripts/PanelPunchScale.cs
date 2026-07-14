using DG.Tweening;
using UnityEngine;

public class PanelPunchScale : MonoBehaviour
{
    [SerializeField] private float punchAmount = 0.15f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private int vibrato = 6;
    [SerializeField, Range(0f, 1f)] private float elasticity = 0.8f;

    private RectTransform rect;
    private Tween punchTween;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        punchTween?.Kill();
        rect.localScale = Vector3.one;

        punchTween = rect
            .DOPunchScale(Vector3.one * punchAmount, duration, vibrato, elasticity)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        punchTween?.Kill();
        rect.localScale = Vector3.one;
    }
}