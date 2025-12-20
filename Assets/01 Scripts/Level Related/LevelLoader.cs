using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }
    
    [Header("Data References")]
    public GameSession gameSession;
    public LevelDatabase levelDatabase;
    
    [Header("Scene")]
    public string levelSelectSceneName = "LevelSelect";
    
    [Header("References")]
    public HexGrid hexGrid;
    public GameManager gameManager;
    
    [Header("Fallback (if no session data)")]
    public LevelConfig fallbackLevel;
    
    private LevelConfig currentLevel;
    private float levelStartTime;
    private bool levelComplete = false;
    
    public LevelConfig CurrentLevel => currentLevel;
    public float ElapsedTime => Time.time - levelStartTime;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        LoadLevelFromSession();
    }
    
    void Start()
    {
        levelStartTime = Time.time;
    }
    
    private void LoadLevelFromSession()
    {
        if (gameSession != null && gameSession.selectedLevel != null)
        {
            currentLevel = gameSession.selectedLevel;
        }
        else if (fallbackLevel != null)
        {
            currentLevel = fallbackLevel;
            Debug.LogWarning("[LevelLoader] No session data, using fallback level");
        }
        else
        {
            Debug.LogError("[LevelLoader] No level config available!");
            return;
        }
        
        ApplyLevelConfig();
        Debug.Log($"[LevelLoader] Loaded level {currentLevel.levelNumber}: {currentLevel.levelName}");
    }
    
    private void ApplyLevelConfig()
    {
        if (currentLevel == null) return;
        
        if (hexGrid != null)
        {
            hexGrid.width = currentLevel.gridWidth;
            hexGrid.startingHeight = currentLevel.gridHeight;
            Debug.Log($"[LevelLoader] Set grid size: {currentLevel.gridWidth}x{currentLevel.gridHeight}");
        }
        else
        {
            Debug.LogError("[LevelLoader] HexGrid reference is missing!");
        }
        
        if (gameManager != null)
        {
            gameManager.winCondition = currentLevel.winCondition;
            gameManager.randomizeWinCondition = false;
            
            switch (currentLevel.winCondition)
            {
                case WinConditionType.ReachTargetScore:
                    gameManager.targetScore = currentLevel.targetScore;
                    break;
                    
                case WinConditionType.Survival:
                    gameManager.survivalStartingInterval = currentLevel.survivalStartingInterval;
                    gameManager.survivalIntervalDeduction = currentLevel.survivalIntervalDeduction;
                    gameManager.survivalMinInterval = currentLevel.survivalMinInterval;
                    break;
            }
        }
    }
    
    public BubbleType[] GetAvailableColors()
    {
        if (currentLevel != null && currentLevel.availableColors.Count > 0)
        {
            return currentLevel.availableColors.ToArray();
        }
        
        return (BubbleType[])System.Enum.GetValues(typeof(BubbleType));
    }
    
    // Called when player wins.
    public void OnLevelWon(bool clearedAllBubbles = false)
    {
        if (levelComplete) return;
        levelComplete = true;
        
        int stars = CalculateAndStoreResults(true, clearedAllBubbles);
        Debug.Log($"[LevelLoader] Level won with {stars} stars!");
    }
    
    // Called when player loses.
    public void OnLevelLost()
    {
        if (levelComplete) return;
        levelComplete = true;
        
        int stars = CalculateAndStoreResults(false, false);
        Debug.Log($"[LevelLoader] Level lost with {stars} stars");
    }
    
    // Calculates stars and stores results in GameSession.
    // Passes won flag so ClearAllBubbles mode returns 0 stars on loss.
    private int CalculateAndStoreResults(bool won, bool clearedAllBubbles)
    {
        if (currentLevel == null || gameSession == null) return 0;
        
        float completionTime = ElapsedTime;
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int rowsSurvived = gameManager != null ? gameManager.GetSurvivalRowsCount() : 0;
        
        // Pass won flag to CalculateStars
        int stars = currentLevel.CalculateStars(won, completionTime, score, rowsSurvived, clearedAllBubbles);
        
        gameSession.SetResults(won, stars, score, completionTime, rowsSurvived);
        
        return stars;
    }
    
    public void ReturnToLevelSelect()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }
    
    public void RestartLevel()
    {
        if (gameSession != null)
        {
            gameSession.ClearResults();
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}