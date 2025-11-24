using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ThemeToggle : MonoBehaviour
{
    [Header("Toggle Checkmark")]
    [SerializeField] private Image toggleImage; // The image that Unity would normally hide/show
    [SerializeField] private ThemeSprite toggleThemeSprite; // Theme sprites for the checkmark
    
    private Toggle toggle;
    private bool isUpdating = false;
    
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        
        // IMPORTANT: Remove the graphic from toggle so it doesn't control visibility
        if (toggleImage != null && toggle.graphic == toggleImage)
        {
            toggle.graphic = null;
        }
    }
    
    private void Start()
    {
        // Subscribe to toggle changes
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Set initial toggle state based on current theme
        UpdateToggleState(ThemeManager.Instance.CurrentTheme);
    }
    
    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }
    
    private void OnToggleValueChanged(bool isOn)
    {
        // Prevent recursive updates
        if (isUpdating) return;
        
        // Toggle ON = Day, Toggle OFF = Night
        ThemeMode newTheme = isOn ? ThemeMode.Day : ThemeMode.Night;
        ThemeManager.Instance.SetTheme(newTheme);
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        UpdateToggleState(newTheme);
    }
    
    private void UpdateToggleState(ThemeMode theme)
    {
        // Update toggle state without triggering the event
        isUpdating = true;
        toggle.isOn = (theme == ThemeMode.Day);
        isUpdating = false;
        
        // Update visual elements
        UpdateVisuals(theme);
    }
    
    private void UpdateVisuals(ThemeMode theme)
    {
        // Update checkmark image (always visible, just change sprite)
        if (toggleImage != null && toggleThemeSprite != null)
        {
            toggleImage.sprite = toggleThemeSprite.GetSprite(theme);
            toggleImage.enabled = true; // Ensure it's always visible
        }
    }
    
    // Ensure checkmark stays visible when toggle state changes
    private void Update()
    {
        if (toggleImage != null && !toggleImage.enabled)
        {
            print("TETE");
            toggleImage.enabled = true;
        }
    }
}