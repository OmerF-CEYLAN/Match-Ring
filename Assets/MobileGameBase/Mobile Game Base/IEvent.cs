// 
//  IEvent.cs
//  All events for the mobile game base template.
//  Add your own game-specific events at the bottom.
// 

using UnityEngine;

// ── Marker interface ─────────────────────────────────────
public interface IEvent { }

// ───────────────────────────────────────────────────────
//  Game State Events  (published by GameManager)
// ───────────────────────────────────────────────────────

public struct GameStartedEvent : IEvent { }

public struct GameOverEvent : IEvent
{
    public float finalScore;
    public int highScore;     // included so subscribers need no GameManager reference
}

public struct ScoreChangedEvent : IEvent
{
    public float currentScore;
}

public struct NewHighScoreEvent : IEvent
{
    public int highScore;
}

// ───────────────────────────────────────────────────────
//  Input Events  (published by InputManager)
// ───────────────────────────────────────────────────────

public struct TapEvent : IEvent
{
    public Vector2 screenPosition;
}

public struct DoubleTapEvent : IEvent
{
    public Vector2 screenPosition;
}

public struct HoldStartEvent : IEvent
{
    public Vector2 screenPosition;
}

public struct HoldEndEvent : IEvent
{
    public Vector2 screenPosition;
}

public struct SwipeEvent : IEvent
{
    public SwipeDirection direction;
    public Vector2 startPosition;
    public Vector2 endPosition;
}

public enum SwipeDirection { Left, Right, Up, Down }

// ───────────────────────────────────────────────────────
//  Audio Events  (published by anyone, consumed by AudioManager)
//  Lets any script trigger audio without an AudioManager reference.
// ───────────────────────────────────────────────────────

public struct PlaySFXEvent : IEvent
{
    public AudioClip clip;
    public float volumeScale;   // default 1
    public float pitchVariance; // default 0
}

public struct PlayMusicEvent : IEvent
{
    public AudioClip clip;
    public bool fade; // true = crossfade, false = instant
}

// ───────────────────────────────────────────────────────
//  Game-specific events go below this line.
// ───────────────────────────────────────────────────────

public struct RingReleasedEvent : IEvent
{
    public float ringSize;
}

public struct ShakeEvent : IEvent
{
}

public struct AccuricyTextEvent : IEvent
{
    public int score;
}