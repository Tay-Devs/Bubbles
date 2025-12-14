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
    
    [Header("Padding (from screen edges)")]
    [Range(0f, 0.5f)]
    public float horizontalPadding = 0.1f;
    [Range(0f, 0.5f)]
    public float bottomPaddingPercent = 0.1f;
    
    [Header("Path Variation")]
    [Range(0f, 1f)]
    public float pathNoise = 0.3f; // How much random zigzag to add (0 = none, 1 = max)
    [Range(0f, 1f)]
    public float zigzagStrength = 0.5f; // How much each level alternates left/right
    
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
    private float viewportWidth;
    private int lowestVisibleLevel = 1;
    private int highestVisibleLevel = 1;
    
    private Vector2 lastPointerPosition;
    private bool isDragging = false;
    
    // Calculated bounds based on screen size
    private float minX;
    private float maxX;
    private float centerX;
    private float bottomPadding;
    private float availableWidth;
    
    // Cached positions for consistent path
    private Dictionary<int, float> cachedXPositions = new Dictionary<int, float>();
    
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
        if (levelDatabase != null)
        {
            totalLevels = levelDatabase.LevelCount;
            Debug.Log($"[LevelMapController] Database has {totalLevels} levels");
        }
        else
        {
            Debug.LogError("[LevelMapController] LevelDatabase is not assigned!");
            totalLevels = 10;
        }
        
        CalculateViewport();
        CalculateBounds();
        GenerateAllPositions();
        
        CheckForResults();
        InitializePool();
        
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
    
    private void CalculateViewport()
    {
        if (contentArea != null)
        {
            viewportHeight = contentArea.rect.height;
            viewportWidth = contentArea.rect.width;
        }
        else
        {
            viewportHeight = Screen.height;
            viewportWidth = Screen.width;
        }
        
        Debug.Log($"[LevelMapController] Viewport size: {viewportWidth}x{viewportHeight}");
    }
    
    private void CalculateBounds()
    {
        float horizontalPaddingPixels = viewportWidth * horizontalPadding;
        float halfWidth = viewportWidth / 2f;
        
        minX = -halfWidth + horizontalPaddingPixels;
        maxX = halfWidth - horizontalPaddingPixels;
        centerX = 0f;
        availableWidth = maxX - minX;
        
        bottomPadding = viewportHeight * bottomPaddingPercent;
        
        Debug.Log($"[LevelMapController] Bounds - X: [{minX}, {maxX}], Width: {availableWidth}, Bottom padding: {bottomPadding}");
    }
    
    // Pre-generates X positions with patterns, zigzag, and noise.
    private void GenerateAllPositions()
    {
        cachedXPositions.Clear();
        
        Random.InitState(12345);
        
        float previousX = centerX;
        int patternType = 0;
        int patternLength = 0;
        int patternProgress = 0;
        float patternStartX = 0f;
        float patternEndX = 0f;
        
        for (int level = 1; level <= totalLevels; level++)
        {
            float x;
            
            // Start new pattern if current one is done
            if (patternProgress >= patternLength)
            {
                patternType = Random.Range(0, 5);
                patternLength = Random.Range(3, 8);
                patternProgress = 0;
                patternStartX = previousX;
                
                switch (patternType)
                {
                    case 0: // Move to opposite side
                        patternEndX = previousX > centerX ? minX * 0.7f : maxX * 0.7f;
                        break;
                    case 1: // Stay on same side
                        patternEndX = previousX > centerX 
                            ? Random.Range(centerX * 0.2f, maxX * 0.8f) 
                            : Random.Range(minX * 0.8f, centerX * 0.2f);
                        break;
                    case 2: // Move to center
                        patternEndX = Random.Range(-availableWidth * 0.1f, availableWidth * 0.1f);
                        break;
                    case 3: // Wide swing to edge
                        patternEndX = previousX > centerX ? minX * 0.9f : maxX * 0.9f;
                        break;
                    case 4: // Gentle drift
                        patternEndX = Random.Range(minX * 0.5f, maxX * 0.5f);
                        break;
                }
            }
            
            float t = (float)patternProgress / Mathf.Max(1, patternLength - 1);
            
            // Base position from pattern
            switch (patternType)
            {
                case 0: // Transition to opposite
                    x = Mathf.Lerp(patternStartX, patternEndX, Mathf.SmoothStep(0, 1, t));
                    break;
                case 1: // Cluster movement
                    x = Mathf.Lerp(patternStartX, patternEndX, t);
                    break;
                case 2: // Smooth to center
                    x = Mathf.Lerp(patternStartX, patternEndX, Mathf.SmoothStep(0, 1, t));
                    break;
                case 3: // Wide swing with curve
                    float swing = Mathf.Sin(t * Mathf.PI);
                    x = Mathf.Lerp(patternStartX, patternEndX, t);
                    x += swing * availableWidth * 0.2f * Mathf.Sign(patternEndX - patternStartX);
                    break;
                case 4: // Gentle wave
                    x = Mathf.Lerp(patternStartX, patternEndX, t);
                    x += Mathf.Sin(t * Mathf.PI * 2) * availableWidth * 0.1f;
                    break;
                default:
                    x = Mathf.Lerp(patternStartX, patternEndX, t);
                    break;
            }
            
            // Apply zigzag - alternates left/right each level
            float zigzagAmount = availableWidth * 0.15f * zigzagStrength;
            float zigzag = (level % 2 == 0) ? zigzagAmount : -zigzagAmount;
            x += zigzag;
            
            // Apply noise - random offset per level
            float noiseAmount = availableWidth * 0.2f * pathNoise;
            float noise = Random.Range(-noiseAmount, noiseAmount);
            x += noise;
            
            // Clamp to bounds
            x = Mathf.Clamp(x, minX, maxX);
            
            cachedXPositions[level] = x;
            previousX = x;
            patternProgress++;
        }
        
        Random.InitState((int)System.DateTime.Now.Ticks);
        
        Debug.Log($"[LevelMapController] Generated positions for {totalLevels} levels (noise: {pathNoise}, zigzag: {zigzagStrength})");
    }
    
    private void CheckForResults()
    {
        if (gameSession == null || !gameSession.hasResults) return;
        
        if (gameSession.selectedLevel != null && LevelDataManager.Instance != null)
        {
            int levelNum = gameSession.selectedLevel.levelNumber;
            
            if (gameSession.starsEarned > 0)
            {
                LevelDataManager.Instance.CompleteLevel(levelNum, gameSession.starsEarned);
                Debug.Log($"[LevelMapController] Saved results for level {levelNum}: {gameSession.starsEarned} stars");
            }
            else
            {
                Debug.Log($"[LevelMapController] No stars earned for level {levelNum}, not saving");
            }
        }
        
        gameSession.ClearResults();
    }
    
    private void InitializePool()
    {
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
        
        float maxScrollY = (totalLevels - 1) * nodeSpacingY;
        currentScrollY = Mathf.Clamp(currentScrollY, 0, maxScrollY);
        
        RefreshVisibleNodes();
    }
    
    public void ScrollToLevel(int levelNumber)
    {
        levelNumber = Mathf.Clamp(levelNumber, 1, totalLevels);
        
        if (levelNumber == 1)
        {
            currentScrollY = 0;
        }
        else
        {
            currentScrollY = (levelNumber - 1) * nodeSpacingY - (viewportHeight / 2f);
        }
        
        float maxScrollY = (totalLevels - 1) * nodeSpacingY;
        currentScrollY = Mathf.Clamp(currentScrollY, 0, maxScrollY);
        
        RefreshVisibleNodes();
    }
    
    private void RefreshVisibleNodes()
    {
        float bottomBufferPixels = viewportHeight * bottomBufferScreens;
        int bottomBufferNodeCount = Mathf.CeilToInt(bottomBufferPixels / nodeSpacingY);
        
        int bottomLevel = Mathf.Max(1, Mathf.FloorToInt(currentScrollY / nodeSpacingY) - bottomBufferNodeCount);
        int topLevel = Mathf.CeilToInt((currentScrollY + viewportHeight) / nodeSpacingY) + topBufferNodes;
        
        bottomLevel = Mathf.Clamp(bottomLevel, 1, totalLevels);
        topLevel = Mathf.Clamp(topLevel, 1, totalLevels);
        
        lowestVisibleLevel = bottomLevel;
        highestVisibleLevel = topLevel;
        
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
        
        for (int level = bottomLevel; level <= topLevel; level++)
        {
            if (level > totalLevels) break;
            
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
        if (cachedXPositions.ContainsKey(level))
        {
            return cachedXPositions[level];
        }
        
        return centerX;
    }
    
    public float GetYPositionForLevel(int level)
    {
        float bottomEdge = -viewportHeight / 2f;
        return bottomEdge + bottomPadding + (level - 1) * nodeSpacingY;
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