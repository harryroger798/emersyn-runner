using UnityEngine;
using EmersynRunner.Environment;
using EmersynRunner.Effects;

namespace EmersynRunner.Core
{
    /// <summary>
    /// Bootstrap script that initializes the game scene with all Modal-generated assets.
    /// Creates the complete runtime environment including player, track, UI, audio, and effects.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RuntimeAssetLoader assetLoader;
        [SerializeField] private BiomeSetupHelper biomeHelper;
        [SerializeField] private PostProcessingSetup postProcessing;

        [Header("Scene Setup")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private BiomeType startingBiome = BiomeType.City;

        private bool _initialized;

        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_initialized) return;

            EnsureRequiredComponents();
            SetupCamera();
            SetupLighting();

            if (biomeHelper != null)
            {
                biomeHelper.ApplyBiome(startingBiome);
            }

            _initialized = true;
            Debug.Log("[GameBootstrap] Game scene initialized with Modal-generated assets");
        }

        private void EnsureRequiredComponents()
        {
            // Ensure RuntimeAssetLoader exists
            if (assetLoader == null)
            {
                assetLoader = FindObjectOfType<RuntimeAssetLoader>();
                if (assetLoader == null)
                {
                    GameObject loaderObj = new GameObject("RuntimeAssetLoader");
                    assetLoader = loaderObj.AddComponent<RuntimeAssetLoader>();
                }
            }

            // Ensure BiomeSetupHelper exists
            if (biomeHelper == null)
            {
                biomeHelper = FindObjectOfType<BiomeSetupHelper>();
                if (biomeHelper == null)
                {
                    biomeHelper = gameObject.AddComponent<BiomeSetupHelper>();
                }
            }

            // Ensure PostProcessingSetup exists
            if (postProcessing == null)
            {
                postProcessing = FindObjectOfType<PostProcessingSetup>();
                if (postProcessing == null)
                {
                    GameObject ppObj = new GameObject("PostProcessing");
                    postProcessing = ppObj.AddComponent<PostProcessingSetup>();
                }
            }
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            // Configure camera for mobile performance
            mainCam.allowHDR = true;
            mainCam.allowMSAA = false;
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 300f;
            mainCam.fieldOfView = 65f;
        }

        private void SetupLighting()
        {
            // Ensure directional light exists
            Light[] lights = FindObjectsOfType<Light>();
            bool hasDirectional = false;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectional = true;
                    break;
                }
            }

            if (!hasDirectional)
            {
                GameObject sunObj = new GameObject("Directional Light");
                Light sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = Color.white;
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
                sunObj.transform.eulerAngles = new Vector3(50f, -30f, 0f);
            }
        }
    }
}
