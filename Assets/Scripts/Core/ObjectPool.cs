using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic object pooling system for obstacles, pickups, and track segments.
/// Reduces GC pressure and improves performance on mobile.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
    }

    [SerializeField] private List<Pool> pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolLookup;
    private Dictionary<GameObject, string> activeObjectTags;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolLookup = new Dictionary<string, Pool>();
        activeObjectTags = new Dictionary<GameObject, string>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            poolLookup[pool.tag] = pool;

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool);
                objectPool.Enqueue(obj);
            }

            poolDictionary[pool.tag] = objectPool;
        }
    }

    private GameObject CreateNewObject(Pool pool)
    {
        GameObject obj = Instantiate(pool.prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    public GameObject Get(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[ObjectPool] Pool with tag '{tag}' not found.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            // Pool exhausted, create new instance
            if (poolLookup.ContainsKey(tag))
            {
                obj = CreateNewObject(poolLookup[tag]);
            }
            else
            {
                return null;
            }
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        activeObjectTags[obj] = tag;

        // Reset any IPoolable components
        IPoolable[] poolables = obj.GetComponents<IPoolable>();
        foreach (IPoolable poolable in poolables)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;

        // Notify IPoolable components
        IPoolable[] poolables = obj.GetComponents<IPoolable>();
        foreach (IPoolable poolable in poolables)
        {
            poolable.OnDespawn();
        }

        obj.SetActive(false);

        if (activeObjectTags.TryGetValue(obj, out string tag))
        {
            activeObjectTags.Remove(obj);
            if (poolDictionary.ContainsKey(tag))
            {
                poolDictionary[tag].Enqueue(obj);
            }
        }
    }

    public void ReturnAll()
    {
        List<GameObject> toReturn = new List<GameObject>(activeObjectTags.Keys);
        foreach (GameObject obj in toReturn)
        {
            Return(obj);
        }
    }
}

/// <summary>
/// Interface for objects that need setup/cleanup when pooled.
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
