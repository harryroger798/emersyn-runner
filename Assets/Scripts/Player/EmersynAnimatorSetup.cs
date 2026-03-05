using UnityEngine;

namespace EmersynRunner.Player
{
    /// <summary>
    /// Sets up the Emersyn character animator controller at runtime.
    /// Creates animation states and transitions programmatically.
    /// </summary>
    public class EmersynAnimatorSetup : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float runAnimSpeed = 1.0f;
        [SerializeField] private float jumpTransitionDuration = 0.1f;
        [SerializeField] private float rollTransitionDuration = 0.08f;
        [SerializeField] private float laneChangeTransitionDuration = 0.05f;
        [SerializeField] private float hitTransitionDuration = 0.1f;

        // Animation state hashes for performance
        public static readonly int HashSpeed = Animator.StringToHash("Speed");
        public static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int HashJumpTrigger = Animator.StringToHash("Jump");
        public static readonly int HashRollTrigger = Animator.StringToHash("Roll");
        public static readonly int HashHitTrigger = Animator.StringToHash("Hit");
        public static readonly int HashLeanAmount = Animator.StringToHash("LeanAmount");
        public static readonly int HashIsDead = Animator.StringToHash("IsDead");
        public static readonly int HashCelebrate = Animator.StringToHash("Celebrate");

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = gameObject.AddComponent<Animator>();
            }
        }

        /// <summary>
        /// Triggers the run animation with speed modifier.
        /// </summary>
        public void SetRunSpeed(float normalizedSpeed)
        {
            if (_animator != null)
            {
                _animator.SetFloat(HashSpeed, normalizedSpeed * runAnimSpeed);
            }
        }

        /// <summary>
        /// Triggers jump animation with proper transition blending.
        /// </summary>
        public void TriggerJump()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashJumpTrigger);
                _animator.SetBool(HashIsGrounded, false);
            }
        }

        /// <summary>
        /// Called when the character lands after a jump.
        /// </summary>
        public void OnLand()
        {
            if (_animator != null)
            {
                _animator.SetBool(HashIsGrounded, true);
            }
        }

        /// <summary>
        /// Triggers roll/slide animation.
        /// </summary>
        public void TriggerRoll()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashRollTrigger);
            }
        }

        /// <summary>
        /// Sets lean amount for lane change animation blending.
        /// -1 = lean left, 0 = center, 1 = lean right
        /// </summary>
        public void SetLeanAmount(float lean)
        {
            if (_animator != null)
            {
                _animator.SetFloat(HashLeanAmount, Mathf.Clamp(lean, -1f, 1f));
            }
        }

        /// <summary>
        /// Triggers hit reaction animation.
        /// </summary>
        public void TriggerHit()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashHitTrigger);
            }
        }

        /// <summary>
        /// Sets the death state for game over.
        /// </summary>
        public void SetDead(bool isDead)
        {
            if (_animator != null)
            {
                _animator.SetBool(HashIsDead, isDead);
            }
        }

        /// <summary>
        /// Triggers celebrate animation (menu idle).
        /// </summary>
        public void TriggerCelebrate()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HashCelebrate);
            }
        }

        /// <summary>
        /// Returns animation state durations for syncing with gameplay.
        /// </summary>
        public float GetJumpTransitionDuration() => jumpTransitionDuration;
        public float GetRollTransitionDuration() => rollTransitionDuration;
        public float GetLaneChangeTransitionDuration() => laneChangeTransitionDuration;
        public float GetHitTransitionDuration() => hitTransitionDuration;
    }
}
