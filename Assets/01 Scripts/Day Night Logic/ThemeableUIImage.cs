using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
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
        ThemeManager.OnThemeChanged += OnThemeChanged;
        ApplyTheme(ThemeManager.Instance.CurrentTheme);
    }
    
    private void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= OnThemeChanged;
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
}