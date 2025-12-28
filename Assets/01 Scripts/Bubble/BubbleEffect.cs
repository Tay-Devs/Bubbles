using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    [Header("Theme Variants")]
    [SerializeField] private GameObject dayEffect;
    [SerializeField] private GameObject nightEffect;
    
    [Header("Visibility Mode")]
    [Tooltip("Use CanvasGroup for UI effects, SpriteRenderer for world-space effects")]
    [SerializeField] private VisibilityMode visibilityMode = VisibilityMode.CanvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Cached components
    private CanvasGroup dayCanvasGroup;
    private CanvasGroup nightCanvasGroup;
    private SpriteRenderer daySpriteRenderer;
    private SpriteRenderer nightSpriteRenderer;
    
    public enum VisibilityMode
    {
        CanvasGroup,
        SpriteRenderer
    }
    
    void Awake()
    {
        CacheComponents();
    }
    
    void Start()
    {
        // Ensure both effects are active so animations play in sync
        if (dayEffect != null) dayEffect.SetActive(true);
        if (nightEffect != null) nightEffect.SetActive(true);
        
        // Set initial theme visibility
        if (ThemeManager.Instance != null)
        {
            UpdateThemeVisibility(ThemeManager.Instance.CurrentTheme);
        }
        
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Self-destruct after lifetime
        Destroy(gameObject, lifetime);
    }
    
    void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }
    
    // Caches CanvasGroup or SpriteRenderer components based on visibility mode.
    private void CacheComponents()
    {
        if (visibilityMode == VisibilityMode.CanvasGroup)
        {
            if (dayEffect != null)
                dayCanvasGroup = dayEffect.GetComponent<CanvasGroup>();
            if (nightEffect != null)
                nightCanvasGroup = nightEffect.GetComponent<CanvasGroup>();
        }
        else
        {
            if (dayEffect != null)
                daySpriteRenderer = dayEffect.GetComponent<SpriteRenderer>();
            if (nightEffect != null)
                nightSpriteRenderer = nightEffect.GetComponent<SpriteRenderer>();
        }
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        UpdateThemeVisibility(newTheme);
    }
    
    // Shows the correct day/night variant using alpha (keeps animations running).
    // Both effects stay active, only visibility changes.
    private void UpdateThemeVisibility(ThemeMode theme)
    {
        bool isDay = theme == ThemeMode.Day;
        
        if (visibilityMode == VisibilityMode.CanvasGroup)
        {
            if (dayCanvasGroup != null)
            {
                dayCanvasGroup.alpha = isDay ? 1f : 0f;
            }
            
            if (nightCanvasGroup != null)
            {
                nightCanvasGroup.alpha = isDay ? 0f : 1f;
            }
        }
        else
        {
            if (daySpriteRenderer != null)
            {
                Color c = daySpriteRenderer.color;
                c.a = isDay ? 1f : 0f;
                daySpriteRenderer.color = c;
            }
            
            if (nightSpriteRenderer != null)
            {
                Color c = nightSpriteRenderer.color;
                c.a = isDay ? 0f : 1f;
                nightSpriteRenderer.color = c;
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BubbleEffect] Theme set to {theme}");
        }
    }
    
    // Spawns an effect at the given position with proper theme handling.
    public static BubbleEffect Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        return instance.GetComponent<BubbleEffect>();
    }
}