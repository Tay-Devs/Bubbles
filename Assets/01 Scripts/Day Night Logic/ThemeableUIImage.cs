using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteAlways] // Run in edit mode
public class ThemeableUIImage : MonoBehaviour
{
    [Header("Theme Settings")]
    [SerializeField] private ThemeSprite themeSprite;
    
    private Image image;
    
    private void Awake()
    {
        image = GetComponent<Image>();
    }
    
    private void Start()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }
    }
    
    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged -= OnThemeChanged;
        }
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        ApplyTheme(newTheme);
    }
    
    private void ApplyTheme(ThemeMode theme)
    {
        if (themeSprite == null || image == null) return;
        
        image.sprite = themeSprite.GetSprite(theme);
    }
    
    // Called in editor when values change
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Update preview in editor
    public void UpdatePreview()
    {
        if (image == null)
            image = GetComponent<Image>();
            
        if (themeSprite != null && image != null)
        {
            // In edit mode, check if ThemeManager exists and use its theme
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            image.sprite = themeSprite.GetSprite(previewTheme);
        }
    }
}