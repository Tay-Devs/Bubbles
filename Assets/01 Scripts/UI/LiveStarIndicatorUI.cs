using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiveStarIndicatorUI : MonoBehaviour
{
    [Header("Star Images")]
    public List<Image> starImages = new List<Image>();
    
    [Header("Star States")]
    public Sprite starEarned;
    public Sprite starEmpty;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private LevelConfig currentLevel;
    private WinConditionType currentMode;
    private int displayedStars = 0;
    private bool isTracking = false;
    
    public int DisplayedStars => displayedStars;
    
    // Event fired when animation should be skipped (game end)
    public Action onForceUpdate;
    
    void Start()
    {
        CacheReferences();
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
    
    // Subscribes to game events for state tracking.
    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onGameStart.AddListener(OnGameStart);
            GameManager.Instance.onWinConditionSet.AddListener(OnWinConditionSet);
            GameManager.Instance.onVictory.AddListener(OnGameEnd);
            GameManager.Instance.onGameOver.AddListener(OnGameEnd);
        }
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
    }
    
    // Sets up the initial star display. All modes now start with empty stars
    // since StarProgressUI handles earning/burning via slider milestones.
    private void InitializeDisplay()
    {
        // All modes start with 0 displayed stars - earned via slider milestones
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
            // Simply increment - works for all modes since each earned star adds 1
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
    
    // Updates the visual state of all star images.
    private void UpdateStarVisuals()
    {
        for (int i = 0; i < starImages.Count; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < displayedStars;
            
            if (starEarned != null && starEmpty != null)
            {
                starImages[i].sprite = isEarned ? starEarned : starEmpty;
            }
        }
    }
    
    // Resets the indicator for a new game.
    public void Reset()
    {
        isTracking = false;
        CacheReferences();
        InitializeDisplay();
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[LiveStarIndicatorUI] {msg}");
    }
}