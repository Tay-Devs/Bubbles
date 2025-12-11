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
    public UnityEvent<int> onSurvivalRowSpawned;
    
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
                int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
                if (currentScore < targetScore)
                {
                    Debug.Log($"[GameManager] Cleared but score {currentScore} < target {targetScore} - Game Over!");
                    GameOver();
                }
                else
                {
                    Victory(true);
                }
                break;
                
            case WinConditionType.Survival:
                Debug.Log("[GameManager] All bubbles cleared in survival - Victory!");
                Victory(true);
                break;
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
        
        currentState = GameState.GameOver;
        Debug.Log("Game Over!");
        
        // Notify LevelLoader
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.OnLevelLost();
        }
        
        if (gameOverUI != null) gameOverUI.SetActive(true);
        
        onGameOver?.Invoke();
    }
    
    // Victory with flag for whether all bubbles were cleared.
    public void Victory(bool clearedAllBubbles = false)
    {
        if (currentState == GameState.GameOver || currentState == GameState.Victory) return;
        
        currentState = GameState.Victory;
        Debug.Log("Victory!");
        
        // Notify LevelLoader
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.OnLevelWon(clearedAllBubbles);
        }
        
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
    
    // Now uses LevelLoader for restart.
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
    
    // Now uses LevelLoader to return to level select.
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