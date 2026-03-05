using UnityEngine;

/// <summary>
/// Powerup pickup component. Activates the powerup effect on the player when collected.
/// </summary>
public class PowerupPickup : MonoBehaviour, IPoolable
{
    public enum PowerupType
    {
        Magnet,
        Multiplier,
        Shield
    }

    [SerializeField] private PowerupType powerupType;
    [SerializeField] private float rotateSpeed = 90f;

    private RunnerTuning tuning;

    private void Start()
    {
        tuning = GameManager.Instance.Tuning;
    }

    private void Update()
    {
        // Rotate for visual flair
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    public void Activate(PlayerController player)
    {
        if (tuning == null) tuning = GameManager.Instance.Tuning;

        switch (powerupType)
        {
            case PowerupType.Magnet:
                PowerupManager.Instance.ActivateMagnet(tuning.magnetDuration);
                break;
            case PowerupType.Multiplier:
                PowerupManager.Instance.ActivateMultiplier(tuning.multiplierDuration);
                break;
            case PowerupType.Shield:
                player.ActivateShield(tuning.shieldDuration);
                break;
        }
    }

    public void OnSpawn()
    {
        // Reset rotation
        transform.rotation = Quaternion.identity;
    }

    public void OnDespawn() { }

    private void OnValidate()
    {
        if (gameObject.tag != "PowerUp")
        {
            gameObject.tag = "PowerUp";
        }
    }
}
