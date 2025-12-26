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
    
    [Header("Optional Color Tint")]
    public bool useColorTint = false;
    public Color earnedColor = Color.yellow;
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private LevelConfig currentLevel;
    private WinConditionType currentMode;
    private float elapsedTime = 0f;
    private int currentStars = 0;
    private bool isTracking = false;
    
    public int CurrentStars => currentStars;
    
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
        
        // Only track time for ClearAllBubbles mode
        if (currentMode == WinConditionType.ClearAllBubbles)
        {
            elapsedTime += Time.deltaTime;
            UpdateStarsForClassicMode();
        }
    }
    
    // Caches level config and win condition from LevelLoader/GameManager.
    // Called once at start to get the current level's star thresholds.
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
    // Score mode listens to score changes, survival listens to row spawns.
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
    // Classic starts at 3 stars, score/survival start at 0.
    private void InitializeDisplay()
    {
        if (currentMode == WinConditionType.ClearAllBubbles)
        {
            currentStars = 3;
        }
        else
        {
            currentStars = 0;
        }
        
        UpdateStarVisuals();
        
        // Start tracking if game already playing (no intro)
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
    
    // Called when game ends (victory or game over).
    // Performs final star calculation to ensure accurate display.
    private void OnGameEnd()
    {
        isTracking = false;
        
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
        
        Log($"Game ended - final stars: {currentStars}");
    }
    
    private void FinalUpdateClassicMode()
    {
        if (currentLevel == null) return;
        
        int newStars;
        
        if (elapsedTime <= currentLevel.threeStarTime)
        {
            newStars = 3;
        }
        else if (elapsedTime <= currentLevel.twoStarTime)
        {
            newStars = 2;
        }
        else if (elapsedTime <= currentLevel.oneStarTime)
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        currentStars = newStars;
        UpdateStarVisuals();
    }
    
    private void FinalUpdateScoreMode()
    {
        if (currentLevel == null) return;
        if (ScoreManager.Instance == null) return;
        
        int score = ScoreManager.Instance.CurrentScore;
        int target = currentLevel.targetScore;
        int newStars;
        
        if (score >= target)
        {
            newStars = 3;
        }
        else if (score >= Mathf.CeilToInt(target * 2f / 3f))
        {
            newStars = 2;
        }
        else if (score >= Mathf.CeilToInt(target / 3f))
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        currentStars = newStars;
        UpdateStarVisuals();
    }
    
    private void FinalUpdateSurvivalMode()
    {
        if (currentLevel == null) return;
        
        // Check if grid was cleared - automatic 3 stars
        HexGrid grid = FindFirstObjectByType<HexGrid>();
        if (grid != null && grid.IsGridEmpty())
        {
            currentStars = 3;
            UpdateStarVisuals();
            Log("Survival mode: Grid cleared - automatic 3 stars");
            return;
        }
        
        int rowsSurvived = GameManager.Instance != null ? GameManager.Instance.GetSurvivalRowsCount() : 0;
        int newStars;
        
        if (rowsSurvived >= currentLevel.threeStarRows)
        {
            newStars = 3;
        }
        else if (rowsSurvived >= currentLevel.twoStarRows)
        {
            newStars = 2;
        }
        else if (rowsSurvived >= currentLevel.oneStarRows)
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        currentStars = newStars;
        UpdateStarVisuals();
    }
    
    // Called when score changes in ReachTargetScore mode.
    // Calculates stars based on score thresholds (1/3, 2/3, 3/3 of target).
    private void OnScoreChanged(int newScore, int pointsAdded)
    {
        if (currentMode != WinConditionType.ReachTargetScore) return;
        
        UpdateStarsForScoreMode(newScore);
    }
    
    // Called when a new row spawns in Survival mode.
    // Calculates stars based on rows survived vs level thresholds.
    private void OnSurvivalRowSpawned(int totalRows)
    {
        if (currentMode != WinConditionType.Survival) return;
        
        UpdateStarsForSurvivalMode(totalRows);
    }
    
    // Updates stars for ClearAllBubbles based on elapsed time.
    // Stars decrease as time passes: 3 → 2 → 1 → 0.
    private void UpdateStarsForClassicMode()
    {
        if (currentLevel == null) return;
        
        int newStars;
        
        if (elapsedTime <= currentLevel.threeStarTime)
        {
            newStars = 3;
        }
        else if (elapsedTime <= currentLevel.twoStarTime)
        {
            newStars = 2;
        }
        else if (elapsedTime <= currentLevel.oneStarTime)
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        if (newStars != currentStars)
        {
            currentStars = newStars;
            UpdateStarVisuals();
            Log($"Classic mode: {elapsedTime:F1}s elapsed, now at {currentStars} stars");
        }
    }
    
    // Updates stars for ReachTargetScore based on current score.
    // Stars increase as score rises: 0 → 1 → 2 → 3.
    private void UpdateStarsForScoreMode(int score)
    {
        if (currentLevel == null) return;
        
        int target = currentLevel.targetScore;
        int newStars;
        
        if (score >= target)
        {
            newStars = 3;
        }
        else if (score >= Mathf.CeilToInt(target * 2f / 3f))
        {
            newStars = 2;
        }
        else if (score >= Mathf.CeilToInt(target / 3f))
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        if (newStars != currentStars)
        {
            currentStars = newStars;
            UpdateStarVisuals();
            Log($"Score mode: {score} points, now at {currentStars} stars");
        }
    }
    
    // Updates stars for Survival based on rows survived.
    // Stars increase as more rows are survived: 0 → 1 → 2 → 3.
    private void UpdateStarsForSurvivalMode(int rowsSurvived)
    {
        if (currentLevel == null) return;
        
        int newStars;
        
        if (rowsSurvived >= currentLevel.threeStarRows)
        {
            newStars = 3;
        }
        else if (rowsSurvived >= currentLevel.twoStarRows)
        {
            newStars = 2;
        }
        else if (rowsSurvived >= currentLevel.oneStarRows)
        {
            newStars = 1;
        }
        else
        {
            newStars = 0;
        }
        
        if (newStars != currentStars)
        {
            currentStars = newStars;
            UpdateStarVisuals();
            Log($"Survival mode: {rowsSurvived} rows, now at {currentStars} stars");
        }
    }
    
    // Updates the visual state of all star images.
    // Uses sprites or color tints based on configuration.
    private void UpdateStarVisuals()
    {
        for (int i = 0; i < starImages.Count; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < currentStars;
            
            // Update sprite if provided
            if (starEarned != null && starEmpty != null)
            {
                starImages[i].sprite = isEarned ? starEarned : starEmpty;
            }
            
            // Update color if enabled
            if (useColorTint)
            {
                starImages[i].color = isEarned ? earnedColor : emptyColor;
            }
        }
    }
    
    // Resets the indicator for a new game.
    // Call this when restarting a level.
    public void Reset()
    {
        elapsedTime = 0f;
        isTracking = false;
        CacheReferences();
        InitializeDisplay();
    }
    
    // Returns the elapsed time for Classic mode.
    // Useful for displaying a timer alongside stars.
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[LiveStarIndicatorUI] {msg}");
    }
}