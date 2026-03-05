using UnityEngine;

namespace EmersynRunner.Player
{
    /// <summary>
    /// Sets up the Emersyn character with required components at runtime.
    /// Attach to the player prefab root.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class CharacterSetup : MonoBehaviour
    {
        [Header("Character Controller")]
        [SerializeField] private float controllerHeight = 1.8f;
        [SerializeField] private float controllerRadius = 0.3f;
        [SerializeField] private Vector3 controllerCenter = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float slopeLimit = 45f;
        [SerializeField] private float stepOffset = 0.3f;

        [Header("Collider")]
        [SerializeField] private float hitboxWidth = 0.7f;
        [SerializeField] private float hitboxHeight = 1.8f;

        [Header("Visual")]
        [SerializeField] private float modelScale = 1f;

        private void Awake()
        {
            SetupCharacterController();
            SetupAnimator();
            SetupTag();
        }

        private void SetupCharacterController()
        {
            CharacterController cc = GetComponent<CharacterController>();
            cc.height = controllerHeight;
            cc.radius = controllerRadius;
            cc.center = controllerCenter;
            cc.slopeLimit = slopeLimit;
            cc.stepOffset = stepOffset;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0.001f;
        }

        private void SetupAnimator()
        {
            Animator animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
        }

        private void SetupTag()
        {
            gameObject.tag = "Player";
            gameObject.layer = LayerMask.NameToLayer("Player");
        }
    }
}
