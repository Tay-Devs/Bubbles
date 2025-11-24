using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
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
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Apply current theme
        ApplyTheme(ThemeManager.Instance.CurrentTheme);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        ThemeManager.OnThemeChanged -= OnThemeChanged;
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
}