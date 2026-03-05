using UnityEngine;

/// <summary>
/// ScriptableObject holding all tunable "feel" parameters for the endless runner.
/// Adjust these in the Inspector to dial in responsiveness and game feel.
/// </summary>
[CreateAssetMenu(fileName = "RunnerTuning", menuName = "EmersynRunner/Runner Tuning Config")]
public class RunnerTuning : ScriptableObject
{
    [Header("Lane Switching")]
    [Tooltip("Duration of lane-change animation in seconds")]
    [Range(0.05f, 0.5f)]
    public float laneChangeDuration = 0.15f;

    [Tooltip("Easing curve for lane switching (0=start, 1=end)")]
    public AnimationCurve laneChangeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Distance between adjacent lanes in world units")]
    public float laneWidth = 2.5f;

    [Header("Jump")]
    [Tooltip("Peak jump height in world units")]
    public float jumpHeight = 3.0f;

    [Tooltip("Total jump duration (up + down) in seconds")]
    public float jumpDuration = 0.7f;

    [Tooltip("Jump gravity curve (normalized time vs height multiplier)")]
    public AnimationCurve jumpCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Roll / Slide")]
    [Tooltip("Duration of the roll/slide in seconds")]
    public float rollDuration = 0.6f;

    [Tooltip("Collider height multiplier during roll (0-1)")]
    [Range(0.1f, 0.8f)]
    public float rollColliderScale = 0.35f;

    [Header("Speed & Acceleration")]
    [Tooltip("Starting forward speed")]
    public float startSpeed = 10f;

    [Tooltip("Maximum forward speed")]
    public float maxSpeed = 30f;

    [Tooltip("Speed increase per second")]
    public float accelerationRate = 0.15f;

    [Tooltip("Acceleration curve (normalized time 0-1 mapped to speed range)")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Time in seconds to reach max speed")]
    public float timeToMaxSpeed = 120f;

    [Header("Camera")]
    [Tooltip("Camera follow damping (lower = smoother/slower)")]
    [Range(0.01f, 1f)]
    public float cameraFollowDamping = 0.15f;

    [Tooltip("Camera offset behind player")]
    public Vector3 cameraOffset = new Vector3(0f, 5f, -8f);

    [Tooltip("Camera look-ahead distance")]
    public float cameraLookAhead = 5f;

    [Header("Input")]
    [Tooltip("Minimum swipe distance in screen-percentage (0-1) to register")]
    [Range(0.01f, 0.3f)]
    public float swipeThreshold = 0.05f;

    [Tooltip("Maximum time in seconds for a swipe gesture")]
    public float swipeMaxTime = 0.5f;

    [Header("Obstacle Spacing")]
    [Tooltip("Minimum distance between obstacle groups")]
    public float minObstacleSpacing = 15f;

    [Tooltip("Maximum distance between obstacle groups")]
    public float maxObstacleSpacing = 35f;

    [Tooltip("Spacing reduction per speed unit")]
    public float spacingSpeedFactor = 0.2f;

    [Header("Pickup Spacing")]
    [Tooltip("Coin spacing along the track")]
    public float coinSpacing = 3f;

    [Tooltip("Coins per cluster")]
    public int coinsPerCluster = 5;

    [Tooltip("Powerup spawn chance per segment (0-1)")]
    [Range(0f, 1f)]
    public float powerupSpawnChance = 0.15f;

    [Header("Collision")]
    [Tooltip("Player hitbox width multiplier (forgiving = smaller)")]
    [Range(0.3f, 1f)]
    public float hitboxWidthScale = 0.7f;

    [Tooltip("Player hitbox height multiplier")]
    [Range(0.5f, 1f)]
    public float hitboxHeightScale = 0.85f;

    [Tooltip("Invincibility duration after hit (seconds)")]
    public float hitInvincibilityDuration = 1.5f;

    [Header("Powerup Durations")]
    public float magnetDuration = 8f;
    public float multiplierDuration = 10f;
    public float shieldDuration = 12f;

    [Tooltip("Magnet attraction radius")]
    public float magnetRadius = 5f;

    [Tooltip("Magnet attraction speed")]
    public float magnetSpeed = 15f;
}
