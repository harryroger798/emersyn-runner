using UnityEngine;
using UnityEngine.Rendering;
using EmersynRunner.Core;

namespace EmersynRunner.Environment
{
    /// <summary>
    /// Helper that applies biome visuals at runtime using Modal-generated assets.
    /// Creates materials from generated textures and applies skyboxes, fog, lighting.
    /// </summary>
    public class BiomeSetupHelper : MonoBehaviour
    {
        [Header("Biome Configurations")]
        [SerializeField] private BiomeConfig cityConfig;
        [SerializeField] private BiomeConfig jungleConfig;
        [SerializeField] private BiomeConfig candyConfig;

        private BiomeType _currentBiome = BiomeType.City;

        private void Start()
        {
            ApplyBiome(_currentBiome);
        }

        /// <summary>
        /// Applies all visual settings for the specified biome using generated assets.
        /// </summary>
        public void ApplyBiome(BiomeType biomeType)
        {
            _currentBiome = biomeType;
            RuntimeAssetLoader loader = RuntimeAssetLoader.Instance;
            if (loader == null)
            {
                Debug.LogWarning("[BiomeSetupHelper] RuntimeAssetLoader not found");
                return;
            }

            switch (biomeType)
            {
                case BiomeType.City:
                    ApplyCityBiome(loader);
                    break;
                case BiomeType.Jungle:
                    ApplyJungleBiome(loader);
                    break;
                case BiomeType.Candy:
                    ApplyCandyBiome(loader);
                    break;
            }

            Debug.Log($"[BiomeSetupHelper] Applied biome: {biomeType}");
        }

        private void ApplyCityBiome(RuntimeAssetLoader loader)
        {
            // Apply city skybox
            Material skybox = loader.CreateSkyboxMaterial("city_skybox");
            RenderSettings.skybox = skybox;

            // Apply city lighting
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.7f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.75f, 0.8f);
            RenderSettings.fogDensity = 0.01f;
            RenderSettings.fogMode = FogMode.Exponential;

            // Set directional light
            SetSunLight(Color.white, 1.2f, new Vector3(50f, -30f, 0f));
        }

        private void ApplyJungleBiome(RuntimeAssetLoader loader)
        {
            Material skybox = loader.CreateSkyboxMaterial("jungle_skybox");
            RenderSettings.skybox = skybox;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.55f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.3f, 0.5f, 0.25f);
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogMode = FogMode.Exponential;

            SetSunLight(new Color(1f, 0.95f, 0.8f), 1.0f, new Vector3(60f, -45f, 0f));
        }

        private void ApplyCandyBiome(RuntimeAssetLoader loader)
        {
            Material skybox = loader.CreateSkyboxMaterial("candy_skybox");
            RenderSettings.skybox = skybox;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.8f, 0.7f, 0.85f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(1f, 0.85f, 0.9f);
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogMode = FogMode.Exponential;

            SetSunLight(new Color(1f, 0.9f, 0.95f), 1.3f, new Vector3(45f, -20f, 0f));
        }

        private void SetSunLight(Color color, float intensity, Vector3 rotation)
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                // Find or create directional light
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sun = light;
                        break;
                    }
                }

                if (sun == null)
                {
                    GameObject sunObj = new GameObject("Directional Light");
                    sun = sunObj.AddComponent<Light>();
                    sun.type = LightType.Directional;
                }
            }

            sun.color = color;
            sun.intensity = intensity;
            sun.transform.eulerAngles = rotation;
            sun.shadows = LightShadows.Soft;
        }

        public BiomeType CurrentBiome => _currentBiome;
    }
}
