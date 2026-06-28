using System;
using UnityEngine;

public class RingVisualController : MonoBehaviour
{
    bool isHolding, isSizeIncreasing;
    public Vector3 minSize, maxSize;
    public bool isActive;
    public float cycleSpeed = 2f;

    float sizeT;

    protected void Awake()
    {
        InputManager.OnHoldStart += HandleHoldStart;
        InputManager.OnHoldEnd += HandleHoldEnd;
        transform.localScale = minSize;
    }

    protected void OnDestroy()
    {
        InputManager.OnHoldStart -= HandleHoldStart;
        InputManager.OnHoldEnd -= HandleHoldEnd;
    }

    private void Update()
    {
        if (isHolding && isActive)
            HandleSizing();
    }

    void HandleSizing()
    {
        sizeT += (isSizeIncreasing ? 1f : -1f) * Time.deltaTime * cycleSpeed;

        if (sizeT >= 1f)
        {
            sizeT = 1f;
            isSizeIncreasing = false;
        }
        else if (sizeT <= 0f)
        {
            sizeT = 0f;
            isSizeIncreasing = true;
        }

        transform.localScale = Vector3.LerpUnclamped(minSize, maxSize, Mathf.SmoothStep(0f, 1f, sizeT));
    }

    public void ResetSize()
    {
        sizeT = 0f;
        isSizeIncreasing = true;
        transform.localScale = minSize;
    }

    public void SetSize(Vector3 size)
    {
        transform.localScale = size;
    }

    private void HandleHoldStart()
    {
        if (!isActive) return;
        isHolding = true;
        isSizeIncreasing = true;
    }

    private void HandleHoldEnd()
    {
        if (!isActive || !isHolding) return;
        isHolding = false;
        EventBus<RingReleasedEvent>.Publish(new RingReleasedEvent { ringSize = transform.localScale.x });
    }
}