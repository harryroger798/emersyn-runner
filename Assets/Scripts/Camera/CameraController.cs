using UnityEngine;

/// <summary>
/// Smooth camera follow with damping, look-ahead, and slight tilt during lane changes.
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;

    private RunnerTuning tuning;
    private Vector3 velocity;
    private Vector3 currentOffset;

    private void Start()
    {
        tuning = GameManager.Instance.Tuning;
        currentOffset = tuning.cameraOffset;

        if (target != null)
        {
            transform.position = target.position + currentOffset;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Menu) return;

        Vector3 targetPos = target.position + currentOffset;

        // Add look-ahead based on speed
        float speedNormalized = GameManager.Instance.CurrentSpeed / tuning.maxSpeed;
        targetPos += Vector3.forward * tuning.cameraLookAhead * speedNormalized;

        // Smooth follow with damping
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            tuning.cameraFollowDamping
        );

        // Look at player with slight forward offset
        Vector3 lookTarget = target.position + Vector3.forward * 3f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        transform.position = target.position + currentOffset;
        Vector3 lookTarget = target.position + Vector3.forward * 3f;
        transform.LookAt(lookTarget);
    }
}
