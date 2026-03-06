using UnityEngine;

/// <summary>
/// Bootstraps the main scene by ensuring all required managers exist.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("Manager Prefabs (assign in Inspector or will auto-create)")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject audioManagerPrefab;
    [SerializeField] private GameObject uiManagerPrefab;

    private void Awake()
    {
        EnsureManager<GameManager>("GameManager", gameManagerPrefab);
        EnsureManager<AudioManager>("AudioManager", audioManagerPrefab);
        EnsureManager<SwipeInput>("SwipeInput", null);
        EnsureManager<ObjectPool>("ObjectPool", null);
        EnsureManager<PowerupManager>("PowerupManager", null);
        EnsureManager<HapticManager>("HapticManager", null);
    }

    private void EnsureManager<T>(string name, GameObject prefab) where T : MonoBehaviour
    {
        if (FindObjectOfType<T>() != null) return;

        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<T>();
        }
    }
}
