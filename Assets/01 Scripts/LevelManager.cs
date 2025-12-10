using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Current Level")]
    public LevelData currentLevel;
    
    [Header("References")]
    public HexGrid grid;
    public GridRowSystem rowSystem;
    public ScoreManager scoreManager;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public Action<LevelData> onLevelLoaded;
    public Action onWinConditionMet;
    public Action<int> onStarsEarned; // Stars earned (1-3)
    public Action<int> onScoreProgress; // Current score for score-based levels
    public Action<float> onTimerTick; // Time until next row for survival mode
    public Action<float> onElapsedTimeChanged; // Total elapsed time
    public Action<int> onBubbleUsed; // Bubble count changed
    
    // Survival mode timer
    private float survivalTimer;
    private float currentSpawnInterval;
    private bool survivalTimerActive = false;
    
    // Performance tracking
    private float elapsedTime;
    private int bubblesUsed;
    private bool isLevelActive = false;
    
    // Score tracking for ReachScore mode
    private bool scoreWinChecked = false;
    
    // Public accessors
    public float ElapsedTime => elapsedTime;
    public int BubblesUsed => bubblesUsed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        if (grid == null) grid = FindFirstObjectByType<HexGrid>();
        if (rowSystem == null && grid != null) rowSystem = grid.RowSystem;
        if (scoreManager == null) scoreManager = ScoreManager.Instance;
        
        // Subscribe to score changes for ReachScore mode
        if (scoreManager != null)
        {
            scoreManager.onScoreChanged += OnScoreChanged;
        }
        
        // Load level if one is assigned
        if (currentLevel != null)
        {
            LoadLevel(currentLevel);
        }
    }
    
    void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.onScoreChanged -= OnScoreChanged;
        }
    }
    
    void Update()
    {
        if (!isLevelActive) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        
        // Track elapsed time
        elapsedTime += Time.deltaTime;
        onElapsedTimeChanged?.Invoke(elapsedTime);
        
        // Survival mode timer
        if (survivalTimerActive)
        {
            survivalTimer -= Time.deltaTime;
            onTimerTick?.Invoke(survivalTimer);
            
            if (survivalTimer <= 0f)
            {
                SpawnSurvivalRow();
            }
        }
    }

    // Loads a level and configures all systems according to LevelData.
    // Call this to start a new level with the specified configuration.
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Log("Cannot load null level data");
            return;
        }
        
        currentLevel = levelData;
        Log($"Loading level: {levelData.levelName}");
        
        // Reset tracking
        elapsedTime = 0f;
        bubblesUsed = 0;
        isLevelActive = true;
        scoreWinChecked = false;
        
        // Configure grid
        if (grid != null)
        {
            grid.width = levelData.gridWidth;
            grid.startingHeight = levelData.startingRows;
        }
        
        // Configure shot system
        if (rowSystem != null)
        {
            if (levelData.winCondition == WinConditionType.SurvivalClear)
            {
                rowSystem.shotsBeforeNewRow = 999;
            }
            else if (levelData.shotsBeforeNewRow > 0)
            {
                rowSystem.shotsBeforeNewRow = levelData.shotsBeforeNewRow;
            }
        }
        
        // Reset score
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        
        // Setup win condition specific logic
        SetupWinCondition(levelData);
        
        onLevelLoaded?.Invoke(levelData);
        Log($"Level loaded: {levelData.levelName}, Win condition: {levelData.winCondition}");
    }
    
    // Configures win condition specific behavior.
    private void SetupWinCondition(LevelData levelData)
    {
        survivalTimerActive = false;
        
        switch (levelData.winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                Log("Win condition: Clear all bubbles");
                break;
                
            case WinConditionType.ReachScore:
                Log($"Win condition: Reach score {levelData.targetScore}");
                break;
                
            case WinConditionType.SurvivalClear:
                currentSpawnInterval = levelData.rowSpawnInterval;
                survivalTimer = currentSpawnInterval;
                survivalTimerActive = true;
                Log($"Win condition: Survival clear, row every {currentSpawnInterval}s");
                break;
        }
    }
    
    // Call this when a bubble is shot to track usage
    public void RegisterBubbleUsed()
    {
        bubblesUsed++;
        onBubbleUsed?.Invoke(bubblesUsed);
        Log($"Bubbles used: {bubblesUsed}");
    }
    
    // Called when score changes. Checks for ReachScore win condition.
    private void OnScoreChanged(int newScore, int pointsAdded)
    {
        if (currentLevel == null) return;
        if (currentLevel.winCondition != WinConditionType.ReachScore) return;
        if (scoreWinChecked) return;
        
        onScoreProgress?.Invoke(newScore);
        
        if (newScore >= currentLevel.targetScore)
        {
            scoreWinChecked = true;
            Log($"Score target reached! {newScore}/{currentLevel.targetScore}");
            TriggerWin();
        }
    }
    
    // Spawns a new row in survival mode and resets the timer.
    private void SpawnSurvivalRow()
    {
        if (rowSystem == null) return;
        
        Log("Survival mode: Spawning new row");
        rowSystem.SpawnNewRowAtTop();
        
        if (currentLevel.accelerateOverTime)
        {
            currentSpawnInterval *= currentLevel.accelerationRate;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, currentLevel.minSpawnInterval);
            Log($"Next row in {currentSpawnInterval}s");
        }
        
        survivalTimer = currentSpawnInterval;
    }
    
    // Call this to trigger a win. Calculates stars earned.
    public void TriggerWin()
    {
        isLevelActive = false;
        survivalTimerActive = false;
        
        // Calculate stars based on win condition
        int stars = CalculateStarsEarned();
        
        Log($"Level complete! Stars earned: {stars}");
        
        onStarsEarned?.Invoke(stars);
        onWinConditionMet?.Invoke();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Victory();
        }
    }
    
    // Calculates how many stars were earned based on performance.
    public int CalculateStarsEarned()
    {
        if (currentLevel == null) return 0;
        
        float performanceValue;
        
        if (currentLevel.winCondition == WinConditionType.ReachScore)
        {
            // Bubble count based
            performanceValue = bubblesUsed;
        }
        else
        {
            // Time based
            performanceValue = elapsedTime;
        }
        
        return currentLevel.CalculateStars(performanceValue);
    }
    
    // Checks if the current win condition is met.
    public bool CheckWinCondition()
    {
        if (currentLevel == null) return false;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        
        switch (currentLevel.winCondition)
        {
            case WinConditionType.ClearAllBubbles:
            case WinConditionType.SurvivalClear:
                if (grid != null && grid.IsGridEmpty())
                {
                    Log("All bubbles cleared - Win!");
                    TriggerWin();
                    return true;
                }
                break;
                
            case WinConditionType.ReachScore:
                // Already handled in OnScoreChanged
                break;
        }
        
        return false;
    }
    
    // Gets progress towards win condition (0-1 range)
    public float GetWinProgress()
    {
        if (currentLevel == null) return 0f;
        
        switch (currentLevel.winCondition)
        {
            case WinConditionType.ReachScore:
                if (scoreManager != null && currentLevel.targetScore > 0)
                {
                    return Mathf.Clamp01((float)scoreManager.CurrentScore / currentLevel.targetScore);
                }
                break;
                
            case WinConditionType.ClearAllBubbles:
            case WinConditionType.SurvivalClear:
                if (grid != null && grid.IsGridEmpty()) return 1f;
                break;
        }
        
        return 0f;
    }
    
    // Gets formatted elapsed time string (M:SS)
    public string GetElapsedTimeFormatted()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        return $"{minutes}:{seconds:D2}";
    }
    
    // Pause/Resume helpers
    public void PauseSurvivalTimer()
    {
        survivalTimerActive = false;
    }
    
    public void ResumeSurvivalTimer()
    {
        if (currentLevel != null && currentLevel.winCondition == WinConditionType.SurvivalClear)
        {
            survivalTimerActive = true;
        }
    }
    
    void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[LevelManager] {msg}");
    }
}