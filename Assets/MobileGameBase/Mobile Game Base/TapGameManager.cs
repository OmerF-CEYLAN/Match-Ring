// ═══════════════════════════════════════════════════════
//  TapGameManager.cs
//  Tap to score. Don't tap for 2 seconds = Game Over.
//
//  Tap-to-start on the Idle screen is handled by the
//  base GameManager via EventBus<TapEvent> — no double
//  subscription needed here.
// ═══════════════════════════════════════════════════════

using UnityEngine;

public class TapGameManager : GameManager
{
    [Header("Tap Game Settings")]
    [SerializeField] private float timeoutDuration = 2f;

    private float timeSinceLastTap;
    private float gameStartTime;

    // ───────────────────────────────────────────────────
    //  Lifecycle
    // ───────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake(); // registers tap-to-start via EventBus<TapEvent>
        InputManager.OnTap += HandleTap;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        InputManager.OnTap -= HandleTap;
    }

    protected override void Update()
    {
        base.Update();

        if (!IsPlaying) return;

        timeSinceLastTap += Time.deltaTime;

        if (timeSinceLastTap >= timeoutDuration)
            TriggerGameOver();
    }

    private void HandleTap()
    {
        if (!IsPlaying) return;
        if (Time.time - gameStartTime < 0.2f) return; // ignore taps right at start

        timeSinceLastTap = 0f;
        AddScore(1);
    }

    protected override void OnGameStarted_Hook()
    {
        timeSinceLastTap = 0f;
        gameStartTime = Time.time;
    }

    protected override void OnRestart_Hook()
    {
        timeSinceLastTap = 0f;
        gameStartTime = Time.time;
    }
}