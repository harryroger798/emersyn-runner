using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages procedural track generation, segment spawning/recycling, and obstacle/pickup placement.
/// Track segments move toward the player (player stays at fixed Z).
/// </summary>
public class TrackManager : MonoBehaviour
{
    public static TrackManager Instance { get; private set; }

    [Header("Track Segments")]
    [SerializeField] private GameObject[] citySegmentPrefabs;
    [SerializeField] private GameObject[] jungleSegmentPrefabs;
    [SerializeField] private GameObject[] candySegmentPrefabs;

    [Header("Obstacles")]
    [SerializeField] private GameObject[] obstaclePrefabs; // 8+ types

    [Header("Pickups")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject magnetPrefab;
    [SerializeField] private GameObject multiplierPrefab;
    [SerializeField] private GameObject shieldPrefab;

    [Header("Settings")]
    [SerializeField] private float segmentLength = 30f;
    [SerializeField] private int activeSegmentCount = 5;
    [SerializeField] private float despawnDistance = -20f;

    public enum Biome { City, Jungle, Candy }
    private Biome currentBiome = Biome.City;
    private int segmentsSinceLastBiomeChange;
    private const int SegmentsPerBiome = 15;

    private RunnerTuning tuning;
    private List<TrackSegment> activeSegments = new List<TrackSegment>();
    private float nextSpawnZ;
    private float lastObstacleZ;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        tuning = GameManager.Instance.Tuning;
        GameManager.Instance.OnGameStart += OnGameStart;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        ClearTrack();
        nextSpawnZ = 0f;
        lastObstacleZ = 0f;
        segmentsSinceLastBiomeChange = 0;
        currentBiome = Biome.City;

        // Spawn initial segments
        for (int i = 0; i < activeSegmentCount; i++)
        {
            SpawnSegment(i < 2); // First 2 segments are safe (no obstacles)
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float speed = GameManager.Instance.CurrentSpeed;
        float moveAmount = speed * Time.deltaTime;

        // Move all segments toward the player
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            TrackSegment seg = activeSegments[i];
            seg.transform.position += Vector3.back * moveAmount;

            // Despawn segments that have passed the player
            if (seg.transform.position.z < despawnDistance)
            {
                DespawnSegment(seg);
                activeSegments.RemoveAt(i);
            }
        }

        // Spawn new segments as needed
        while (activeSegments.Count < activeSegmentCount)
        {
            SpawnSegment(false);
        }
    }

    private void SpawnSegment(bool safe)
    {
        // Select biome-appropriate prefab
        GameObject prefab = GetRandomSegmentPrefab();

        Vector3 spawnPos = Vector3.forward * nextSpawnZ;
        GameObject segObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);

        TrackSegment segment = segObj.GetComponent<TrackSegment>();
        if (segment == null) segment = segObj.AddComponent<TrackSegment>();

        segment.Initialize(segmentLength);
        activeSegments.Add(segment);
        nextSpawnZ += segmentLength;

        if (!safe)
        {
            PlaceObstacles(segment);
            PlacePickups(segment);
        }

        // Biome transition
        segmentsSinceLastBiomeChange++;
        if (segmentsSinceLastBiomeChange >= SegmentsPerBiome)
        {
            segmentsSinceLastBiomeChange = 0;
            currentBiome = (Biome)(((int)currentBiome + 1) % 3);
        }
    }

    private GameObject GetRandomSegmentPrefab()
    {
        GameObject[] prefabs;
        switch (currentBiome)
        {
            case Biome.Jungle:
                prefabs = jungleSegmentPrefabs;
                break;
            case Biome.Candy:
                prefabs = candySegmentPrefabs;
                break;
            default:
                prefabs = citySegmentPrefabs;
                break;
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            // Fallback: use city
            prefabs = citySegmentPrefabs;
        }

        if (prefabs == null || prefabs.Length == 0) return new GameObject("EmptySegment");
        return prefabs[Random.Range(0, prefabs.Length)];
    }

    private void PlaceObstacles(TrackSegment segment)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        float speed = GameManager.Instance.CurrentSpeed;
        float spacing = Mathf.Lerp(tuning.maxObstacleSpacing, tuning.minObstacleSpacing,
            speed / tuning.maxSpeed * tuning.spacingSpeedFactor);

        float z = segment.transform.position.z + spacing;
        float segEnd = segment.transform.position.z + segmentLength;

        while (z < segEnd)
        {
            if (z - lastObstacleZ >= spacing)
            {
                // Pick random obstacle
                GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                int lane = Random.Range(-1, 2); // -1, 0, or 1
                Vector3 pos = new Vector3(lane * tuning.laneWidth, 0f, z);
                pos = segment.transform.TransformPoint(pos - segment.transform.position);

                GameObject obs = Instantiate(prefab, pos, Quaternion.identity, segment.transform);
                lastObstacleZ = z;
            }
            z += spacing;
        }
    }

    private void PlacePickups(TrackSegment segment)
    {
        float z = segment.transform.position.z + 5f;
        float segEnd = segment.transform.position.z + segmentLength - 5f;
        int lane = Random.Range(-1, 2);

        // Place coin clusters
        while (z < segEnd)
        {
            for (int i = 0; i < tuning.coinsPerCluster; i++)
            {
                float coinZ = z + i * tuning.coinSpacing;
                if (coinZ >= segEnd) break;

                Vector3 pos = new Vector3(lane * tuning.laneWidth, 1f, coinZ);
                pos = segment.transform.TransformPoint(pos - segment.transform.position);

                if (coinPrefab != null)
                {
                    Instantiate(coinPrefab, pos, Quaternion.identity, segment.transform);
                }
            }

            z += tuning.coinsPerCluster * tuning.coinSpacing + 10f;
            lane = Random.Range(-1, 2);
        }

        // Chance to place a powerup
        if (Random.value < tuning.powerupSpawnChance)
        {
            float powerupZ = Random.Range(segment.transform.position.z + 10f, segEnd);
            int powerupLane = Random.Range(-1, 2);
            Vector3 powerupPos = new Vector3(powerupLane * tuning.laneWidth, 1.5f, powerupZ);
            powerupPos = segment.transform.TransformPoint(powerupPos - segment.transform.position);

            GameObject[] powerups = { magnetPrefab, multiplierPrefab, shieldPrefab };
            GameObject powerupPrefab = powerups[Random.Range(0, powerups.Length)];
            if (powerupPrefab != null)
            {
                Instantiate(powerupPrefab, powerupPos, Quaternion.identity, segment.transform);
            }
        }
    }

    private void DespawnSegment(TrackSegment segment)
    {
        Destroy(segment.gameObject);
    }

    private void ClearTrack()
    {
        foreach (TrackSegment seg in activeSegments)
        {
            if (seg != null) Destroy(seg.gameObject);
        }
        activeSegments.Clear();
    }
}
