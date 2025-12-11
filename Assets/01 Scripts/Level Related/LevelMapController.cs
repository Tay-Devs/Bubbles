using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelMapController : MonoBehaviour
{
    public static LevelMapController Instance { get; private set; }
    
    [Header("Data References")]
    public LevelDatabase levelDatabase;
    public GameSession gameSession;
    
    [Header("Scene")]
    public string gameSceneName = "Game";
    
    [Header("Node Settings")]
    public LevelNode nodePrefab;
    public int poolSize = 20;
    public float nodeSpacingY = 200f;
    
    [Header("Zigzag Pattern")]
    public float minX = -300f;
    public float maxX = 300f;
    public float zigzagVariation = 50f;
    
    [Header("Scroll Settings")]
    public RectTransform contentArea;
    public float scrollSpeed = 500f;
    public float dragSensitivity = 1f;
    
    [Header("Path Rendering")]
    public LevelPathRenderer pathRenderer;
    
    [Header("Buffer Settings")]
    public int topBufferNodes = 3;
    public float bottomBufferScreens = 1f;
    
    private List<LevelNode> nodePool = new List<LevelNode>();
    private Dictionary<int, LevelNode> activeNodes = new Dictionary<int, LevelNode>();
    
    private float currentScrollY = 0f;
    private float viewportHeight;
    private int lowestVisibleLevel = 1;
    private int highestVisibleLevel = 1;
    
    private Vector2 lastPointerPosition;
    private bool isDragging = false;
    
    private Dictionary<int, float> cachedXPositions = new Dictionary<int, float>();
    
    // Total levels available from database
    private int totalLevels = 0;
    
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
        // Get total level count from database
        if (levelDatabase != null)
        {
            totalLevels = levelDatabase.LevelCount;
            Debug.Log($"[LevelMapController] Database has {totalLevels} levels");
        }
        else
        {
            Debug.LogError("[LevelMapController] LevelDatabase is not assigned!");
            totalLevels = 10; // Fallback
        }
        
        CheckForResults();
        InitializePool();
        CalculateViewport();
        
        // Scroll to highest unlocked level, but cap at total levels
        int targetLevel = Mathf.Min(
            LevelDataManager.Instance.GetHighestUnlockedLevel(),
            totalLevels
        );
        ScrollToLevel(targetLevel);
        
        RefreshVisibleNodes();
    }
    
    void Update()
    {
        HandleScrollInput();
    }
    
    private void CheckForResults()
    {
        if (gameSession == null || !gameSession.hasResults) return;
        
        if (gameSession.selectedLevel != null && LevelDataManager.Instance != null)
        {
            int levelNum = gameSession.selectedLevel.levelNumber;
            
            if (gameSession.levelWon || gameSession.starsEarned > 0)
            {
                LevelDataManager.Instance.CompleteLevel(levelNum, gameSession.starsEarned);
                Debug.Log($"[LevelMapController] Saved results for level {levelNum}: {gameSession.starsEarned} stars");
            }
        }
        
        gameSession.ClearResults();
    }
    
    private void InitializePool()
    {
        // Pool size should be at least enough for visible + buffer, but not more than total levels
        int actualPoolSize = Mathf.Min(poolSize, totalLevels);
        
        for (int i = 0; i < actualPoolSize; i++)
        {
            LevelNode node = Instantiate(nodePrefab);
            node.transform.SetParent(contentArea, false);
            node.gameObject.SetActive(false);
            nodePool.Add(node);
        }
        
        Debug.Log($"[LevelMapController] Created pool of {actualPoolSize} nodes");
    }
    
    private void CalculateViewport()
    {
        if (contentArea != null)
        {
            viewportHeight = contentArea.rect.height;
        }
        else
        {
            viewportHeight = Screen.height;
        }
    }
    
    private void HandleScrollInput()
    {
        float scrollDelta = 0f;
        
        if (Mouse.current != null)
        {
            scrollDelta += Mouse.current.scroll.ReadValue().y * scrollSpeed * Time.deltaTime * 0.01f;
        }
        
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchDelta = Touchscreen.current.primaryTouch.delta.ReadValue();
            scrollDelta += touchDelta.y * dragSensitivity;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            if (isDragging)
            {
                Vector2 delta = mousePosition - lastPointerPosition;
                scrollDelta += delta.y * dragSensitivity;
            }
            
            lastPointerPosition = mousePosition;
            isDragging = true;
        }
        else
        {
            isDragging = false;
        }
        
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            Scroll(scrollDelta);
        }
    }
    
    public void Scroll(float delta)
    {
        currentScrollY += delta;
        
        // Clamp scroll between level 1 and max level
        float maxScrollY = (totalLevels - 1) * nodeSpacingY;
        currentScrollY = Mathf.Clamp(currentScrollY, 0, maxScrollY);
        
        RefreshVisibleNodes();
    }
    
    public void ScrollToLevel(int levelNumber)
    {
        // Clamp to valid level range
        levelNumber = Mathf.Clamp(levelNumber, 1, totalLevels);
        
        currentScrollY = (levelNumber - 1) * nodeSpacingY - (viewportHeight / 2f);
        
        float maxScrollY = (totalLevels - 1) * nodeSpacingY;
        currentScrollY = Mathf.Clamp(currentScrollY, 0, maxScrollY);
        
        RefreshVisibleNodes();
    }
    
    // Calculates which levels should be visible and updates node pool.
    // Only creates nodes for levels that exist in the database.
    private void RefreshVisibleNodes()
    {
        float bottomBufferPixels = viewportHeight * bottomBufferScreens;
        int bottomBufferNodeCount = Mathf.CeilToInt(bottomBufferPixels / nodeSpacingY);
        
        // Calculate visible range, clamped to actual level count
        int bottomLevel = Mathf.Max(1, Mathf.FloorToInt(currentScrollY / nodeSpacingY) - bottomBufferNodeCount);
        int topLevel = Mathf.CeilToInt((currentScrollY + viewportHeight) / nodeSpacingY) + topBufferNodes;
        
        // Clamp to database level count
        bottomLevel = Mathf.Clamp(bottomLevel, 1, totalLevels);
        topLevel = Mathf.Clamp(topLevel, 1, totalLevels);
        
        lowestVisibleLevel = bottomLevel;
        highestVisibleLevel = topLevel;
        
        // Remove nodes outside visible range
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeNodes)
        {
            if (kvp.Key < bottomLevel || kvp.Key > topLevel)
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (int level in toRemove)
        {
            ReturnNodeToPool(level);
        }
        
        // Add nodes for visible levels (only if they exist in database)
        for (int level = bottomLevel; level <= topLevel; level++)
        {
            if (level > totalLevels) break; // Don't create nodes beyond database
            
            if (!activeNodes.ContainsKey(level))
            {
                SpawnNodeForLevel(level);
            }
            else
            {
                UpdateNodePosition(activeNodes[level], level);
            }
        }
        
        if (pathRenderer != null)
        {
            pathRenderer.UpdatePath(activeNodes, lowestVisibleLevel, highestVisibleLevel);
        }
    }
    
    private void SpawnNodeForLevel(int level)
    {
        // Don't spawn if level doesn't exist
        if (level < 1 || level > totalLevels) return;
        
        LevelNode node = GetNodeFromPool();
        if (node == null)
        {
            Debug.LogWarning($"[LevelMapController] No available node in pool for level {level}");
            return;
        }
        
        bool unlocked = LevelDataManager.Instance.IsLevelUnlocked(level);
        int stars = LevelDataManager.Instance.GetStarsForLevel(level);
        
        node.Setup(level, unlocked, stars);
        UpdateNodePosition(node, level);
        node.gameObject.SetActive(true);
        
        activeNodes[level] = node;
    }
    
    private void UpdateNodePosition(LevelNode node, int level)
    {
        float x = GetXPositionForLevel(level);
        float y = GetYPositionForLevel(level) - currentScrollY;
        
        node.transform.localPosition = new Vector3(x, y, 0);
    }
    
    public float GetXPositionForLevel(int level)
    {
        if (!cachedXPositions.ContainsKey(level))
        {
            Random.InitState(level * 12345);
            
            float baseX = (level % 2 == 0) ? maxX : minX;
            float variation = Random.Range(-zigzagVariation, zigzagVariation);
            
            if (Random.value < 0.2f)
            {
                baseX = Random.Range(minX * 0.5f, maxX * 0.5f);
            }
            
            cachedXPositions[level] = baseX + variation;
            
            Random.InitState((int)System.DateTime.Now.Ticks);
        }
        
        return cachedXPositions[level];
    }
    
    public float GetYPositionForLevel(int level)
    {
        return (level - 1) * nodeSpacingY;
    }
    
    private LevelNode GetNodeFromPool()
    {
        foreach (var node in nodePool)
        {
            if (!node.gameObject.activeSelf)
            {
                return node;
            }
        }
        
        Debug.LogWarning("[LevelMapController] Node pool exhausted!");
        return null;
    }
    
    private void ReturnNodeToPool(int level)
    {
        if (activeNodes.ContainsKey(level))
        {
            activeNodes[level].gameObject.SetActive(false);
            activeNodes.Remove(level);
        }
    }
    
    // Called by LevelNode when clicked.
    public void LoadLevel(int levelNumber)
    {
        Debug.Log($"[LevelMapController] LoadLevel called for level {levelNumber}");
        
        if (levelDatabase == null)
        {
            Debug.LogError("[LevelMapController] LevelDatabase is not assigned!");
            return;
        }
        
        if (gameSession == null)
        {
            Debug.LogError("[LevelMapController] GameSession is not assigned!");
            return;
        }
        
        LevelConfig config = levelDatabase.GetLevel(levelNumber);
        if (config == null)
        {
            Debug.LogError($"[LevelMapController] Level {levelNumber} not found in database!");
            return;
        }
        
        gameSession.SelectLevel(config);
        
        Debug.Log($"[LevelMapController] Loading scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }
}