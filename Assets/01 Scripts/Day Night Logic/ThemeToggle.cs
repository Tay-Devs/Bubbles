using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
[ExecuteAlways] // Run in edit mode
public class ThemeToggle : MonoBehaviour
{
    [Header("Toggle Checkmark")]
    [SerializeField] private Image toggleImage;
    [SerializeField] private ThemeSprite toggleThemeSprite;
    
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
    
    private void UpdateToggleState(ThemeMode theme)
    {
        isUpdating = true;
        toggle.isOn = (theme == ThemeMode.Day);
        isUpdating = false;
        
        UpdateVisuals(theme);
    }
    
    private void UpdateVisuals(ThemeMode theme)
    {
        if (toggleImage != null && toggleThemeSprite != null)
        {
            toggleImage.sprite = toggleThemeSprite.GetSprite(theme);
            if (Application.isPlaying)
            {
                toggleImage.enabled = true;
            }
        }
    }
    
    private void Update()
    {
        if (Application.isPlaying && toggleImage != null && !toggleImage.enabled)
        {
            toggleImage.enabled = true;
        }
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
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        if (toggleImage == null)
            return;
            
        if (toggleThemeSprite != null && toggleImage != null)
        {
            // In edit mode, check if ThemeManager exists and use its theme
            ThemeManager manager = FindObjectOfType<ThemeManager>();
            ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
            
            toggleImage.sprite = toggleThemeSprite.GetSprite(previewTheme);
        }
    }
}