using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class ThemeableUIImage : MonoBehaviour
{
    [Header("Theme Settings")]
    [Tooltip("Optional: Use a ThemeSprite ScriptableObject. Takes priority over individual sprites.")]
    [SerializeField] private ThemeSprite themeSprite;
    
    [Header("Direct Sprite Assignment (Used if ThemeSprite is not assigned)")]
    [SerializeField] private Sprite daySprite;
    [SerializeField] private Sprite nightSprite;
    
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
    
    // Applies the correct sprite based on current theme.
    // Checks if ScriptableObject is assigned first, otherwise uses direct sprite fields.
    private void ApplyTheme(ThemeMode theme)
    {
        if (image == null) return;
        
        Sprite targetSprite = GetSpriteForTheme(theme);
        if (targetSprite != null)
        {
            image.sprite = targetSprite;
        }
    }
    
    // Returns the appropriate sprite for the given theme.
    // Priority: ThemeSprite ScriptableObject > Direct sprite assignment.
    private Sprite GetSpriteForTheme(ThemeMode theme)
    {
        // If ScriptableObject is assigned, use it
        if (themeSprite != null)
        {
            return themeSprite.GetSprite(theme);
        }
        
        // Otherwise use direct sprite assignment
        return theme == ThemeMode.Day ? daySprite : nightSprite;
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the sprite preview in the editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (image == null)
            image = GetComponent<Image>();
            
        if (image != null)
        {
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            Sprite targetSprite = GetSpriteForTheme(previewTheme);
            if (targetSprite != null)
            {
                image.sprite = targetSprite;
            }
        }
    }
}