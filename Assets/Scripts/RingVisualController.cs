using System;
using UnityEngine;

public class RingVisualController : MonoBehaviour
{
    bool isHolding,isSizeIncreasing;

    public Vector3 minSize, maxSize;

    public bool isActive;

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
        if(isHolding && isActive)
        {
            HandleSizing();
        }
    }

    void HandleSizing()
    {
        if(transform.localScale.x <= minSize.x)
        {
            isSizeIncreasing = true;

        }
        else if(transform.localScale.x >= maxSize.x)
        {
            isSizeIncreasing = false;
        }

        if(isSizeIncreasing)
        {
            transform.localScale += new Vector3(7f, 7f) * Time.deltaTime;
        }
        else
        {
            transform.localScale -= new Vector3(7f, 7f) * Time.deltaTime;
        }
    }

    public void ResetSize()
    {
        transform.localScale = minSize;
    }

    public void SetSize(Vector3 size)
    {
        transform.localScale = size;
    }

    private void HandleHoldStart()
    {
        if (!isActive)
            return;

        isHolding = true;
    }

    private void HandleHoldEnd()
    {
        if (!isActive || !isHolding)
            return;

        isHolding = false;
        EventBus<RingReleasedEvent>.Publish(new RingReleasedEvent { ringSize = gameObject.transform.localScale.x });
    }
}
