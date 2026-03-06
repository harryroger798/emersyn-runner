using UnityEngine;

/// <summary>
/// Base class for all obstacle types. Provides common setup and collision tagging.
/// </summary>
public class ObstacleBase : MonoBehaviour, IPoolable
{
    public enum ObstacleType
    {
        Barrier,        // Standard waist-high barrier (jump over)
        Overhead,       // Overhead bar (slide under)
        LowObstacle,    // Low ground obstacle (jump over)
        Moving,         // Moves between lanes
        TrainLarge,     // Large train-like obstacle spanning 2 lanes
        Gap,            // Gap in the ground (jump over)
        LaneBlocker,    // Blocks one full lane
        StaggeredCombo  // Multiple obstacles in a pattern
    }

    [SerializeField] private ObstacleType obstacleType;
    public ObstacleType Type => obstacleType;

    [Header("Movement (for Moving type)")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveRange = 2.5f;

    private Vector3 startPosition;
    private float moveTimer;

    public void OnSpawn()
    {
        startPosition = transform.localPosition;
        moveTimer = Random.Range(0f, Mathf.PI * 2f);
    }

    public void OnDespawn()
    {
        // Reset state
    }

    private void Update()
    {
        if (obstacleType == ObstacleType.Moving)
        {
            moveTimer += Time.deltaTime * moveSpeed;
            float xOffset = Mathf.Sin(moveTimer) * moveRange;
            Vector3 pos = startPosition;
            pos.x += xOffset;
            transform.localPosition = pos;
        }
    }

    private void OnValidate()
    {
        // Ensure proper tag
        if (gameObject.tag != "Obstacle")
        {
            gameObject.tag = "Obstacle";
        }
    }
}
