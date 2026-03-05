using UnityEngine;
using System.Collections;

/// <summary>
/// Manages active powerup states: magnet, multiplier, shield.
/// </summary>
public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance { get; private set; }

    public bool IsMagnetActive { get; private set; }
    public bool IsMultiplierActive { get; private set; }

    private RunnerTuning tuning;
    private Coroutine magnetCoroutine;
    private Coroutine multiplierCoroutine;

    public event System.Action<bool> OnMagnetChanged;
    public event System.Action<bool> OnMultiplierChanged;

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
        GameManager.Instance.OnGameStart += ResetPowerups;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStart -= ResetPowerups;
    }

    private void Update()
    {
        if (!IsMagnetActive) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Attract nearby coins
        AttractCoins();
    }

    private void AttractCoins()
    {
        Collider[] coins = Physics.OverlapSphere(
            transform.position, tuning.magnetRadius, LayerMask.GetMask("Pickup"));

        foreach (Collider coin in coins)
        {
            if (coin.CompareTag("Coin"))
            {
                Vector3 dir = (transform.position - coin.transform.position).normalized;
                coin.transform.position += dir * tuning.magnetSpeed * Time.deltaTime;
            }
        }
    }

    public void ActivateMagnet(float duration)
    {
        if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
        magnetCoroutine = StartCoroutine(MagnetCoroutine(duration));
    }

    private IEnumerator MagnetCoroutine(float duration)
    {
        IsMagnetActive = true;
        OnMagnetChanged?.Invoke(true);
        yield return new WaitForSeconds(duration);
        IsMagnetActive = false;
        OnMagnetChanged?.Invoke(false);
    }

    public void ActivateMultiplier(float duration)
    {
        if (multiplierCoroutine != null) StopCoroutine(multiplierCoroutine);
        multiplierCoroutine = StartCoroutine(MultiplierCoroutine(duration));
    }

    private IEnumerator MultiplierCoroutine(float duration)
    {
        IsMultiplierActive = true;
        GameManager.Instance.ScoreMultiplier = 2;
        OnMultiplierChanged?.Invoke(true);
        yield return new WaitForSeconds(duration);
        IsMultiplierActive = false;
        GameManager.Instance.ScoreMultiplier = 1;
        OnMultiplierChanged?.Invoke(false);
    }

    private void ResetPowerups()
    {
        if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
        if (multiplierCoroutine != null) StopCoroutine(multiplierCoroutine);
        IsMagnetActive = false;
        IsMultiplierActive = false;
        GameManager.Instance.ScoreMultiplier = 1;
    }
}
