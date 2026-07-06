using System;
using UnityEngine;
using DG.Tweening;

public class RingVisualController : MonoBehaviour
{
    bool isHolding;
    public Vector3 minSize, maxSize;
    public bool isActive;
    public float cycleSpeed = 2f;

    Tween sizeTween;

    void OnEnable()
    {
        InputManager.OnHoldStart += HandleHoldStart;
        InputManager.OnHoldEnd += HandleHoldEnd;
        transform.localScale = minSize;
    }

    void OnDisable()
    {
        InputManager.OnHoldStart -= HandleHoldStart;
        InputManager.OnHoldEnd -= HandleHoldEnd;
        sizeTween?.Kill();
    }

    public void ResetSize()
    {
        sizeTween?.Kill();
        transform.localScale = minSize;
    }

    public void SetSize(Vector3 size)
    {
        sizeTween?.Kill();
        transform.localScale = size;
    }

    public void SetSpeed(float speed)
    {
        cycleSpeed = speed;
        if (sizeTween != null && sizeTween.IsActive())
            sizeTween.timeScale = speed;
    }

    private void HandleHoldStart()
    {
        if (!isActive) return;
        isHolding = true;

        sizeTween?.Kill();
        transform.localScale = minSize;

        float halfCycleDuration = 1f / cycleSpeed;

        sizeTween = transform
            .DOScale(maxSize, halfCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void HandleHoldEnd()
    {
        if (!isActive || !isHolding) return;
        isHolding = false;

        sizeTween?.Kill();

        EventBus<RingReleasedEvent>.Publish(new RingReleasedEvent { ringSize = transform.localScale.x });
    }
}