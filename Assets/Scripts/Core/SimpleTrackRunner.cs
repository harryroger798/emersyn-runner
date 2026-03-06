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

    // Phase 15E: Environment detail materials
    private Material overpassMat;
    private Material brickWallMat;
    private Material chainlinkMat;
    private Material manholeHDMat;
    private Material drainGrateMat;
    private Material puddleMat;

    // Phase 15F: Visual gap fix materials
    private Material subwayTrainMat;
    private Material taxiMat;
    private Material warningBarrierMat;
    private Material trainTrackMat;
    private Material gravelMat;
    private Material cityBusMat;
    private Material constructionBarrierMat;
    private Material laneDividerMat;
    private Material coinShineMat;

    // Phase 15G: Visual polish materials
    private Material crosswalkNewMat;
    private Material fenceRailingMat;
    private Material coinEmbossedMat;
    private Material rooftopDetailMat;
    private Material manholeDetailedMat;
    private Material grassDetailedMat;
    private Material buildingApartmentMat;

    // Phase 14G: Curved world DISABLED — was causing orange streak artifacts
    // The per-frame vertex manipulation stretched textures at perspective angles
    private float curvedWorldIntensity = 0f;
    private float curvedWorldStartZ = 9999f; // effectively disabled

    private Shader litShader;
    private int segmentsSpawned = 0;
    private Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
    private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();

    // Phase 16: Load Blender FBX model from Resources/Models with caching
    private GameObject LoadModel(string modelName)
    {
        if (modelCache.ContainsKey(modelName))
        {
            GameObject cached = modelCache[modelName];
            if (cached != null) return Instantiate(cached);
        }
        GameObject prefab = Resources.Load<GameObject>("Models/" + modelName);
        if (prefab != null)
        {
            modelCache[modelName] = prefab;
            return Instantiate(prefab);
        }
        return null;
    }

    // Phase 16: Apply material to all renderers in a model
    private void ApplyModelMaterial(GameObject model, Material mat)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.material = mat;
    }

    // Phase 16: Strip all colliders from a loaded model
    private void StripColliders(GameObject model)
    {
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);
    }

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

        // Phase 15G: Use asphalt grain road (visible texture detail), fallback chain
        roadMat = CreateTexturedMaterialTiled("tex_road_asphalt_grain", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadMat.mainTexture == null) roadMat = CreateTexturedMaterialTiled("tex_road_smooth_dark", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadMat.mainTexture == null) roadMat = CreateTexturedMaterialTiled("tex_road_subway_style", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadMat.mainTexture == null) roadMat = CreateTexturedMaterialTiled("tex_road_clean_asphalt", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        if (roadMat.mainTexture == null) roadMat = CreateTexturedMaterialTiled("tex_road_plain_grey", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);
        // Phase 15G: Use clear brick sidewalk texture, fallback chain
        sidewalkMat = CreateTexturedMaterial("tex_sidewalk_brick_clear", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_paver", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_subway_style", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_clean_grey", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_hd", new Color(0.6f, 0.6f, 0.55f));
        if (sidewalkMat.mainTexture == null) sidewalkMat = CreateTexturedMaterial("tex_sidewalk_stone", new Color(0.6f, 0.6f, 0.55f));
        // Phase 14: Use dark green grass texture, fallback to clean asphalt color
        grassMat = CreateTexturedMaterialTiled("tex_grass_dark_green", new Color(0.15f, 0.3f, 0.15f), 4f, 4f);
        if (grassMat.mainTexture == null) grassMat = CreateTexturedMaterialTiled("tex_ground_dark_fill", new Color(0.25f, 0.25f, 0.28f), 4f, 4f);
        if (grassMat.mainTexture == null) grassMat = CreateColorMaterial(new Color(0.2f, 0.3f, 0.2f));
        // Phase 15C: UV-safe gradient textures for obstacles (PIL-generated, clean on primitives)
        barrierMat = CreateTexturedMaterial("tex_obstacle_barrier_clean", new Color(0.95f, 0.55f, 0.1f)); // Phase 16D: orange-yellow instead of red
        trainMat = CreateTexturedMaterial("tex_obstacle_train_clean", new Color(0.3f, 0.35f, 0.7f));
        coneMat = CreateTexturedMaterial("tex_obstacle_cone_clean", new Color(1f, 0.5f, 0f));
        fenceMat = CreateColorMaterial(new Color(0.55f, 0.55f, 0.55f));
        lampMat = CreateColorMaterial(new Color(0.35f, 0.35f, 0.38f));
        graffitiMat = CreateTexturedMaterial("tex_graffiti_wall", new Color(0.6f, 0.5f, 0.5f));

        // Phase 15C: UV-safe gold coin texture (PIL gradient)
        coinMat = CreateTexturedMaterial("tex_coin_clean_gold", new Color(1f, 0.85f, 0.1f));
        if (coinMat.HasProperty("_Metallic")) coinMat.SetFloat("_Metallic", 0.8f);
        if (coinMat.HasProperty("_Smoothness")) coinMat.SetFloat("_Smoothness", 0.9f);

        // Phase 16B: Use solid colors for building base materials (SDXL textures look noisy on 3D cubes)
        buildingMats = new Material[]
        {
            CreateColorMaterial(new Color(0.7f, 0.4f, 0.35f)),   // red brick
            CreateColorMaterial(new Color(0.35f, 0.45f, 0.7f)),  // blue
            CreateColorMaterial(new Color(0.7f, 0.65f, 0.35f)),  // yellow
            CreateColorMaterial(new Color(0.55f, 0.55f, 0.55f)), // grey concrete
            CreateColorMaterial(new Color(0.7f, 0.45f, 0.55f)),  // pink
            CreateColorMaterial(new Color(0.4f, 0.55f, 0.7f)),   // modern glass blue
            CreateColorMaterial(new Color(0.5f, 0.35f, 0.25f)),  // brownstone
            CreateColorMaterial(new Color(0.6f, 0.5f, 0.4f)),    // shop front
            CreateColorMaterial(new Color(0.3f, 0.2f, 0.5f)),    // neon purple
            CreateColorMaterial(new Color(0.5f, 0.4f, 0.45f))    // graffiti wall
        };

        cobbleMat = CreateTexturedMaterial("tex_ground_cobblestone", new Color(0.45f, 0.45f, 0.4f));
        crosswalkMat = CreateTexturedMaterial("tex_ground_crosswalk", new Color(0.9f, 0.9f, 0.9f));
        manholeMat = CreateTexturedMaterial("tex_ground_manhole", new Color(0.35f, 0.35f, 0.35f));

        // Phase 15C: UV-safe gradient textures for props (PIL-generated)
        propMats = new Material[]
        {
            CreateTexturedMaterial("tex_prop_trashcan_clean", new Color(0.2f, 0.5f, 0.2f)),
            CreateTexturedMaterial("tex_prop_bench_clean", new Color(0.45f, 0.3f, 0.15f)),
            CreateTexturedMaterial("tex_prop_mailbox_clean", new Color(0.2f, 0.3f, 0.7f)),
            CreateTexturedMaterial("tex_prop_hydrant_clean", new Color(0.8f, 0.15f, 0.1f)),
            CreateColorMaterial(new Color(0.7f, 0.65f, 0.1f)),   // newspaper - yellow
            CreateTexturedMaterial("tex_prop_bollard_clean", new Color(0.6f, 0.6f, 0.6f)),
            CreateColorMaterial(new Color(0.4f, 0.55f, 0.3f)),   // planter - green
            CreateColorMaterial(new Color(0.3f, 0.3f, 0.35f)),   // streetlight - dark grey
            CreateTexturedMaterial("tex_prop_vending_machine_hd", new Color(0.3f, 0.4f, 0.7f)),    // vending machine - HD texture
            CreateColorMaterial(new Color(0.8f, 0.2f, 0.15f)),   // phone booth - red
            CreateColorMaterial(new Color(0.4f, 0.4f, 0.4f)),    // fire escape - grey
            CreateColorMaterial(new Color(0.8f, 0.3f, 0.2f)),    // awning - red-orange
            CreateColorMaterial(new Color(0.3f, 0.55f, 0.25f)),  // potted plant - green
            CreateTexturedMaterial("tex_prop_food_cart_hd", new Color(0.8f, 0.5f, 0.2f)),    // food cart - HD texture
            CreateColorMaterial(new Color(0.5f, 0.6f, 0.7f)),    // bus stop - steel blue
            CreateColorMaterial(new Color(0.3f, 0.3f, 0.3f))     // traffic light - dark grey
        };

        // Phase 15C: UV-safe gradient textures for obstacles
        dumpsterMat = CreateTexturedMaterial("tex_obstacle_dumpster_clean", new Color(0.2f, 0.45f, 0.2f));
        constructionMat = CreateTexturedMaterial("tex_obstacle_construction_clean", new Color(0.9f, 0.5f, 0.1f));
        carMat = CreateTexturedMaterial("tex_obstacle_car_clean", new Color(0.95f, 0.85f, 0.1f));
        busMat = CreateTexturedMaterial("tex_obstacle_bus_clean", new Color(0.7f, 0.2f, 0.15f));

        // Phase 15C: UV-safe gradient textures for train variants
        trainVariantMats = new Material[]
        {
            trainMat,
            CreateTexturedMaterial("tex_train_purple_clean", new Color(0.4f, 0.3f, 0.5f)),
            CreateTexturedMaterial("tex_train_silver_clean", new Color(0.75f, 0.75f, 0.8f)),
            CreateTexturedMaterial("tex_train_subway_blue_clean", new Color(0.3f, 0.4f, 0.7f)),
            CreateTexturedMaterial("tex_train_subway_red_clean", new Color(0.7f, 0.25f, 0.2f))
        };

        // Phase 15G: Use asphalt grain HD road texture, fallback chain
        roadHDMat = CreateTexturedMaterialTiled("tex_road_asphalt_grain", new Color(0.2f, 0.2f, 0.25f), 2f, 8f);
        if (roadHDMat.mainTexture == null) roadHDMat = CreateTexturedMaterialTiled("tex_road_smooth_dark", new Color(0.2f, 0.2f, 0.25f), 2f, 8f);
        if (roadHDMat.mainTexture == null) roadHDMat = CreateTexturedMaterialTiled("tex_road_subway_style", new Color(0.2f, 0.2f, 0.25f), 2f, 8f);
        if (roadHDMat.mainTexture == null) roadHDMat = CreateTexturedMaterialTiled("tex_road_clean_asphalt", new Color(0.2f, 0.2f, 0.25f), 2f, 8f);
        if (roadHDMat.mainTexture == null) roadHDMat = CreateTexturedMaterialTiled("tex_road_asphalt_hd", new Color(0.25f, 0.25f, 0.3f), 2f, 8f);

        // Phase 14: Use dark grey curb texture
        curbMat = CreateTexturedMaterial("tex_curb_dark_grey", new Color(0.3f, 0.3f, 0.32f));
        if (curbMat.mainTexture == null) curbMat = CreateTexturedMaterial("tex_road_curb", new Color(0.3f, 0.3f, 0.32f));
        crosswalkHDMat = CreateTexturedMaterial("tex_road_crosswalk_hd", new Color(0.9f, 0.9f, 0.9f));

        // Phase 15H: Clean PIL building textures (UV-safe for cube primitives, no SDXL distortion)
        // Each building texture uses procedural gradient + window grid pattern
        hiResBuildingMats = new Material[]
        {
            CreateTexturedMaterial("tex_building_highrise_clean_1", new Color(0.5f, 0.7f, 0.9f)),
            CreateTexturedMaterial("tex_building_highrise_clean_2", new Color(0.8f, 0.7f, 0.6f)),
            CreateTexturedMaterial("tex_building_industrial_clean", new Color(0.4f, 0.4f, 0.4f)),
            CreateTexturedMaterial("tex_building_restaurant_clean", new Color(0.9f, 0.4f, 0.2f)),
            CreateTexturedMaterial("tex_building_arcade_clean", new Color(0.9f, 0.8f, 0.2f)),
            CreateTexturedMaterial("tex_building_hospital_clean", new Color(0.9f, 0.9f, 0.95f)),
            CreateTexturedMaterial("tex_building_school_clean", new Color(0.7f, 0.5f, 0.3f)),
            CreateTexturedMaterial("tex_building_cinema_clean", new Color(0.6f, 0.2f, 0.3f)),
            CreateTexturedMaterial("tex_building_apartment_v2", new Color(0.6f, 0.45f, 0.35f)),
            CreateTexturedMaterial("tex_building_office_clean", new Color(0.4f, 0.55f, 0.75f)),
            CreateTexturedMaterial("tex_building_warehouse_clean", new Color(0.45f, 0.45f, 0.45f)),
            CreateTexturedMaterial("tex_building_diner_clean", new Color(0.8f, 0.3f, 0.25f)),
            CreateTexturedMaterial("tex_building_bookstore_clean", new Color(0.55f, 0.4f, 0.3f)),
            CreateTexturedMaterial("tex_building_skyscraper_clean", new Color(0.4f, 0.6f, 0.8f)),
            CreateTexturedMaterial("tex_building_brick_clean", new Color(0.6f, 0.4f, 0.3f)),
            CreateTexturedMaterial("tex_building_hotel_clean", new Color(0.7f, 0.6f, 0.5f)),
            CreateTexturedMaterial("tex_building_apartment_clean", new Color(0.7f, 0.68f, 0.65f))
        };

        // Phase 15D: Use new Modal SDXL billboard textures (flat surfaces), fallback to Phase 3
        billboardMat1 = CreateTexturedMaterial("tex_billboard_sneakers", new Color(0.6f, 0.5f, 0.9f));
        if (billboardMat1.mainTexture == null) billboardMat1 = CreateTexturedMaterial("tex_env_billboard_1", new Color(0.6f, 0.5f, 0.9f));
        billboardMat2 = CreateTexturedMaterial("tex_billboard_energy_drink", new Color(0.9f, 0.5f, 0.3f));
        if (billboardMat2.mainTexture == null) billboardMat2 = CreateTexturedMaterial("tex_env_billboard_2", new Color(0.9f, 0.5f, 0.3f));
        // Phase 15G: Use rooftop detail texture, fallback to solid color
        rooftopMat = CreateTexturedMaterial("tex_rooftop_detail", new Color(0.45f, 0.45f, 0.48f));
        if (rooftopMat.mainTexture == null) rooftopMat = CreateColorMaterial(new Color(0.45f, 0.45f, 0.48f));
        // Phase 15I: Use clean PIL tunnel wall texture (UV-safe for cubes), fallback chain
        tunnelMat = CreateTexturedMaterial("tex_tunnel_wall_clean", new Color(0.25f, 0.25f, 0.28f));
        if (tunnelMat.mainTexture == null) tunnelMat = CreateTexturedMaterial("tex_tunnel_wall_subway", new Color(0.25f, 0.25f, 0.28f));
        if (tunnelMat.mainTexture == null) tunnelMat = CreateTexturedMaterial("tex_tunnel_interior_clean", new Color(0.25f, 0.25f, 0.28f));

        // Phase 15I: Clean PIL environment materials (UV-safe for cubes)
        overpassMat = CreateTexturedMaterial("tex_overpass_clean", new Color(0.5f, 0.5f, 0.52f));
        if (overpassMat.mainTexture == null) overpassMat = CreateTexturedMaterial("tex_overpass_concrete", new Color(0.5f, 0.5f, 0.52f));
        brickWallMat = CreateTexturedMaterial("tex_brick_wall_clean", new Color(0.6f, 0.35f, 0.25f));
        if (brickWallMat.mainTexture == null) brickWallMat = CreateTexturedMaterial("tex_wall_brick_detail", new Color(0.6f, 0.35f, 0.25f));
        chainlinkMat = CreateTexturedMaterial("tex_fence_chainlink", new Color(0.6f, 0.6f, 0.6f));

        // Phase 15E: Ground detail materials
        manholeHDMat = CreateTexturedMaterial("tex_ground_manhole_hd", new Color(0.35f, 0.35f, 0.35f));
        drainGrateMat = CreateTexturedMaterial("tex_ground_drain_grate", new Color(0.4f, 0.4f, 0.4f));
        puddleMat = CreateTexturedMaterial("tex_ground_puddle", new Color(0.5f, 0.6f, 0.7f));

        // Phase 15F: Visual gap fix materials
        subwayTrainMat = CreateTexturedMaterial("tex_obstacle_subway_train", new Color(0.3f, 0.4f, 0.7f));
        taxiMat = CreateTexturedMaterial("tex_obstacle_taxi", new Color(0.95f, 0.8f, 0.2f));
        warningBarrierMat = CreateTexturedMaterial("tex_obstacle_warning_barrier", new Color(0.9f, 0.7f, 0.1f));
        trainTrackMat = CreateTexturedMaterialTiled("tex_ground_train_tracks", new Color(0.4f, 0.35f, 0.3f), 1f, 4f);
        gravelMat = CreateTexturedMaterialTiled("tex_ground_gravel", new Color(0.45f, 0.4f, 0.35f), 2f, 4f);
        cityBusMat = CreateTexturedMaterial("tex_obstacle_city_bus", new Color(0.7f, 0.15f, 0.15f));
        constructionBarrierMat = CreateTexturedMaterial("tex_obstacle_construction_barrier", new Color(0.9f, 0.5f, 0.1f));
        laneDividerMat = CreateTexturedMaterial("tex_road_lane_divider", new Color(0.9f, 0.9f, 0.9f));
        coinShineMat = CreateTexturedMaterial("tex_coin_golden_shine", new Color(1f, 0.85f, 0.1f));
        if (coinShineMat.mainTexture != null)
        {
            if (coinShineMat.HasProperty("_Metallic")) coinShineMat.SetFloat("_Metallic", 0.9f);
            if (coinShineMat.HasProperty("_Smoothness")) coinShineMat.SetFloat("_Smoothness", 0.95f);
        }

        // Phase 15G: Visual polish materials
        crosswalkNewMat = CreateTexturedMaterial("tex_road_crosswalk", new Color(0.9f, 0.9f, 0.9f));
        fenceRailingMat = CreateTexturedMaterial("tex_fence_railing", new Color(0.55f, 0.55f, 0.58f));
        coinEmbossedMat = CreateTexturedMaterial("tex_coin_embossed_gold", new Color(1f, 0.85f, 0.1f));
        if (coinEmbossedMat.mainTexture != null)
        {
            if (coinEmbossedMat.HasProperty("_Metallic")) coinEmbossedMat.SetFloat("_Metallic", 0.95f);
            if (coinEmbossedMat.HasProperty("_Smoothness")) coinEmbossedMat.SetFloat("_Smoothness", 0.98f);
        }
        rooftopDetailMat = CreateTexturedMaterial("tex_rooftop_detail", new Color(0.45f, 0.45f, 0.48f));
        manholeDetailedMat = CreateTexturedMaterial("tex_manhole_detailed", new Color(0.35f, 0.35f, 0.35f));
        grassDetailedMat = CreateTexturedMaterialTiled("tex_grass_detailed", new Color(0.3f, 0.55f, 0.2f), 2f, 4f);
        buildingApartmentMat = CreateTexturedMaterial("tex_building_apartment_clean", new Color(0.7f, 0.68f, 0.65f));
    }

    public void StartTrack()
    {
        ClearAll();
        isRunning = true;
        // Phase 14M: Spawn the first segment slightly behind player (but above despawnZ) so camera never sees "void" behind Z=0
        nextSpawnZ = -segmentLength * 0.5f;
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

        // Phase 14G: Curved world disabled — caused orange streak artifacts
        // UpdateCurvedWorld();
    }

    private void SpawnSegment(bool safe)
    {
        GameObject segment = new GameObject("Segment_" + segmentsSpawned);
        segment.transform.position = new Vector3(0f, 0f, nextSpawnZ);

        // Phase 14E: Extra-wide road (20 units) to fully cover screen bottom and prevent building bleed
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(segment.transform);
        road.transform.localPosition = new Vector3(0f, -0.55f, segmentLength / 2f);
        road.transform.localScale = new Vector3(20f, 1f, segmentLength + 0.5f);
        road.GetComponent<Renderer>().material = roadHDMat != null ? roadHDMat : roadMat;
        Destroy(road.GetComponent<Collider>());

        // Sidewalks
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk";
            sw.transform.SetParent(segment.transform);
            // Phase 14E: Sidewalks pushed further out (road is now 20 wide)
            sw.transform.localPosition = new Vector3(side * 12.5f, -0.3f, segmentLength / 2f);
            sw.transform.localScale = new Vector3(5f, 0.6f, segmentLength);
            // Phase 15J: Use warm concrete sidewalk texture (UV-safe PIL), fallback chain
            Material swEdgeMat = CreateTexturedMaterial("tex_sidewalk_warm", new Color(0.55f, 0.53f, 0.50f));
            if (swEdgeMat.mainTexture == null) swEdgeMat = CreateTexturedMaterial("tex_sidewalk_wide_clean", new Color(0.55f, 0.53f, 0.50f));
            if (swEdgeMat.mainTexture == null) swEdgeMat = CreateTexturedMaterial("tex_sidewalk_light_concrete", new Color(0.55f, 0.53f, 0.50f));
            if (swEdgeMat.mainTexture == null) swEdgeMat = sidewalkMat;
            sw.GetComponent<Renderer>().material = swEdgeMat;
            Destroy(sw.GetComponent<Collider>());

            // Phase 14E: Curb pushed out to match wider road
            if (curbMat != null)
            {
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = "Curb";
                curb.transform.SetParent(segment.transform);
                curb.transform.localPosition = new Vector3(side * 10.1f, -0.05f, segmentLength / 2f);
                curb.transform.localScale = new Vector3(0.3f, 0.15f, segmentLength);
                curb.GetComponent<Renderer>().material = curbMat;
                Destroy(curb.GetComponent<Collider>());
            }

            // Phase 16B: Removed dark wall panels — they created ugly black bars on screen edges
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

        // Phase 14D: Crosswalks removed — their colored textures caused orange streak artifacts
        // when rendered at steep perspective angles near the camera

        // Phase 14D: Manholes removed — caused colored streaks at perspective angles

        // Phase 14D: Puddles removed — caused colored streaks at perspective angles

        // Phase 14D: Grates removed — caused colored streaks at perspective angles

        // Phase 14D: Road arrows removed — caused colored streaks at perspective angles

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
                // Phase 14E: Buildings pushed much further from road to prevent color bleeding
                building.transform.localPosition = new Vector3(
                    side * (13f + Random.Range(0f, 2f)),
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

                // Phase 15J: Awning on ground floor shops with stripe texture overlay
                if (height < 10f && Random.value < 0.35f)
                {
                    GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    awning.name = "Awning";
                    awning.transform.SetParent(building.transform);
                    awning.transform.localPosition = new Vector3(-side * 0.55f, -0.35f, 0f);
                    awning.transform.localScale = new Vector3(0.3f, 0.02f, 0.4f);
                    Material awningStripeMat = CreateTexturedMaterial("tex_awning_stripe", new Color(0.8f, 0.3f, 0.2f));
                    if (awningStripeMat.mainTexture != null)
                        awning.GetComponent<Renderer>().material = awningStripeMat;
                    else
                    {
                        Color awningColor = new Color(Random.Range(0.5f, 1f), Random.Range(0.2f, 0.6f), Random.Range(0.1f, 0.4f));
                        awning.GetComponent<Renderer>().material = CreateColorMaterial(awningColor);
                    }
                    Destroy(awning.GetComponent<Collider>());
                }

                // Phase 15J: Neon sign overlay on building front (flat quad)
                if (height > 8f && Random.value < 0.2f)
                {
                    Material neonMat = CreateTexturedMaterial("tex_neon_sign", new Color(0.8f, 0.3f, 0.7f));
                    if (neonMat.mainTexture != null)
                    {
                        GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        neon.name = "NeonSign";
                        neon.transform.SetParent(building.transform);
                        neon.transform.localPosition = new Vector3(-side * 0.51f, 0.1f, 0f);
                        neon.transform.localScale = new Vector3(0.25f, 0.1f, 1f);
                        neon.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
                        neon.GetComponent<Renderer>().material = neonMat;
                        Destroy(neon.GetComponent<Collider>());
                    }
                }

                // Phase 15J: Graffiti overlay on building side (flat quad)
                if (height > 6f && height < 14f && Random.value < 0.15f)
                {
                    Material graffitiMat = CreateTexturedMaterial("tex_graffiti_overlay", new Color(0.7f, 0.3f, 0.3f));
                    if (graffitiMat.mainTexture != null)
                    {
                        GameObject graffiti = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        graffiti.name = "Graffiti";
                        graffiti.transform.SetParent(building.transform);
                        graffiti.transform.localPosition = new Vector3(-side * 0.51f, -0.15f, 0.1f);
                        graffiti.transform.localScale = new Vector3(0.35f, 0.15f, 1f);
                        graffiti.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
                        graffiti.GetComponent<Renderer>().material = graffitiMat;
                        Destroy(graffiti.GetComponent<Collider>());
                    }
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
                lhead.transform.localScale = new Vector3(1.5f, 0.6f, 1.5f);
                lhead.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.95f, 0.7f));
                Destroy(lhead.GetComponent<Collider>());
            }

            // Phase 15E: Fences with chainlink texture quad overlay
            if (Random.value < 0.25f)
            {
                GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fence.name = "Fence";
                fence.transform.SetParent(segment.transform);
                fence.transform.localPosition = new Vector3(side * 4.5f, 0.5f, segmentLength / 2f);
                fence.transform.localScale = new Vector3(0.1f, 1f, segmentLength * 0.8f);
                fence.GetComponent<Renderer>().material = fenceMat;
                Destroy(fence.GetComponent<Collider>());
                // Phase 15E: Chainlink texture overlay (flat quad)
                if (chainlinkMat != null && chainlinkMat.mainTexture != null)
                {
                    GameObject chainQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    chainQuad.name = "ChainlinkOverlay";
                    chainQuad.transform.SetParent(fence.transform);
                    chainQuad.transform.localPosition = new Vector3(-side * 0.6f, 0f, 0f);
                    chainQuad.transform.localScale = new Vector3(12f, 1f, 1f);
                    chainQuad.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    chainQuad.GetComponent<Renderer>().material = chainlinkMat;
                    Destroy(chainQuad.GetComponent<Collider>());
                }
            }

            // Phase 16D: Moved all small props further out to sidewalk edge (side * 12f+) to avoid visual clutter on road
            // Trash cans — pushed to sidewalk
            if (Random.value < 0.3f)
            {
                GameObject trashcan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trashcan.name = "TrashCan";
                trashcan.transform.SetParent(segment.transform);
                float tcZ = Random.Range(3f, segmentLength - 3f);
                trashcan.transform.localPosition = new Vector3(side * 12f, 0.4f, tcZ);
                trashcan.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                trashcan.GetComponent<Renderer>().material = propMats[0];
                Destroy(trashcan.GetComponent<Collider>());
            }

            // Benches — on sidewalk
            if (Random.value < 0.2f)
            {
                GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "Bench";
                bench.transform.SetParent(segment.transform);
                float bZ = Random.Range(5f, segmentLength - 5f);
                bench.transform.localPosition = new Vector3(side * 11.5f, 0.3f, bZ);
                bench.transform.localScale = new Vector3(0.5f, 0.4f, 1.4f);
                bench.GetComponent<Renderer>().material = propMats[1];
                Destroy(bench.GetComponent<Collider>());
            }
        }

        // Grass beyond sidewalks
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "Grass";
            grass.transform.SetParent(segment.transform);
            grass.transform.localPosition = new Vector3(side * 18f, -1.0f, segmentLength / 2f);
            grass.transform.localScale = new Vector3(18f, 0.5f, segmentLength);
            grass.GetComponent<Renderer>().material = grassMat;
            Destroy(grass.GetComponent<Collider>());
        }

        // Phase 16D: Removed inner curb detail — was creating visible thin lines on road edge

        // Phase 16B: Overpasses removed — plain grey slabs looked too blocky and unpolished

        // Phase 15E: Brick wall sections on building sides (flat quad for SDXL texture)
        if (Random.value < 0.15f && brickWallMat != null && brickWallMat.mainTexture != null)
        {
            int brickSide = Random.value < 0.5f ? -1 : 1;
            GameObject brickWall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            brickWall.name = "BrickWall";
            brickWall.transform.SetParent(segment.transform);
            brickWall.transform.localPosition = new Vector3(brickSide * 12f, 4f, segmentLength / 2f);
            brickWall.transform.localScale = new Vector3(8f, 8f, 1f);
            brickWall.transform.localRotation = Quaternion.Euler(0f, brickSide > 0 ? -90f : 90f, 0f);
            brickWall.GetComponent<Renderer>().material = brickWallMat;
            Destroy(brickWall.GetComponent<Collider>());
        }

        // Phase 15E: Ground details (manholes, drain grates, puddles) — flat quads on road
        if (Random.value < 0.2f && manholeHDMat != null && manholeHDMat.mainTexture != null)
        {
            GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Quad);
            manhole.name = "ManholeHD";
            manhole.transform.SetParent(segment.transform);
            int mhLane = Random.Range(-1, 2);
            manhole.transform.localPosition = new Vector3(mhLane * laneWidth, 0.06f, Random.Range(5f, segmentLength - 5f));
            manhole.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            manhole.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            manhole.GetComponent<Renderer>().material = manholeHDMat;
            Destroy(manhole.GetComponent<Collider>());
        }
        if (Random.value < 0.15f && drainGrateMat != null && drainGrateMat.mainTexture != null)
        {
            GameObject drain = GameObject.CreatePrimitive(PrimitiveType.Quad);
            drain.name = "DrainGrate";
            drain.transform.SetParent(segment.transform);
            int drSide = Random.value < 0.5f ? -1 : 1;
            drain.transform.localPosition = new Vector3(drSide * 3.5f, 0.06f, Random.Range(5f, segmentLength - 5f));
            drain.transform.localScale = new Vector3(0.8f, 1.5f, 1f);
            drain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            drain.GetComponent<Renderer>().material = drainGrateMat;
            Destroy(drain.GetComponent<Collider>());
        }
        if (Random.value < 0.1f && puddleMat != null && puddleMat.mainTexture != null)
        {
            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            puddle.name = "Puddle";
            puddle.transform.SetParent(segment.transform);
            int pdLane = Random.Range(-1, 2);
            puddle.transform.localPosition = new Vector3(pdLane * laneWidth, 0.07f, Random.Range(5f, segmentLength - 5f));
            puddle.transform.localScale = new Vector3(1.5f, 2f, 1f);
            puddle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            puddle.GetComponent<Renderer>().material = puddleMat;
            Destroy(puddle.GetComponent<Collider>());
        }

        // Phase 15F: Train track sections alongside road (flat quads)
        if (segmentsSpawned > 1 && Random.value < 0.15f && trainTrackMat != null && trainTrackMat.mainTexture != null)
        {
            int trackSide = Random.value < 0.5f ? -1 : 1;
            // Gravel bed
            if (gravelMat != null && gravelMat.mainTexture != null)
            {
                GameObject gravelBed = GameObject.CreatePrimitive(PrimitiveType.Quad);
                gravelBed.name = "GravelBed";
                gravelBed.transform.SetParent(segment.transform);
                gravelBed.transform.localPosition = new Vector3(trackSide * 8f, 0.04f, segmentLength / 2f);
                gravelBed.transform.localScale = new Vector3(4f, segmentLength, 1f);
                gravelBed.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                gravelBed.GetComponent<Renderer>().material = gravelMat;
                Destroy(gravelBed.GetComponent<Collider>());
            }
            // Track rails overlay
            GameObject trackQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            trackQuad.name = "TrainTracks";
            trackQuad.transform.SetParent(segment.transform);
            trackQuad.transform.localPosition = new Vector3(trackSide * 8f, 0.05f, segmentLength / 2f);
            trackQuad.transform.localScale = new Vector3(3f, segmentLength, 1f);
            trackQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            trackQuad.GetComponent<Renderer>().material = trainTrackMat;
            Destroy(trackQuad.GetComponent<Collider>());
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

        // Phase 14G: Curved world disabled — caused orange streak artifacts
        // ApplyCurvedWorld(segment);

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
            case 0: // Low barrier — Phase 16B enhanced primitive
            {
                obs = new GameObject("Barrier");
                // Main barrier body
                GameObject barBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barBody.transform.SetParent(obs.transform);
                barBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                barBody.transform.localScale = new Vector3(2f, 0.8f, 0.4f);
                barBody.GetComponent<Renderer>().material = barrierMat;
                Destroy(barBody.GetComponent<Collider>());
                // Stripe detail
                GameObject stripe1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe1.transform.SetParent(obs.transform);
                stripe1.transform.localPosition = new Vector3(0f, 0.6f, 0.21f);
                stripe1.transform.localScale = new Vector3(2.01f, 0.15f, 0.01f);
                stripe1.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.8f, 0f));
                Destroy(stripe1.GetComponent<Collider>());
                // Support legs
                for (int bl = -1; bl <= 1; bl += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leg.transform.SetParent(obs.transform);
                    leg.transform.localPosition = new Vector3(bl * 0.8f, 0.15f, 0f);
                    leg.transform.localScale = new Vector3(0.08f, 0.3f, 0.3f);
                    leg.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.4f, 0.4f, 0.4f));
                    Destroy(leg.GetComponent<Collider>());
                }
                break;
            }

            case 1: // Tall barrier — Phase 16B enhanced
            {
                obs = new GameObject("TallBarrier");
                // Main body
                GameObject tbBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tbBody.transform.SetParent(obs.transform);
                tbBody.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                tbBody.transform.localScale = new Vector3(1.5f, 2.5f, 0.5f);
                tbBody.GetComponent<Renderer>().material = barrierMat;
                Destroy(tbBody.GetComponent<Collider>());
                // Yellow caution stripe
                GameObject tbStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tbStripe.transform.SetParent(obs.transform);
                tbStripe.transform.localPosition = new Vector3(0f, 2.3f, 0.26f);
                tbStripe.transform.localScale = new Vector3(1.51f, 0.2f, 0.01f);
                tbStripe.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.8f, 0f));
                Destroy(tbStripe.GetComponent<Collider>());
                break;
            }

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

            case 3: // Cone — Phase 16B enhanced
            {
                obs = new GameObject("Cone");
                // Cone body (tapered cylinder)
                GameObject coneBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coneBody.transform.SetParent(obs.transform);
                coneBody.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                coneBody.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                coneBody.GetComponent<Renderer>().material = coneMat;
                Destroy(coneBody.GetComponent<Collider>());
                // Orange tip
                GameObject coneTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coneTip.transform.SetParent(obs.transform);
                coneTip.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                coneTip.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
                coneTip.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.5f, 0f));
                Destroy(coneTip.GetComponent<Collider>());
                // White stripe
                GameObject coneStripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coneStripe.transform.SetParent(obs.transform);
                coneStripe.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                coneStripe.transform.localScale = new Vector3(0.32f, 0.06f, 0.32f);
                coneStripe.GetComponent<Renderer>().material = CreateColorMaterial(Color.white);
                Destroy(coneStripe.GetComponent<Collider>());
                // Base
                GameObject coneBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                coneBase.transform.SetParent(obs.transform);
                coneBase.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                coneBase.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                coneBase.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.3f, 0.3f, 0.3f));
                Destroy(coneBase.GetComponent<Collider>());
                break;
            }

            case 4: // Wide barrier — Phase 16B enhanced
            {
                obs = new GameObject("WideBarrier");
                // Main body
                GameObject wbBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wbBody.transform.SetParent(obs.transform);
                wbBody.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                wbBody.transform.localScale = new Vector3(4f, 1.5f, 0.5f);
                wbBody.GetComponent<Renderer>().material = constructionMat;
                Destroy(wbBody.GetComponent<Collider>());
                // Yellow/black chevron stripes
                for (int wsi = 0; wsi < 6; wsi++)
                {
                    GameObject wStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wStripe.transform.SetParent(obs.transform);
                    wStripe.transform.localPosition = new Vector3(-1.5f + wsi * 0.6f, 0.75f, 0.26f);
                    wStripe.transform.localScale = new Vector3(0.15f, 1.2f, 0.01f);
                    wStripe.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
                    wStripe.GetComponent<Renderer>().material = CreateColorMaterial(wsi % 2 == 0 ? new Color(1f, 0.8f, 0f) : new Color(0.15f, 0.15f, 0.15f));
                    Destroy(wStripe.GetComponent<Collider>());
                }
                break;
            }

            case 5: // Train — Phase 16B enhanced with windows/doors
            {
                obs = new GameObject("Train");
                Material selectedTrainMat = trainVariantMats[Random.Range(0, trainVariantMats.Length)];
                int trainCars = Random.Range(3, 6);
                for (int c = 0; c < trainCars; c++)
                {
                    GameObject tcar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tcar.transform.SetParent(obs.transform);
                    tcar.transform.localPosition = new Vector3(0f, 1.2f, c * 2.2f);
                    tcar.transform.localScale = new Vector3(1.8f, 2.2f, 2f);
                    tcar.GetComponent<Renderer>().material = selectedTrainMat;
                    Destroy(tcar.GetComponent<Collider>());
                    // Door
                    GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    door.transform.SetParent(tcar.transform);
                    door.transform.localPosition = new Vector3(-0.51f, -0.1f, 0f);
                    door.transform.localScale = new Vector3(0.02f, 0.6f, 0.25f);
                    door.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.5f, 0.5f, 0.55f));
                    Destroy(door.GetComponent<Collider>());
                    // Windows on each side
                    for (int ws = -1; ws <= 1; ws += 2)
                    {
                        GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        win.transform.SetParent(tcar.transform);
                        win.transform.localPosition = new Vector3(ws * 0.51f, 0.1f, 0.25f);
                        win.transform.localScale = new Vector3(0.02f, 0.35f, 0.3f);
                        win.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.6f, 0.78f, 0.95f));
                        Destroy(win.GetComponent<Collider>());
                    }
                }
                // Roof
                GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.transform.SetParent(obs.transform);
                roof.transform.localPosition = new Vector3(0f, 2.4f, (trainCars - 1) * 1.1f);
                roof.transform.localScale = new Vector3(1.9f, 0.2f, trainCars * 2.2f + 0.3f);
                roof.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.3f, 0.3f, 0.35f));
                Destroy(roof.GetComponent<Collider>());
                // Wheels
                for (int wside = -1; wside <= 1; wside += 2)
                {
                    for (int wc = 0; wc < trainCars; wc++)
                    {
                        GameObject tw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        tw.transform.SetParent(obs.transform);
                        tw.transform.localPosition = new Vector3(wside * 0.85f, 0.15f, wc * 2.2f);
                        tw.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
                        tw.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                        tw.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.15f, 0.15f, 0.15f));
                        Destroy(tw.GetComponent<Collider>());
                    }
                }
                break;
            }

            case 6: // Warning zone — Phase 16B enhanced
            {
                obs = new GameObject("WarningZone");
                // Base plate
                GameObject wzBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wzBase.transform.SetParent(obs.transform);
                wzBase.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                wzBase.transform.localScale = new Vector3(2f, 0.1f, 2f);
                wzBase.GetComponent<Renderer>().material = coneMat;
                Destroy(wzBase.GetComponent<Collider>());
                // Corner cones
                for (int cx = -1; cx <= 1; cx += 2)
                {
                    for (int cz = -1; cz <= 1; cz += 2)
                    {
                        GameObject wcone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        wcone.transform.SetParent(obs.transform);
                        wcone.transform.localPosition = new Vector3(cx * 0.8f, 0.35f, cz * 0.8f);
                        wcone.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
                        wcone.GetComponent<Renderer>().material = coneMat;
                        Destroy(wcone.GetComponent<Collider>());
                    }
                }
                break;
            }

            case 7: // Staggered combo — Phase 16B enhanced
            {
                obs = new GameObject("Staggered");
                for (int s = 0; s < 2; s++)
                {
                    GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.transform.SetParent(obs.transform);
                    piece.transform.localPosition = new Vector3(s * 1.5f - 0.75f, 0.75f, s * 1.5f);
                    piece.transform.localScale = new Vector3(1f, 1.5f, 0.5f);
                    piece.GetComponent<Renderer>().material = constructionMat;
                    Destroy(piece.GetComponent<Collider>());
                    // Stripe on each piece
                    GameObject sStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sStripe.transform.SetParent(piece.transform);
                    sStripe.transform.localPosition = new Vector3(0f, 0.3f, 0.51f);
                    sStripe.transform.localScale = new Vector3(1.01f, 0.15f, 0.01f);
                    sStripe.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.8f, 0f));
                    Destroy(sStripe.GetComponent<Collider>());
                }
                break;
            }

            case 8: // Dumpster — Phase 16B enhanced with lid
            {
                obs = new GameObject("Dumpster");
                // Main body
                GameObject dBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dBody.transform.SetParent(obs.transform);
                dBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                dBody.transform.localScale = new Vector3(1.6f, 1.2f, 1.0f);
                dBody.GetComponent<Renderer>().material = dumpsterMat;
                Destroy(dBody.GetComponent<Collider>());
                // Lid
                GameObject dLid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dLid.transform.SetParent(obs.transform);
                dLid.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                dLid.transform.localScale = new Vector3(1.7f, 0.1f, 1.1f);
                dLid.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.25f, 0.4f, 0.25f));
                Destroy(dLid.GetComponent<Collider>());
                // Wheels
                for (int dw = -1; dw <= 1; dw += 2)
                {
                    GameObject dWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    dWheel.transform.SetParent(obs.transform);
                    dWheel.transform.localPosition = new Vector3(dw * 0.65f, 0.1f, -0.4f);
                    dWheel.transform.localScale = new Vector3(0.15f, 0.06f, 0.15f);
                    dWheel.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    dWheel.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.15f, 0.15f, 0.15f));
                    Destroy(dWheel.GetComponent<Collider>());
                }
                break;
            }

            case 9: // Construction barrier — Phase 16B enhanced with stripes
            {
                obs = new GameObject("Construction");
                GameObject cBarrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cBarrier.transform.SetParent(obs.transform);
                cBarrier.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                cBarrier.transform.localScale = new Vector3(2.5f, 1.2f, 0.3f);
                cBarrier.GetComponent<Renderer>().material = constructionMat;
                Destroy(cBarrier.GetComponent<Collider>());
                // Yellow/black stripes
                for (int si = 0; si < 4; si++)
                {
                    GameObject cStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cStripe.transform.SetParent(obs.transform);
                    cStripe.transform.localPosition = new Vector3(-0.8f + si * 0.55f, 0.6f, 0.16f);
                    cStripe.transform.localScale = new Vector3(0.2f, 1.0f, 0.01f);
                    cStripe.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
                    cStripe.GetComponent<Renderer>().material = CreateColorMaterial(si % 2 == 0 ? new Color(1f, 0.8f, 0f) : new Color(0.15f, 0.15f, 0.15f));
                    Destroy(cStripe.GetComponent<Collider>());
                }
                // Warning cones on sides
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
            }

            case 10: // Car — Phase 16B enhanced with windshield/headlights
            {
                obs = new GameObject("Car");
                bool isTaxi = Random.value < 0.5f;
                Material carPaintMat = isTaxi ? CreateColorMaterial(new Color(0.95f, 0.85f, 0.1f)) : CreateColorMaterial(new Color(0.3f, 0.5f, 0.8f));
                // Body
                GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carBody.transform.SetParent(obs.transform);
                carBody.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                carBody.transform.localScale = new Vector3(1.6f, 1.0f, 3.0f);
                carBody.GetComponent<Renderer>().material = carPaintMat;
                Destroy(carBody.GetComponent<Collider>());
                // Cabin/roof
                GameObject carRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carRoof.transform.SetParent(obs.transform);
                carRoof.transform.localPosition = new Vector3(0f, 1.3f, 0.2f);
                carRoof.transform.localScale = new Vector3(1.4f, 0.5f, 1.5f);
                carRoof.GetComponent<Renderer>().material = carPaintMat;
                Destroy(carRoof.GetComponent<Collider>());
                // Windshield
                GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                windshield.transform.SetParent(obs.transform);
                windshield.transform.localPosition = new Vector3(0f, 1.2f, 1.15f);
                windshield.transform.localScale = new Vector3(1.3f, 0.45f, 0.05f);
                windshield.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.6f, 0.78f, 0.95f));
                Destroy(windshield.GetComponent<Collider>());
                // Headlights
                for (int hl = -1; hl <= 1; hl += 2)
                {
                    GameObject headlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    headlight.transform.SetParent(obs.transform);
                    headlight.transform.localPosition = new Vector3(hl * 0.55f, 0.65f, 1.51f);
                    headlight.transform.localScale = new Vector3(0.2f, 0.15f, 0.08f);
                    headlight.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.95f, 0.7f));
                    Destroy(headlight.GetComponent<Collider>());
                }
                // Taillights
                for (int tl = -1; tl <= 1; tl += 2)
                {
                    GameObject taillight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    taillight.transform.SetParent(obs.transform);
                    taillight.transform.localPosition = new Vector3(tl * 0.6f, 0.65f, -1.51f);
                    taillight.transform.localScale = new Vector3(0.18f, 0.12f, 0.04f);
                    taillight.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.9f, 0.1f, 0.05f));
                    Destroy(taillight.GetComponent<Collider>());
                }
                // Wheels
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
                        // Hub cap
                        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        hub.transform.SetParent(wheel.transform);
                        hub.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                        hub.transform.localScale = new Vector3(0.4f, 0.15f, 0.4f);
                        hub.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.7f, 0.7f, 0.72f));
                        Destroy(hub.GetComponent<Collider>());
                    }
                }
                break;
            }

            default: // Bus — Phase 16B enhanced with windows/doors/wheels
            {
                obs = new GameObject("Bus");
                Material busPaintMat = busMat;
                // Bus body
                GameObject busBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                busBody.transform.SetParent(obs.transform);
                busBody.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                busBody.transform.localScale = new Vector3(2.0f, 2.2f, 5.0f);
                busBody.GetComponent<Renderer>().material = busPaintMat;
                Destroy(busBody.GetComponent<Collider>());
                // Windows on both sides
                for (int bside = -1; bside <= 1; bside += 2)
                {
                    for (int bw = 0; bw < 4; bw++)
                    {
                        GameObject busWin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        busWin.transform.SetParent(obs.transform);
                        busWin.transform.localPosition = new Vector3(bside * 1.01f, 1.6f, -1.5f + bw * 1.0f);
                        busWin.transform.localScale = new Vector3(0.02f, 0.6f, 0.6f);
                        busWin.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.6f, 0.78f, 0.95f));
                        Destroy(busWin.GetComponent<Collider>());
                    }
                }
                // Door
                GameObject busDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                busDoor.transform.SetParent(obs.transform);
                busDoor.transform.localPosition = new Vector3(1.01f, 0.8f, 1.0f);
                busDoor.transform.localScale = new Vector3(0.02f, 1.4f, 0.8f);
                busDoor.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.5f, 0.5f, 0.55f));
                Destroy(busDoor.GetComponent<Collider>());
                // Wheels
                for (int bwx = -1; bwx <= 1; bwx += 2)
                {
                    for (int bwz = -1; bwz <= 1; bwz += 2)
                    {
                        GameObject busWheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        busWheel.transform.SetParent(obs.transform);
                        busWheel.transform.localPosition = new Vector3(bwx * 0.95f, 0.2f, bwz * 1.6f);
                        busWheel.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
                        busWheel.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                        busWheel.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.15f, 0.15f, 0.15f));
                        Destroy(busWheel.GetComponent<Collider>());
                    }
                }
                // Headlights
                for (int bhl = -1; bhl <= 1; bhl += 2)
                {
                    GameObject busHL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    busHL.transform.SetParent(obs.transform);
                    busHL.transform.localPosition = new Vector3(bhl * 0.7f, 0.8f, 2.51f);
                    busHL.transform.localScale = new Vector3(0.25f, 0.2f, 0.1f);
                    busHL.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 0.95f, 0.7f));
                    Destroy(busHL.GetComponent<Collider>());
                }
                break;
            }
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

            // Phase 16B: Enhanced coin with rim detail
            Material activeCoinMat = (coinEmbossedMat != null && coinEmbossedMat.mainTexture != null) ? coinEmbossedMat : (coinShineMat != null && coinShineMat.mainTexture != null) ? coinShineMat : coinMat;
            GameObject coin = new GameObject("Coin");
            coin.transform.position = new Vector3(lane * laneWidth, 1.2f, z);
            // Main disc
            GameObject coinDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinDisc.transform.SetParent(coin.transform);
            coinDisc.transform.localPosition = Vector3.zero;
            coinDisc.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            coinDisc.GetComponent<Renderer>().material = activeCoinMat;
            Destroy(coinDisc.GetComponent<Collider>());
            // Rim ring (slightly larger, darker gold)
            GameObject coinRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinRim.transform.SetParent(coin.transform);
            coinRim.transform.localPosition = Vector3.zero;
            coinRim.transform.localScale = new Vector3(0.62f, 0.03f, 0.62f);
            coinRim.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.8f, 0.6f, 0.1f));
            Destroy(coinRim.GetComponent<Collider>());
            activeCoins.Add(coin);
        }

        if (Random.value < 0.5f)
        {
            int lane2 = Random.Range(-1, 2);
            while (lane2 == lane) lane2 = Random.Range(-1, 2);
            float startZ2 = segStartZ + segmentLength * 0.5f;
            int count2 = Random.Range(2, 6);
            Material activeCoinMat2 = (coinEmbossedMat != null && coinEmbossedMat.mainTexture != null) ? coinEmbossedMat : (coinShineMat != null && coinShineMat.mainTexture != null) ? coinShineMat : coinMat;
            for (int i = 0; i < count2; i++)
            {
                float z = startZ2 + i * coinSpacing;
                if (z >= segStartZ + segmentLength - 5f) break;

                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = new Vector3(lane2 * laneWidth, 1.2f, z);
                coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
                coin.GetComponent<Renderer>().material = activeCoinMat2;
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
            Material activeCoinMat3 = (coinEmbossedMat != null && coinEmbossedMat.mainTexture != null) ? coinEmbossedMat : (coinShineMat != null && coinShineMat.mainTexture != null) ? coinShineMat : coinMat;
            for (int i = 0; i < elevCount; i++)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = "Coin";
                coin.transform.position = new Vector3(elevLane * laneWidth, 3.5f, elevZ + i * 2.5f);
                coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
                coin.GetComponent<Renderer>().material = activeCoinMat3;
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
        if (distZ > curvedWorldStartZ)
        {
            float drop = curvedWorldIntensity * (distZ - curvedWorldStartZ) * (distZ - curvedWorldStartZ);
            Vector3 pos = segment.transform.position;
            pos.y -= drop;
            segment.transform.position = pos;
        }
    }

    // Phase 14: Update curved-world each frame — only distant segments curve
    // Near-camera segments (Z < curvedWorldStartZ) stay perfectly flat
    private void UpdateCurvedWorld()
    {
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] == null) continue;
            Vector3 pos = activeSegments[i].transform.position;
            float distZ = pos.z;
            float flatY = 0f;
            if (distZ > curvedWorldStartZ)
            {
                float drop = curvedWorldIntensity * (distZ - curvedWorldStartZ) * (distZ - curvedWorldStartZ);
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
            if (distZ > curvedWorldStartZ)
            {
                float drop = curvedWorldIntensity * (distZ - curvedWorldStartZ) * (distZ - curvedWorldStartZ);
                pos.y = baseY - drop * 0.3f;
                activeObstacles[i].transform.position = pos;
            }
        }

        // Phase 4: Curve coins too
        for (int i = 0; i < activeCoins.Count; i++)
        {
            if (activeCoins[i] == null) continue;
            Vector3 pos = activeCoins[i].transform.position;
            float distZ = pos.z;
            if (distZ > curvedWorldStartZ)
            {
                float drop = curvedWorldIntensity * (distZ - curvedWorldStartZ) * (distZ - curvedWorldStartZ);
                pos.y -= drop * 0.3f * Time.deltaTime * speed * 0.1f;
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
