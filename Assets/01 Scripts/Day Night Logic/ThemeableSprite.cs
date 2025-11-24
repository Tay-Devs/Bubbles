using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[ExecuteAlways] // Run in edit mode
public class ThemeableSprite : MonoBehaviour
{
    [Header("Theme Settings")]
    [SerializeField] private ThemeSprite themeSprite;
    
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (themeSprite == null || spriteRenderer == null) return;
        
        spriteRenderer.sprite = themeSprite.GetSprite(theme);
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
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (themeSprite != null && spriteRenderer != null)
        {
            // In edit mode, check if ThemeManager exists and use its theme
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            spriteRenderer.sprite = themeSprite.GetSprite(previewTheme);
        }
    }
}