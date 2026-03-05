using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Main player controller handling lane switching, jumping, rolling, and collision.
/// Movement is code-driven but synced with Animator for smooth blending.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CapsuleCollider hitCollider;

    private RunnerTuning tuning;

    // Lane state: -1 = left, 0 = center, 1 = right
    private int currentLane;
    private int targetLane;
    private float laneChangeTimer;
    private float laneChangeStartX;
    private float laneChangeTargetX;
    private bool isChangingLane;

    // Jump state
    private bool isJumping;
    private float jumpTimer;
    private float groundY;

    // Roll state
    private bool isRolling;
    private float rollTimer;
    private float originalColliderHeight;
    private float originalColliderCenterY;

    // Shield/invincibility
    private bool hasShield;
    private bool isInvincible;

    // Animator hashes
    private static readonly int AnimRun = Animator.StringToHash("Run");
    private static readonly int AnimJump = Animator.StringToHash("Jump");
    private static readonly int AnimRoll = Animator.StringToHash("Roll");
    private static readonly int AnimHit = Animator.StringToHash("Hit");
    private static readonly int AnimLean = Animator.StringToHash("Lean");
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");

    public event Action OnPlayerHit;
    public event Action OnPlayerDied;

    private void Start()
    {
        tuning = GameManager.Instance.Tuning;
        currentLane = 0;
        targetLane = 0;
        groundY = transform.position.y;

        if (hitCollider != null)
        {
            originalColliderHeight = hitCollider.height;
            originalColliderCenterY = hitCollider.center.y;
        }

        // Subscribe to input
        SwipeInput.Instance.OnSwipeLeft += OnSwipeLeft;
        SwipeInput.Instance.OnSwipeRight += OnSwipeRight;
        SwipeInput.Instance.OnSwipeUp += OnSwipeUp;
        SwipeInput.Instance.OnSwipeDown += OnSwipeDown;
        SwipeInput.Instance.OnDoubleTap += OnDoubleTap;

        // Subscribe to game events
        GameManager.Instance.OnGameStart += OnGameStart;
        GameManager.Instance.OnRevive += OnRevive;
    }

    private void OnDestroy()
    {
        if (SwipeInput.Instance != null)
        {
            SwipeInput.Instance.OnSwipeLeft -= OnSwipeLeft;
            SwipeInput.Instance.OnSwipeRight -= OnSwipeRight;
            SwipeInput.Instance.OnSwipeUp -= OnSwipeUp;
            SwipeInput.Instance.OnSwipeDown -= OnSwipeDown;
            SwipeInput.Instance.OnDoubleTap -= OnDoubleTap;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnRevive -= OnRevive;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        UpdateLaneChange();
        UpdateJump();
        UpdateRoll();
        UpdateAnimator();
        MoveForward();
    }

    #region Lane Change

    private void OnSwipeLeft()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (isChangingLane && Mathf.Abs(laneChangeTimer / tuning.laneChangeDuration) < 0.3f) return;

        int newLane = Mathf.Max(currentLane - 1, -1);
        if (newLane != currentLane)
        {
            StartLaneChange(newLane);
        }
    }

    private void OnSwipeRight()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (isChangingLane && Mathf.Abs(laneChangeTimer / tuning.laneChangeDuration) < 0.3f) return;

        int newLane = Mathf.Min(currentLane + 1, 1);
        if (newLane != currentLane)
        {
            StartLaneChange(newLane);
        }
    }

    private void StartLaneChange(int newLane)
    {
        targetLane = newLane;
        laneChangeTimer = 0f;
        laneChangeStartX = transform.position.x;
        laneChangeTargetX = targetLane * tuning.laneWidth;
        isChangingLane = true;

        AudioManager.Instance?.PlaySFX("Whoosh");
    }

    private void UpdateLaneChange()
    {
        if (!isChangingLane) return;

        laneChangeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(laneChangeTimer / tuning.laneChangeDuration);
        float easedT = tuning.laneChangeEasing.Evaluate(t);

        float newX = Mathf.Lerp(laneChangeStartX, laneChangeTargetX, easedT);
        Vector3 pos = transform.position;
        pos.x = newX;
        transform.position = pos;

        if (t >= 1f)
        {
            isChangingLane = false;
            currentLane = targetLane;
            // Snap exactly
            pos.x = currentLane * tuning.laneWidth;
            transform.position = pos;
        }
    }

    #endregion

    #region Jump

    private void OnSwipeUp()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (isJumping) return;

        // Cancel roll if rolling
        if (isRolling) CancelRoll();

        isJumping = true;
        jumpTimer = 0f;
        if (animator != null) animator.SetTrigger(AnimJump);
        AudioManager.Instance?.PlaySFX("Jump");
    }

    private void UpdateJump()
    {
        if (!isJumping) return;

        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / tuning.jumpDuration);
        float heightMult = tuning.jumpCurve.Evaluate(t);

        Vector3 pos = transform.position;
        pos.y = groundY + tuning.jumpHeight * heightMult;
        transform.position = pos;

        if (t >= 1f)
        {
            isJumping = false;
            pos.y = groundY;
            transform.position = pos;
        }
    }

    #endregion

    #region Roll / Slide

    private void OnSwipeDown()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (isRolling) return;

        // Cancel jump early if in the air (fast-fall to roll)
        if (isJumping)
        {
            isJumping = false;
            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;
        }

        isRolling = true;
        rollTimer = 0f;

        // Shrink collider
        if (hitCollider != null)
        {
            hitCollider.height = originalColliderHeight * tuning.rollColliderScale;
            hitCollider.center = new Vector3(
                hitCollider.center.x,
                originalColliderCenterY * tuning.rollColliderScale,
                hitCollider.center.z
            );
        }

        if (animator != null) animator.SetTrigger(AnimRoll);
        AudioManager.Instance?.PlaySFX("Roll");
    }

    private void UpdateRoll()
    {
        if (!isRolling) return;

        rollTimer += Time.deltaTime;
        if (rollTimer >= tuning.rollDuration)
        {
            CancelRoll();
        }
    }

    private void CancelRoll()
    {
        isRolling = false;
        if (hitCollider != null)
        {
            hitCollider.height = originalColliderHeight;
            hitCollider.center = new Vector3(
                hitCollider.center.x,
                originalColliderCenterY,
                hitCollider.center.z
            );
        }
    }

    #endregion

    #region Shield / Powerups

    private void OnDoubleTap()
    {
        // Activate shield if available
        if (hasShield) return;
        // Shield activation handled via PowerupManager
    }

    public void ActivateShield(float duration)
    {
        hasShield = true;
        StartCoroutine(ShieldCoroutine(duration));
        AudioManager.Instance?.PlaySFX("Shield");
    }

    private IEnumerator ShieldCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        hasShield = false;
    }

    public void SetInvincible(float duration)
    {
        StartCoroutine(InvincibilityCoroutine(duration));
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
    }

    #endregion

    #region Movement & Collision

    private void MoveForward()
    {
        // Forward movement is handled by moving the world/track toward the player
        // The player stays at a fixed Z position — track segments move toward them
        // This is common in endless runners for better floating-point precision
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            HandleObstacleHit();
        }
        else if (other.CompareTag("Coin"))
        {
            CollectCoin(other.gameObject);
        }
        else if (other.CompareTag("PowerUp"))
        {
            CollectPowerup(other.gameObject);
        }
    }

    private void HandleObstacleHit()
    {
        if (isInvincible) return;

        if (hasShield)
        {
            hasShield = false;
            SetInvincible(tuning.hitInvincibilityDuration);
            AudioManager.Instance?.PlaySFX("ShieldBreak");
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            return;
        }

        // Hit!
        OnPlayerHit?.Invoke();
        if (animator != null) animator.SetTrigger(AnimHit);
        AudioManager.Instance?.PlaySFX("Hit");
        HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Heavy);

        GameManager.Instance.TriggerGameOver();
    }

    private void CollectCoin(GameObject coinObj)
    {
        GameManager.Instance.AddCoins(1);
        AudioManager.Instance?.PlaySFX("Coin");
        HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
        ObjectPool.Instance.Return(coinObj);
    }

    private void CollectPowerup(GameObject powerupObj)
    {
        PowerupPickup pickup = powerupObj.GetComponent<PowerupPickup>();
        if (pickup != null)
        {
            pickup.Activate(this);
        }
        AudioManager.Instance?.PlaySFX("Powerup");
        HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
        ObjectPool.Instance.Return(powerupObj);
    }

    #endregion

    #region Game Events

    private void OnGameStart()
    {
        // Reset player state
        currentLane = 0;
        targetLane = 0;
        isChangingLane = false;
        isJumping = false;
        isRolling = false;
        hasShield = false;
        isInvincible = false;

        Vector3 pos = transform.position;
        pos.x = 0f;
        pos.y = groundY;
        transform.position = pos;
    }

    private void OnRevive()
    {
        SetInvincible(tuning.hitInvincibilityDuration * 2f);
        if (isRolling) CancelRoll();
        isJumping = false;
        Vector3 pos = transform.position;
        pos.y = groundY;
        transform.position = pos;
    }

    #endregion

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Set lean for lane change visual feedback
        float lean = 0f;
        if (isChangingLane)
        {
            float t = Mathf.Clamp01(laneChangeTimer / tuning.laneChangeDuration);
            lean = (targetLane - currentLane) * (1f - t);
        }
        animator.SetFloat(AnimLean, lean);
        animator.SetFloat(AnimSpeed, GameManager.Instance.CurrentSpeed / tuning.maxSpeed);
    }
}
