using UnityEngine;

/// <summary>
/// Manages player animation states with smooth blending.
/// Syncs animation speed with movement velocity to prevent foot sliding.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animation Speed Sync")]
    [SerializeField] private float baseRunAnimSpeed = 1f;
    [SerializeField] private float maxRunAnimSpeed = 2.0f;
    [SerializeField] private float runSpeedReference = 15f;

    private static readonly int AnimRunSpeed = Animator.StringToHash("RunSpeed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimJumpTrigger = Animator.StringToHash("Jump");
    private static readonly int AnimRollTrigger = Animator.StringToHash("Roll");
    private static readonly int AnimHitTrigger = Animator.StringToHash("Hit");
    private static readonly int AnimCelebrate = Animator.StringToHash("Celebrate");
    private static readonly int AnimLean = Animator.StringToHash("Lean");

    private bool isGrounded = true;

    private void Update()
    {
        if (animator == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Sync run animation speed with game speed to prevent foot sliding
        float currentSpeed = GameManager.Instance.CurrentSpeed;
        float animSpeed = Mathf.Lerp(baseRunAnimSpeed, maxRunAnimSpeed,
            currentSpeed / (GameManager.Instance.Tuning.maxSpeed));
        animator.SetFloat(AnimRunSpeed, animSpeed);
        animator.SetBool(AnimIsGrounded, isGrounded);
    }

    public void TriggerJump()
    {
        if (animator == null) return;
        isGrounded = false;
        animator.SetTrigger(AnimJumpTrigger);
    }

    public void TriggerLand()
    {
        isGrounded = true;
    }

    public void TriggerRoll()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimRollTrigger);
    }

    public void TriggerHit()
    {
        if (animator == null) return;
        animator.SetTrigger(AnimHitTrigger);
    }

    public void SetCelebrate(bool celebrate)
    {
        if (animator == null) return;
        animator.SetBool(AnimCelebrate, celebrate);
    }

    public void SetLean(float leanValue)
    {
        if (animator == null) return;
        animator.SetFloat(AnimLean, leanValue);
    }
}
