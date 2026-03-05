using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural track generator that creates and moves track segments,
/// obstacles, and coins without requiring any prefabs or serialized references.
/// Everything is built from Unity primitives at runtime.
/// </summary>
public class SimpleTrackRunner : MonoBehaviour
{
    private bool isRunning = false;
    private float speed = 10f;
    private float laneWidth = 2.5f;

    // Track segments
    private List<GameObject> activeSegments = new List<GameObject>();
    private float segmentLength = 40f;
    private float nextSpawnZ = 0f;
    private float despawnZ = -30f;
    private int maxSegments = 6;

    // Obstacles and coins
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<GameObject> activeCoins = new List<GameObject>();

    // Materials (created once)
    private Material obstacleMat;
    private Material coinMat;
    private Material roadMat;
    private Material buildingMat1;
    private Material buildingMat2;
    private Material buildingMat3;

    private Shader litShader;
    private int segmentsSpawned = 0;

    private Shader FindWorkingShader()
    {
        // Try multiple shader names in priority order
        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Standard",
            "Mobile/Diffuse",
            "Diffuse",
            "UI/Default",
            "Sprites/Default"
        };

        foreach (string sn in shaderNames)
        {
            Shader s = Shader.Find(sn);
            if (s != null)
            {
                Debug.Log($"[SimpleTrackRunner] Using shader: {sn}");
                return s;
            }
        }

