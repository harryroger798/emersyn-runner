using UnityEngine;

namespace EmersynRunner.Environment
{
    /// <summary>
    /// Spawns decorative props alongside track segments based on biome config.
    /// Uses object pooling for performance.
    /// </summary>
    public class PropSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float trackHalfWidth = 5f;
        [SerializeField] private float propOffsetMin = 4f;
        [SerializeField] private float propOffsetMax = 8f;
        [SerializeField] private float propHeightVariation = 0.2f;

        /// <summary>
        /// Spawn props along a segment based on current biome.
        /// </summary>
        public void SpawnPropsForSegment(Vector3 segmentStart, float segmentLength, BiomeConfig biome)
        {
            if (biome == null || biome.propPrefabs == null || biome.propPrefabs.Length == 0)
                return;

            float spacing = Mathf.Lerp(biome.propMaxDistance, biome.propMinDistance, biome.propDensity);
            if (spacing <= 0f) spacing = 10f;

            float z = segmentStart.z;
            float endZ = z + segmentLength;

            while (z < endZ)
            {
                // Left side
                if (Random.value < biome.propDensity)
                {
                    SpawnProp(biome, new Vector3(
                        -(trackHalfWidth + Random.Range(propOffsetMin, propOffsetMax)),
                        Random.Range(-propHeightVariation, propHeightVariation),
                        z + Random.Range(-1f, 1f)
                    ));
                }

                // Right side
                if (Random.value < biome.propDensity)
                {
                    SpawnProp(biome, new Vector3(
                        trackHalfWidth + Random.Range(propOffsetMin, propOffsetMax),
                        Random.Range(-propHeightVariation, propHeightVariation),
                        z + Random.Range(-1f, 1f)
                    ));
                }

                z += spacing;
            }
        }

        private void SpawnProp(BiomeConfig biome, Vector3 position)
        {
            int index = Random.Range(0, biome.propPrefabs.Length);
            GameObject prefab = biome.propPrefabs[index];
            if (prefab == null) return;

            GameObject prop = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            float scale = Random.Range(0.8f, 1.2f);
            prop.transform.localScale = Vector3.one * scale;
            prop.transform.SetParent(transform);
        }

        /// <summary>
        /// Destroy all child props (for cleanup on segment despawn).
        /// </summary>
        public void ClearProps()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
