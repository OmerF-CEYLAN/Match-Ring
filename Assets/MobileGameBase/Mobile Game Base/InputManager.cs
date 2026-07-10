using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Swipe")]
    [SerializeField, Range(0.02f, 0.3f)] private float swipeThreshold = 0.07f;

    [Header("Double Tap")]
    [SerializeField, Range(0.1f, 0.6f)] private float doubleTapWindow = 0.25f;

    [Header("Hold")]
    [SerializeField, Range(0.2f, 1.5f)] private float holdThreshold = 0.4f;
    [SerializeField, Range(0.01f, 0.1f)] private float holdMoveTolerance = 0.02f;

    public static event Action OnTap;
    public static event Action OnDoubleTap;
    public static event Action OnHoldStart;
    public static event Action OnHoldEnd;
    public static event Action<SwipeDirection> OnSwipe;
    public static event Action OnSwipeLeft;
    public static event Action OnSwipeRight;
    public static event Action OnSwipeUp;
    public static event Action OnSwipeDown;

    public bool IsHolding { get; private set; }
    public SwipeDirection LastSwipeDirection { get; private set; }
    public Vector2 TouchStartPosition { get; private set; }

    private float touchStartTime;
    private float lastTapTime;
    private bool holdTriggered;
    private bool touchActive;
    private bool touchStartedOverBlockingUI;

    private static readonly List<RaycastResult> raycastResultsBuffer = new List<RaycastResult>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            if (touchActive) EndHold();
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                OnTouchBegan(touch.position);
                break;
            case TouchPhase.Stationary:
            case TouchPhase.Moved:
                OnTouchHeld(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnTouchEnded(touch.position);
                break;
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
            OnTouchBegan(Input.mousePosition);
        else if (Input.GetMouseButton(0))
            OnTouchHeld(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            OnTouchEnded(Input.mousePosition);
        else if (touchActive)
            EndHold();
    }

    private bool IsOverBlockingUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
        raycastResultsBuffer.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResultsBuffer);

        for (int i = 0; i < raycastResultsBuffer.Count; i++)
        {
            GameObject hitObject = raycastResultsBuffer[i].gameObject;

            if (hitObject.GetComponentInParent<Selectable>() != null)
                return true;

            if (hitObject.GetComponentInParent<UIInputBlocker>() != null)
                return true;
        }

        return false;
    }

    private void OnTouchBegan(Vector2 position)
    {
        TouchStartPosition = position;
        touchStartTime = Time.time;
        holdTriggered = false;
        IsHolding = false;
        touchActive = true;
        touchStartedOverBlockingUI = IsOverBlockingUI(position);
    }

    private void OnTouchHeld(Vector2 position)
    {
        if (touchStartedOverBlockingUI) return;
        if (holdTriggered) return;

        float heldFor = Time.time - touchStartTime;
        float movement = Vector2.Distance(position, TouchStartPosition) / Screen.width;

        if (movement < holdMoveTolerance && heldFor >= holdThreshold)
        {
            holdTriggered = true;
            IsHolding = true;

            OnHoldStart?.Invoke();
            EventBus<HoldStartEvent>.Publish(new HoldStartEvent { screenPosition = TouchStartPosition });
        }
    }

    private void OnTouchEnded(Vector2 position)
    {
        touchActive = false;

        if (touchStartedOverBlockingUI)
        {
            touchStartedOverBlockingUI = false;
            holdTriggered = false;
            IsHolding = false;
            return;
        }

        if (holdTriggered)
        {
            EndHold();
            return;
        }

        float deltaX = position.x - TouchStartPosition.x;
        float deltaY = position.y - TouchStartPosition.y;
        float movedH = Mathf.Abs(deltaX) / Screen.width;
        float movedV = Mathf.Abs(deltaY) / Screen.height;
        bool swiped = movedH > swipeThreshold || movedV > swipeThreshold;

        if (swiped)
        {
            SwipeDirection dir;
            if (movedH >= movedV)
                dir = deltaX > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            else
                dir = deltaY > 0 ? SwipeDirection.Up : SwipeDirection.Down;

            LastSwipeDirection = dir;

            OnSwipe?.Invoke(dir);
            EventBus<SwipeEvent>.Publish(new SwipeEvent
            {
                direction = dir,
                startPosition = TouchStartPosition,
                endPosition = position
            });

            switch (dir)
            {
                case SwipeDirection.Left: OnSwipeLeft?.Invoke(); break;
                case SwipeDirection.Right: OnSwipeRight?.Invoke(); break;
                case SwipeDirection.Up: OnSwipeUp?.Invoke(); break;
                case SwipeDirection.Down: OnSwipeDown?.Invoke(); break;
            }
            return;
        }

        float timeSinceLastTap = Time.time - lastTapTime;
        bool isDoubleTap = lastTapTime > 0f && timeSinceLastTap <= doubleTapWindow;

        if (isDoubleTap)
        {
            OnDoubleTap?.Invoke();
            EventBus<DoubleTapEvent>.Publish(new DoubleTapEvent { screenPosition = position });
            lastTapTime = 0f;
        }
        else
        {
            OnTap?.Invoke();
            EventBus<TapEvent>.Publish(new TapEvent { screenPosition = position });
            lastTapTime = Time.time;
        }
    }

    private void EndHold()
    {
        if (IsHolding)
        {
            OnHoldEnd?.Invoke();
            EventBus<HoldEndEvent>.Publish(new HoldEndEvent { screenPosition = TouchStartPosition });
        }

        IsHolding = false;
        holdTriggered = false;
    }
}