using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ThemeToggle : MonoBehaviour
{
    [Header("Toggle Settings")]
    [SerializeField] private bool dayIsOn = true; // True = Day when toggle is on, False = Night when toggle is on
    
    [Header("Optional Visual Elements")]
    [SerializeField] private Image handleImage;
    [SerializeField] private ThemeSprite handleThemeSprite;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private ThemeSprite backgroundThemeSprite;
    
    private Toggle toggle;
    
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }
    
    private void Start()
    {
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Set initial toggle state based on current theme
        UpdateToggleState(ThemeManager.Instance.CurrentTheme);
    }
    
    private void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= OnThemeChanged;
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }
    
    private void OnToggleValueChanged(bool isOn)
    {
        // Convert toggle state to theme
        ThemeMode newTheme = (isOn == dayIsOn) ? ThemeMode.Day : ThemeMode.Night;
        ThemeManager.Instance.SetTheme(newTheme);
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        // Update toggle visual state
        UpdateToggleState(newTheme);
    }
    
    private void UpdateToggleState(ThemeMode theme)
    {
        // Update toggle isOn state without triggering the event
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        toggle.isOn = (theme == ThemeMode.Day) == dayIsOn;
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        
        // Update visual elements if assigned
        UpdateVisuals(theme);
    }
    
    private void UpdateVisuals(ThemeMode theme)
    {
        // Update handle image if assigned
        if (handleImage != null && handleThemeSprite != null)
        {
            handleImage.sprite = handleThemeSprite.GetSprite(theme);
        }
        
        // Update background image if assigned
        if (backgroundImage != null && backgroundThemeSprite != null)
        {
            backgroundImage.sprite = backgroundThemeSprite.GetSprite(theme);
        }
    }
}