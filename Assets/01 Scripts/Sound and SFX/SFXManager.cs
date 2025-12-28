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
    [Header("SFX Toggle")]
    [SerializeField] private bool sfxEnabled = true;
    
    public bool SFXEnabled
    {
        get => sfxEnabled;
        set => sfxEnabled = value;
    }
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
    
    private void CreatePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Add(source);
        }
    }
    
    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < audioSourcePool.Count; i++)
        {
            int index = (currentIndex + i) % audioSourcePool.Count;
            if (!audioSourcePool[index].isPlaying)
            {
                currentIndex = (index + 1) % audioSourcePool.Count;
                return audioSourcePool[index];
            }
        }
        
        AudioSource source = audioSourcePool[currentIndex];
        currentIndex = (currentIndex + 1) % audioSourcePool.Count;
        return source;
    }
    
    // ============================================
    // STATIC PLAY METHODS
    // ============================================
    
    // Play SFX with default settings.
    public static void Play(SFXData sfxData)
    {
        if (Instance == null || !Instance.sfxEnabled) return;
        Instance.PlaySound(sfxData, 0);
    }
    
    // Play SFX with combo index for pitch scaling.
    public static void Play(SFXData sfxData, int comboIndex)
    {
        if (Instance == null || !Instance.sfxEnabled) return;
        Instance.PlaySound(sfxData, comboIndex);
    }
    
    // Play SFX at position with combo index.
    public static void PlayAtPosition(SFXData sfxData, Vector3 position, int comboIndex = 0)
    {
        if (Instance == null) return;
        Instance.PlaySoundAtPosition(sfxData, position, comboIndex);
    }
    
    // Play a one-off AudioClip.
    public static void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || !Instance.sfxEnabled) return;
        Instance.PlayOneShot(clip, volume);
    }
    
    // Stop all sounds.
    public static void StopAll()
    {
        if (Instance == null) return;
        foreach (var source in Instance.audioSourcePool)
            source.Stop();
    }
    
    // Set master volume.
    public static void SetMasterVolume(float volume)
    {
        if (Instance == null) return;
        Instance.masterVolume = Mathf.Clamp01(volume);
    }
    
    // ============================================
    // INSTANCE METHODS
    // ============================================
    
    public void PlaySound(SFXData sfxData, int comboIndex = 0)
    {
        if (sfxData == null) return;
        
        AudioClip clip = sfxData.GetClip();
        if (clip == null) return;
        
        AudioSource source = GetAvailableSource();
        ConfigureSource(source, sfxData, comboIndex);
        source.clip = clip;
        source.Play();
    }
    
    public void PlaySoundAtPosition(SFXData sfxData, Vector3 position, int comboIndex = 0)
    {
        if (sfxData == null) return;
        
        AudioClip clip = sfxData.GetClip();
        if (clip == null) return;
        
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        
        AudioSource source = tempGO.AddComponent<AudioSource>();
        ConfigureSource(source, sfxData, comboIndex);
        source.spatialBlend = 0f;
        source.clip = clip;
        source.Play();
        
        Destroy(tempGO, clip.length / source.pitch + 0.1f);
    }
    
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
    
    private void ConfigureSource(AudioSource source, SFXData sfxData, int comboIndex)
    {
        source.volume = sfxData.volume * masterVolume;
        source.pitch = sfxData.GetPitch(comboIndex);
        source.spatialBlend = sfxData.spatialBlend;
        source.priority = sfxData.priority;
        source.loop = sfxData.loop;
    }
}