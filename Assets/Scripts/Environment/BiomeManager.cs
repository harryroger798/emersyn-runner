using UnityEngine;

namespace EmersynRunner.Environment
{
    /// <summary>
    /// Manages biome transitions and applies biome visual settings.
    /// Rotates through biomes as the player progresses.
    /// </summary>
    public class BiomeManager : MonoBehaviour
    {
        public static BiomeManager Instance { get; private set; }

        [Header("Biome Configs")]
        [SerializeField] private BiomeConfig[] biomes;
        [SerializeField] private int segmentsPerBiome = 15;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 2f;

        private int currentBiomeIndex;
        private int segmentsSinceLastChange;
        private BiomeConfig currentBiome;
        private Light directionalLight;
        private AudioSource ambientSource;

        // Transition state
        private bool isTransitioning;
        private float transitionTimer;
        private Color targetAmbientColor;
        private Color targetFogColor;
        private float targetFogDensity;

        public BiomeConfig CurrentBiome => currentBiome;
        public event System.Action<BiomeConfig> OnBiomeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;
        }

        private void Start()
        {
            // Find directional light
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (biomes != null && biomes.Length > 0)
            {
                ApplyBiome(biomes[0], instant: true);
            }
        }

        private void Update()
        {
            if (!isTransitioning) return;

            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Lerp ambient and fog
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbientColor, smooth);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetFogColor, smooth);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFogDensity, smooth);

            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }

        /// <summary>
        /// Called by TrackManager each time a new segment is spawned.
        /// </summary>
        public void OnSegmentSpawned()
        {
            segmentsSinceLastChange++;

            if (segmentsSinceLastChange >= segmentsPerBiome)
            {
                segmentsSinceLastChange = 0;
                AdvanceBiome();
            }
        }

        /// <summary>
        /// Move to the next biome in the rotation.
        /// </summary>
        public void AdvanceBiome()
        {
            if (biomes == null || biomes.Length == 0) return;

            currentBiomeIndex = (currentBiomeIndex + 1) % biomes.Length;
            ApplyBiome(biomes[currentBiomeIndex], instant: false);
        }

        /// <summary>
        /// Apply a biome's visual settings.
        /// </summary>
        private void ApplyBiome(BiomeConfig biome, bool instant)
        {
            currentBiome = biome;

            if (instant)
            {
                RenderSettings.ambientLight = biome.ambientColor;
                RenderSettings.fogColor = biome.fogColor;
                RenderSettings.fogDensity = biome.fogDensity;

                if (biome.skyboxMaterial != null)
                {
                    RenderSettings.skybox = biome.skyboxMaterial;
                }
            }
            else
            {
                targetAmbientColor = biome.ambientColor;
                targetFogColor = biome.fogColor;
                targetFogDensity = biome.fogDensity;
                transitionTimer = 0f;
                isTransitioning = true;

                if (biome.skyboxMaterial != null)
                {
                    RenderSettings.skybox = biome.skyboxMaterial;
                }
            }

            // Apply directional light
            if (directionalLight != null)
            {
                directionalLight.color = biome.sunColor;
                directionalLight.intensity = biome.sunIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(biome.sunRotation);
            }

            // Apply ambient audio
            if (biome.ambientLoop != null)
            {
                ambientSource.clip = biome.ambientLoop;
                ambientSource.volume = biome.ambientVolume;
                ambientSource.Play();
            }
            else
            {
                ambientSource.Stop();
            }

            RenderSettings.fog = true;

            OnBiomeChanged?.Invoke(biome);
        }

        /// <summary>
        /// Reset to first biome (e.g., on game restart).
        /// </summary>
        public void ResetBiome()
        {
            currentBiomeIndex = 0;
            segmentsSinceLastChange = 0;
            if (biomes != null && biomes.Length > 0)
            {
                ApplyBiome(biomes[0], instant: true);
            }
        }
    }
}
