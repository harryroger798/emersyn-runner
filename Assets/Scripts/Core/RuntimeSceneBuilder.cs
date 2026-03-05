using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Procedurally builds the entire game scene at runtime.
/// This is necessary because headless Unity builds cannot wire up
/// serialized prefab/UI references through the Inspector.
/// Creates: ground, player, camera, lighting, UI, track segments, obstacles, coins.
/// </summary>
public class RuntimeSceneBuilder : MonoBehaviour
{
    private Camera mainCamera;
    private GameObject player;
    private GameObject ground;
    private GameObject uiCanvas;
    private SimpleTrackRunner trackRunner;

    // UI references
    private GameObject mainMenuPanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;
    private Text scoreText;
    private Text coinText;
    private Text titleText;
    private Text gameOverScoreText;
    private Text highScoreText;

    private bool gameStarted = false;
    private int score = 0;
    private int coins = 0;
    private int bestScore = 0;
    private float distanceTraveled = 0f;
    private float currentSpeed = 10f;
    private float playTime = 0f;
    private bool isGameOver = false;

    // Player movement
    private int currentLane = 0; // -1, 0, 1
    private float targetX = 0f;
    private float laneWidth = 2.5f;
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpDuration = 0.7f;
    private float jumpHeight = 3f;
    private float groundY = 0.5f;

    // Touch input
    private Vector2 touchStartPos;
    private float touchStartTime;
    private float swipeThreshold = 30f;

    // Cached shader and font - found once, reused everywhere
    private Shader cachedShader;
    private Font cachedFont;

