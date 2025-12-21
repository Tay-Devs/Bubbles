using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    
    [Header("Settings")]
    public int audioSourcePoolSize = 10;
    
    [Header("Master Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    private int currentIndex = 0;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        CreatePool();
    }
    
    // Creates a pool of AudioSources for playing sounds.
    private void CreatePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
        }
    }
    
    // Gets the next available AudioSource from the pool.
    private AudioSource GetAvailableSource()
    {
        // Try to find one that's not playing
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            int index = (currentIndex + i) % audioSourcePool.Count;
            if (!audioSourcePool[index].isPlaying)
            {
                currentIndex = (index + 1) % audioSourcePool.Count;
                return audioSourcePool[index];
            }
        }
        
        // All are playing, use the next one anyway (will cut off oldest)
        AudioSource source = audioSourcePool[currentIndex];
        currentIndex = (currentIndex + 1) % audioSourcePool.Count;
        return source;
    }
    
    // ============================================
    // STATIC PLAY METHODS (Easy to call from anywhere)
    // ============================================
    
    // Play an SFX using SFXData settings.
    public static void Play(SFXData sfxData)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SFXManager] No instance found!");
            return;
        }
        
        Instance.PlaySound(sfxData);
    }
    
    // Play an SFX at a specific position (3D sound).
    public static void PlayAtPosition(SFXData sfxData, Vector3 position)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SFXManager] No instance found!");
            return;
        }
        
        Instance.PlaySoundAtPosition(sfxData, position);
    }
    
    // Play a one-off AudioClip with default settings.
    public static void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[SFXManager] No instance found!");
            return;
        }
        
        Instance.PlayOneShot(clip, volume);
    }
    
    // ============================================
    // INSTANCE METHODS
    // ============================================
    
    // Plays an SFX using the SFXData configuration.
    public void PlaySound(SFXData sfxData)
    {
        if (sfxData == null) return;
        
        AudioClip clip = sfxData.GetClip();
        if (clip == null) return;
        
        AudioSource source = GetAvailableSource();
        ConfigureSource(source, sfxData);
        source.clip = clip;
        source.Play();
    }
    
    // Plays an SFX at a world position using AudioSource.PlayClipAtPoint style.
    public void PlaySoundAtPosition(SFXData sfxData, Vector3 position)
    {
        if (sfxData == null) return;
        
        AudioClip clip = sfxData.GetClip();
        if (clip == null) return;
        
        // Create temporary object at position
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        
        AudioSource source = tempGO.AddComponent<AudioSource>();
        ConfigureSource(source, sfxData);
        source.spatialBlend = 1f; // Force 3D for positional
        source.clip = clip;
        source.Play();
        
        Destroy(tempGO, clip.length / sfxData.GetPitch() + 0.1f);
    }
    
    // Plays a simple AudioClip.
    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        
        AudioSource source = GetAvailableSource();
        source.pitch = 1f;
        source.volume = volume * masterVolume;
        source.spatialBlend = 0f;
        source.loop = false;
        source.PlayOneShot(clip);
    }
    
    // Configures an AudioSource with SFXData settings.
    private void ConfigureSource(AudioSource source, SFXData sfxData)
    {
        source.volume = sfxData.volume * masterVolume;
        source.pitch = sfxData.GetPitch();
        source.spatialBlend = sfxData.spatialBlend;
        source.priority = sfxData.priority;
        source.loop = sfxData.loop;
    }
    
    // ============================================
    // UTILITY METHODS
    // ============================================
    
    // Stops all currently playing sounds.
    public static void StopAll()
    {
        if (Instance == null) return;
        
        foreach (var source in Instance.audioSourcePool)
        {
            source.Stop();
        }
    }
    
    // Sets the master volume.
    public static void SetMasterVolume(float volume)
    {
        if (Instance == null) return;
        Instance.masterVolume = Mathf.Clamp01(volume);
    }
}