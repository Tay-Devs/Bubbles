using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
[ExecuteAlways]
public class ThemeToggle : MonoBehaviour
{
    [Header("Toggle Checkmark")]
    [SerializeField] private Image toggleImage;
    
    [Header("Theme Settings")]
    [Tooltip("Optional: Use a ThemeSprite ScriptableObject. Takes priority over individual sprites.")]
    [SerializeField] private ThemeSprite toggleThemeSprite;
    
    [Header("Direct Sprite Assignment (Used if ThemeSprite is not assigned)")]
    [SerializeField] private Sprite daySprite;
    [SerializeField] private Sprite nightSprite;
    
    private Toggle toggle;
    private bool isUpdating = false;
    
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        
        if (Application.isPlaying)
        {
            // Remove the graphic from toggle so it doesn't control visibility
            if (toggleImage != null && toggle.graphic == toggleImage)
            {
                toggle.graphic = null;
            }
        }
    }
    
    private void Start()
    {
        if (Application.isPlaying)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            ThemeManager.OnThemeChanged += OnThemeChanged;
            UpdateToggleState(ThemeManager.Instance.CurrentTheme);
        }
    }
    
    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            ThemeManager.OnThemeChanged -= OnThemeChanged;
        }
    }
    
    // Called when user clicks the toggle. Sets isUpdating flag to prevent recursive calls.
    // Maps toggle on/off to Day/Night theme modes.
    private void OnToggleValueChanged(bool isOn)
    {
        if (isUpdating) return;
        
        ThemeMode newTheme = isOn ? ThemeMode.Day : ThemeMode.Night;
        ThemeManager.Instance.SetTheme(newTheme);
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        UpdateToggleState(newTheme);
    }
    
    // Syncs toggle isOn state with the current theme and updates visuals.
    // Uses isUpdating flag to prevent OnToggleValueChanged from firing during sync.
    private void UpdateToggleState(ThemeMode theme)
    {
        isUpdating = true;
        toggle.isOn = (theme == ThemeMode.Day);
        isUpdating = false;
        
        UpdateVisuals(theme);
    }
    
    // Updates the toggle image sprite based on current theme.
    // Uses ScriptableObject if assigned, otherwise falls back to direct sprites.
    private void UpdateVisuals(ThemeMode theme)
    {
        if (toggleImage == null) return;
        
        Sprite targetSprite = GetSpriteForTheme(theme);
        if (targetSprite != null)
        {
            toggleImage.sprite = targetSprite;
        }
        
        if (Application.isPlaying)
        {
            toggleImage.enabled = true;
        }
    }
    
    // Returns the appropriate sprite for the given theme.
    // Priority: ThemeSprite ScriptableObject > Direct sprite assignment.
    private Sprite GetSpriteForTheme(ThemeMode theme)
    {
        // If ScriptableObject is assigned, use it
        if (toggleThemeSprite != null)
        {
            return toggleThemeSprite.GetSprite(theme);
        }
        
        // Otherwise use direct sprite assignment
        return theme == ThemeMode.Day ? daySprite : nightSprite;
    }
    
    private void Update()
    {
        if (Application.isPlaying && toggleImage != null && !toggleImage.enabled)
        {
            toggleImage.enabled = true;
        }
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the toggle preview in the editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        if (toggleImage == null)
            return;
            
        ThemeManager manager = FindObjectOfType<ThemeManager>();
        ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
        
        Sprite targetSprite = GetSpriteForTheme(previewTheme);
        if (targetSprite != null)
        {
            toggleImage.sprite = targetSprite;
        }
    }
}