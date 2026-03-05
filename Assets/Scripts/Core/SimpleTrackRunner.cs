using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AAA-quality procedural track generator with SDXL texture support.
/// Creates textured track segments, obstacles, coins, buildings, and props.
/// </summary>
public class SimpleTrackRunner : MonoBehaviour
{
    private bool isRunning = false;
    private float speed = 10f;
    private float laneWidth = 2.5f;

    private List<GameObject> activeSegments = new List<GameObject>();
    private float segmentLength = 40f;
    private float nextSpawnZ = 0f;
    private float despawnZ = -30f;
    private int maxSegments = 6;

    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<GameObject> activeCoins = new List<GameObject>();

    // Textured materials
    private Material roadMat;
    private Material sidewalkMat;
    private Material grassMat;
    private Material barrierMat;
    private Material trainMat;
    private Material coneMat;
    private Material coinMat;
    private Material[] buildingMats;
    private Material fenceMat;
    private Material lampMat;
    private Material graffitiMat;

    // NEW: Additional materials for AAA upgrade
    private Material cobbleMat;
    private Material crosswalkMat;
    private Material manholeMat;
    private Material[] propMats;
    private Material dumpsterMat;
    private Material constructionMat;
    private Material carMat;
    private Material busMat;
    private Material[] trainVariantMats;

    // Phase 3: Hi-res building materials, environment details, billboards
    private Material roadHDMat;
    private Material[] hiResBuildingMats;
    private Material billboardMat1;
    private Material billboardMat2;
    private Material rooftopMat;
    private Material tunnelMat;

    // Phase 3: Curved world effect
    private float curvedWorldIntensity = 0.008f;

    private Shader litShader;
    private int segmentsSpawned = 0;
    private Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

