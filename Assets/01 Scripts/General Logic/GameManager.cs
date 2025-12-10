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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    
    [Header("UI References")]
    public GameObject gameOverUI;
    public GameObject pauseMenuUI;
    public GameObject victoryUI;
    
    [Header("Events")]
    public UnityEvent onGameOver;
    public UnityEvent onPause;
    public UnityEvent onResume;
    public UnityEvent onVictory;
    
    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Make sure UI is in correct state at start
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        
        currentState = GameState.Playing;
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}