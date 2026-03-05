using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// Central game state manager. Controls game flow: menu, playing, paused, game over.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver }

    [Header("References")]
    [SerializeField] private RunnerTuning tuning;

    public RunnerTuning Tuning => tuning;
    public GameState CurrentState { get; private set; } = GameState.Menu;

    // Score
    public int Score { get; private set; }
    public int Coins { get; private set; }
    public int HighScore { get; private set; }
    public float DistanceTraveled { get; private set; }
    public int ScoreMultiplier { get; set; } = 1;

    // Speed
    public float CurrentSpeed { get; private set; }
    private float playTime;

    // Events
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnCoinCollected;
    public event Action OnRevive;

    private bool hasRevived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        HighScore = PlayerPrefs.GetInt("HighScore", 0);

        if (tuning == null)
        {
            tuning = Resources.Load<RunnerTuning>("RunnerTuning");
        }
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        playTime += Time.deltaTime;
        UpdateSpeed();
        UpdateScore();
    }

    private void UpdateSpeed()
    {
        float t = Mathf.Clamp01(playTime / tuning.timeToMaxSpeed);
        float curveValue = tuning.accelerationCurve.Evaluate(t);
        CurrentSpeed = Mathf.Lerp(tuning.startSpeed, tuning.maxSpeed, curveValue);
    }

    private void UpdateScore()
    {
        DistanceTraveled += CurrentSpeed * Time.deltaTime;
        int newScore = Mathf.FloorToInt(DistanceTraveled) * ScoreMultiplier;
        if (newScore != Score)
        {
            Score = newScore;
            OnScoreChanged?.Invoke(Score);
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        Score = 0;
        Coins = 0;
        DistanceTraveled = 0f;
        playTime = 0f;
        ScoreMultiplier = 1;
        hasRevived = false;
        CurrentSpeed = tuning.startSpeed;
        OnGameStart?.Invoke();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        OnGamePause?.Invoke();
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        OnGameResume?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }

        OnGameOver?.Invoke();
    }

    public bool CanRevive()
    {
        return !hasRevived;
    }

    public void Revive()
    {
        if (!CanRevive()) return;
        hasRevived = true;
        CurrentState = GameState.Playing;
        OnRevive?.Invoke();
    }

    public void AddCoins(int amount)
    {
        Coins += amount * ScoreMultiplier;
        OnCoinCollected?.Invoke(Coins);
    }

    public void ReturnToMenu()
    {
        CurrentState = GameState.Menu;
        Time.timeScale = 1f;
    }
}