    private Shader FindWorkingShader()
    {
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
                Debug.Log("[SimpleTrackRunner] Using shader: " + sn);
                return s;
            }
        }

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

    private Material CreateTexturedMaterial(string texName, Color fallback)
    {
        Texture2D tex = LoadTex(texName);
        if (tex != null)
        {
            Material mat = new Material(litShader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            return mat;
        }
        return CreateColorMaterial(fallback);
    }

    private Material CreateTexturedMaterialTiled(string texName, Color fallback, float tilingX, float tilingY)
    {
        Material mat = CreateTexturedMaterial(texName, fallback);
        mat.mainTextureScale = new Vector2(tilingX, tilingY);
        if (mat.HasProperty("_BaseMap"))
            mat.SetTextureScale("_BaseMap", new Vector2(tilingX, tilingY));
        return mat;
    }

    private Texture2D LoadTex(string name)
    {
        if (texCache.ContainsKey(name)) return texCache[name];
        Texture2D tex = Resources.Load<Texture2D>("Textures/" + name);
        if (tex != null) texCache[name] = tex;
        return tex;
    }

    private void Awake()
    {
        litShader = FindWorkingShader();

        roadMat = CreateTexturedMaterialTiled("tex_road_asphalt", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        sidewalkMat = CreateTexturedMaterial("tex_road_sidewalk", new Color(0.6f, 0.6f, 0.55f));
        grassMat = CreateTexturedMaterial("tex_grass", new Color(0.3f, 0.7f, 0.2f));
        barrierMat = CreateTexturedMaterial("tex_barrier_red", new Color(0.9f, 0.2f, 0.15f));
        trainMat = CreateTexturedMaterial("tex_train_side", new Color(0.3f, 0.3f, 0.7f));
        coneMat = CreateTexturedMaterial("tex_cone_orange", new Color(1f, 0.5f, 0f));
        fenceMat = CreateTexturedMaterial("tex_fence_metal", new Color(0.5f, 0.5f, 0.5f));
        lampMat = CreateTexturedMaterial("tex_streetlamp", new Color(0.4f, 0.4f, 0.4f));
        graffitiMat = CreateTexturedMaterial("tex_graffiti_wall", new Color(0.6f, 0.5f, 0.5f));

        coinMat = CreateTexturedMaterial("tex_coin_gold", new Color(1f, 0.85f, 0.1f));
        if (coinMat.HasProperty("_Metallic")) coinMat.SetFloat("_Metallic", 0.8f);
        if (coinMat.HasProperty("_Smoothness")) coinMat.SetFloat("_Smoothness", 0.9f);

        buildingMats = new Material[]
        {
            CreateTexturedMaterial("tex_building_red", new Color(0.7f, 0.4f, 0.35f)),
            CreateTexturedMaterial("tex_building_blue", new Color(0.35f, 0.45f, 0.7f)),
            CreateTexturedMaterial("tex_building_yellow", new Color(0.7f, 0.65f, 0.35f)),
            CreateTexturedMaterial("tex_building_grey", new Color(0.55f, 0.55f, 0.55f)),
            CreateTexturedMaterial("tex_building_pink", new Color(0.7f, 0.45f, 0.55f)),
            CreateTexturedMaterial("tex_building_modern_glass", new Color(0.4f, 0.55f, 0.7f)),
            CreateTexturedMaterial("tex_building_brownstone", new Color(0.5f, 0.35f, 0.25f)),
            CreateTexturedMaterial("tex_building_shop_front", new Color(0.6f, 0.5f, 0.4f)),
            CreateTexturedMaterial("tex_building_neon", new Color(0.3f, 0.2f, 0.5f)),
            CreateTexturedMaterial("tex_building_graffiti", new Color(0.5f, 0.4f, 0.45f))
        };

        cobbleMat = CreateTexturedMaterial("tex_ground_cobblestone", new Color(0.45f, 0.45f, 0.4f));
        crosswalkMat = CreateTexturedMaterial("tex_ground_crosswalk", new Color(0.9f, 0.9f, 0.9f));
        manholeMat = CreateTexturedMaterial("tex_ground_manhole", new Color(0.35f, 0.35f, 0.35f));

        propMats = new Material[]
        {
            CreateTexturedMaterial("tex_prop_trashcan", new Color(0.2f, 0.5f, 0.2f)),
            CreateTexturedMaterial("tex_prop_bench", new Color(0.45f, 0.3f, 0.15f)),
            CreateTexturedMaterial("tex_prop_mailbox", new Color(0.2f, 0.3f, 0.7f)),
            CreateTexturedMaterial("tex_prop_hydrant", new Color(0.8f, 0.15f, 0.1f)),
            CreateTexturedMaterial("tex_prop_newspaper", new Color(0.7f, 0.65f, 0.1f)),
            CreateTexturedMaterial("tex_prop_bollard", new Color(0.6f, 0.6f, 0.6f)),
            CreateTexturedMaterial("tex_prop_planter", new Color(0.4f, 0.55f, 0.3f)),
            CreateTexturedMaterial("tex_prop_streetlight", new Color(0.3f, 0.3f, 0.35f))
        };

        dumpsterMat = CreateTexturedMaterial("tex_obstacle_dumpster", new Color(0.2f, 0.45f, 0.2f));
        constructionMat = CreateTexturedMaterial("tex_obstacle_construction", new Color(0.9f, 0.5f, 0.1f));
        carMat = CreateTexturedMaterial("tex_obstacle_car_side", new Color(0.8f, 0.7f, 0.1f));
        busMat = CreateTexturedMaterial("tex_obstacle_bus", new Color(0.3f, 0.4f, 0.7f));

        trainVariantMats = new Material[]
        {
            trainMat,
            CreateTexturedMaterial("tex_train_graffiti_2", new Color(0.4f, 0.3f, 0.5f)),
            CreateTexturedMaterial("tex_train_clean", new Color(0.7f, 0.7f, 0.75f))
        };

        // Phase 3: Hi-res road texture (1024px)
        roadHDMat = CreateTexturedMaterialTiled("tex_road_hd", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);

        // Phase 3: Hi-res building textures (1024px)
        hiResBuildingMats = new Material[]
        {
            CreateTexturedMaterial("tex_building_highrise_1", new Color(0.5f, 0.7f, 0.9f)),
            CreateTexturedMaterial("tex_building_highrise_2", new Color(0.8f, 0.7f, 0.6f)),
            CreateTexturedMaterial("tex_building_industrial", new Color(0.4f, 0.4f, 0.4f)),
            CreateTexturedMaterial("tex_building_restaurant", new Color(0.9f, 0.4f, 0.2f)),
            CreateTexturedMaterial("tex_building_arcade", new Color(0.9f, 0.8f, 0.2f))
        };

        // Phase 3: Environment details
        billboardMat1 = CreateTexturedMaterial("tex_env_billboard_1", new Color(0.6f, 0.5f, 0.9f));
        billboardMat2 = CreateTexturedMaterial("tex_env_billboard_2", new Color(0.9f, 0.5f, 0.3f));
        rooftopMat = CreateTexturedMaterial("tex_env_rooftop", new Color(0.5f, 0.5f, 0.5f));
        tunnelMat = CreateTexturedMaterial("tex_env_tunnel_interior", new Color(0.3f, 0.3f, 0.35f));
    }

    public void StartTrack()
    {
        ClearAll();
        isRunning = true;
        nextSpawnZ = 0f;
        segmentsSpawned = 0;

        for (int i = 0; i < maxSegments; i++)
        {
            SpawnSegment(i < 2);
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

        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            if (activeSegments[i] == null) { activeSegments.RemoveAt(i); continue; }
            activeSegments[i].transform.position += Vector3.back * moveAmount;
            if (activeSegments[i].transform.position.z < despawnZ)
            {
                Destroy(activeSegments[i]);
                activeSegments.RemoveAt(i);
            }
        }

        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null) { activeObstacles.RemoveAt(i); continue; }
            activeObstacles[i].transform.position += Vector3.back * moveAmount;
            if (activeObstacles[i].transform.position.z < despawnZ)
            {
                Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }

        for (int i = activeCoins.Count - 1; i >= 0; i--)
        {
            if (activeCoins[i] == null) { activeCoins.RemoveAt(i); continue; }
            activeCoins[i].transform.position += Vector3.back * moveAmount;
            activeCoins[i].transform.Rotate(Vector3.up, 120f * Time.deltaTime);
            if (activeCoins[i].transform.position.z < despawnZ)
            {
                Destroy(activeCoins[i]);
                activeCoins.RemoveAt(i);
            }
        }

        while (activeSegments.Count < maxSegments)
        {
            SpawnSegment(false);
        }
    }

    private void SpawnSegment(bool safe)
    {
        GameObject segment = new GameObject("Segment_" + segmentsSpawned);
        segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);

        // Road surface — use Phase 3 HD texture, lowered to avoid z-fighting
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(segment.transform);
        road.transform.localPosition = new Vector3(0f, -0.55f, segmentLength / 2f);
        road.transform.localScale = new Vector3(10f, 1f, segmentLength + 0.2f);
        road.GetComponent<Renderer>().material = roadHDMat != null ? roadHDMat : roadMat;
        Destroy(road.GetComponent<Collider>());

        // Sidewalks
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk";
            sw.transform.SetParent(segment.transform);
            sw.transform.localPosition = new Vector3(side * 5.8f, -0.3f, segmentLength / 2f);
            sw.transform.localScale = new Vector3(2f, 0.6f, segmentLength);
            sw.GetComponent<Renderer>().material = sidewalkMat;
            Destroy(sw.GetComponent<Collider>());
        }

        // Lane dividers (dashed)
        for (float lx = -1.25f; lx <= 1.25f; lx += 2.5f)
        {
            for (int d = 0; d < 5; d++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "LaneDash";
                line.transform.SetParent(segment.transform);
                line.transform.localPosition = new Vector3(lx, 0.08f, d * 8f + 2f);
                line.transform.localScale = new Vector3(0.12f, 0.04f, 4f);
                line.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 1f, 0.8f));
                Destroy(line.GetComponent<Collider>());
            }
        }

        // Crosswalk patches
        if (Random.value < 0.3f)
        {
            GameObject crosswalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crosswalk.name = "Crosswalk";
            crosswalk.transform.SetParent(segment.transform);
            float cwZ = Random.Range(5f, segmentLength - 5f);
            crosswalk.transform.localPosition = new Vector3(0f, 0.08f, cwZ);
            crosswalk.transform.localScale = new Vector3(8f, 0.04f, 3f);
            crosswalk.GetComponent<Renderer>().material = crosswalkMat;
            Destroy(crosswalk.GetComponent<Collider>());
        }

        // Manhole covers
        if (Random.value < 0.2f)
        {
            GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manhole.name = "Manhole";
            manhole.transform.SetParent(segment.transform);
            float mhZ = Random.Range(5f, segmentLength - 5f);
            int mhLane = Random.Range(-1, 2);
            manhole.transform.localPosition = new Vector3(mhLane * laneWidth, 0.08f, mhZ);
            manhole.transform.localScale = new Vector3(1f, 0.04f, 1f);
            manhole.GetComponent<Renderer>().material = manholeMat;
            Destroy(manhole.GetComponent<Collider>());
        }

        // Buildings with textures — Phase 3: mix hi-res (1024px) + original variants
        Material[] allBuildingMats;
        if (hiResBuildingMats != null && hiResBuildingMats.Length > 0)
        {
            allBuildingMats = new Material[buildingMats.Length + hiResBuildingMats.Length];
            buildingMats.CopyTo(allBuildingMats, 0);
            hiResBuildingMats.CopyTo(allBuildingMats, buildingMats.Length);
        }
        else
        {
            allBuildingMats = buildingMats;
        }

        for (int side = -1; side <= 1; side += 2)
        {
            int buildingCount = Random.Range(2, 5);
            float zOffset = 0f;
            for (int b = 0; b < buildingCount; b++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "Building";
                building.transform.SetParent(segment.transform);
                float height = Random.Range(6f, 22f);
                float depth = Random.Range(6f, segmentLength / buildingCount);
                building.transform.localPosition = new Vector3(
                    side * (7f + Random.Range(0f, 2f)),
                    height / 2f,
                    zOffset + depth / 2f
                );
                building.transform.localScale = new Vector3(
                    Random.Range(3f, 6f), height, depth - 0.5f
                );
                building.GetComponent<Renderer>().material = allBuildingMats[Random.Range(0, allBuildingMats.Length)];
                Destroy(building.GetComponent<Collider>());

                // Windows on buildings — denser grid
                if (height > 7f)
                {
                    int windowRows = Mathf.Min(5, (int)(height / 3f));
                    int windowCols = Random.Range(2, 4);
                    for (int wr = 0; wr < windowRows; wr++)
                    {
                        for (int wc = 0; wc < windowCols; wc++)
                        {
                            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            window.name = "Window";
                            window.transform.SetParent(building.transform);
                            window.transform.localPosition = new Vector3(
                                -side * 0.51f,
                                -0.35f + wr * 0.18f,
                                -0.3f + wc * 0.25f
                            );
                            window.transform.localScale = new Vector3(0.02f, 0.1f, 0.08f);
                            Color winColor = Random.value < 0.6f
                                ? new Color(0.95f, 0.9f, 0.7f, 0.9f)
                                : new Color(0.2f, 0.25f, 0.35f, 0.8f);
                            window.GetComponent<Renderer>().material = CreateColorMaterial(winColor);
                            Destroy(window.GetComponent<Collider>());
                        }
                    }
                }

                // Phase 3: Rooftop detail on tall buildings
                if (height > 12f && Random.value < 0.4f && rooftopMat != null)
                {
                    GameObject rooftop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rooftop.name = "Rooftop";
                    rooftop.transform.SetParent(building.transform);
                    rooftop.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                    rooftop.transform.localScale = new Vector3(1.02f, 0.04f, 1.02f);
                    rooftop.GetComponent<Renderer>().material = rooftopMat;
                    Destroy(rooftop.GetComponent<Collider>());
                }

                // Awning on ground floor shops
                if (height < 10f && Random.value < 0.3f)
                {
                    GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    awning.name = "Awning";
                    awning.transform.SetParent(building.transform);
                    awning.transform.localPosition = new Vector3(-side * 0.55f, -0.35f, 0f);
                    awning.transform.localScale = new Vector3(0.3f, 0.02f, 0.4f);
                    Color awningColor = new Color(Random.Range(0.5f, 1f), Random.Range(0.2f, 0.6f), Random.Range(0.1f, 0.4f));
                    awning.GetComponent<Renderer>().material = CreateColorMaterial(awningColor);
                    Destroy(awning.GetComponent<Collider>());
                }

                zOffset += depth;
            }

            // Phase 3: Billboard on building side
            if (Random.value < 0.35f && (billboardMat1 != null || billboardMat2 != null))
            {
                GameObject billboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                billboard.name = "Billboard";
                billboard.transform.SetParent(segment.transform);
                float bbZ = Random.Range(5f, segmentLength - 5f);
                float bbHeight = Random.Range(6f, 12f);
                billboard.transform.localPosition = new Vector3(side * 6.9f, bbHeight, bbZ);
                billboard.transform.localScale = new Vector3(0.15f, 3f, 5f);
                billboard.GetComponent<Renderer>().material = Random.value < 0.5f ? billboardMat1 : billboardMat2;
                Destroy(billboard.GetComponent<Collider>());
            }
        }

        // Street props — dense variety
        for (int side = -1; side <= 1; side += 2)
        {
            // Street lamps
            if (Random.value < 0.65f)
            {
                float lampZ = Random.Range(5f, 35f);
                GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lamp.name = "StreetLamp";
                lamp.transform.SetParent(segment.transform);
                lamp.transform.localPosition = new Vector3(side * 4.8f, 2f, lampZ);
                lamp.transform.localScale = new Vector3(0.12f, 2f, 0.12f);
                lamp.GetComponent<Renderer>().material = propMats.Length > 7 ? propMats[7] : lampMat;
                Destroy(lamp.GetComponent<Collider>());

                GameObject lhead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lhead.transform.SetParent(lamp.transform);
                lhead.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                lhead.transform.localScale = new Vector3(3f, 1f, 3f);
                lhead.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.95f, 0.7f));
                Destroy(lhead.GetComponent<Collider>());
            }

            // Fences
            if (Random.value < 0.25f)
            {
                GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fence.name = "Fence";
                fence.transform.SetParent(segment.transform);
                fence.transform.localPosition = new Vector3(side * 4.5f, 0.5f, segmentLength / 2f);
                fence.transform.localScale = new Vector3(0.1f, 1f, segmentLength * 0.8f);
                fence.GetComponent<Renderer>().material = fenceMat;
                Destroy(fence.GetComponent<Collider>());
            }

            // Trash cans
            if (Random.value < 0.4f)
            {
                GameObject trashcan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trashcan.name = "TrashCan";
                trashcan.transform.SetParent(segment.transform);
                float tcZ = Random.Range(3f, segmentLength - 3f);
                trashcan.transform.localPosition = new Vector3(side * 4.6f, 0.4f, tcZ);
                trashcan.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                trashcan.GetComponent<Renderer>().material = propMats[0];
                Destroy(trashcan.GetComponent<Collider>());
            }

            // Benches
            if (Random.value < 0.25f)
            {
                GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "Bench";
                bench.transform.SetParent(segment.transform);
                float bZ = Random.Range(5f, segmentLength - 5f);
                bench.transform.localPosition = new Vector3(side * 4.7f, 0.3f, bZ);
                bench.transform.localScale = new Vector3(0.4f, 0.35f, 1.2f);
                bench.GetComponent<Renderer>().material = propMats[1];
                Destroy(bench.GetComponent<Collider>());
            }

            // Mailboxes
            if (Random.value < 0.15f)
            {
                GameObject mailbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mailbox.name = "Mailbox";
                mailbox.transform.SetParent(segment.transform);
                float mbZ = Random.Range(5f, segmentLength - 5f);
                mailbox.transform.localPosition = new Vector3(side * 4.5f, 0.55f, mbZ);
                mailbox.transform.localScale = new Vector3(0.35f, 0.65f, 0.3f);
                mailbox.GetComponent<Renderer>().material = propMats[2];
                Destroy(mailbox.GetComponent<Collider>());
            }

            // Fire hydrants
            if (Random.value < 0.2f)
            {
                GameObject hydrant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hydrant.name = "Hydrant";
                hydrant.transform.SetParent(segment.transform);
                float hyZ = Random.Range(3f, segmentLength - 3f);
                hydrant.transform.localPosition = new Vector3(side * 4.4f, 0.25f, hyZ);
                hydrant.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);
                hydrant.GetComponent<Renderer>().material = propMats[3];
                Destroy(hydrant.GetComponent<Collider>());
            }

            // Bollards
            if (Random.value < 0.3f)
            {
                int bollardCount = Random.Range(2, 5);
                float bollardStartZ = Random.Range(3f, segmentLength - 10f);
                for (int bi = 0; bi < bollardCount; bi++)
                {
                    GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bollard.name = "Bollard";
                    bollard.transform.SetParent(segment.transform);
                    bollard.transform.localPosition = new Vector3(side * 4.3f, 0.35f, bollardStartZ + bi * 2f);
                    bollard.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                    bollard.GetComponent<Renderer>().material = propMats[5];
                    Destroy(bollard.GetComponent<Collider>());
                }
            }

            // Planters with flowers
            if (Random.value < 0.2f)
            {
                GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                planter.name = "Planter";
                planter.transform.SetParent(segment.transform);
                float plZ = Random.Range(5f, segmentLength - 5f);
                planter.transform.localPosition = new Vector3(side * 4.6f, 0.3f, plZ);
                planter.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
                planter.GetComponent<Renderer>().material = propMats[6];
                Destroy(planter.GetComponent<Collider>());

                GameObject flowers = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flowers.name = "Flowers";
                flowers.transform.SetParent(planter.transform);
                flowers.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                flowers.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
                Color flowerColor = new Color(Random.Range(0.6f, 1f), Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.6f));
                flowers.GetComponent<Renderer>().material = CreateColorMaterial(flowerColor);
                Destroy(flowers.GetComponent<Collider>());
            }
        }

        // Grass beyond sidewalks
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "Grass";
            grass.transform.SetParent(segment.transform);
            grass.transform.localPosition = new Vector3(side * 15f, -0.6f, segmentLength / 2f);
            grass.transform.localScale = new Vector3(18f, 0.5f, segmentLength);
            grass.GetComponent<Renderer>().material = grassMat;
            Destroy(grass.GetComponent<Collider>());
        }

        // Phase 3: Tunnel sections (occasional)
        if (segmentsSpawned > 3 && Random.value < 0.12f && tunnelMat != null)
        {
            GameObject tunnel = new GameObject("Tunnel");
            tunnel.transform.SetParent(segment.transform);
            float tunnelZ = segmentLength / 2f;

            // Tunnel ceiling
            GameObject ceil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceil.name = "TunnelCeiling";
            ceil.transform.SetParent(tunnel.transform);
            ceil.transform.localPosition = new Vector3(0f, 6f, tunnelZ);
            ceil.transform.localScale = new Vector3(10f, 0.3f, 15f);
            ceil.GetComponent<Renderer>().material = tunnelMat;
            Destroy(ceil.GetComponent<Collider>());

            // Tunnel walls
            for (int ts = -1; ts <= 1; ts += 2)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "TunnelWall";
                wall.transform.SetParent(tunnel.transform);
                wall.transform.localPosition = new Vector3(ts * 5f, 3f, tunnelZ);
                wall.transform.localScale = new Vector3(0.3f, 6f, 15f);
                wall.GetComponent<Renderer>().material = tunnelMat;
                Destroy(wall.GetComponent<Collider>());
            }

            // Tunnel lights
            for (int tl = -1; tl <= 1; tl += 2)
            {
                GameObject tLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tLight.name = "TunnelLight";
                tLight.transform.SetParent(tunnel.transform);
                tLight.transform.localPosition = new Vector3(tl * 3f, 5.8f, tunnelZ);
                tLight.transform.localScale = new Vector3(0.3f, 0.1f, 12f);
                tLight.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.95f, 0.7f, 0.8f));
                Destroy(tLight.GetComponent<Collider>());
            }
        }

        // Phase 3: Apply curved-world visual offset to distant segments
        ApplyCurvedWorld(segment);

        activeSegments.Add(segment);

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
        int count = Random.Range(1, 4);
        float spacing = segmentLength / (count + 1);

        for (int i = 0; i < count; i++)
        {
            float z = segStartZ + spacing * (i + 1);
            int lane = Random.Range(-1, 2);
            int obstacleType = Random.Range(0, 12);

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
                obs.GetComponent<Renderer>().material = barrierMat;
                break;

            case 1: // Tall barrier
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(1.5f, 2.5f, 0.5f);
                obs.transform.position += Vector3.up * 1.25f;
                obs.GetComponent<Renderer>().material = barrierMat;
                break;

            case 2: // Overhead bar
                obs = new GameObject("Overhead");
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.transform.SetParent(obs.transform);
                bar.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                bar.transform.localScale = new Vector3(3f, 0.3f, 0.3f);
                bar.GetComponent<Renderer>().material = barrierMat;
                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pillar.transform.SetParent(obs.transform);
                    pillar.transform.localPosition = new Vector3(s * 1.3f, 1.25f, 0f);
                    pillar.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
                    pillar.GetComponent<Renderer>().material = fenceMat;
                    Destroy(pillar.GetComponent<Collider>());
                }
                Destroy(bar.GetComponent<Collider>());
                break;

            case 3: // Cone
                obs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obs.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                obs.transform.position += Vector3.up * 0.5f;
                obs.GetComponent<Renderer>().material = coneMat;
                break;

            case 4: // Wide barrier
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(4f, 1.5f, 0.5f);
                obs.transform.position += Vector3.up * 0.75f;
                obs.GetComponent<Renderer>().material = barrierMat;
                break;

            case 5: // Train with variant textures
                obs = new GameObject("Train");
                Material selectedTrainMat = trainVariantMats[Random.Range(0, trainVariantMats.Length)];
                for (int c = 0; c < 3; c++)
                {
                    GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    car.transform.SetParent(obs.transform);
                    car.transform.localPosition = new Vector3(0f, 1.2f, c * 2.2f);
                    car.transform.localScale = new Vector3(1.8f, 2.2f, 2f);
                    car.GetComponent<Renderer>().material = selectedTrainMat;
                    Destroy(car.GetComponent<Collider>());
                }
                GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.transform.SetParent(obs.transform);
                roof.transform.localPosition = new Vector3(0f, 2.4f, 2.2f);
                roof.transform.localScale = new Vector3(1.9f, 0.2f, 6.5f);
                roof.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.3f, 0.3f, 0.35f));
                Destroy(roof.GetComponent<Collider>());
                break;

            case 6: // Warning zone
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(2f, 0.1f, 2f);
                obs.transform.position += Vector3.up * 0.05f;
                obs.GetComponent<Renderer>().material = coneMat;
                break;

            case 7: // Staggered combo
                obs = new GameObject("Staggered");
                for (int s = 0; s < 2; s++)
                {
                    GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.transform.SetParent(obs.transform);
                    piece.transform.localPosition = new Vector3(s * 1.5f - 0.75f, 0.75f, s * 1.5f);
                    piece.transform.localScale = new Vector3(1f, 1.5f, 0.5f);
                    piece.GetComponent<Renderer>().material = barrierMat;
                    Destroy(piece.GetComponent<Collider>());
                }
                break;

            case 8: // Dumpster
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.localScale = new Vector3(1.8f, 1.4f, 1.2f);
                obs.transform.position += Vector3.up * 0.7f;
                obs.GetComponent<Renderer>().material = dumpsterMat;
                break;

            case 9: // Construction barrier with cones
                obs = new GameObject("Construction");
                GameObject cBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cBarrier.transform.SetParent(obs.transform);
                cBarrier.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                cBarrier.transform.localScale = new Vector3(2.5f, 1.2f, 0.3f);
                cBarrier.GetComponent<Renderer>().material = constructionMat;
                Destroy(cBarrier.GetComponent<Collider>());
                for (int cs = -1; cs <= 1; cs += 2)
                {
                    GameObject cone2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cone2.transform.SetParent(obs.transform);
                    cone2.transform.localPosition = new Vector3(cs * 1.4f, 0.4f, 0f);
                    cone2.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
                    cone2.GetComponent<Renderer>().material = coneMat;
                    Destroy(cone2.GetComponent<Collider>());
                }
                break;

            case 10: // Parked car
                obs = new GameObject("Car");
                GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carBody.transform.SetParent(obs.transform);
                carBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                carBody.transform.localScale = new Vector3(1.6f, 1.0f, 3.0f);
                carBody.GetComponent<Renderer>().material = carMat;
                Destroy(carBody.GetComponent<Collider>());
                GameObject carRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carRoof.transform.SetParent(obs.transform);
                carRoof.transform.localPosition = new Vector3(0f, 1.3f, 0.2f);
                carRoof.transform.localScale = new Vector3(1.4f, 0.5f, 1.5f);
                carRoof.GetComponent<Renderer>().material = carMat;
                Destroy(carRoof.GetComponent<Collider>());
                for (int wx = -1; wx <= 1; wx += 2)
                {
                    for (int wz = -1; wz <= 1; wz += 2)
                    {
                        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        wheel.transform.SetParent(obs.transform);
                        wheel.transform.localPosition = new Vector3(wx * 0.75f, 0.2f, wz * 1.0f);
                        wheel.transform.localScale = new Vector3(0.35f, 0.1f, 0.35f);
                        wheel.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                        wheel.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.15f, 0.15f, 0.15f));
                        Destroy(wheel.GetComponent<Collider>());
                    }
                }
                break;

            default: // Bus obstacle
                obs = new GameObject("Bus");
                GameObject busBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                busBody.transform.SetParent(obs.transform);
                busBody.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                busBody.transform.localScale = new Vector3(2.0f, 2.2f, 5.0f);
                busBody.GetComponent<Renderer>().material = busMat;
                Destroy(busBody.GetComponent<Collider>());
                for (int bw = 0; bw < 4; bw++)
                {
                    GameObject busWin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    busWin.transform.SetParent(obs.transform);
                    busWin.transform.localPosition = new Vector3(1.01f, 1.6f, -1.5f + bw * 1.0f);
                    busWin.transform.localScale = new Vector3(0.02f, 0.6f, 0.6f);
                    busWin.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.7f, 0.85f, 1f, 0.8f));
                    Destroy(busWin.GetComponent<Collider>());
                }
                break;
        }

        obs.name = "Obstacle";
        return obs;
    }

    private void PlaceCoins(float segStartZ)
    {
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
            coin.transform.position = new Vector3(lane * laneWidth, 1.2f, z);
            coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
            coin.GetComponent<Renderer>().material = coinMat;
            Destroy(coin.GetComponent<Collider>());
            activeCoins.Add(coin);
        }

        if (Random.value < 0.5f)
        {
            int lane2 = Random.Range(-1, 2);
            while (lane2 == lane) lane2 = Random.Range(-1, 2);
            float startZ2 = segStartZ + segmentLength * 0.5f;
            int count2 = Random.Range(2, 6);
            for (int i = 0; i < count2; i++)
            {
                float z = startZ2 + i * coinSpacing;
                if (z >= segStartZ + segmentLength - 5f) break;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = new Vector3(lane2 * laneWidth, 1.2f, z);
                coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
                coin.GetComponent<Renderer>().material = coinMat;
                Destroy(coin.GetComponent<Collider>());
                activeCoins.Add(coin);
            }
        }

        // Elevated coins (jump to collect)
        if (Random.value < 0.25f)
        {
            int elevLane = Random.Range(-1, 2);
            float elevZ = segStartZ + Random.Range(10f, segmentLength - 10f);
            int elevCount = Random.Range(3, 6);
            for (int i = 0; i < elevCount; i++)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = new Vector3(elevLane * laneWidth, 3.5f, elevZ + i * 2.5f);
                coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
                coin.GetComponent<Renderer>().material = coinMat;
                Destroy(coin.GetComponent<Collider>());
                activeCoins.Add(coin);
            }
        }
    }

    // Phase 3: Curved-world effect — bends distant segments downward like Subway Surfers horizon
    private void ApplyCurvedWorld(GameObject segment)
    {
        if (segment == null) return;
        float distZ = segment.transform.position.z;
        if (distZ > 20f)
        {
            float drop = curvedWorldIntensity * (distZ - 20f) * (distZ - 20f);
            Vector3 pos = segment.transform.position;
            pos.y -= drop;
            segment.transform.position = pos;
        }
    }

    // Phase 3: Update curved-world each frame for all active segments
    private void UpdateCurvedWorld()
    {
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] == null) continue;
            // Reset Y first (undo previous curve), then reapply
            Vector3 pos = activeSegments[i].transform.position;
            // We store the flat Y in the segment name won't work, so use position.z to compute
            float distZ = pos.z;
            float flatY = 0f; // segments are spawned at Y=0
            if (distZ > 20f)
            {
                float drop = curvedWorldIntensity * (distZ - 20f) * (distZ - 20f);
                pos.y = flatY - drop;
            }
            else
            {
                pos.y = flatY;
            }
            activeSegments[i].transform.position = pos;
        }

        // Also curve obstacles and coins
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            if (activeObstacles[i] == null) continue;
            Vector3 pos = activeObstacles[i].transform.position;
            float distZ = pos.z;
            if (distZ > 20f)
            {
                float drop = curvedWorldIntensity * (distZ - 20f) * (distZ - 20f);
                // Obstacles are at varying Y heights, preserve their base offset
                // We only adjust the curve component
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
