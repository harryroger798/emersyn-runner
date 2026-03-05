using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// AAA-quality procedural scene builder for Emersyn Runner.
/// Builds the entire game scene at runtime with:
/// - SDXL-generated texture loading from Resources
/// - Animated character with limb movement
/// - Particle effects (coins, speed lines, dust)
/// - Fog and atmosphere
/// - Polished UI with shadows and animations
/// - Audio system with music and SFX
/// </summary>
public class RuntimeSceneBuilder : MonoBehaviour
{
    private Camera mainCamera;
    private GameObject player;
    private GameObject ground;
    private GameObject uiCanvas;
    private SimpleTrackRunner trackRunner;

    // Player body parts for animation
    private Transform playerBody;
    private Transform playerHead;
    private Transform playerHair;
    private Transform playerLeftLeg;
    private Transform playerRightLeg;
    private Transform playerLeftArm;
    private Transform playerRightArm;

    // UI references
    private GameObject mainMenuPanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;
    private Text scoreText;
    private Text coinText;
    private Text titleText;
    private Text gameOverScoreText;
    private Text highScoreText;
    private Text speedText;

    private bool gameStarted = false;
    private int score = 0;
    private int coins = 0;
    private int bestScore = 0;
    private float distanceTraveled = 0f;
    private float currentSpeed = 10f;
    private float playTime = 0f;
    private bool isGameOver = false;

    // Player movement
    private int currentLane = 0;
    private float targetX = 0f;
    private float laneWidth = 2.5f;
    private bool isJumping = false;
    private bool isRolling = false;
    private float jumpTimer = 0f;
    private float jumpDuration = 0.65f;
    private float jumpHeight = 3.5f;
    private float groundY = 0.75f;
    private float rollTimer = 0f;
    private float rollDuration = 0.6f;

    // Touch input
    private Vector2 touchStartPos;
    private float touchStartTime;
    private float swipeThreshold = 30f;

    // Camera shake
    private float cameraShakeTimer = 0f;
    private float cameraShakeIntensity = 0f;
    private Vector3 baseCameraOffset = new Vector3(0f, 7f, -12f);

    // Particle systems
    private ParticleSystem speedLinesPS;
    private ParticleSystem dustPS;
    private ParticleSystem coinCollectPS;

    // Audio
    private AudioSource musicSource;
    private AudioSource sfxSource;

    // Cached resources
    private Shader cachedShader;
    private Font cachedFont;
    private Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
    private Dictionary<string, AudioClip> loadedAudio = new Dictionary<string, AudioClip>();

    // Score animation
    private float scorePopTimer = 0f;
    private int lastDisplayedScore = 0;

