using UnityEngine;
using System.Collections.Generic;

namespace EmersynRunner.Effects
{
    /// <summary>
    /// Manages particle effects for coins, powerups, hits, and trails.
    /// Pools particle systems for performance.
    /// </summary>
    public class ParticleEffectManager : MonoBehaviour
    {
        public static ParticleEffectManager Instance { get; private set; }

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject coinCollectEffect;
        [SerializeField] private GameObject powerupCollectEffect;
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject shieldActivateEffect;
        [SerializeField] private GameObject magnetFieldEffect;
        [SerializeField] private GameObject trailEffect;

        [Header("Pool Settings")]
        [SerializeField] private int poolSizePerEffect = 5;

        private readonly Dictionary<string, Queue<ParticleSystem>> pools = new Dictionary<string, Queue<ParticleSystem>>();

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
            InitPool("coin", coinCollectEffect);
            InitPool("powerup", powerupCollectEffect);
            InitPool("hit", hitEffect);
            InitPool("shield", shieldActivateEffect);
            InitPool("magnet", magnetFieldEffect);
        }

        private void InitPool(string key, GameObject prefab)
        {
            if (prefab == null) return;

            Queue<ParticleSystem> queue = new Queue<ParticleSystem>();
            for (int i = 0; i < poolSizePerEffect; i++)
            {
                GameObject go = Instantiate(prefab, transform);
                go.SetActive(false);
                ParticleSystem ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    queue.Enqueue(ps);
                }
            }
            pools[key] = queue;
        }

        public void PlayEffect(string key, Vector3 position)
        {
            if (!pools.ContainsKey(key) || pools[key].Count == 0) return;

            ParticleSystem ps = pools[key].Dequeue();
            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Play();

            // Return to pool after duration
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            StartCoroutine(ReturnToPool(key, ps, duration));
        }

        private System.Collections.IEnumerator ReturnToPool(string key, ParticleSystem ps, float delay)
        {
            yield return new WaitForSeconds(delay);
            ps.Stop();
            ps.gameObject.SetActive(false);
            if (pools.ContainsKey(key))
            {
                pools[key].Enqueue(ps);
            }
        }

        public void PlayCoinCollect(Vector3 position) => PlayEffect("coin", position);
        public void PlayPowerupCollect(Vector3 position) => PlayEffect("powerup", position);
        public void PlayHit(Vector3 position) => PlayEffect("hit", position);
        public void PlayShieldActivate(Vector3 position) => PlayEffect("shield", position);
    }
}
