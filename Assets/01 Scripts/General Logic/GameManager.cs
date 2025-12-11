using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum GameState
{
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
    [SerializeField] private GameState currentState = GameState.Playing;
    
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
    public GameObject gameOverUI;
    public GameObject pauseMenuUI;
    public GameObject victoryUI;
    
    [Header("Events")]
    public UnityEvent onGameOver;
    public UnityEvent onPause;
    public UnityEvent onResume;
    public UnityEvent onVictory;
    public UnityEvent<WinConditionType> onWinConditionSet;
    public UnityEvent<int> onSurvivalRowSpawned; // (totalRowsSpawned)
    
    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
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
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        
        if (randomizeWinCondition)
        {
            winCondition = (WinConditionType)Random.Range(0, 3);
        }
        
        currentState = GameState.Playing;
        
        onWinConditionSet?.Invoke(winCondition);
        
        string conditionInfo = winCondition switch
        {
            WinConditionType.ReachTargetScore => $" (Target: {targetScore})",
            WinConditionType.Survival => $" (Start: {survivalStartingInterval}s, Deduct: {survivalIntervalDeduction}s, Min: {survivalMinInterval}s)",
            _ => ""
        };
        Debug.Log($"[GameManager] Win condition: {winCondition}{conditionInfo}");
    }
    
    // Called by GridRowSystem when a survival row is spawned.
    // Tracks total rows for future star rating system.
    public void OnSurvivalRowSpawned(int totalRowsSpawned)
    {
        onSurvivalRowSpawned?.Invoke(totalRowsSpawned);
        Debug.Log($"[GameManager] Survival row spawned. Total: {totalRowsSpawned}");
    }
    
    // Called by GridMatchSystem when all bubbles are cleared.
    // Handles win/loss logic based on active win condition.
    public void OnAllBubblesCleared()
    {
        if (!IsPlaying) return;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                Debug.Log("[GameManager] All bubbles cleared - Victory!");
                Victory();
                break;
                
            case WinConditionType.ReachTargetScore:
                int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
                if (currentScore < targetScore)
                {
                    Debug.Log($"[GameManager] Cleared but score {currentScore} < target {targetScore} - Game Over!");
                    GameOver();
                }
                else
                {
                    Victory();
                }
                break;
                
            case WinConditionType.Survival:
                Debug.Log("[GameManager] All bubbles cleared in survival - Victory!");
                Victory();
                break;
        }
    }
    
    // Called by GridMatchSystem after destruction sequence completes.
    // Returns true if victory was triggered.
    public bool CheckScoreVictory()
    {
        if (!IsPlaying) return false;
        if (winCondition != WinConditionType.ReachTargetScore) return false;
        
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        
        if (currentScore >= targetScore)
        {
            Debug.Log($"[GameManager] Target score {targetScore} reached! Score: {currentScore}");
            Victory();
            return true;
        }
        
        return false;
    }
    
    // Returns the total rows survived for star rating calculation.
    // Call this when game ends to get final survival stats.
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
        
        currentState = GameState.GameOver;
        Debug.Log("Game Over!");
        
        if (gameOverUI != null) gameOverUI.SetActive(true);
        
        onGameOver?.Invoke();
    }
    
    public void Victory()
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        currentState = GameState.Victory;
        Debug.Log("Victory!");
        
        if (victoryUI != null) victoryUI.SetActive(true);
        
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}