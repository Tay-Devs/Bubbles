using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[ExecuteAlways]
public class ThemeableOpacity : MonoBehaviour
{
    [Header("Opacity Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float dayOpacity = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float nightOpacity = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
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
    
    // Applies the correct opacity based on current theme.
    // Modifies only the alpha channel, preserving the existing RGB values.
    private void ApplyTheme(ThemeMode theme)
    {
        if (image == null) return;
        
        float targetOpacity = GetOpacityForTheme(theme);
        Color color = image.color;
        color.a = targetOpacity;
        image.color = color;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ThemeableOpacity] Applied {theme} opacity: {targetOpacity}");
        }
    }
    
    // Returns the appropriate opacity value for the given theme.
    // Day and Night each have their own configurable alpha value.
    private float GetOpacityForTheme(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? dayOpacity : nightOpacity;
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the opacity preview in the editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (image == null)
            image = GetComponent<Image>();
        
        if (image != null)
        {
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            float targetOpacity = GetOpacityForTheme(previewTheme);
            Color color = image.color;
            color.a = targetOpacity;
            image.color = color;
        }
    }
}