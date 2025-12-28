using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
    WaitingToStart, // New state - waiting for intro popup
    Playing,
    Paused,
    GameOver,
    Victory
}

public enum WinConditionType
{
    ClearAllBubbles,
    ReachTargetScore,
    Survival
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("State")]
    [SerializeField] private GameState currentState = GameState.WaitingToStart;
    
    [Header("Intro Settings")]
    public bool showLevelIntro = true; // Set false to skip intro
    
    [Header("Win Condition")]
    public WinConditionType winCondition = WinConditionType.ClearAllBubbles;
    public bool randomizeWinCondition = false;
    
    [Header("Target Score Settings")]
    public int targetScore = 1000;
    
    [Header("Survival Settings")]
    public float survivalStartingInterval = 10f;
    public float survivalIntervalDeduction = 0.5f;
    public float survivalMinInterval = 2f;
    
    [Header("UI References")]
    public GameObject resultsUI;
    public GameObject pauseMenuUI;
    
    [Header("Events")]
    public UnityEvent onGameOver;
    public UnityEvent onPause;
    public UnityEvent onResume;
    public UnityEvent onVictory;
    public UnityEvent onGameStart; // New event - fired when intro dismissed
    public UnityEvent<WinConditionType> onWinConditionSet;
    public UnityEvent<int> onSurvivalRowSpawned;
    
    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public bool IsWaitingToStart => currentState == GameState.WaitingToStart;
    public WinConditionType ActiveWinCondition => winCondition;
    public int TargetScore => targetScore;
    public float SurvivalStartingInterval => survivalStartingInterval;
    public float SurvivalIntervalDeduction => survivalIntervalDeduction;
    public float SurvivalMinInterval => survivalMinInterval;
    
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
        if (resultsUI != null) resultsUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        
        if (randomizeWinCondition)
        {
            winCondition = (WinConditionType)Random.Range(0, 3);
        }
        
        // Start in waiting state if showing intro, otherwise start playing
        if (showLevelIntro)
        {
            currentState = GameState.WaitingToStart;
            Debug.Log("[GameManager] Waiting for level intro to complete...");
        }
        else
        {
            currentState = GameState.Playing;
            Debug.Log("[GameManager] No intro - starting immediately");
        }
        
        onWinConditionSet?.Invoke(winCondition);
        
        string conditionInfo = winCondition switch
        {
            WinConditionType.ReachTargetScore => $" (Target: {targetScore})",
            WinConditionType.Survival => $" (Start: {survivalStartingInterval}s, Deduct: {survivalIntervalDeduction}s, Min: {survivalMinInterval}s)",
            _ => ""
        };
        //Debug.Log($"[GameManager] Win condition: {winCondition}{conditionInfo}");
    }
    
    // Called by LevelIntroUI when intro popup is dismissed.
    public void StartGame()
    {
        if (currentState != GameState.WaitingToStart) return;
        
        currentState = GameState.Playing;
        Debug.Log("[GameManager] Level intro complete - game started!");
        
        onGameStart?.Invoke();
    }
    
    public void OnSurvivalRowSpawned(int totalRowsSpawned)
    {
        onSurvivalRowSpawned?.Invoke(totalRowsSpawned);
        Debug.Log($"[GameManager] Survival row spawned. Total: {totalRowsSpawned}");
    }
    
    public void OnAllBubblesCleared()
    {
        if (!IsPlaying) return;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                Debug.Log("[GameManager] All bubbles cleared - Victory!");
                Victory(true);
                break;
                
            case WinConditionType.ReachTargetScore:
                Debug.Log("[GameManager] All bubbles cleared in score mode - Victory!");
                Victory(true);
                break;
                
            case WinConditionType.Survival:
                // Trigger bonus star animations before victory
                TriggerBonusStarsForClear();
                Debug.Log("[GameManager] All bubbles cleared in survival - Victory!");
                Victory(true);
                break;
        }
    }
    
    // Finds LiveStarIndicatorUI and triggers bonus star animations for grid clear.
    // Awards remaining stars when player clears the grid in Survival mode.
    private void TriggerBonusStarsForClear()
    {
        LiveStarIndicatorUI starIndicator = FindFirstObjectByType<LiveStarIndicatorUI>();
        if (starIndicator != null)
        {
            starIndicator.TriggerBonusStarsOnClear();
        }
    }
    
    public bool CheckScoreVictory()
    {
        if (!IsPlaying) return false;
        if (winCondition != WinConditionType.ReachTargetScore) return false;
        
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        
        if (currentScore >= targetScore)
        {
            Debug.Log($"[GameManager] Target score {targetScore} reached! Score: {currentScore}");
            Victory(false);
            return true;
        }
        
        return false;
    }
    
    public int GetSurvivalRowsCount()
    {
        var grid = FindFirstObjectByType<HexGrid>();
        if (grid != null && grid.RowSystem != null)
        {
            return grid.RowSystem.SurvivalRowsSpawned;
        }
        return 0;
    }
    
    public void GameOver()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        // In Survival mode, check if player earned 3 stars - if so, it's a victory
        if (winCondition == WinConditionType.Survival)
        {
            int rowsSurvived = GetSurvivalRowsCount();
            LevelConfig currentLevel = LevelLoader.Instance != null ? LevelLoader.Instance.CurrentLevel : null;
            
            if (currentLevel != null && rowsSurvived >= currentLevel.threeStarRows)
            {
                Debug.Log($"[GameManager] Survival mode - reached 3 stars ({rowsSurvived} rows) - Victory!");
                Victory(false);
                return;
            }
        }
        
        currentState = GameState.GameOver;
        Debug.Log("Game Over!");
        
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.OnLevelLost();
        }
        
        if (resultsUI != null) resultsUI.SetActive(true);
        
        onGameOver?.Invoke();
    }
    
    public void Victory(bool clearedAllBubbles = false)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        currentState = GameState.Victory;
        Debug.Log("Victory!");
        
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.OnLevelWon(clearedAllBubbles);
        }
        
        if (resultsUI != null) resultsUI.SetActive(true);
        
        onVictory?.Invoke();
    }
    
    public void Pause()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        
        onPause?.Invoke();
    }
    
    public void Resume()
    {
        if (currentState != GameState.Paused) return;
        
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        
        onResume?.Invoke();
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.RestartLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.ReturnToLevelSelect();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}