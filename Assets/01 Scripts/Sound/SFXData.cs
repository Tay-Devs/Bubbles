using UnityEngine;

[CreateAssetMenu(fileName = "SFX_", menuName = "Audio/SFX Data")]
public class SFXData : ScriptableObject
{
    [Header("Audio Clips")]
    [Tooltip("If multiple clips, one will be chosen randomly")]
    public AudioClip[] clips;
    
    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("Pitch Variation")]
    public float minPitch = 1f;
    public float maxPitch = 1f;
    
    [Header("Spatial Settings")]
    [Range(0f, 1f)]
    [Tooltip("0 = 2D, 1 = 3D")]
    public float spatialBlend = 0f;
    
    [Header("Advanced")]
    [Range(0, 256)]
    public int priority = 128;
    public bool loop = false;
    
    // Returns a random clip from the array.
    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
    
    // Returns a random pitch within the range.
    public float GetPitch()
    {
        return Random.Range(minPitch, maxPitch);
    }
}