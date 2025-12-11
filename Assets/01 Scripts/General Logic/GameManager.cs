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
    ReachTargetScore
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    
    [Header("Win Condition")]
    public WinConditionType winCondition = WinConditionType.ClearAllBubbles;
    public bool randomizeWinCondition = false;
    public int targetScore = 1000;
    
    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject pauseMenuUI;
    public GameObject victoryUI;
    
    [Header("Events")]
    public UnityEvent onGameOver;
    public UnityEvent onPause;
    public UnityEvent onResume;
    public UnityEvent onVictory;
    public UnityEvent<WinConditionType> onWinConditionSet; // UI can subscribe to show/hide elements
    
    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    public WinConditionType ActiveWinCondition => winCondition;
    public int TargetScore => targetScore;
    
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
            winCondition = (WinConditionType)Random.Range(0, 2);
        }
        
        currentState = GameState.Playing;
        
        onWinConditionSet?.Invoke(winCondition);
        Debug.Log($"[GameManager] Win condition: {winCondition}" + 
                  (winCondition == WinConditionType.ReachTargetScore ? $" (Target: {targetScore})" : ""));
    }
    
    // Called by GridMatchSystem when all bubbles are cleared.
    // Decides win/loss based on active win condition.
    public void OnAllBubblesCleared()
    {
        if (!IsPlaying) return;
        
        if (winCondition == WinConditionType.ClearAllBubbles)
        {
            Debug.Log("[GameManager] All bubbles cleared - Victory!");
            Victory();
        }
        else if (winCondition == WinConditionType.ReachTargetScore)
        {
            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            if (currentScore < targetScore)
            {
                Debug.Log($"[GameManager] Cleared but score {currentScore} < target {targetScore} - Game Over!");
                GameOver();
            }
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