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
    private float elapsedTime = 0f;
    private int currentStars = 0;
    private int displayedStars = 0;
    private bool isTracking = false;
    private bool useAnimations = true;
    
    public int CurrentStars => currentStars;
    public int DisplayedStars => displayedStars;
    
    // Events for star changes (fired before visual update)
    // Parameters: (starIndex, isEarning, starWorldPosition)
    public Action<int, bool, Vector3> onStarChanging;
    
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
    
    void Update()
    {
        if (!isTracking) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        
        if (currentMode == WinConditionType.ClearAllBubbles)
        {
            elapsedTime += Time.deltaTime;
            UpdateStarsForClassicMode();
        }
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
    
    // Subscribes to game events based on the current win condition.
    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onGameStart.AddListener(OnGameStart);
            GameManager.Instance.onWinConditionSet.AddListener(OnWinConditionSet);
            GameManager.Instance.onSurvivalRowSpawned.AddListener(OnSurvivalRowSpawned);
            GameManager.Instance.onVictory.AddListener(OnGameEnd);
            GameManager.Instance.onGameOver.AddListener(OnGameEnd);
        }
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.onScoreChanged += OnScoreChanged;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.onWinConditionSet.RemoveListener(OnWinConditionSet);
            GameManager.Instance.onSurvivalRowSpawned.RemoveListener(OnSurvivalRowSpawned);
            GameManager.Instance.onVictory.RemoveListener(OnGameEnd);
            GameManager.Instance.onGameOver.RemoveListener(OnGameEnd);
        }
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.onScoreChanged -= OnScoreChanged;
        }
    }
    
    // Sets up the initial star display based on win condition.
    private void InitializeDisplay()
    {
        if (currentMode == WinConditionType.ClearAllBubbles)
        {
            currentStars = 3;
            displayedStars = 3;
        }
        else
        {
            currentStars = 0;
            displayedStars = 0;
        }
        
        UpdateStarVisuals();
        
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            isTracking = true;
        }
        
        Log($"Initialized for {currentMode} mode with {currentStars} stars");
    }
    
    private void OnGameStart()
    {
        elapsedTime = 0f;
        isTracking = true;
        Log("Game started - tracking enabled");
    }
    
    private void OnWinConditionSet(WinConditionType condition)
    {
        currentMode = condition;
        InitializeDisplay();
    }
    
    // Called when game ends. Forces immediate visual update without animation.
    private void OnGameEnd()
    {
        isTracking = false;
        useAnimations = false;
        
        switch (currentMode)
        {
            case WinConditionType.ClearAllBubbles:
                FinalUpdateClassicMode();
                break;
            case WinConditionType.ReachTargetScore:
                FinalUpdateScoreMode();
                break;
            case WinConditionType.Survival:
                FinalUpdateSurvivalMode();
                break;
        }
        
        onForceUpdate?.Invoke();
        Log($"Game ended - final stars: {currentStars}");
    }
    
    private void FinalUpdateClassicMode()
    {
        if (currentLevel == null) return;
        
        if (elapsedTime <= currentLevel.threeStarTime)
            currentStars = 3;
        else if (elapsedTime <= currentLevel.twoStarTime)
            currentStars = 2;
        else if (elapsedTime <= currentLevel.oneStarTime)
            currentStars = 1;
        else
            currentStars = 0;
        
        displayedStars = currentStars;
        UpdateStarVisuals();
    }
    
    private void FinalUpdateScoreMode()
    {
        if (currentLevel == null || ScoreManager.Instance == null) return;
        
        int score = ScoreManager.Instance.CurrentScore;
        int target = currentLevel.targetScore;
        
        if (score >= target)
            currentStars = 3;
        else if (score >= Mathf.CeilToInt(target * 2f / 3f))
            currentStars = 2;
        else if (score >= Mathf.CeilToInt(target / 3f))
            currentStars = 1;
        else
            currentStars = 0;
        
        displayedStars = currentStars;
        UpdateStarVisuals();
    }
    
    private void FinalUpdateSurvivalMode()
    {
        if (currentLevel == null) return;
        
        HexGrid grid = FindFirstObjectByType<HexGrid>();
        if (grid != null && grid.IsGridEmpty())
        {
            currentStars = 3;
            displayedStars = 3;
            UpdateStarVisuals();
            return;
        }
        
        int rowsSurvived = GameManager.Instance != null ? GameManager.Instance.GetSurvivalRowsCount() : 0;
        
        if (rowsSurvived >= currentLevel.threeStarRows)
            currentStars = 3;
        else if (rowsSurvived >= currentLevel.twoStarRows)
            currentStars = 2;
        else if (rowsSurvived >= currentLevel.oneStarRows)
            currentStars = 1;
        else
            currentStars = 0;
        
        displayedStars = currentStars;
        UpdateStarVisuals();
    }
    
    private void OnScoreChanged(int newScore, int pointsAdded)
    {
        if (currentMode != WinConditionType.ReachTargetScore) return;
        UpdateStarsForScoreMode(newScore);
    }
    
    private void OnSurvivalRowSpawned(int totalRows)
    {
        if (currentMode != WinConditionType.Survival) return;
        UpdateStarsForSurvivalMode(totalRows);
    }
    
    // Updates stars for ClearAllBubbles. Fires event when star is lost.
    private void UpdateStarsForClassicMode()
    {
        if (currentLevel == null) return;
        
        int newStars;
        
        if (elapsedTime <= currentLevel.threeStarTime)
            newStars = 3;
        else if (elapsedTime <= currentLevel.twoStarTime)
            newStars = 2;
        else if (elapsedTime <= currentLevel.oneStarTime)
            newStars = 1;
        else
            newStars = 0;
        
        if (newStars != currentStars)
        {
            int oldStars = currentStars;
            currentStars = newStars;
            
            // Fire event for each star lost (from right to left)
            // Visual update happens when animation completes
            if (useAnimations && newStars < oldStars)
            {
                for (int i = oldStars - 1; i >= newStars; i--)
                {
                    Vector3 starPos = GetStarWorldPosition(i);
                    onStarChanging?.Invoke(i, false, starPos);
                }
            }
            else
            {
                displayedStars = currentStars;
                UpdateStarVisuals();
            }
            
            Log($"Classic mode: {elapsedTime:F1}s, stars {oldStars} -> {newStars}");
        }
    }
    
    // Updates stars for ReachTargetScore. Fires event when star is earned.
    private void UpdateStarsForScoreMode(int score)
    {
        if (currentLevel == null) return;
        
        int target = currentLevel.targetScore;
        int newStars;
        
        if (score >= target)
            newStars = 3;
        else if (score >= Mathf.CeilToInt(target * 2f / 3f))
            newStars = 2;
        else if (score >= Mathf.CeilToInt(target / 3f))
            newStars = 1;
        else
            newStars = 0;
        
        if (newStars != currentStars)
        {
            int oldStars = currentStars;
            currentStars = newStars;
            
            // Fire event for each star earned (from left to right)
            if (useAnimations && newStars > oldStars)
            {
                for (int i = oldStars; i < newStars; i++)
                {
                    Vector3 starPos = GetStarWorldPosition(i);
                    onStarChanging?.Invoke(i, true, starPos);
                }
            }
            else
            {
                displayedStars = currentStars;
                UpdateStarVisuals();
            }
            
            Log($"Score mode: {score} pts, stars {oldStars} -> {newStars}");
        }
    }
    
    // Updates stars for Survival. Fires event when star is earned.
    private void UpdateStarsForSurvivalMode(int rowsSurvived)
    {
        if (currentLevel == null) return;
        
        int newStars;
        
        if (rowsSurvived >= currentLevel.threeStarRows)
            newStars = 3;
        else if (rowsSurvived >= currentLevel.twoStarRows)
            newStars = 2;
        else if (rowsSurvived >= currentLevel.oneStarRows)
            newStars = 1;
        else
            newStars = 0;
        
        if (newStars != currentStars)
        {
            int oldStars = currentStars;
            currentStars = newStars;
            
            // Fire event for each star earned (from left to right)
            if (useAnimations && newStars > oldStars)
            {
                for (int i = oldStars; i < newStars; i++)
                {
                    Vector3 starPos = GetStarWorldPosition(i);
                    onStarChanging?.Invoke(i, true, starPos);
                }
            }
            else
            {
                displayedStars = currentStars;
                UpdateStarVisuals();
            }
            
            Log($"Survival mode: {rowsSurvived} rows, stars {oldStars} -> {newStars}");
        }
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
    
    // Called by animation system when a star animation completes.
    // Updates the displayed star count and refreshes visuals.
    public void OnStarAnimationComplete(int starIndex, bool wasEarned)
    {
        if (wasEarned)
        {
            displayedStars = Mathf.Max(displayedStars, starIndex + 1);
        }
        else
        {
            displayedStars = Mathf.Min(displayedStars, starIndex);
        }
        
        UpdateStarVisuals();
        Log($"Animation complete for star {starIndex}, displayed: {displayedStars}");
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
        elapsedTime = 0f;
        isTracking = false;
        useAnimations = true;
        CacheReferences();
        InitializeDisplay();
    }
    
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[LiveStarIndicatorUI] {msg}");
    }
}