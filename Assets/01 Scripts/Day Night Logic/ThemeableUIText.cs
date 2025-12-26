using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class ThemeableUIText : MonoBehaviour
{
    [Header("Theme Settings")]
    [Tooltip("Optional: Use a ThemeColor ScriptableObject. Takes priority over individual colors.")]
    [SerializeField] private ThemeColor themeColor;
    
    [Header("Direct Color Assignment (Used if ThemeColor is not assigned)")]
    [SerializeField] private Color dayColor = Color.black;
    [SerializeField] private Color nightColor = Color.white;
    
    private TMP_Text textComponent;
    
    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
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
    
    // Applies the correct color to the text based on current theme.
    // Checks if ScriptableObject is assigned first, otherwise uses direct color fields.
    private void ApplyTheme(ThemeMode theme)
    {
        if (textComponent == null) return;
        
        textComponent.color = GetColorForTheme(theme);
    }
    
    // Returns the appropriate color for the given theme.
    // Priority: ThemeColor ScriptableObject > Direct color assignment.
    private Color GetColorForTheme(ThemeMode theme)
    {
        // If ScriptableObject is assigned, use it
        if (themeColor != null)
        {
            return themeColor.GetColor(theme);
        }
        
        // Otherwise use direct color assignment
        return theme == ThemeMode.Day ? dayColor : nightColor;
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the text color preview in the editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();
            
        if (textComponent != null)
        {
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            textComponent.color = GetColorForTheme(previewTheme);
        }
    }
}