        // Last resort: get shader from a primitive's default material
        Debug.LogWarning("[SimpleTrackRunner] No named shader found, using primitive default");
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Shader sh = tmp.GetComponent<Renderer>().sharedMaterial.shader;
        Destroy(tmp);
        return sh;
    }

    private Material CreateColorMaterial(Color color)
    {
        Material mat = new Material(litShader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        return mat;
    }

    private void Awake()
    {
        litShader = FindWorkingShader();

        // Pre-create materials
        obstacleMat = CreateColorMaterial(new Color(0.9f, 0.2f, 0.15f)); // Red obstacles

        coinMat = CreateColorMaterial(new Color(1f, 0.85f, 0.1f)); // Gold coins
        if (coinMat.HasProperty("_Metallic")) coinMat.SetFloat("_Metallic", 0.8f);
        if (coinMat.HasProperty("_Smoothness")) coinMat.SetFloat("_Smoothness", 0.9f);

        roadMat = CreateColorMaterial(new Color(0.35f, 0.35f, 0.4f));

        buildingMat1 = CreateColorMaterial(new Color(0.6f, 0.55f, 0.5f));
        buildingMat2 = CreateColorMaterial(new Color(0.5f, 0.5f, 0.6f));
        buildingMat3 = CreateColorMaterial(new Color(0.55f, 0.6f, 0.55f));
    }

    public void StartTrack()
    {
        ClearAll();
        isRunning = true;
        nextSpawnZ = 0f;
        segmentsSpawned = 0;

        // Spawn initial segments
        for (int i = 0; i < maxSegments; i++)
        {
            SpawnSegment(i < 2); // First 2 are safe
        }
    }

    public void StopTrack()
    {
        isRunning = false;
        ClearAll();
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public List<GameObject> GetActiveObstacles()
    {
        return activeObstacles;
    }

    public List<GameObject> GetActiveCoins()
    {
        return activeCoins;
    }

    private void Update()
    {
        if (!isRunning) return;

        float moveAmount = speed * Time.deltaTime;

        // Move all segments toward the player
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            if (activeSegments[i] == null)
            {
                activeSegments.RemoveAt(i);
                continue;
            }
            activeSegments[i].transform.position += Vector3.back * moveAmount;

            if (activeSegments[i].transform.position.z < despawnZ)
            {
                Destroy(activeSegments[i]);
                activeSegments.RemoveAt(i);
            }
        }

        // Move obstacles
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }
            activeObstacles[i].transform.position += Vector3.back * moveAmount;

            if (activeObstacles[i].transform.position.z < despawnZ)
            {
                Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }

        // Move coins
        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i] == null)
            {
                activeCoins.RemoveAt(i);
                continue;
            }
            activeCoins[i].transform.position += Vector3.back * moveAmount;

            // Spin coins
            activeCoins[i].transform.Rotate(Vector3.up, 120f * Time.deltaTime);

            if (activeCoins[i].transform.position.z < despawnZ)
            {
                Destroy(activeCoins[i]);
                activeCoins.RemoveAt(i);
            }
        }

        // Spawn new segments as needed
        while (activeSegments.Count < maxSegments)
        {
            SpawnSegment(false);
        }
    }

    private void SpawnSegment(bool safe)
    {
        GameObject segment = new GameObject($"Segment_{segmentsSpawned}");
        segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);

        // Road surface
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(segment.transform);
        road.transform.localPosition = new Vector3(0f, -0.5f, segmentLength / 2f);
        road.transform.localScale = new Vector3(10f, 1f, segmentLength);
        road.GetComponent<Renderer>().material = roadMat;
        Destroy(road.GetComponent<Collider>());

        // Lane divider lines
        for (float lx = -1.25f; lx <= 1.25f; lx += 2.5f)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneLine";
            line.transform.SetParent(segment.transform);
            line.transform.localPosition = new Vector3(lx, 0.01f, segmentLength / 2f);
            line.transform.localScale = new Vector3(0.08f, 0.02f, segmentLength);
            line.GetComponent<Renderer>().material = CreateColorMaterial(Color.white);
            Destroy(line.GetComponent<Collider>());
        }

        // Side buildings/walls
        for (int side = -1; side <= 1; side += 2)
        {
            int buildingCount = Random.Range(1, 4);
            float zOffset = 0f;
            for (int b = 0; b < buildingCount; b++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "Building";
                building.transform.SetParent(segment.transform);
                float height = Random.Range(5f, 15f);
                float depth = Random.Range(8f, segmentLength / buildingCount);
                building.transform.localPosition = new Vector3(
                    side * (6f + Random.Range(0f, 2f)),
                    height / 2f,
                    zOffset + depth / 2f
                );
                building.transform.localScale = new Vector3(
                    Random.Range(3f, 6f),
                    height,
                    depth - 0.5f
                );

                Material[] mats = { buildingMat1, buildingMat2, buildingMat3 };
                building.GetComponent<Renderer>().material = mats[Random.Range(0, mats.Length)];
                Destroy(building.GetComponent<Collider>());
                zOffset += depth;
            }
        }

        activeSegments.Add(segment);

        // Place obstacles and coins (skip first 2 segments for safety)
        if (!safe)
        {
            PlaceObstacles(nextSpawnZ);
            PlaceCoins(nextSpawnZ);
        }

        nextSpawnZ += segmentLength;
        segmentsSpawned++;
    }

    private void PlaceObstacles(float segStartZ)
    {
        // Place 1-3 obstacles per segment
        int count = Random.Range(1, 4);
        float spacing = segmentLength / (count + 1);

        for (int i = 0; i < count; i++)
        {
            float z = segStartZ + spacing * (i + 1);
            int lane = Random.Range(-1, 2);
            int obstacleType = Random.Range(0, 8);

            GameObject obs = CreateObstacle(obstacleType);
            obs.transform.position = new Vector3(lane * laneWidth, 0f, z);
            activeObstacles.Add(obs);
        }
    }

    private GameObject CreateObstacle(int type)
    {
        GameObject obs;

        switch (type)
        {
            case 0: // Low barrier
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(2f, 1f, 0.5f);
                obs.transform.position += Vector3.up * 0.5f;
                break;

            case 1: // Tall barrier
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(1.5f, 2.5f, 0.5f);
                obs.transform.position += Vector3.up * 1.25f;
                break;

            case 2: // Overhead bar (duck under)
                obs = new GameObject("Overhead");
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.transform.SetParent(obs.transform);
                bar.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                bar.transform.localScale = new Vector3(3f, 0.3f, 0.3f);
                bar.GetComponent<Renderer>().material = obstacleMat;
                // Support pillars
                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pillar.transform.SetParent(obs.transform);
                    pillar.transform.localPosition = new Vector3(s * 1.3f, 1.25f, 0f);
                    pillar.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
                    pillar.GetComponent<Renderer>().material = obstacleMat;
                    Destroy(pillar.GetComponent<Collider>());
                }
                Destroy(bar.GetComponent<Collider>());
                break;

            case 3: // Cone
                obs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obs.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                obs.transform.position += Vector3.up * 0.5f;
                break;

            case 4: // Wide barrier (2 lanes)
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(4f, 1.5f, 0.5f);
                obs.transform.position += Vector3.up * 0.75f;
                break;

            case 5: // Train-like large object
                obs = new GameObject("Train");
                for (int c = 0; c < 3; c++)
                {
                    GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    car.transform.SetParent(obs.transform);
                    car.transform.localPosition = new Vector3(0f, 1f, c * 2f);
                    car.transform.localScale = new Vector3(1.8f, 2f, 1.8f);
                    car.GetComponent<Renderer>().material = obstacleMat;
                    Destroy(car.GetComponent<Collider>());
                }
                break;

            case 6: // Gap/pit (visual only - flat red warning)
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(2f, 0.1f, 2f);
                obs.transform.position += Vector3.up * 0.05f;
                break;

            default: // Staggered combo
                obs = new GameObject("Staggered");
                for (int s = 0; s < 2; s++)
                {
                    GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.transform.SetParent(obs.transform);
                    piece.transform.localPosition = new Vector3(s * 1.5f - 0.75f, 0.75f, s * 1.5f);
                    piece.transform.localScale = new Vector3(1f, 1.5f, 0.5f);
                    piece.GetComponent<Renderer>().material = obstacleMat;
                    Destroy(piece.GetComponent<Collider>());
                }
                break;
        }

        obs.name = "Obstacle";
        obs.tag = "Untagged"; // Don't use tag-based collision

        // Apply material to main renderer
        Renderer rend = obs.GetComponent<Renderer>();
        if (rend != null) rend.material = obstacleMat;

        return obs;
    }

    private void PlaceCoins(float segStartZ)
    {
        // Place a line of coins in a random lane
        int lane = Random.Range(-1, 2);
        int coinCount = Random.Range(3, 8);
        float startZ = segStartZ + 5f;
        float coinSpacing = 3f;

        for (int i = 0; i < coinCount; i++)
        {
            float z = startZ + i * coinSpacing;
            if (z >= segStartZ + segmentLength - 5f) break;

            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin";
            coin.transform.position = new Vector3(lane * laneWidth, 1f, z);
            coin.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            coin.GetComponent<Renderer>().material = coinMat;
            Destroy(coin.GetComponent<Collider>());
            activeCoins.Add(coin);
        }

        // Occasionally place a second cluster in a different lane
        if (Random.value < 0.4f)
        {
            int lane2 = Random.Range(-1, 2);
            while (lane2 == lane) lane2 = Random.Range(-1, 2);
            float startZ2 = segStartZ + segmentLength * 0.5f;
            int count2 = Random.Range(2, 5);
            for (int i = 0; i < count2; i++)
            {
                float z = startZ2 + i * coinSpacing;
                if (z >= segStartZ + segmentLength - 5f) break;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = new Vector3(lane2 * laneWidth, 1f, z);
                coin.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
                coin.GetComponent<Renderer>().material = coinMat;
                Destroy(coin.GetComponent<Collider>());
                activeCoins.Add(coin);
            }
        }
    }

    private void ClearAll()
    {
        foreach (GameObject seg in activeSegments)
        {
            if (seg != null) Destroy(seg);
        }
        activeSegments.Clear();

        foreach (GameObject obs in activeObstacles)
        {
            if (obs != null) Destroy(obs);
        }
        activeObstacles.Clear();

        foreach (GameObject coin in activeCoins)
        {
            if (coin != null) Destroy(coin);
        }
        activeCoins.Clear();

        nextSpawnZ = 0f;
        segmentsSpawned = 0;
    }
}
