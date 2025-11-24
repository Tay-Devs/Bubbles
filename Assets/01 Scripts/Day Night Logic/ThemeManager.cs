using UnityEngine;
using System;

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
                if (instance == null)
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
    
    private void Start()
    {
        // Apply initial theme
        ApplyTheme(currentTheme);
    }
    
    // Set theme directly
    public void SetTheme(ThemeMode newTheme)
    {
        if (currentTheme == newTheme) return;
        
        currentTheme = newTheme;
        SaveThemePreference();
        ApplyTheme(newTheme);
    }
    
    // Toggle between day and night
    public void ToggleTheme()
    {
        ThemeMode newTheme = currentTheme == ThemeMode.Day ? ThemeMode.Night : ThemeMode.Day;
        SetTheme(newTheme);
    }
    
    // Apply theme and notify listeners
    private void ApplyTheme(ThemeMode theme)
    {
        OnThemeChanged?.Invoke(theme);
    }
    
    // Save theme preference to PlayerPrefs
    private void SaveThemePreference()
    {
        PlayerPrefs.SetInt(THEME_PREF_KEY, (int)currentTheme);
        PlayerPrefs.Save();
    }
    
    // Load theme preference from PlayerPrefs
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
}