using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class LevelPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_Text levelNameText;
    public Button playButton;
    public Button closeButton;
    
    [Header("Star References")]
    public Image[] starImages;
    
    [Header("Star Sprites")]
    public Sprite starEarnedSprite;
    
    [Header("Unearned Star Theme Sprites")]
    [SerializeField] private Sprite unearnedStarDaySprite;
    [SerializeField] private Sprite unearnedStarNightSprite;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private int selectedLevelNumber;
    private int currentStarsEarned;
    private bool isOpen;
    private ThemeMode currentTheme;
    
    public bool IsOpen => isOpen;
    
    void Awake()
    {
        SetupButtons();
        Hide();
    }
    
    void Start()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            
            if (ThemeManager.Instance != null)
            {
                currentTheme = ThemeManager.Instance.CurrentTheme;
            }
        }
    }
    
    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged -= OnThemeChanged;
        }
    }
    
    // Called when theme changes. Updates unearned star sprites.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        currentTheme = newTheme;
        UpdateStars(currentStarsEarned);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelPopup] Theme changed to {newTheme}, updated stars");
        }
    }
    
    // Returns the appropriate unearned star sprite for the given theme.
    private Sprite GetUnearnedSpriteForTheme(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? unearnedStarDaySprite : unearnedStarNightSprite;
    }
    
    // Subscribes button click events to their respective handlers.
    // Play button loads the selected level, close button hides the popup.
    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }
    
    // Opens the popup and displays level name and star progress.
    // Stars earned determines how many gold vs grey stars to show.
    public void Show(int levelNumber, int starsEarned)
    {
        selectedLevelNumber = levelNumber;
        currentStarsEarned = starsEarned;
        
        // Get current theme
        if (ThemeManager.Instance != null)
        {
            currentTheme = ThemeManager.Instance.CurrentTheme;
        }
        
        if (levelNameText != null)
        {
            levelNameText.text = "LEVEL " + levelNumber;
        }
        
        UpdateStars(starsEarned);
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        
        isOpen = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelPopup] Opened popup for level {levelNumber} with {starsEarned} stars");
        }
    }
    
    // Updates star images based on how many stars the player earned.
    // Earned stars show gold sprite, unearned use day/night themed sprite.
    private void UpdateStars(int starsEarned)
    {
        currentStarsEarned = starsEarned;
        
        if (starImages == null) return;
        
        Sprite unearnedSprite = GetUnearnedSpriteForTheme(currentTheme);
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < starsEarned;
            starImages[i].sprite = isEarned ? starEarnedSprite : unearnedSprite;
        }
    }
    
    // Hides the popup and resets the selected level.
    public void Hide()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        isOpen = false;
        
        if (enableDebugLogs)
        {
            Debug.Log("[LevelPopup] Popup closed");
        }
    }
    
    // Called when play button is clicked. Loads the selected level through LevelMapController.
    private void OnPlayClicked()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelPopup] Play clicked for level {selectedLevelNumber}");
        }
        
        if (LevelMapController.Instance != null)
        {
            LevelMapController.Instance.LoadLevel(selectedLevelNumber);
        }
        else
        {
            Debug.LogError("[LevelPopup] LevelMapController.Instance is null!");
        }
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the star preview in editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (starImages == null) return;
        
        ThemeManager manager = FindObjectOfType<ThemeManager>();
        ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
        
        Sprite unearnedSprite = previewTheme == ThemeMode.Day ? unearnedStarDaySprite : unearnedStarNightSprite;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < currentStarsEarned;
            if (!isEarned && unearnedSprite != null)
            {
                starImages[i].sprite = unearnedSprite;
            }
        }
    }
}