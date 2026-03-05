using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI screens: main menu, HUD, pause, game over, revive, settings, shop.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject hudScreen;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject reviveScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject shopScreen;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image magnetIcon;
    [SerializeField] private Image shieldIcon;

    [Header("Main Menu")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopButton;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseMenuButton;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalCoinsText;
    [SerializeField] private TextMeshProUGUI finalHighScoreText;
    [SerializeField] private Button reviveButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button gameOverMenuButton;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle hapticsToggle;
    [SerializeField] private Button settingsBackButton;

    [Header("Shop")]
    [SerializeField] private Button shopBackButton;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSFX;

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
        SetupButtons();
        SetupSettings();

        // Subscribe to game events
        GameManager.Instance.OnGameStart += OnGameStart;
        GameManager.Instance.OnGamePause += OnGamePause;
        GameManager.Instance.OnGameResume += OnGameResume;
        GameManager.Instance.OnGameOver += OnGameOver;
        GameManager.Instance.OnScoreChanged += OnScoreChanged;
        GameManager.Instance.OnCoinCollected += OnCoinCollected;
        GameManager.Instance.OnRevive += OnRevive;

        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart -= OnGameStart;
            GameManager.Instance.OnGamePause -= OnGamePause;
            GameManager.Instance.OnGameResume -= OnGameResume;
            GameManager.Instance.OnGameOver -= OnGameOver;
            GameManager.Instance.OnScoreChanged -= OnScoreChanged;
            GameManager.Instance.OnCoinCollected -= OnCoinCollected;
            GameManager.Instance.OnRevive -= OnRevive;
        }
    }

    private void SetupButtons()
    {
        // Main menu
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (shopButton != null) shopButton.onClick.AddListener(OnShopClicked);

        // Pause
        if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (pauseMenuButton != null) pauseMenuButton.onClick.AddListener(OnMenuClicked);

        // Game over
        if (reviveButton != null) reviveButton.onClick.AddListener(OnReviveClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (gameOverMenuButton != null) gameOverMenuButton.onClick.AddListener(OnMenuClicked);

        // Settings
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

        // Shop
        if (shopBackButton != null) shopBackButton.onClick.AddListener(OnShopBackClicked);
    }

    private void SetupSettings()
    {
        if (AudioManager.Instance == null) return;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
            musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
        if (hapticsToggle != null && HapticManager.Instance != null)
        {
            hapticsToggle.isOn = HapticManager.Instance.IsEnabled();
            hapticsToggle.onValueChanged.AddListener(HapticManager.Instance.SetEnabled);
        }
    }

    #region Screen Management

    private void HideAllScreens()
    {
        if (mainMenuScreen != null) mainMenuScreen.SetActive(false);
        if (hudScreen != null) hudScreen.SetActive(false);
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (reviveScreen != null) reviveScreen.SetActive(false);
        if (settingsScreen != null) settingsScreen.SetActive(false);
        if (shopScreen != null) shopScreen.SetActive(false);
    }

    private void ShowMainMenu()
    {
        HideAllScreens();
        if (mainMenuScreen != null) mainMenuScreen.SetActive(true);
        if (highScoreText != null) highScoreText.text = $"Best: {GameManager.Instance.HighScore}";
    }

    private void ShowHUD()
    {
        HideAllScreens();
        if (hudScreen != null) hudScreen.SetActive(true);
        if (scoreText != null) scoreText.text = "0";
        if (coinText != null) coinText.text = "0";
        if (multiplierText != null) multiplierText.gameObject.SetActive(false);
        if (magnetIcon != null) magnetIcon.gameObject.SetActive(false);
        if (shieldIcon != null) shieldIcon.gameObject.SetActive(false);
    }

    private void ShowGameOver()
    {
        if (hudScreen != null) hudScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"Score: {GameManager.Instance.Score}";
        if (finalCoinsText != null) finalCoinsText.text = $"Coins: {GameManager.Instance.Coins}";
        if (finalHighScoreText != null) finalHighScoreText.text = $"Best: {GameManager.Instance.HighScore}";

        // Show revive button only if available
        if (reviveButton != null) reviveButton.gameObject.SetActive(GameManager.Instance.CanRevive());
    }

    #endregion

    #region Button Handlers

    private void OnPlayClicked()
    {
        PlayButtonSound();
        GameManager.Instance.StartGame();
    }

    private void OnPauseClicked()
    {
        PlayButtonSound();
        GameManager.Instance.PauseGame();
    }

    private void OnResumeClicked()
    {
        PlayButtonSound();
        GameManager.Instance.ResumeGame();
    }

    private void OnMenuClicked()
    {
        PlayButtonSound();
        GameManager.Instance.ReturnToMenu();
        ShowMainMenu();
    }

    private void OnReviveClicked()
    {
        PlayButtonSound();
        GameManager.Instance.Revive();
    }

    private void OnRetryClicked()
    {
        PlayButtonSound();
        GameManager.Instance.StartGame();
    }

    private void OnSettingsClicked()
    {
        PlayButtonSound();
        HideAllScreens();
        if (settingsScreen != null) settingsScreen.SetActive(true);
    }

    private void OnSettingsBackClicked()
    {
        PlayButtonSound();
        ShowMainMenu();
    }

    private void OnShopClicked()
    {
        PlayButtonSound();
        HideAllScreens();
        if (shopScreen != null) shopScreen.SetActive(true);
    }

    private void OnShopBackClicked()
    {
        PlayButtonSound();
        ShowMainMenu();
    }

    private void PlayButtonSound()
    {
        if (buttonClickSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISFX(buttonClickSFX);
        }
    }

    #endregion

    #region Game Events

    private void OnGameStart()
    {
        ShowHUD();
    }

    private void OnGamePause()
    {
        if (pauseScreen != null) pauseScreen.SetActive(true);
    }

    private void OnGameResume()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    private void OnGameOver()
    {
        ShowGameOver();
    }

    private void OnScoreChanged(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    private void OnCoinCollected(int totalCoins)
    {
        if (coinText != null) coinText.text = totalCoins.ToString();
    }

    private void OnRevive()
    {
        HideAllScreens();
        ShowHUD();
    }

    #endregion
}
