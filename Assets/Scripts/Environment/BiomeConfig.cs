using UnityEngine;

namespace EmersynRunner.Environment
{
    /// <summary>
    /// Defines visual and gameplay properties for a single biome.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiome", menuName = "Emersyn Runner/Biome Config")]
    public class BiomeConfig : ScriptableObject
    {
        [Header("Identity")]
        public string biomeName = "City";
        public BiomeType biomeType = BiomeType.City;

        [Header("Track Visuals")]
        public Material groundMaterial;
        public Material wallMaterial;
        public Material railMaterial;
        public Color ambientColor = new Color(0.6f, 0.65f, 0.7f);
        public Color fogColor = new Color(0.7f, 0.75f, 0.8f);
        public float fogDensity = 0.01f;

        [Header("Lighting")]
        public Color sunColor = Color.white;
        public float sunIntensity = 1.2f;
        public Vector3 sunRotation = new Vector3(50f, -30f, 0f);

        [Header("Skybox")]
        public Material skyboxMaterial;

        [Header("Props")]
        [Tooltip("Prefabs spawned alongside the track as decoration.")]
        public GameObject[] propPrefabs;
        [Range(0f, 1f)]
        public float propDensity = 0.5f;
        public float propMinDistance = 5f;
        public float propMaxDistance = 15f;

        [Header("Obstacles")]
        [Tooltip("Override obstacle prefabs for this biome (optional). Falls back to default if empty.")]
        public GameObject[] obstacleOverrides;

        [Header("Audio")]
        [Tooltip("Optional ambient sound loop for this biome.")]
        public AudioClip ambientLoop;
        [Range(0f, 1f)]
        public float ambientVolume = 0.3f;

        [Header("Post-Processing")]
        [Tooltip("Color grading temperature shift for biome mood.")]
        [Range(-50f, 50f)]
        public float colorTemperature = 0f;
        [Range(-50f, 50f)]
        public float colorTint = 0f;
        [Range(0f, 1f)]
        public float bloomIntensity = 0.3f;
        [Range(0f, 1f)]
        public float vignetteIntensity = 0.25f;
    }

    public enum BiomeType
    {
        City = 0,
        Jungle = 1,
        Candy = 2,
    }
}
