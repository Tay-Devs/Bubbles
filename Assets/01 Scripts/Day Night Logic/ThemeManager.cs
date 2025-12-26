using UnityEngine;
using System;

[ExecuteAlways]
public class ThemeManager : MonoBehaviour
{
    // Singleton
    private static ThemeManager instance;
    public static ThemeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ThemeManager>();
                if (instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("ThemeManager");
                    instance = go.AddComponent<ThemeManager>();
                }
            }
            return instance;
        }
    }
    
    [Header("Current Theme")]
    [SerializeField] private ThemeMode currentTheme = ThemeMode.Day;
    
    // Events
    public static event Action<ThemeMode> OnThemeChanged;
    
    // Properties
    public ThemeMode CurrentTheme => currentTheme;
    
    private const string THEME_PREF_KEY = "SelectedTheme";
    
    private void Awake()
    {
        if (Application.isPlaying)
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load saved theme preference
            LoadThemePreference();
        }
    }
    
    private void Start()
    {
        if (Application.isPlaying)
        {
            // Apply initial theme
            ApplyTheme(currentTheme);
        }
    }
    
    // Sets the theme to the specified mode and saves the preference.
    // Skips if already on the requested theme to avoid unnecessary updates.
    public void SetTheme(ThemeMode newTheme)
    {
        if (currentTheme == newTheme) return;
        
        currentTheme = newTheme;
        
        if (Application.isPlaying)
        {
            SaveThemePreference();
        }
        
        ApplyTheme(newTheme);
    }
    
    // Toggles between Day and Night themes.
    // Convenience method for toggle buttons.
    public void ToggleTheme()
    {
        ThemeMode newTheme = currentTheme == ThemeMode.Day ? ThemeMode.Night : ThemeMode.Day;
        SetTheme(newTheme);
    }
    
    // Invokes the OnThemeChanged event to notify all subscribers.
    // All themeable components listen to this event.
    private void ApplyTheme(ThemeMode theme)
    {
        OnThemeChanged?.Invoke(theme);
    }
    
    private void SaveThemePreference()
    {
        PlayerPrefs.SetInt(THEME_PREF_KEY, (int)currentTheme);
        PlayerPrefs.Save();
    }
    
    private void LoadThemePreference()
    {
        if (PlayerPrefs.HasKey(THEME_PREF_KEY))
        {
            currentTheme = (ThemeMode)PlayerPrefs.GetInt(THEME_PREF_KEY);
        }
    }
    
    // Utility methods
    public bool IsDay() => currentTheme == ThemeMode.Day;
    public bool IsNight() => currentTheme == ThemeMode.Night;
    
    // Finds all themeable components in the scene and updates their previews.
    // Used in editor mode when theme is changed via inspector.
    public void UpdateAllThemeables()
    {
        // Find all themeable UI images
        ThemeableUIImage[] uiImages = FindObjectsOfType<ThemeableUIImage>();
        foreach (var img in uiImages)
        {
            img.UpdatePreview();
        }
        
        // Find all themeable sprites
        ThemeableSprite[] sprites = FindObjectsOfType<ThemeableSprite>();
        foreach (var sprite in sprites)
        {
            sprite.UpdatePreview();
        }
        
        // Find all theme toggles
        ThemeToggle[] toggles = FindObjectsOfType<ThemeToggle>();
        foreach (var toggle in toggles)
        {
            toggle.UpdatePreview();
        }
        
        // Find all themeable UI text
        ThemeableUIText[] texts = FindObjectsOfType<ThemeableUIText>();
        foreach (var text in texts)
        {
            text.UpdatePreview();
        }
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdateAllThemeables();
        }
    }
}