    private Shader FindWorkingShader()
    {
        if (cachedShader != null) return cachedShader;

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
                Debug.Log($"[RuntimeSceneBuilder] Using shader: {sn}");
                cachedShader = s;
                return s;
            }
        }

        // Last resort: get shader from a primitive's default material
        Debug.LogWarning("[RuntimeSceneBuilder] No named shader found, using primitive default");
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cachedShader = tmp.GetComponent<Renderer>().sharedMaterial.shader;
        Destroy(tmp);
        return cachedShader;
    }

    private Material CreateColorMaterial(Color color)
    {
        Shader s = FindWorkingShader();
        Material mat = new Material(s);
        // Try setting color via multiple property names for compatibility
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        return mat;
    }

    private Font FindWorkingFont()
    {
        if (cachedFont != null) return cachedFont;

        // Try Arial first (always available in Unity)
        cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cachedFont != null) return cachedFont;

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont != null) return cachedFont;

        // Fallback: find any loaded font
        cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        return cachedFont;
    }

    private void Awake()
    {
        Debug.Log("[RuntimeSceneBuilder] Starting full scene construction...");
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
    }

    private void Start()
    {
        BuildScene();
    }

    private void BuildScene()
    {
        // 1. Setup camera
        SetupCamera();

        // 2. Setup lighting
        SetupLighting();

        // 3. Create ground
        CreateGround();

        // 4. Create player
        CreatePlayer();

        // 5. Create UI
        CreateUI();

        // 6. Ensure EventSystem exists for UI interaction
        EnsureEventSystem();

        // 7. Create track runner (handles procedural track, obstacles, coins)
        CreateTrackRunner();

        // 7. Position camera to look at player
        PositionCamera();

        // Show main menu
        ShowMainMenu();

        Debug.Log("[RuntimeSceneBuilder] Scene construction complete!");
    }

    private void SetupCamera()
    {
        // Find or create main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCamera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.529f, 0.808f, 0.922f, 1f); // Light sky blue
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 500f;
    }

    private void SetupLighting()
    {
        // Remove existing lights
        Light[] existingLights = FindObjectsOfType<Light>();
        foreach (Light l in existingLights)
        {
            if (l.type == LightType.Directional) return; // Already have one
        }

        GameObject sunObj = new GameObject("Directional Light");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 1.5f;
        sun.shadows = LightShadows.Soft;
        sunObj.transform.eulerAngles = new Vector3(50f, -30f, 0f);

        // Ambient light
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    private void CreateGround()
    {
        // Create a long ground plane
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 100f);
        ground.transform.localScale = new Vector3(12f, 1f, 400f);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        groundRenderer.material = CreateColorMaterial(new Color(0.35f, 0.35f, 0.4f)); // Dark grey road

        // Lane markers
        for (int lane = -1; lane <= 1; lane++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"LaneMarker_{lane}";
            marker.transform.position = new Vector3(lane * laneWidth, 0.01f, 100f);
            marker.transform.localScale = new Vector3(0.1f, 0.02f, 400f);
            Renderer mr = marker.GetComponent<Renderer>();
            mr.material = CreateColorMaterial(Color.white);
            Destroy(marker.GetComponent<Collider>());
        }

        // Side walls for visual depth
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Building_{side}_{i}";
                float height = Random.Range(4f, 12f);
                float zPos = i * 40f - 20f;
                wall.transform.position = new Vector3(side * 8f, height / 2f, zPos);
                wall.transform.localScale = new Vector3(3f, height, Random.Range(8f, 15f));
                Renderer wr = wall.GetComponent<Renderer>();
                float shade = Random.Range(0.5f, 0.8f);
                wr.material = CreateColorMaterial(new Color(shade, shade * 0.9f, shade * 0.85f));
                Destroy(wall.GetComponent<Collider>());
            }
        }
    }

    private void CreatePlayer()
    {
        player = new GameObject("Player");
        player.transform.position = new Vector3(0f, groundY, 0f);

        // Body (capsule)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        body.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.2f, 0.6f, 0.9f)); // Blue shirt
        Destroy(body.GetComponent<Collider>());

        // Head (sphere)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        head.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        head.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.95f, 0.8f, 0.7f)); // Skin tone
        Destroy(head.GetComponent<Collider>());

        // Hair
        GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hair.name = "Hair";
        hair.transform.SetParent(player.transform);
        hair.transform.localPosition = new Vector3(0f, 1.35f, -0.05f);
        hair.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
        hair.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.3f, 0.15f, 0.05f)); // Brown hair
        Destroy(hair.GetComponent<Collider>());

        // Legs
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = $"Leg_{(side < 0 ? "L" : "R")}";
            leg.transform.SetParent(player.transform);
            leg.transform.localPosition = new Vector3(side * 0.15f, -0.1f, 0f);
            leg.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
            leg.GetComponent<Renderer>().material = CreateColorMaterial(new Color(0.25f, 0.25f, 0.35f)); // Dark pants
            Destroy(leg.GetComponent<Collider>());
        }

        // Add a main collider for trigger detection
        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.5f, 0f);
        col.radius = 0.3f;
        col.height = 1.5f;
        col.isTrigger = true;

        // Add rigidbody for trigger detection
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void PositionCamera()
    {
        if (mainCamera != null && player != null)
        {
            mainCamera.transform.position = player.transform.position + new Vector3(0f, 6f, -10f);
            mainCamera.transform.LookAt(player.transform.position + Vector3.forward * 5f + Vector3.up * 1f);
        }
    }

    private void CreateUI()
    {
        // Create Canvas
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

        // Main Menu Panel
        mainMenuPanel = CreatePanel(canvasObj.transform, "MainMenuPanel");
        CreateUIText(mainMenuPanel.transform, "TitleText", "EMERSYN\nRUNNER",
            new Vector2(0f, 200f), 72, Color.white, FontStyle.Bold);
        titleText = mainMenuPanel.transform.Find("TitleText").GetComponent<Text>();

        highScoreText = CreateUIText(mainMenuPanel.transform, "HighScoreText", $"Best: {bestScore}",
            new Vector2(0f, 80f), 32, Color.yellow, FontStyle.Normal);

        CreateButton(mainMenuPanel.transform, "PlayButton", "TAP TO PLAY",
            new Vector2(0f, -100f), new Vector2(400f, 80f),
            new Color(0.2f, 0.8f, 0.3f), OnPlayClicked);

        // HUD Panel
        hudPanel = CreatePanel(canvasObj.transform, "HUDPanel");
        hudPanel.SetActive(false);

        scoreText = CreateUIText(hudPanel.transform, "ScoreText", "0",
            new Vector2(0f, 400f), 48, Color.white, FontStyle.Bold);

        coinText = CreateUIText(hudPanel.transform, "CoinText", "Coins: 0",
            new Vector2(0f, 340f), 28, Color.yellow, FontStyle.Normal);

        // Pause button
        CreateButton(hudPanel.transform, "PauseBtn", "||",
            new Vector2(-450f, 850f), new Vector2(80f, 80f),
            new Color(0.3f, 0.3f, 0.3f, 0.5f), OnPauseClicked);

        // Game Over Panel
        gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel");
        gameOverPanel.SetActive(false);

        CreateUIText(gameOverPanel.transform, "GameOverTitle", "GAME OVER",
            new Vector2(0f, 200f), 64, Color.red, FontStyle.Bold);

        gameOverScoreText = CreateUIText(gameOverPanel.transform, "FinalScore", "Score: 0",
            new Vector2(0f, 80f), 40, Color.white, FontStyle.Normal);

        CreateButton(gameOverPanel.transform, "RetryButton", "RETRY",
            new Vector2(0f, -80f), new Vector2(300f, 70f),
            new Color(0.2f, 0.8f, 0.3f), OnRetryClicked);

        CreateButton(gameOverPanel.transform, "MenuButton", "MENU",
            new Vector2(0f, -180f), new Vector2(300f, 70f),
            new Color(0.8f, 0.3f, 0.2f), OnMenuClicked);
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

        // Semi-transparent background for menu/gameover panels
        if (name != "HUDPanel")
        {
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);
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
        rt.sizeDelta = new Vector2(800f, 100f);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = FindWorkingFont();
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // Add outline for readability
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
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

        // Button text
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
        text.fontSize = 30;
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

    // --- UI Callbacks ---

    private void OnPlayClicked()
    {
        StartGame();
    }

    private void OnPauseClicked()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void OnRetryClicked()
    {
        StartGame();
    }

    private void OnMenuClicked()
    {
        ShowMainMenu();
    }

    // --- Game Flow ---

    private void ShowMainMenu()
    {
        isGameOver = false;
        gameStarted = false;
        Time.timeScale = 1f;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (highScoreText != null) highScoreText.text = $"Best: {bestScore}";

        // Reset player position
        if (player != null) player.transform.position = new Vector3(0f, groundY, 0f);

        // Stop track
        if (trackRunner != null) trackRunner.StopTrack();
    }

    private void StartGame()
    {
        gameStarted = true;
        isGameOver = false;
        score = 0;
        coins = 0;
        distanceTraveled = 0f;
        currentSpeed = 10f;
        playTime = 0f;
        currentLane = 0;
        targetX = 0f;
        isJumping = false;
        Time.timeScale = 1f;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (scoreText != null) scoreText.text = "0";
        if (coinText != null) coinText.text = "Coins: 0";

        // Reset player
        if (player != null) player.transform.position = new Vector3(0f, groundY, 0f);

        // Start track generation
        if (trackRunner != null) trackRunner.StartTrack();
    }

    private void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        gameStarted = false;

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverScoreText != null) gameOverScoreText.text = $"Score: {score}\nBest: {bestScore}\nCoins: {coins}";
    }

    // --- Update Loop ---

    private void Update()
    {
        if (!gameStarted || isGameOver) return;

        playTime += Time.deltaTime;

        // Speed ramp
        currentSpeed = Mathf.Lerp(10f, 30f, Mathf.Clamp01(playTime / 120f));

        // Update score
        distanceTraveled += currentSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distanceTraveled);
        if (scoreText != null) scoreText.text = score.ToString();

        // Handle input
        HandleInput();

        // Move player toward target lane
        MovePlayer();

        // Update camera
        UpdateCamera();

        // Update track speed
        if (trackRunner != null) trackRunner.SetSpeed(currentSpeed);

        // Check for collisions with obstacles
        CheckCollisions();
    }

    private void HandleInput()
    {
        // Touch input
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
                        // Horizontal swipe
                        if (delta.x > 0) SwipeRight();
                        else SwipeLeft();
                    }
                    else
                    {
                        // Vertical swipe
                        if (delta.y > 0) SwipeUp();
                        else SwipeDown();
                    }
                }
                else if (delta.magnitude <= swipeThreshold)
                {
                    // Tap - treat as jump for fuzz test compatibility
                    SwipeUp();
                }
            }
        }

        // Keyboard input (for testing)
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
        }
    }

    private void SwipeRight()
    {
        if (currentLane < 1)
        {
            currentLane++;
            targetX = currentLane * laneWidth;
        }
    }

    private void SwipeUp()
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpTimer = 0f;
        }
    }

    private void SwipeDown()
    {
        // Quick roll/slide - just duck the collider briefly
        if (isJumping)
        {
            isJumping = false;
            Vector3 pos = player.transform.position;
            pos.y = groundY;
            player.transform.position = pos;
        }
    }

    private void MovePlayer()
    {
        if (player == null) return;

        Vector3 pos = player.transform.position;

        // Smooth lane change
        pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * 12f);

        // Jump
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / jumpDuration);
            float heightMult = Mathf.Sin(t * Mathf.PI); // Smooth arc
            pos.y = groundY + jumpHeight * heightMult;

            if (t >= 1f)
            {
                isJumping = false;
                pos.y = groundY;
            }
        }
        else
        {
            pos.y = groundY;
        }

        player.transform.position = pos;

        // Simple run animation - bob the player slightly
        float bob = Mathf.Sin(Time.time * 12f) * 0.05f;
        player.transform.localScale = new Vector3(1f, 1f + bob, 1f);
    }

    private void UpdateCamera()
    {
        if (mainCamera == null || player == null) return;

        Vector3 targetCamPos = player.transform.position + new Vector3(0f, 6f, -10f);
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamPos, Time.deltaTime * 5f);
        mainCamera.transform.LookAt(player.transform.position + Vector3.forward * 5f + Vector3.up * 1f);
    }

    private void CheckCollisions()
    {
        if (player == null || trackRunner == null) return;

        // Check distance to obstacles
        foreach (GameObject obs in trackRunner.GetActiveObstacles())
        {
            if (obs == null) continue;
            float dist = Vector3.Distance(player.transform.position, obs.transform.position);
            if (dist < 1.2f && !isJumping)
            {
                // Hit!
                GameOver();
                return;
            }
        }

        // Check coin collection
        foreach (GameObject coin in trackRunner.GetActiveCoins())
        {
            if (coin == null || !coin.activeSelf) continue;
            float dist = Vector3.Distance(player.transform.position, coin.transform.position);
            if (dist < 1.5f)
            {
                coins++;
                if (coinText != null) coinText.text = $"Coins: {coins}";
                coin.SetActive(false);
            }
        }
    }
}
