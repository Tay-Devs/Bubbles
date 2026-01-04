using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiveStarIndicatorUI : MonoBehaviour
{
    [Header("Star Images")]
    public List<Image> starImages = new List<Image>();
    
    [Header("Star States (Themeable)")]
    [SerializeField] private ThemeSprite starEarnedTheme;
    [SerializeField] private ThemeSprite starEmptyTheme;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private LevelConfig currentLevel;
    private WinConditionType currentMode;
    private int displayedStars = 0;
    private bool isTracking = false;
    private ThemeMode currentTheme;
    
    public int DisplayedStars => displayedStars;
    
    // Event fired when animation should be skipped (game end)
    public Action onForceUpdate;
    
    void Start()
    {
        CacheReferences();
        CacheCurrentTheme();
        SubscribeToEvents();
        InitializeDisplay();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    // Caches level config and win condition from LevelLoader/GameManager.
    private void CacheReferences()
    {
        if (LevelLoader.Instance != null)
        {
            currentLevel = LevelLoader.Instance.CurrentLevel;
        }
        
        if (GameManager.Instance != null)
        {
            currentMode = GameManager.Instance.ActiveWinCondition;
        }
    }
    
    // Stores the current theme from ThemeManager for sprite lookups.
    private void CacheCurrentTheme()
    {
        if (ThemeManager.Instance != null)
        {
            currentTheme = ThemeManager.Instance.CurrentTheme;
        }
    }
    
    // Subscribes to game events and theme changes for state tracking.
    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onGameStart.AddListener(OnGameStart);
            GameManager.Instance.onWinConditionSet.AddListener(OnWinConditionSet);
            GameManager.Instance.onVictory.AddListener(OnGameEnd);
            GameManager.Instance.onGameOver.AddListener(OnGameEnd);
        }
        
        ThemeManager.OnThemeChanged += OnThemeChanged;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.onWinConditionSet.RemoveListener(OnWinConditionSet);
            GameManager.Instance.onVictory.RemoveListener(OnGameEnd);
            GameManager.Instance.onGameOver.RemoveListener(OnGameEnd);
        }
        
        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }
    
    // Called when theme changes. Updates cached theme and refreshes star visuals.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        currentTheme = newTheme;
        UpdateStarVisuals();
        Log($"Theme changed to {newTheme}, updated star visuals");
    }
    
    // Sets up the initial star display. All modes now start with empty stars
    // since StarProgressUI handles earning/burning via slider milestones.
    private void InitializeDisplay()
    {
        displayedStars = 0;
        UpdateStarVisuals();
        
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            isTracking = true;
        }
        
        Log($"Initialized for {currentMode} mode with {displayedStars} displayed stars");
    }
    
    private void OnGameStart()
    {
        isTracking = true;
        Log("Game started - tracking enabled");
    }
    
    private void OnWinConditionSet(WinConditionType condition)
    {
        currentMode = condition;
        InitializeDisplay();
    }
    
    // Called when game ends. May force immediate visual update if needed.
    private void OnGameEnd()
    {
        isTracking = false;
        onForceUpdate?.Invoke();
        Log($"Game ended - final displayed stars: {displayedStars}");
    }
    
    // Returns the world position of a star image by index.
    public Vector3 GetStarWorldPosition(int index)
    {
        if (index >= 0 && index < starImages.Count && starImages[index] != null)
        {
            return starImages[index].transform.position;
        }
        return transform.position;
    }
    
    // Returns the RectTransform of a star image by index.
    public RectTransform GetStarRectTransform(int index)
    {
        if (index >= 0 && index < starImages.Count && starImages[index] != null)
        {
            return starImages[index].rectTransform;
        }
        return null;
    }
    
    // Called by StarProgressUI when a star animation completes.
    // Updates the displayed star count and refreshes visuals.
    public void OnStarAnimationComplete(int starIndex, bool wasEarned)
    {
        if (wasEarned)
        {
            displayedStars = Mathf.Min(displayedStars + 1, 3);
        }
    
        UpdateStarVisuals();
        Log($"Animation complete for star {starIndex} (earned={wasEarned}), displayed: {displayedStars}");
    }
    
    // Forces a specific star count display without animation.
    // Used for immediate updates when animations are skipped.
    public void ForceDisplayStars(int starCount)
    {
        displayedStars = Mathf.Clamp(starCount, 0, 3);
        UpdateStarVisuals();
        Log($"Force display: {displayedStars} stars");
    }
    
    // Updates the visual state of all star images using theme-appropriate sprites.
    // Gets sprites from ThemeSprite ScriptableObjects based on current theme.
    private void UpdateStarVisuals()
    {
        Sprite earnedSprite = GetSpriteFromTheme(starEarnedTheme);
        Sprite emptySprite = GetSpriteFromTheme(starEmptyTheme);
        
        for (int i = 0; i < starImages.Count; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < displayedStars;
            Sprite targetSprite = isEarned ? earnedSprite : emptySprite;
            
            if (targetSprite != null)
            {
                starImages[i].sprite = targetSprite;
            }
        }
    }
    
    // Returns the correct sprite from a ThemeSprite based on current theme.
    // Returns null if ThemeSprite is not assigned.
    private Sprite GetSpriteFromTheme(ThemeSprite themeSprite)
    {
        if (themeSprite == null) return null;
        return themeSprite.GetSprite(currentTheme);
    }
    
    // Resets the indicator for a new game.
    public void Reset()
    {
        isTracking = false;
        CacheReferences();
        CacheCurrentTheme();
        InitializeDisplay();
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[LiveStarIndicatorUI] {msg}");
    }
}