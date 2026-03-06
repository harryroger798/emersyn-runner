using UnityEngine;

/// <summary>
/// Coin pickup. Rotates and bobs for visual appeal. Collected on trigger.
/// </summary>
public class CoinPickup : MonoBehaviour, IPoolable
{
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobFrequency = 2f;

    private Vector3 startPos;
    private float bobOffset;

    public void OnSpawn()
    {
        startPos = transform.localPosition;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    public void OnDespawn() { }

    private void Update()
    {
        // Rotate
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Bob up and down
        Vector3 pos = startPos;
        pos.y += Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;
        transform.localPosition = pos;
    }

    private void OnValidate()
    {
        if (gameObject.tag != "Coin")
        {
            gameObject.tag = "Coin";
        }
    }
}
