using UnityEngine;
using UnityEngine.Rendering;

namespace EmersynRunner.Core
{
    /// <summary>
    /// Loads Modal-generated assets at runtime and creates materials/prefabs dynamically.
    /// This handles the integration of generated textures, audio, and models into Unity.
    /// </summary>
    public class RuntimeAssetLoader : MonoBehaviour
    {
        [Header("Asset Paths (Resources folder relative)")]
        private const string TexturePath = "Textures/Generated/";
        private const string AudioPath = "Audio/";
        private const string ModelPath = "Models/Generated/";

        [Header("Generated Materials")]
        public Material playerMaterial;
        public Material cityGroundMat;
        public Material cityWallMat;
        public Material jungleGroundMat;
        public Material jungleWallMat;
        public Material candyGroundMat;
        public Material candyWallMat;

        private static RuntimeAssetLoader _instance;
        public static RuntimeAssetLoader Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            CreateMaterials();
        }

        /// <summary>
        /// Creates PBR materials from generated textures at runtime.
        /// Uses URP/Lit shader for proper PBR rendering.
        /// </summary>
        private void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
            {
                Debug.LogWarning("URP Lit shader not found, falling back to Standard");
                litShader = Shader.Find("Standard");
            }

            // Player material
            playerMaterial = CreateMaterialFromTexture(litShader, "player/emersyn_diffuse", "player/emersyn_normal", "Emersyn_Mat");

            // City biome materials
            cityGroundMat = CreateMaterialFromTexture(litShader, "environment/city_ground", null, "City_Ground_Mat");
            cityWallMat = CreateMaterialFromTexture(litShader, "environment/city_wall", null, "City_Wall_Mat");

            // Jungle biome materials
            jungleGroundMat = CreateMaterialFromTexture(litShader, "environment/jungle_ground", null, "Jungle_Ground_Mat");
            jungleWallMat = CreateMaterialFromTexture(litShader, "environment/jungle_wall", null, "Jungle_Wall_Mat");

            // Candy biome materials
            candyGroundMat = CreateMaterialFromTexture(litShader, "environment/candy_ground", null, "Candy_Ground_Mat");
            candyWallMat = CreateMaterialFromTexture(litShader, "environment/candy_wall", null, "Candy_Wall_Mat");

            Debug.Log("[RuntimeAssetLoader] Created all materials from generated textures");
        }

        private Material CreateMaterialFromTexture(Shader shader, string diffusePath, string normalPath, string matName)
        {
            Material mat = new Material(shader);
            mat.name = matName;

            Texture2D diffuse = Resources.Load<Texture2D>(TexturePath + diffusePath);
            if (diffuse != null)
            {
                mat.SetTexture("_BaseMap", diffuse);
                mat.SetTexture("_MainTex", diffuse);
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                Texture2D normal = Resources.Load<Texture2D>(TexturePath + normalPath);
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_Metallic", 0.0f);

            return mat;
        }

        /// <summary>
        /// Creates a skybox material from a generated panoramic texture.
        /// </summary>
        public Material CreateSkyboxMaterial(string skyboxTextureName)
        {
            Shader skyboxShader = Shader.Find("Skybox/Panoramic");
            if (skyboxShader == null)
            {
                skyboxShader = Shader.Find("Skybox/6 Sided");
            }

            Material mat = new Material(skyboxShader);
            Texture2D tex = Resources.Load<Texture2D>(TexturePath + "skybox/" + skyboxTextureName);
            if (tex != null)
            {
                mat.SetTexture("_MainTex", tex);
            }

            return mat;
        }

        /// <summary>
        /// Loads an audio clip from the generated audio assets.
        /// </summary>
        public AudioClip LoadAudioClip(string category, string clipName)
        {
            string path = AudioPath + category + "/" + clipName;
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[RuntimeAssetLoader] Audio clip not found: {path}");
            }
            return clip;
        }

        /// <summary>
        /// Creates a simple mesh-based GameObject from a generated OBJ model.
        /// </summary>
        public GameObject CreateModelInstance(string modelPath, Material material, string objectName)
        {
            Mesh mesh = Resources.Load<Mesh>(ModelPath + modelPath);
            if (mesh == null)
            {
                Debug.LogWarning($"[RuntimeAssetLoader] Model not found: {modelPath}");
                return CreateFallbackPrimitive(objectName, material);
            }

            GameObject obj = new GameObject(objectName);
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = mesh;
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = material ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));

            return obj;
        }

        private GameObject CreateFallbackPrimitive(string name, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name + "_Fallback";
            if (material != null)
            {
                obj.GetComponent<MeshRenderer>().material = material;
            }
            return obj;
        }
    }
}