    private Shader FindWorkingShader()
    {
        if (cachedShader != null) return cachedShader;

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
                Debug.Log("[RuntimeSceneBuilder] Using shader: " + sn);
                cachedShader = s;
                return s;
            }
        }

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cachedShader = tmp.GetComponent<Renderer>().sharedMaterial.shader;
        Destroy(tmp);
        return cachedShader;
    }

    private Material CreateColorMaterial(Color color)
    {
        Shader s = FindWorkingShader();
        Material mat = new Material(s);
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        return mat;
    }

    private Material CreateTexturedMaterial(string textureName, Color fallbackColor)
    {
        Texture2D tex = LoadTexture(textureName);
        if (tex != null)
        {
            Shader s = FindWorkingShader();
            Material mat = new Material(s);
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
        return CreateColorMaterial(fallbackColor);
    }

    private Texture2D LoadTexture(string name)
    {
        if (loadedTextures.ContainsKey(name))
            return loadedTextures[name];

        Texture2D tex = Resources.Load<Texture2D>("Textures/" + name);
        if (tex != null)
        {
            loadedTextures[name] = tex;
            Debug.Log("[RuntimeSceneBuilder] Loaded texture: " + name);
        }
        return tex;
    }

    private AudioClip LoadAudio(string name)
    {
        if (loadedAudio.ContainsKey(name))
            return loadedAudio[name];

        AudioClip clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip != null)
        {
            loadedAudio[name] = clip;
            Debug.Log("[RuntimeSceneBuilder] Loaded audio: " + name);
        }
        return clip;
    }

    private Font FindWorkingFont()
    {
        if (cachedFont != null) return cachedFont;
        cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cachedFont != null) return cachedFont;
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont != null) return cachedFont;
        cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return cachedFont;
    }

    private void Awake()
    {
        Debug.Log("[RuntimeSceneBuilder] Starting AAA scene construction...");
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    private void Start()
    {
        BuildScene();
    }

    private void BuildScene()
    {
        SetupCamera();
        SetupLighting();
        SetupFog();
        CreateSkybox();
        CreateGround();
        CreatePlayer();
        SetupAudio();
        CreateParticleSystems();
        CreateUI();
        EnsureEventSystem();
        CreateTrackRunner();
        PositionCamera();
        ShowMainMenu();
        Debug.Log("[RuntimeSceneBuilder] AAA Scene construction complete!");
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.4f, 0.7f, 0.95f, 1f);
        mainCamera.fieldOfView = 65f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 300f;
    }

    private void SetupLighting()
    {
        Light[] existingLights = FindObjectsOfType<Light>();
        foreach (Light l in existingLights)
        {
            if (l.type == LightType.Directional) return;
        }

        GameObject sunObj = new GameObject("Sun Light");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 1.8f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.6f;
        sunObj.transform.eulerAngles = new Vector3(45f, -30f, 0f);

        GameObject fillObj = new GameObject("Fill Light");
        Light fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.6f, 0.7f, 0.9f);
        fill.intensity = 0.4f;
        fill.shadows = LightShadows.None;
        fillObj.transform.eulerAngles = new Vector3(30f, 150f, 0f);

        RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    private void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.55f, 0.75f, 0.95f);
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 200f;
    }

    private void CreateSkybox()
    {
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.4f, 0.7f, 0.95f);
        }

        for (int i = 0; i < 12; i++)
        {
            GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cloud.name = "Cloud";
            float x = Random.Range(-80f, 80f);
            float y = Random.Range(25f, 50f);
            float z = Random.Range(60f, 180f);
            cloud.transform.position = new Vector3(x, y, z);
            float scaleX = Random.Range(15f, 35f);
            float scaleY = Random.Range(3f, 8f);
            cloud.transform.localScale = new Vector3(scaleX, scaleY, Random.Range(8f, 15f));
            cloud.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 1f, 1f, 0.85f));
            Destroy(cloud.GetComponent<Collider>());
        }
    }

    private void CreateGround()
    {
        ground = new GameObject("Ground");

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(ground.transform);
        road.transform.position = new Vector3(0f, -0.5f, 100f);
        road.transform.localScale = new Vector3(10f, 1f, 400f);
        road.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_road_asphalt",
            new Color(0.25f, 0.25f, 0.3f));
        Destroy(road.GetComponent<Collider>());

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sidewalk.name = "Sidewalk";
            sidewalk.transform.SetParent(ground.transform);
            sidewalk.transform.position = new Vector3(side * 5.8f, -0.3f, 100f);
            sidewalk.transform.localScale = new Vector3(2f, 0.6f, 400f);
            sidewalk.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_road_sidewalk",
                new Color(0.6f, 0.6f, 0.55f));
            Destroy(sidewalk.GetComponent<Collider>());
        }

        for (float lx = -1.25f; lx <= 1.25f; lx += 2.5f)
        {
            for (int dash = 0; dash < 50; dash++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "LaneDash";
                line.transform.SetParent(ground.transform);
                line.transform.position = new Vector3(lx, 0.02f, dash * 8f);
                line.transform.localScale = new Vector3(0.12f, 0.02f, 4f);
                line.GetComponent<Renderer>().material = CreateColorMaterial(new Color(1f, 1f, 0.8f));
                Destroy(line.GetComponent<Collider>());
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grass.name = "Grass";
            grass.transform.SetParent(ground.transform);
            grass.transform.position = new Vector3(side * 15f, -0.6f, 100f);
            grass.transform.localScale = new Vector3(18f, 0.5f, 400f);
            grass.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_grass",
                new Color(0.3f, 0.7f, 0.2f));
            Destroy(grass.GetComponent<Collider>());
        }
    }

    private void CreatePlayer()
    {
        player = new GameObject("Player");
        player.transform.position = new Vector3(0f, groundY, 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(0.55f, 0.45f, 0.4f);
        body.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_emersyn_shirt",
            new Color(0.2f, 0.5f, 0.9f));
        Destroy(body.GetComponent<Collider>());
        playerBody = body.transform;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
        head.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_emersyn_skin",
            new Color(0.95f, 0.8f, 0.7f));
        Destroy(head.GetComponent<Collider>());
        playerHead = head.transform;

        GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hair.name = "Hair";
        hair.transform.SetParent(player.transform);
        hair.transform.localPosition = new Vector3(0f, 1.28f, -0.04f);
        hair.transform.localScale = new Vector3(0.48f, 0.28f, 0.48f);
        hair.GetComponent<Renderer>().material = CreateTexturedMaterial("tex_emersyn_hair",
            new Color(0.3f, 0.15f, 0.05f));
        Destroy(hair.GetComponent<Collider>());
        playerHair = hair.transform;

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(head.transform);
            eye.transform.localPosition = new Vector3(side * 0.3f, 0.1f, 0.4f);
            eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.15f);
            eye.GetComponent<Renderer>().material = CreateColorMaterial(Color.white);
            Destroy(eye.GetComponent<Collider>());

            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eye.transform);
            pupil.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            pupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.4f);
            pupil.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.1f, 0.1f, 0.15f));
            Destroy(pupil.GetComponent<Collider>());
        }

        GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mouth.name = "Mouth";
        mouth.transform.SetParent(head.transform);
        mouth.transform.localPosition = new Vector3(0f, -0.2f, 0.4f);
        mouth.transform.localScale = new Vector3(0.25f, 0.08f, 0.1f);
        mouth.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.8f, 0.3f, 0.3f));
        Destroy(mouth.GetComponent<Collider>());

        Material shirtMat = CreateTexturedMaterial("tex_emersyn_shirt", new Color(0.2f, 0.5f, 0.9f));
        Material skinMat = CreateTexturedMaterial("tex_emersyn_skin", new Color(0.95f, 0.8f, 0.7f));

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = side < 0 ? "LeftArm" : "RightArm";
            arm.transform.SetParent(player.transform);
            arm.transform.localPosition = new Vector3(side * 0.35f, 0.5f, 0f);
            arm.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
            arm.GetComponent<Renderer>().material = shirtMat;
            Destroy(arm.GetComponent<Collider>());

            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "Hand";
            hand.transform.SetParent(arm.transform);
            hand.transform.localPosition = new Vector3(0f, -1.2f, 0f);
            hand.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
            hand.GetComponent<Renderer>().material = skinMat;
            Destroy(hand.GetComponent<Collider>());

            if (side < 0) playerLeftArm = arm.transform;
            else playerRightArm = arm.transform;
        }

        Material pantsMat = CreateTexturedMaterial("tex_emersyn_pants", new Color(0.2f, 0.2f, 0.35f));
        Material shoesMat = CreateTexturedMaterial("tex_emersyn_shoes", new Color(0.9f, 0.2f, 0.15f));

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = side < 0 ? "LeftLeg" : "RightLeg";
            leg.transform.SetParent(player.transform);
            leg.transform.localPosition = new Vector3(side * 0.15f, -0.15f, 0f);
            leg.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            leg.GetComponent<Renderer>().material = pantsMat;
            Destroy(leg.GetComponent<Collider>());

            GameObject shoe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoe.name = "Shoe";
            shoe.transform.SetParent(leg.transform);
            shoe.transform.localPosition = new Vector3(0f, -1.0f, 0.2f);
            shoe.transform.localScale = new Vector3(0.8f, 0.4f, 1.5f);
            shoe.GetComponent<Renderer>().material = shoesMat;
            Destroy(shoe.GetComponent<Collider>());

            if (side < 0) playerLeftLeg = leg.transform;
            else playerRightLeg = leg.transform;
        }

        GameObject backpack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backpack.name = "Backpack";
        backpack.transform.SetParent(player.transform);
        backpack.transform.localPosition = new Vector3(0f, 0.45f, -0.25f);
        backpack.transform.localScale = new Vector3(0.35f, 0.4f, 0.2f);
        backpack.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.9f, 0.4f, 0.1f));
        Destroy(backpack.GetComponent<Collider>());

        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.5f, 0f);
        col.radius = 0.3f;
        col.height = 1.5f;
        col.isTrigger = true;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void SetupAudio()
    {
        GameObject musicObj = new GameObject("MusicPlayer");
        musicObj.transform.SetParent(transform);
        musicSource = musicObj.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.3f;
        musicSource.playOnAwake = false;

        GameObject sfxObj = new GameObject("SFXPlayer");
        sfxObj.transform.SetParent(transform);
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 0.5f;
        sfxSource.playOnAwake = false;

        AudioClip menuClip = LoadAudio("menu_loop");
        if (menuClip != null)
        {
            musicSource.clip = menuClip;
            musicSource.Play();
        }
    }

    private void PlaySFX(string clipName)
    {
        if (sfxSource == null) return;
        AudioClip clip = LoadAudio(clipName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void SwitchMusic(string clipName)
    {
        if (musicSource == null) return;
        AudioClip clip = LoadAudio(clipName);
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    private void CreateParticleSystems()
    {
        GameObject speedObj = new GameObject("SpeedLines");
        speedObj.transform.SetParent(player.transform);
        speedObj.transform.localPosition = new Vector3(0f, 1f, -2f);
        speedLinesPS = speedObj.AddComponent<ParticleSystem>();
        var speedMain = speedLinesPS.main;
        speedMain.startSpeed = 20f;
        speedMain.startLifetime = 0.3f;
        speedMain.startSize = 0.05f;
        speedMain.startColor = new Color(1f, 1f, 1f, 0.4f);
        speedMain.maxParticles = 50;
        speedMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var speedEmission = speedLinesPS.emission;
        speedEmission.rateOverTime = 0f;
        var speedShape = speedLinesPS.shape;
        speedShape.shapeType = ParticleSystemShapeType.Box;
        speedShape.scale = new Vector3(3f, 3f, 0.1f);
        ParticleSystemRenderer speedRend = speedObj.GetComponent<ParticleSystemRenderer>();
        speedRend.material = CreateColorMaterial(Color.white);

        GameObject dustObj = new GameObject("DustParticles");
        dustObj.transform.SetParent(player.transform);
        dustObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        dustPS = dustObj.AddComponent<ParticleSystem>();
        var dustMain = dustPS.main;
        dustMain.startSpeed = 2f;
        dustMain.startLifetime = 0.5f;
        dustMain.startSize = 0.3f;
        dustMain.startColor = new Color(0.7f, 0.65f, 0.5f, 0.5f);
        dustMain.maxParticles = 20;
        dustMain.gravityModifier = -0.5f;
        var dustEmission = dustPS.emission;
        dustEmission.rateOverTime = 0f;
        var dustShape = dustPS.shape;
        dustShape.shapeType = ParticleSystemShapeType.Hemisphere;
        dustShape.radius = 0.3f;
        ParticleSystemRenderer dustRend = dustObj.GetComponent<ParticleSystemRenderer>();
        dustRend.material = CreateColorMaterial(new Color(0.7f, 0.65f, 0.5f, 0.5f));

        GameObject coinPObj = new GameObject("CoinParticles");
        coinPObj.transform.SetParent(player.transform);
        coinPObj.transform.localPosition = new Vector3(0f, 1f, 0f);
        coinCollectPS = coinPObj.AddComponent<ParticleSystem>();
        var coinMain = coinCollectPS.main;
        coinMain.startSpeed = 5f;
        coinMain.startLifetime = 0.4f;
        coinMain.startSize = 0.15f;
        coinMain.startColor = new Color(1f, 0.85f, 0.1f, 0.9f);
        coinMain.maxParticles = 15;
        coinMain.gravityModifier = 1f;
        var coinEmission = coinCollectPS.emission;
        coinEmission.rateOverTime = 0f;
        var coinShape = coinCollectPS.shape;
        coinShape.shapeType = ParticleSystemShapeType.Sphere;
        coinShape.radius = 0.3f;
        ParticleSystemRenderer coinRend = coinPObj.GetComponent<ParticleSystemRenderer>();
        coinRend.material = CreateColorMaterial(new Color(1f, 0.85f, 0.1f));
    }

    private void PositionCamera()
    {
        if (mainCamera != null && player != null)
        {
            mainCamera.transform.position = player.transform.position + baseCameraOffset;
            mainCamera.transform.LookAt(player.transform.position + Vector3.forward * 8f + Vector3.up * 1.5f);
        }
    }

    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        uiCanvas = canvasObj;

        mainMenuPanel = CreatePanel(canvasObj.transform, "MainMenuPanel");

        CreateUIText(mainMenuPanel.transform, "TitleShadow", "EMERSYN\nRUNNER",
            new Vector2(3f, 197f), 80, new Color(0f, 0f, 0f, 0.5f), FontStyle.Bold);
        titleText = CreateUIText(mainMenuPanel.transform, "TitleText", "EMERSYN\nRUNNER",
            new Vector2(0f, 200f), 80, new Color(1f, 0.85f, 0.1f), FontStyle.Bold);

        CreateUIText(mainMenuPanel.transform, "Subtitle", "ENDLESS RUNNER",
            new Vector2(0f, 100f), 28, new Color(0.8f, 0.8f, 0.8f), FontStyle.Italic);

        highScoreText = CreateUIText(mainMenuPanel.transform, "HighScoreText", "BEST: " + bestScore,
            new Vector2(0f, 40f), 36, new Color(1f, 0.85f, 0.1f), FontStyle.Bold);

        CreateButton(mainMenuPanel.transform, "PlayButton", "PLAY",
            new Vector2(0f, -100f), new Vector2(350f, 80f),
            new Color(0.1f, 0.75f, 0.3f), OnPlayClicked);

        CreateUIText(mainMenuPanel.transform, "VersionText", "v2.0 AAA - Modal GPU Generated",
            new Vector2(0f, -800f), 18, new Color(0.5f, 0.5f, 0.5f), FontStyle.Normal);

        hudPanel = CreatePanel(canvasObj.transform, "HUDPanel");
        hudPanel.SetActive(false);

        scoreText = CreateUIText(hudPanel.transform, "ScoreText", "0",
            new Vector2(0f, 850f), 64, Color.white, FontStyle.Bold);

        coinText = CreateUIText(hudPanel.transform, "CoinText", "0",
            new Vector2(0f, 780f), 32, new Color(1f, 0.85f, 0.1f), FontStyle.Bold);

        speedText = CreateUIText(hudPanel.transform, "SpeedText", "",
            new Vector2(400f, 850f), 20, new Color(0.7f, 0.7f, 0.7f), FontStyle.Normal);

        CreateButton(hudPanel.transform, "PauseBtn", "||",
            new Vector2(-450f, 870f), new Vector2(70f, 70f),
            new Color(0f, 0f, 0f, 0.4f), OnPauseClicked);

        gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel");
        gameOverPanel.SetActive(false);

        CreateUIText(gameOverPanel.transform, "GOShadow", "GAME OVER",
            new Vector2(3f, 197f), 72, new Color(0f, 0f, 0f, 0.5f), FontStyle.Bold);
        CreateUIText(gameOverPanel.transform, "GameOverTitle", "GAME OVER",
            new Vector2(0f, 200f), 72, new Color(0.95f, 0.3f, 0.2f), FontStyle.Bold);

        gameOverScoreText = CreateUIText(gameOverPanel.transform, "FinalScore", "Score: 0",
            new Vector2(0f, 80f), 44, Color.white, FontStyle.Bold);

        CreateButton(gameOverPanel.transform, "RetryButton", "RETRY",
            new Vector2(0f, -60f), new Vector2(300f, 70f),
            new Color(0.1f, 0.75f, 0.3f), OnRetryClicked);

        CreateButton(gameOverPanel.transform, "MenuButton", "MENU",
            new Vector2(0f, -160f), new Vector2(300f, 70f),
            new Color(0.7f, 0.25f, 0.2f), OnMenuClicked);
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (name != "HUDPanel")
        {
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0.05f, 0.7f);
        }

        return panel;
    }

    private Text CreateUIText(Transform parent, string name, string content,
        Vector2 position, int fontSize, Color color, FontStyle style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(900f, 120f);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = FindWorkingFont();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private void CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text text = labelObj.AddComponent<Text>();
        text.text = label;
        text.font = FindWorkingFont();
        text.fontSize = 32;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateTrackRunner()
    {
        GameObject trackObj = new GameObject("SimpleTrackRunner");
        trackRunner = trackObj.AddComponent<SimpleTrackRunner>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void OnPlayClicked()
    {
        PlaySFX("sfx_click");
        StartGame();
    }

    private void OnPauseClicked()
    {
        PlaySFX("sfx_click");
        Time.timeScale = Time.timeScale > 0 ? 0 : 1;
    }

    private void OnRetryClicked()
    {
        PlaySFX("sfx_click");
        StartGame();
    }

    private void OnMenuClicked()
    {
        PlaySFX("sfx_click");
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        gameStarted = false;
        isGameOver = false;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (highScoreText != null) highScoreText.text = "BEST: " + bestScore;

        if (trackRunner != null) trackRunner.StopTrack();

        if (player != null)
        {
            player.transform.position = new Vector3(0f, groundY, 0f);
            player.transform.rotation = Quaternion.identity;
        }

        Time.timeScale = 1f;
        SwitchMusic("menu_loop");
    }

    private void StartGame()
    {
        gameStarted = true;
        isGameOver = false;
        score = 0;
        coins = 0;
        distanceTraveled = 0f;
        playTime = 0f;
        currentSpeed = 12f;
        currentLane = 0;
        targetX = 0f;
        isJumping = false;
        isRolling = false;

        if (player != null)
        {
            player.transform.position = new Vector3(0f, groundY, 0f);
            player.transform.rotation = Quaternion.identity;
        }

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (scoreText != null) scoreText.text = "0";
        if (coinText != null) coinText.text = "0";

        if (trackRunner != null) trackRunner.StartTrack();

        Time.timeScale = 1f;
        SwitchMusic("gameplay_loop");
    }

    private void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        gameStarted = false;

        PlaySFX("sfx_gameover");

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverScoreText != null)
        {
            string newBest = score >= bestScore ? "\nNEW BEST!" : "";
            gameOverScoreText.text = "Score: " + score + "\nCoins: " + coins + "\nBest: " + bestScore + newBest;
        }

        cameraShakeTimer = 0.3f;
        cameraShakeIntensity = 0.5f;

        PlaySFX("sfx_hit");
    }

    private void Update()
    {
        if (!gameStarted || isGameOver) return;

        playTime += Time.deltaTime;

        currentSpeed = Mathf.Lerp(12f, 35f, Mathf.Clamp01(playTime / 180f));

        distanceTraveled += currentSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distanceTraveled);

        if (score != lastDisplayedScore)
        {
            lastDisplayedScore = score;
            scorePopTimer = 0.1f;
        }
        if (scorePopTimer > 0f)
        {
            scorePopTimer -= Time.deltaTime;
            float popScale = 1f + scorePopTimer * 3f;
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
                scoreText.transform.localScale = Vector3.one * popScale;
            }
        }
        else if (scoreText != null)
        {
            scoreText.transform.localScale = Vector3.one;
        }

        if (speedText != null)
        {
            int speedKmh = Mathf.FloorToInt(currentSpeed * 3.6f);
            speedText.text = speedKmh + " km/h";
        }

        if (speedLinesPS != null)
        {
            var emission = speedLinesPS.emission;
            float intensity = Mathf.Clamp01((currentSpeed - 15f) / 20f) * 30f;
            emission.rateOverTime = intensity;
        }

        HandleInput();
        MovePlayer();
        AnimatePlayer();
        UpdateCamera();

        if (trackRunner != null) trackRunner.SetSpeed(currentSpeed);

        CheckCollisions();
    }

    private void HandleInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                touchStartTime = Time.time;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                Vector2 delta = touch.position - touchStartPos;
                float elapsed = Time.time - touchStartTime;

                if (elapsed < 0.5f && delta.magnitude > swipeThreshold)
                {
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        if (delta.x > 0) SwipeRight();
                        else SwipeLeft();
                    }
                    else
                    {
                        if (delta.y > 0) SwipeUp();
                        else SwipeDown();
                    }
                }
                else if (delta.magnitude <= swipeThreshold)
                {
                    SwipeUp();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SwipeLeft();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SwipeRight();
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) SwipeUp();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) SwipeDown();
    }

    private void SwipeLeft()
    {
        if (currentLane > -1)
        {
            currentLane--;
            targetX = currentLane * laneWidth;
            PlaySFX("sfx_whoosh");
        }
    }

    private void SwipeRight()
    {
        if (currentLane < 1)
        {
            currentLane++;
            targetX = currentLane * laneWidth;
            PlaySFX("sfx_whoosh");
        }
    }

    private void SwipeUp()
    {
        if (!isJumping)
        {
            isJumping = true;
            isRolling = false;
            jumpTimer = 0f;
            PlaySFX("sfx_jump");
        }
    }

    private void SwipeDown()
    {
        if (isJumping)
        {
            isJumping = false;
            Vector3 pos = player.transform.position;
            pos.y = groundY;
            player.transform.position = pos;
            if (dustPS != null) dustPS.Emit(8);
        }
        else if (!isRolling)
        {
            isRolling = true;
            rollTimer = 0f;
            PlaySFX("sfx_roll");
        }
    }

    private void MovePlayer()
    {
        if (player == null) return;

        Vector3 pos = player.transform.position;

        float laneChangeSpeed = 15f;
        pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * laneChangeSpeed);

        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / jumpDuration);
            float heightMult = Mathf.Sin(t * Mathf.PI);
            pos.y = groundY + jumpHeight * heightMult;

            if (t >= 1f)
            {
                isJumping = false;
                pos.y = groundY;
                if (dustPS != null) dustPS.Emit(5);
            }
        }
        else
        {
            pos.y = groundY;
        }

        if (isRolling)
        {
            rollTimer += Time.deltaTime;
            if (rollTimer >= rollDuration)
            {
                isRolling = false;
            }
        }

        player.transform.position = pos;

        float targetLean = (targetX - pos.x) * -8f;
        targetLean = Mathf.Clamp(targetLean, -15f, 15f);
        float currentLeanAngle = player.transform.eulerAngles.z;
        if (currentLeanAngle > 180f) currentLeanAngle -= 360f;
        float newLean = Mathf.Lerp(currentLeanAngle, targetLean, Time.deltaTime * 10f);
        player.transform.rotation = Quaternion.Euler(0f, 0f, newLean);
    }

    private void AnimatePlayer()
    {
        if (player == null) return;

        float runCycle = Time.time * (currentSpeed * 0.8f);

        if (isRolling)
        {
            if (playerBody != null)
                playerBody.localPosition = new Vector3(0f, 0.1f, 0f);
            if (playerHead != null)
                playerHead.localPosition = new Vector3(0f, 0.5f, 0.1f);
            if (playerLeftLeg != null)
                playerLeftLeg.localPosition = new Vector3(-0.15f, -0.05f, 0.1f);
            if (playerRightLeg != null)
                playerRightLeg.localPosition = new Vector3(0.15f, -0.05f, 0.1f);
            if (playerLeftArm != null)
                playerLeftArm.localPosition = new Vector3(-0.35f, 0.2f, 0.1f);
            if (playerRightArm != null)
                playerRightArm.localPosition = new Vector3(0.35f, 0.2f, 0.1f);
        }
        else
        {
            float legSwing = Mathf.Sin(runCycle) * 25f;
            float armSwing = Mathf.Sin(runCycle) * 20f;
            float bodyBob = Mathf.Abs(Mathf.Sin(runCycle * 2f)) * 0.06f;

            if (playerBody != null)
                playerBody.localPosition = new Vector3(0f, 0.4f + bodyBob, 0f);
            if (playerHead != null)
                playerHead.localPosition = new Vector3(0f, 1.1f + bodyBob, 0f);

            if (playerLeftLeg != null)
            {
                playerLeftLeg.localPosition = new Vector3(-0.15f, -0.15f, 0f);
                playerLeftLeg.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
            }
            if (playerRightLeg != null)
            {
                playerRightLeg.localPosition = new Vector3(0.15f, -0.15f, 0f);
                playerRightLeg.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
            }

            if (playerLeftArm != null)
            {
                playerLeftArm.localPosition = new Vector3(-0.35f, 0.5f, 0f);
                playerLeftArm.localRotation = Quaternion.Euler(-armSwing, 0f, 0f);
            }
            if (playerRightArm != null)
            {
                playerRightArm.localPosition = new Vector3(0.35f, 0.5f, 0f);
                playerRightArm.localRotation = Quaternion.Euler(armSwing, 0f, 0f);
            }
        }

        if (isJumping)
        {
            float jumpProgress = jumpTimer / jumpDuration;
            float tuckAngle = Mathf.Sin(jumpProgress * Mathf.PI) * 30f;

            if (playerLeftLeg != null)
                playerLeftLeg.localRotation = Quaternion.Euler(-tuckAngle, 0f, 0f);
            if (playerRightLeg != null)
                playerRightLeg.localRotation = Quaternion.Euler(-tuckAngle, 0f, 0f);
            if (playerLeftArm != null)
                playerLeftArm.localRotation = Quaternion.Euler(tuckAngle * 0.5f, 0f, -15f);
            if (playerRightArm != null)
                playerRightArm.localRotation = Quaternion.Euler(tuckAngle * 0.5f, 0f, 15f);
        }
    }

    private void UpdateCamera()
    {
        if (mainCamera == null || player == null) return;

        float targetFOV = Mathf.Lerp(60f, 75f, Mathf.Clamp01((currentSpeed - 12f) / 23f));
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * 3f);

        Vector3 targetCamPos = player.transform.position + baseCameraOffset;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamPos, Time.deltaTime * 6f);

        if (cameraShakeTimer > 0f)
        {
            cameraShakeTimer -= Time.deltaTime;
            float shakeAmount = cameraShakeIntensity * (cameraShakeTimer / 0.3f);
            mainCamera.transform.position += new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
        }

        Vector3 lookTarget = player.transform.position + Vector3.forward * 10f + Vector3.up * 2f;
        mainCamera.transform.LookAt(lookTarget);
    }

    private void CheckCollisions()
    {
        if (player == null || trackRunner == null) return;

        float hitRadius = isRolling ? 0.8f : 1.1f;

        foreach (GameObject obs in trackRunner.GetActiveObstacles())
        {
            if (obs == null) continue;
            Vector3 obsPos = obs.transform.position;
            Vector3 playerPos = player.transform.position;

            float horizDist = Mathf.Abs(obsPos.x - playerPos.x);
            float vertDist = Mathf.Abs(obsPos.z - playerPos.z);

            if (horizDist < hitRadius && vertDist < 1.5f)
            {
                float obsHeight = 1.5f;
                if (playerPos.y > obsHeight + 0.3f)
                    continue;

                if (isRolling && obsHeight > 2f)
                    continue;

                GameOver();
                return;
            }
        }

        foreach (GameObject coin in trackRunner.GetActiveCoins())
        {
            if (coin == null || !coin.activeSelf) continue;
            float dist = Vector3.Distance(player.transform.position, coin.transform.position);
            if (dist < 1.8f)
            {
                coins++;
                if (coinText != null) coinText.text = coins.ToString();
                coin.SetActive(false);

                if (coinCollectPS != null) coinCollectPS.Emit(8);
                PlaySFX("sfx_coin");

                distanceTraveled += 5f;
            }
        }
    }
}
