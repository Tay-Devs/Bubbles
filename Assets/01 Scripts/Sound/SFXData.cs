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
    
    [Header("Pitch Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 1.5f;
    
    [Header("Pitch Mode")]
    public PitchMode pitchMode = PitchMode.Random;
    [Tooltip("How fast pitch increases in Combo mode (higher = faster curve)")]
    [Range(0.1f, 1f)]
    public float comboRate = 0.3f;
    
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
    public float GetRandomPitch()
    {
        return Random.Range(minPitch, maxPitch);
    }
    
    // Returns pitch based on combo index (0 = first, 1 = second, etc).
    // Uses exponential curve: starts at minPitch, approaches maxPitch asymptotically.
    public float GetComboPitch(int comboIndex)
    {
        // Exponential approach: 1 - e^(-rate * index)
        // This gives 0 at index 0, and approaches 1 as index increases
        float t = 1f - Mathf.Exp(-comboRate * comboIndex);
        return Mathf.Lerp(minPitch, maxPitch, t);
    }
    
    // Returns appropriate pitch based on mode and combo index.
    public float GetPitch(int comboIndex = 0)
    {
        return pitchMode switch
        {
            PitchMode.Random => GetRandomPitch(),
            PitchMode.Combo => GetComboPitch(comboIndex),
            PitchMode.Fixed => minPitch,
            _ => minPitch
        };
    }
}

public enum PitchMode
{
    Random, // Random pitch between min and max
    Combo,  // Pitch increases with combo index
    Fixed   // Always uses minPitch
}