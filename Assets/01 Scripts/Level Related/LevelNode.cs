using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNumberText;
    public Button levelButton;
    public Image nodeBackground;
    
    [Header("Star References")]
    public Image[] starImages;
    
    [Header("Star Sprites")]
    public Sprite starEarnedSprite;
    
    [Header("Unearned Star Theme Sprites")]
    [SerializeField] private Sprite unearnedStarDaySprite;
    [SerializeField] private Sprite unearnedStarNightSprite;
    
    [Header("Lock State")]
    public GameObject lockIcon;
    public GameObject closedLock;
    public GameObject starsContainer;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    [Header("Level Number Display")]
    public Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedTextColor = Color.white;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private int levelNumber;
    private bool isUnlocked;
    private int currentStarsEarned;
    private ThemeMode currentTheme;
    
    public int LevelNumber => levelNumber;
    public Vector3 Position => transform.position;
    
    void Awake()
    {
        if (levelButton == null)
        {
            levelButton = GetComponent<Button>();
        }
        
        DisableChildRaycasts();
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
        UpdateStars(currentStarsEarned, isUnlocked);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelNode] Theme changed to {newTheme}, updated stars");
        }
    }
    
    // Returns the appropriate unearned star sprite for the given theme.
    private Sprite GetUnearnedSpriteForTheme(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? unearnedStarDaySprite : unearnedStarNightSprite;
    }
    
    private void DisableChildRaycasts()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (levelButton != null && img == levelButton.targetGraphic)
            {
                continue;
            }
            img.raycastTarget = false;
        }
        
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            txt.raycastTarget = false;
        }
    }
    
    // Main setup method with optional unlock animation trigger.
    // When playUnlockAnimation is true, activates closedLock to trigger DOTween's Play On Enable.
    public void Setup(int level, bool unlocked, int starsEarned, bool playUnlockAnimation = false)
    {
        levelNumber = level;
        isUnlocked = unlocked;
        currentStarsEarned = starsEarned;
        
        if (ThemeManager.Instance != null)
        {
            currentTheme = ThemeManager.Instance.CurrentTheme;
        }
        
        if (levelNumberText != null)
        {
            levelNumberText.text = level.ToString();
        }
        
        if (levelButton != null)
        {
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(OnNodeClicked);
            levelButton.interactable = unlocked;
        }
        
        if (playUnlockAnimation)
        {
            SetupForUnlockAnimation();
        }
        else
        {
            UpdateLockVisual(unlocked);
        }
        
        UpdateLevelNumberColor(unlocked);
        UpdateStars(starsEarned, unlocked);
        UpdateNodeColor(unlocked, starsEarned > 0);
        
        if (enableDebugLogs && playUnlockAnimation)
        {
            Debug.Log($"[LevelNode] Level {levelNumber} setup with unlock animation");
        }
    }
    
    // Prepares visuals for unlock animation by activating closedLock.
    // DOTween's Play On Enable will automatically start the animation sequence.
    private void SetupForUnlockAnimation()
    {
        // Hide normal lock icon since closedLock will handle the animation
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }
        
        if (starsContainer != null)
        {
            starsContainer.SetActive(false);
        }
        
        // Activate closedLock to trigger DOTween's Play On Enable
        if (closedLock != null)
        {
            closedLock.SetActive(true);
        }
    }
    
    // Legacy setup method for backwards compatibility.
    public void Setup(int level, bool unlocked, int starsEarned)
    {
        Setup(level, unlocked, starsEarned, false);
    }
    
    // Updates lock visibility for normal levels (not animation).
    // closedLock is never touched here - it's only for the unlock animation.
    private void UpdateLockVisual(bool unlocked)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(!unlocked);
        }
        
        // Ensure closedLock is disabled for normal display
        if (closedLock != null)
        {
            closedLock.SetActive(false);
        }
    }
    
    // Updates level number text color based on lock state.
    private void UpdateLevelNumberColor(bool unlocked)
    {
        if (levelNumberText == null) return;
        
        levelNumberText.color = unlocked ? unlockedTextColor : lockedTextColor;
    }
    
    // Updates star visuals based on stars earned and current theme.
    // Earned stars use starEarnedSprite, unearned use day/night themed sprite.
    private void UpdateStars(int starsEarned, bool unlocked)
    {
        currentStarsEarned = starsEarned;
        
        if (starsContainer != null)
        {
            starsContainer.SetActive(unlocked);
        }
        
        if (starImages == null) return;
        
        Sprite unearnedSprite = GetUnearnedSpriteForTheme(currentTheme);
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            starImages[i].gameObject.SetActive(true);
            
            bool isEarned = i < starsEarned;
            starImages[i].sprite = isEarned ? starEarnedSprite : unearnedSprite;
            starImages[i].color = Color.white;
        }
    }
    
    private void UpdateNodeColor(bool unlocked, bool completed)
    {
        if (nodeBackground == null) return;
        
        if (!unlocked)
        {
            nodeBackground.color = lockedColor;
        }
        else if (completed)
        {
            nodeBackground.color = completedColor;
        }
        else
        {
            nodeBackground.color = unlockedColor;
        }
    }
    
    // Called when the node is clicked. Opens the level popup instead of loading directly.
    // Only responds if the level is unlocked.
    private void OnNodeClicked()
    {
        if (!isUnlocked)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[LevelNode] Level is locked, ignoring click");
            }
            return;
        }
        
        if (LevelMapController.Instance == null)
        {
            Debug.LogError("[LevelNode] LevelMapController.Instance is null!");
            return;
        }
        
        LevelMapController.Instance.OpenLevelPopup(levelNumber);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelNode] Opening popup for level {levelNumber}");
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