using UnityEngine;

/// <summary>
/// Represents a single track segment. Holds segment length and biome info.
/// </summary>
public class TrackSegment : MonoBehaviour
{
    public float Length { get; private set; }

    public void Initialize(float length)
    {
        Length = length;
    }
}
