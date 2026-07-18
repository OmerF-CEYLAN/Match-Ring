using UnityEngine;
public class RingMover : MonoBehaviour
{
    bool isItemMoving;
    public bool isActive;
    public float moveSpeed;
    Vector3 startPoint, endPoint;
    float positionT;
    bool isPositionIncreasing;
    private float roundStartTime;

    void OnEnable()
    {
        InputManager.OnTap += HandleOnTap;
        isItemMoving = true;
    }
    void OnDisable()
    {
        InputManager.OnTap -= HandleOnTap;
    }
    private void Update()
    {
        if (isItemMoving && isActive)
            HandleMoving();
    }
    public void StartMoving(Vector3 from, Vector3 to)
    {
        startPoint = from;
        endPoint = to;
        positionT = 0f;
        isPositionIncreasing = true;
        transform.position = startPoint;
        roundStartTime = Time.time;
    }
    void HandleMoving()
    {
        positionT += (isPositionIncreasing ? 1f : -1f) * Time.deltaTime * moveSpeed;
        if (positionT >= 1f)
        {
            positionT = 1f;
            isPositionIncreasing = false;
        }
        else if (positionT <= 0f)
        {
            positionT = 0f;
            isPositionIncreasing = true;
        }
        transform.position = Vector3.LerpUnclamped(startPoint, endPoint, Mathf.SmoothStep(0f, 1f, positionT));
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void ResetMovement()
    {
        isItemMoving = true;
    }
    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }
    public void SetSize(Vector3 size)
    {
        transform.localScale = size;
    }
    private void HandleOnTap()
    {
        if (!isActive) return;

        if (Time.unscaledTime - UIClickGuard.LastUIClickTime < 0.15f)
        {
            Debug.Log($"[INPUT_DEBUG] Tap BLOCKED by UIClickGuard t={Time.realtimeSinceStartup:F3}");
            return;
        }

        Debug.Log($"[INPUT_DEBUG] RingMover HandleOnTap ACCEPTED t={Time.realtimeSinceStartup:F3} pos={transform.position}");
        EventBus<ItemClickedEvent>.Publish(new ItemClickedEvent());
        isItemMoving = false;
    }


}