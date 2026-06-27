//  InputManager.cs  —  REUSABLE. NEVER MODIFY.
//  Drop onto the same GameObject as GameManager.
//  Subscribe to events from any gameplay script via
//  static events (InputManager.OnTap) OR via EventBus<TapEvent>.

using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // ───────────────────────────────────────────────────
    //  Singleton
    // ───────────────────────────────────────────────────
    public static InputManager Instance { get; private set; }

    // ───────────────────────────────────────────────────
    //  Inspector Settings
    // ───────────────────────────────────────────────────
    [Header("Swipe")]
    [Tooltip("Minimum swipe distance as a fraction of screen width (0.05 = 5%).")]
    [SerializeField, Range(0.02f, 0.3f)] private float swipeThreshold = 0.07f;

    [Header("Double Tap")]
    [Tooltip("Max seconds between two taps to register as a double tap.")]
    [SerializeField, Range(0.1f, 0.6f)] private float doubleTapWindow = 0.25f;

    [Header("Hold")]
    [Tooltip("How long the finger must stay still to register as a hold.")]
    [SerializeField, Range(0.2f, 1.5f)] private float holdThreshold = 0.4f;

    [Tooltip("Max movement (% of screen) allowed while holding.")]
    [SerializeField, Range(0.01f, 0.1f)] private float holdMoveTolerance = 0.02f;

    // ───────────────────────────────────────────────────
    //  Static Events — subscribe from any script
    // ───────────────────────────────────────────────────
    public static event Action OnTap;
    public static event Action OnDoubleTap;
    public static event Action OnHoldStart;
    public static event Action OnHoldEnd;
    public static event Action<SwipeDirection> OnSwipe;
    public static event Action OnSwipeLeft;
    public static event Action OnSwipeRight;
    public static event Action OnSwipeUp;
    public static event Action OnSwipeDown;

    // ───────────────────────────────────────────────────
    //  Public State (read-only)
    // ───────────────────────────────────────────────────
    public bool IsHolding { get; private set; }
    public SwipeDirection LastSwipeDirection { get; private set; }
    public Vector2 TouchStartPosition { get; private set; }

    // ───────────────────────────────────────────────────
    //  Internal
    // ───────────────────────────────────────────────────
    private float touchStartTime;
    private float lastTapTime;
    private bool holdTriggered;
    private bool touchActive;

    // ───────────────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────────────
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

    // ───────────────────────────────────────────────────
    //  Touch Input (device)
    // ───────────────────────────────────────────────────
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

    // ───────────────────────────────────────────────────
    //  Mouse Input (editor only)
    // ───────────────────────────────────────────────────
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

    // ───────────────────────────────────────────────────
    //  Phase Handlers
    // ───────────────────────────────────────────────────
    private void OnTouchBegan(Vector2 position)
    {
        TouchStartPosition = position;
        touchStartTime = Time.time;
        holdTriggered = false;
        IsHolding = false;
        touchActive = true;
    }

    private void OnTouchHeld(Vector2 position)
    {
        if (holdTriggered) return;

        float heldFor = Time.time - touchStartTime;
        float movement = Vector2.Distance(position, TouchStartPosition) / Screen.width;

        if (movement < holdMoveTolerance && heldFor >= holdThreshold)
        {
            holdTriggered = true;
            IsHolding = true;

            // Fire static event + EventBus
            OnHoldStart?.Invoke();
            EventBus<HoldStartEvent>.Publish(new HoldStartEvent { screenPosition = TouchStartPosition });
        }
    }

    private void OnTouchEnded(Vector2 position)
    {
        touchActive = false;

        // Was a hold
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

        // Was a swipe
        if (swiped)
        {
            SwipeDirection dir;
            if (movedH >= movedV)
                dir = deltaX > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            else
                dir = deltaY > 0 ? SwipeDirection.Up : SwipeDirection.Down;

            LastSwipeDirection = dir;

            // Fire static events + EventBus
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

        // Was a tap
        float timeSinceLastTap = Time.time - lastTapTime;
        bool isDoubleTap = lastTapTime > 0f && timeSinceLastTap <= doubleTapWindow;

        if (isDoubleTap)
        {
            // Fire static event + EventBus
            OnDoubleTap?.Invoke();
            EventBus<DoubleTapEvent>.Publish(new DoubleTapEvent { screenPosition = position });
            lastTapTime = 0f; // reset so triple-tap doesn't count as double
        }
        else
        {
            // Fire static event + EventBus
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