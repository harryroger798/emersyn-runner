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

    // Phase 8: Road curb and HD crosswalk materials
    private Material curbMat;
    private Material crosswalkHDMat;

    // Phase 3+10: Curved world effect (reduced from 0.008 to 0.005 to minimize grass stretching)
    private float curvedWorldIntensity = 0.005f;

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

        // Phase 8: Use HD road asphalt and stone sidewalk textures from Modal
        roadMat = CreateTexturedMaterialTiled("tex_road_asphalt_hd", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadMat.mainTexture == null) roadMat = CreateTexturedMaterialTiled("tex_road_asphalt", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        // Phase 10: Use HD sidewalk texture from Modal, fallback chain
        sidewalkMat = CreateTexturedMaterial("tex_sidewalk_hd", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_stone", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_road_sidewalk", new Color(0.6f, 0.6f, 0.55f));
        // Phase 10: Use HD grass texture from Modal with tiling to reduce stretching
        grassMat = CreateTexturedMaterialTiled("tex_ground_grass_hd", new Color(0.35f, 0.55f, 0.25f), 4f, 4f);
        if (grassMat.mainTexture == null) grassMat = CreateTexturedMaterialTiled("tex_ground_grass_patch", new Color(0.35f, 0.55f, 0.25f), 4f, 4f);
        if (grassMat.mainTexture == null) grassMat = CreateTexturedMaterialTiled("tex_grass", new Color(0.35f, 0.55f, 0.25f), 4f, 4f);
        // Phase 8: Use HD barrier texture from Modal
        barrierMat = CreateTexturedMaterial("tex_obstacle_barrier_hd", new Color(0.9f, 0.2f, 0.15f));
        if (barrierMat.mainTexture == null) barrierMat = CreateTexturedMaterial("tex_barrier_red", new Color(0.9f, 0.2f, 0.15f));
        // Phase 8: Use HD train and cone textures from Modal
        // Phase 10: Use train front texture from Modal
        trainMat = CreateTexturedMaterial("tex_train_front", new Color(0.3f, 0.3f, 0.7f));
        if (trainMat.mainTexture == null) trainMat = CreateTexturedMaterial("tex_train_side_hd", new Color(0.3f, 0.3f, 0.7f));
        if (trainMat.mainTexture == null) trainMat = CreateTexturedMaterial("tex_train_side", new Color(0.3f, 0.3f, 0.7f));
        coneMat = CreateTexturedMaterial("tex_obstacle_cone_hd", new Color(1f, 0.5f, 0f));
        if (coneMat.mainTexture == null) coneMat = CreateTexturedMaterial("tex_cone_orange", new Color(1f, 0.5f, 0f));
        // Phase 10: Use chain link fence and streetlight textures from Modal
        fenceMat = CreateTexturedMaterial("tex_fence_chain_link", new Color(0.5f, 0.5f, 0.5f));
        if (fenceMat.mainTexture == null) fenceMat = CreateTexturedMaterial("tex_fence_metal", new Color(0.5f, 0.5f, 0.5f));
        lampMat = CreateTexturedMaterial("tex_streetlight_pole", new Color(0.4f, 0.4f, 0.4f));
        if (lampMat.mainTexture == null) lampMat = CreateTexturedMaterial("tex_streetlamp", new Color(0.4f, 0.4f, 0.4f));
        graffitiMat = CreateTexturedMaterial("tex_graffiti_wall", new Color(0.6f, 0.5f, 0.5f));

        // Phase 6: Use detailed coin texture if available, fallback to gold
        coinMat = CreateTexturedMaterial("tex_coin_detailed", new Color(1f, 0.85f, 0.1f));
        if (coinMat.mainTexture == null) coinMat = CreateTexturedMaterial("tex_coin_gold", new Color(1f, 0.85f, 0.1f));
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

        // Phase 9: Expanded props array with new Modal textures
        propMats = new Material[]
        {
            CreateTexturedMaterial("tex_prop_trashcan", new Color(0.2f, 0.5f, 0.2f)),
            CreateTexturedMaterial("tex_prop_bench", new Color(0.45f, 0.3f, 0.15f)),
            CreateTexturedMaterial("tex_prop_mailbox", new Color(0.2f, 0.3f, 0.7f)),
            CreateTexturedMaterial("tex_prop_hydrant", new Color(0.8f, 0.15f, 0.1f)),
            CreateTexturedMaterial("tex_prop_newspaper", new Color(0.7f, 0.65f, 0.1f)),
            CreateTexturedMaterial("tex_prop_bollard", new Color(0.6f, 0.6f, 0.6f)),
            CreateTexturedMaterial("tex_prop_planter", new Color(0.4f, 0.55f, 0.3f)),
            CreateTexturedMaterial("tex_prop_streetlight", new Color(0.3f, 0.3f, 0.35f)),
            CreateTexturedMaterial("tex_prop_vending_machine", new Color(0.3f, 0.4f, 0.7f)),
            CreateTexturedMaterial("tex_prop_phone_booth", new Color(0.8f, 0.2f, 0.15f)),
            CreateTexturedMaterial("tex_prop_fire_escape", new Color(0.4f, 0.4f, 0.4f)),
            CreateTexturedMaterial("tex_prop_awning_striped", new Color(0.8f, 0.3f, 0.2f)),
            CreateTexturedMaterial("tex_prop_potted_plant", new Color(0.3f, 0.55f, 0.25f)),
            // Phase 11: 3 new props from Modal
            CreateTexturedMaterial("tex_prop_food_cart", new Color(0.8f, 0.5f, 0.2f)),
            CreateTexturedMaterial("tex_prop_bus_stop", new Color(0.5f, 0.6f, 0.7f)),
            CreateTexturedMaterial("tex_prop_traffic_light", new Color(0.3f, 0.3f, 0.3f))
        };

        // Phase 8: Use HD obstacle textures from Modal with fallbacks
        dumpsterMat = CreateTexturedMaterial("tex_obstacle_dumpster", new Color(0.2f, 0.45f, 0.2f));
        constructionMat = CreateTexturedMaterial("tex_obstacle_barrier_hd", new Color(0.9f, 0.5f, 0.1f));
        if (constructionMat.mainTexture == null) constructionMat = CreateTexturedMaterial("tex_obstacle_construction", new Color(0.9f, 0.5f, 0.1f));
        carMat = CreateTexturedMaterial("tex_obstacle_car_side", new Color(0.8f, 0.7f, 0.1f));
        busMat = CreateTexturedMaterial("tex_obstacle_bus_side", new Color(0.3f, 0.4f, 0.7f));
        if (busMat.mainTexture == null) busMat = CreateTexturedMaterial("tex_obstacle_bus", new Color(0.3f, 0.4f, 0.7f));

        // Phase 10: Use HD graffiti train texture from Modal
        trainVariantMats = new Material[]
        {
            trainMat,
            CreateTexturedMaterial("tex_train_graffiti_hd", new Color(0.4f, 0.3f, 0.5f)),
            CreateTexturedMaterial("tex_train_clean", new Color(0.7f, 0.7f, 0.75f))
        };

        // Phase 8: Hi-res road texture — prefer Phase 8 HD, fallback to Phase 3
        roadHDMat = CreateTexturedMaterialTiled("tex_road_asphalt_hd", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadHDMat.mainTexture == null) roadHDMat = CreateTexturedMaterialTiled("tex_road_hd", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);

        // Phase 8: Road curb material
        curbMat = CreateTexturedMaterial("tex_road_curb", new Color(0.55f, 0.55f, 0.5f));
        crosswalkHDMat = CreateTexturedMaterial("tex_road_crosswalk_hd", new Color(0.9f, 0.9f, 0.9f));

        // Phase 3+6+9: Hi-res building textures (1024px) — expanded with Phase 9 buildings
        hiResBuildingMats = new Material[]
        {
            CreateTexturedMaterial("tex_building_highrise_1", new Color(0.5f, 0.7f, 0.9f)),
            CreateTexturedMaterial("tex_building_highrise_2", new Color(0.8f, 0.7f, 0.6f)),
            CreateTexturedMaterial("tex_building_industrial", new Color(0.4f, 0.4f, 0.4f)),
            CreateTexturedMaterial("tex_building_restaurant", new Color(0.9f, 0.4f, 0.2f)),
            CreateTexturedMaterial("tex_building_arcade", new Color(0.9f, 0.8f, 0.2f)),
            CreateTexturedMaterial("tex_building_hospital", new Color(0.9f, 0.9f, 0.95f)),
            CreateTexturedMaterial("tex_building_school", new Color(0.7f, 0.5f, 0.3f)),
            CreateTexturedMaterial("tex_building_cinema", new Color(0.6f, 0.2f, 0.3f)),
            // Phase 9: 5 new building types from Modal
            CreateTexturedMaterial("tex_building_apartment", new Color(0.6f, 0.45f, 0.35f)),
            CreateTexturedMaterial("tex_building_office_tower", new Color(0.4f, 0.55f, 0.75f)),
            CreateTexturedMaterial("tex_building_warehouse", new Color(0.45f, 0.45f, 0.45f)),
            CreateTexturedMaterial("tex_building_diner", new Color(0.8f, 0.3f, 0.25f)),
            CreateTexturedMaterial("tex_building_bookstore", new Color(0.55f, 0.4f, 0.3f)),
            // Phase 10: 4 new building types from Modal
            CreateTexturedMaterial("tex_building_skyscraper", new Color(0.4f, 0.6f, 0.8f)),
            CreateTexturedMaterial("tex_building_brick_shop", new Color(0.6f, 0.4f, 0.3f)),
            CreateTexturedMaterial("tex_building_hotel", new Color(0.7f, 0.6f, 0.5f)),
            CreateTexturedMaterial("tex_building_gym", new Color(0.5f, 0.5f, 0.6f)),
            // Phase 11: 4 new building types from Modal
            CreateTexturedMaterial("tex_building_pizzeria", new Color(0.8f, 0.4f, 0.3f)),
            CreateTexturedMaterial("tex_building_bank", new Color(0.7f, 0.7f, 0.75f)),
            CreateTexturedMaterial("tex_building_laundromat", new Color(0.5f, 0.6f, 0.7f)),
            CreateTexturedMaterial("tex_building_music_shop", new Color(0.6f, 0.3f, 0.6f))
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

        // Phase 4: Apply curved-world effect every frame so segments bend as they move
        UpdateCurvedWorld();
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
            // Phase 11: Wider sidewalks (3.5 instead of 2) to cover grass gap and reduce orange streaks
            sw.transform.localPosition = new Vector3(side * 6.2f, -0.3f, segmentLength / 2f);
            sw.transform.localScale = new Vector3(3.5f, 0.6f, segmentLength);
            sw.GetComponent<Renderer>().material = sidewalkMat;
            Destroy(sw.GetComponent<Collider>());

            // Phase 8: Road curb between road and sidewalk
            if (curbMat != null)
            {
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = "Curb";
                curb.transform.SetParent(segment.transform);
                curb.transform.localPosition = new Vector3(side * 4.7f, -0.05f, segmentLength / 2f);
                curb.transform.localScale = new Vector3(0.3f, 0.15f, segmentLength);
                curb.GetComponent<Renderer>().material = curbMat;
                Destroy(curb.GetComponent<Collider>());
            }
        }

        // Phase 7: Thinner, subtler lane dividers (white dashed, not bright yellow)
        for (float lx = -1.25f; lx <= 1.25f; lx += 2.5f)
        {
            for (int d = 0; d < 5; d++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "LaneDash";
                line.transform.SetParent(segment.transform);
                line.transform.localPosition = new Vector3(lx, 0.08f, d * 8f + 2f);
                line.transform.localScale = new Vector3(0.08f, 0.03f, 3f);
                line.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.9f, 0.9f, 0.85f, 0.8f));
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

        // Phase 9: Ground detail - puddles (rain effect)
        if (Random.value < 0.15f)
        {
            Material puddleMat = CreateTexturedMaterial("tex_ground_puddle", new Color(0.3f, 0.4f, 0.6f));
            if (puddleMat.mainTexture != null)
            {
                GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                puddle.name = "Puddle";
                puddle.transform.SetParent(segment.transform);
                float pdZ = Random.Range(5f, segmentLength - 5f);
                int pdLane = Random.Range(-1, 2);
                puddle.transform.localPosition = new Vector3(pdLane * laneWidth, 0.07f, pdZ);
                puddle.transform.localScale = new Vector3(1.5f, 0.02f, 1f);
                puddle.GetComponent<Renderer>().material = puddleMat;
                Destroy(puddle.GetComponent<Collider>());
            }
        }

        // Phase 9: Ground detail - drainage grates
        if (Random.value < 0.12f)
        {
            Material grateMat = CreateTexturedMaterial("tex_ground_grate", new Color(0.35f, 0.35f, 0.35f));
            if (grateMat.mainTexture != null)
            {
                GameObject grate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grate.name = "Grate";
                grate.transform.SetParent(segment.transform);
                float grZ = Random.Range(5f, segmentLength - 5f);
                int grSide = Random.value < 0.5f ? -1 : 1;
                grate.transform.localPosition = new Vector3(grSide * 4.2f, 0.07f, grZ);
                grate.transform.localScale = new Vector3(0.8f, 0.02f, 0.5f);
                grate.GetComponent<Renderer>().material = grateMat;
                Destroy(grate.GetComponent<Collider>());
            }
        }

        // Phase 9: Road arrow markings
        if (Random.value < 0.1f)
        {
            Material arrowMat = CreateTexturedMaterial("tex_road_marking_arrow", new Color(0.9f, 0.9f, 0.9f));
            if (arrowMat.mainTexture != null)
            {
                GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = "RoadArrow";
                arrow.transform.SetParent(segment.transform);
                float arZ = Random.Range(10f, segmentLength - 10f);
                int arLane = Random.Range(-1, 2);
                arrow.transform.localPosition = new Vector3(arLane * laneWidth, 0.08f, arZ);
                arrow.transform.localScale = new Vector3(1.2f, 0.02f, 2f);
                arrow.GetComponent<Renderer>().material = arrowMat;
                Destroy(arrow.GetComponent<Collider>());
            }
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

        // Phase 4: Curb/gutter detail between sidewalk and road
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = "Curb";
            curb.transform.SetParent(segment.transform);
            curb.transform.localPosition = new Vector3(side * 4.7f, -0.15f, segmentLength / 2f);
            curb.transform.localScale = new Vector3(0.15f, 0.3f, segmentLength);
            curb.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.55f, 0.55f, 0.5f));
            Destroy(curb.GetComponent<Collider>());
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
        // Phase 5: Pattern-based obstacle placement for more interesting gameplay
        int pattern = Random.Range(0, 6);
        float baseZ = segStartZ + 8f;

        switch (pattern)
        {
            case 0: // Single obstacle — easy
            {
                int lane = Random.Range(-1, 2);
                int type = Random.Range(0, 12);
                GameObject obs = CreateObstacle(type);
                obs.transform.position = new Vector3(lane * laneWidth, 0f, baseZ);
                activeObstacles.Add(obs);
                break;
            }
            case 1: // Two-lane block — forces player to specific lane
            {
                int safeLane = Random.Range(-1, 2);
                int type = Random.Range(0, 5);
                for (int lane = -1; lane <= 1; lane++)
                {
                    if (lane == safeLane) continue;
                    GameObject obs = CreateObstacle(type);
                    obs.transform.position = new Vector3(lane * laneWidth, 0f, baseZ);
                    activeObstacles.Add(obs);
                }
                break;
            }
            case 2: // Staggered — obstacles in sequence forcing lane switches
            {
                int lane1 = Random.Range(-1, 2);
                int lane2 = lane1;
                while (lane2 == lane1) lane2 = Random.Range(-1, 2);
                GameObject obs1 = CreateObstacle(Random.Range(0, 8));
                obs1.transform.position = new Vector3(lane1 * laneWidth, 0f, baseZ);
                activeObstacles.Add(obs1);
                GameObject obs2 = CreateObstacle(Random.Range(0, 8));
                obs2.transform.position = new Vector3(lane2 * laneWidth, 0f, baseZ + 12f);
                activeObstacles.Add(obs2);
                break;
            }
            case 3: // Train in one lane + barrier in another
            {
                int trainLane = Random.Range(-1, 2);
                GameObject train = CreateObstacle(5); // Train
                train.transform.position = new Vector3(trainLane * laneWidth, 0f, baseZ);
                activeObstacles.Add(train);
                int barrierLane = trainLane;
                while (barrierLane == trainLane) barrierLane = Random.Range(-1, 2);
                GameObject barrier = CreateObstacle(Random.Range(0, 4));
                barrier.transform.position = new Vector3(barrierLane * laneWidth, 0f, baseZ + 6f);
                activeObstacles.Add(barrier);
                break;
            }
            case 4: // Jump-or-slide choice
            {
                int lane = Random.Range(-1, 2);
                // Low barrier (slideable) + overhead bar nearby
                GameObject low = CreateObstacle(0);
                low.transform.position = new Vector3(lane * laneWidth, 0f, baseZ);
                activeObstacles.Add(low);
                int otherLane = lane;
                while (otherLane == lane) otherLane = Random.Range(-1, 2);
                GameObject over = CreateObstacle(2);
                over.transform.position = new Vector3(otherLane * laneWidth, 0f, baseZ);
                activeObstacles.Add(over);
                break;
            }
            default: // Classic random 1-3 obstacles
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
                break;
            }
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

    // Phase 4: Curved-world effect — bends distant segments downward like Subway Surfers horizon
    private void ApplyCurvedWorld(GameObject segment)
    {
        // Now handled by UpdateCurvedWorld() per-frame, but still apply initial curve at spawn
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

        // Phase 4: Also curve obstacles and coins for consistent visual
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            if (activeObstacles[i] == null) continue;
            Vector3 pos = activeObstacles[i].transform.position;
            float distZ = pos.z;
            float baseY = pos.y;
            // Only adjust Y for far obstacles, keeping their height offset
            if (distZ > 20f)
            {
                float drop = curvedWorldIntensity * (distZ - 20f) * (distZ - 20f);
                // Find the nearest segment's expected Y and offset accordingly
                float segY = -drop;
                // We store obstacles at ground-relative heights, so just apply the curve
                pos.y = baseY - drop * 0.3f; // partial curve for obstacles (less aggressive)
                activeObstacles[i].transform.position = pos;
            }
        }

        // Phase 4: Curve coins too
        for (int i = 0; i < activeCoins.Count; i++)
        {
            if (activeCoins[i] == null) continue;
            Vector3 pos = activeCoins[i].transform.position;
            float distZ = pos.z;
            if (distZ > 20f)
            {
                float drop = curvedWorldIntensity * (distZ - 20f) * (distZ - 20f);
                pos.y -= drop * 0.3f * Time.deltaTime * speed * 0.1f; // gradual curve
                activeCoins[i].transform.position = pos;
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
