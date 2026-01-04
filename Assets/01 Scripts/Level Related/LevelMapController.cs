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
    public string gameSceneName = "Game";
    
    [Header("Popup")]
    public LevelPopup levelPopup;
    
    [Header("Node Settings")]
    public LevelNode nodePrefab;
    public RectTransform contentArea;
    public int poolSize = 20;
    public float nodeSpacingY = 200f;
    
    [Header("Padding (% of screen)")]
    [Range(0f, 0.5f)] public float horizontalPadding = 0.1f;
    [Range(0f, 0.5f)] public float bottomPadding = 0.1f;
    [Range(0f, 0.5f)] public float topPadding = 0.1f;
    
    [Header("Path Variation")]
    [Range(0f, 1f)] public float pathNoise = 0.3f;
    [Range(0f, 1f)] public float zigzagStrength = 0.5f;
    public bool randomizePattern = true;
    public int patternSeed = 12345;
    
    [Header("Scroll Settings")]
    public float scrollSpeed = 500f;
    public float dragSensitivity = 1f;
    
    [Header("Buffer (extra nodes to render)")]
    public int bufferNodes = 3;
    
    [Header("References")]
    public LevelPathRenderer pathRenderer;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Runtime
    private List<LevelNode> nodePool = new List<LevelNode>();
    private Dictionary<int, LevelNode> activeNodes = new Dictionary<int, LevelNode>();
    private Dictionary<int, float> cachedXPositions = new Dictionary<int, float>();
    
    private float currentScrollY;
    private float viewportWidth, viewportHeight;
    private float minX, maxX, availableWidth;
    private int totalLevels;
    
    private Vector2 lastPointerPos;
    private bool isDragging;
    
    // Tracks which levels were just unlocked this session
    private HashSet<int> newlyUnlockedLevels = new HashSet<int>();
    
    // Calculated padding in pixels
    private float BottomPaddingPx => viewportHeight * bottomPadding;
    private float TopPaddingPx => viewportHeight * topPadding;
    private float MaxScrollY => Mathf.Max(0, BottomPaddingPx + (totalLevels - 1) * nodeSpacingY - (viewportHeight - TopPaddingPx));
    
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    void Start()
    {
        totalLevels = levelDatabase != null ? levelDatabase.LevelCount : 10;
        
        InitializeViewport();
        GeneratePositions();
        CheckForResults();
        CreatePool();
        
        int targetLevel = DetermineScrollTarget();
        ScrollToLevel(targetLevel);
    }
    
    // Determines which level to scroll to on start.
    // If there are newly unlocked levels, scrolls to the first one.
    private int DetermineScrollTarget()
    {
        if (newlyUnlockedLevels.Count > 0)
        {
            int lowestUnlocked = int.MaxValue;
            foreach (int level in newlyUnlockedLevels)
            {
                if (level < lowestUnlocked) lowestUnlocked = level;
            }
            return Mathf.Min(lowestUnlocked, totalLevels);
        }
        
        return Mathf.Min(LevelDataManager.Instance.GetFirstIncompleteLevel(), totalLevels);
    }
    
    void Update() => HandleInput();
    
    // Initializes viewport dimensions and calculates bounds.
    private void InitializeViewport()
    {
        viewportWidth = contentArea != null ? contentArea.rect.width : Screen.width;
        viewportHeight = contentArea != null ? contentArea.rect.height : Screen.height;
        
        float padPx = viewportWidth * horizontalPadding;
        minX = -viewportWidth / 2f + padPx;
        maxX = viewportWidth / 2f - padPx;
        availableWidth = maxX - minX;
    }
    
    // Pre-generates X positions for all levels using varied patterns.
    private void GeneratePositions()
    {
        cachedXPositions.Clear();
    
        int seed = randomizePattern ? (int)System.DateTime.Now.Ticks : patternSeed;
        Random.InitState(seed);
    
        float prevX = 0f;
        int patternLen = 0, patternIdx = 0;
        float startX = 0f, endX = 0f;
        int patternType = 0;
    
        for (int level = 1; level <= totalLevels; level++)
        {
            if (patternIdx >= patternLen)
            {
                patternType = Random.Range(0, 5);
                patternLen = Random.Range(3, 8);
                patternIdx = 0;
                startX = prevX;
                endX = GetPatternEndX(patternType, prevX);
            }
        
            float t = patternLen > 1 ? (float)patternIdx / (patternLen - 1) : 0f;
            float x = CalculatePatternX(patternType, startX, endX, t);
        
            float zigzag = availableWidth * 0.15f * zigzagStrength * (level % 2 == 0 ? 1 : -1);
            float noise = Random.Range(-1f, 1f) * availableWidth * 0.2f * pathNoise;
        
            x = Mathf.Clamp(x + zigzag + noise, minX, maxX);
            cachedXPositions[level] = x;
        
            prevX = x;
            patternIdx++;
        }
    
        Random.InitState((int)System.DateTime.Now.Ticks);
    }
    
    // Returns target X position for pattern end based on type.
    private float GetPatternEndX(int type, float currentX)
    {
        bool onRight = currentX > 0;
        return type switch
        {
            0 => (onRight ? minX : maxX) * 0.7f,
            1 => Random.Range(minX, maxX) * 0.8f,
            2 => Random.Range(-0.1f, 0.1f) * availableWidth,
            3 => (onRight ? minX : maxX) * 0.9f,
            _ => Random.Range(minX, maxX) * 0.5f
        };
    }
    
    // Calculates X position along pattern curve.
    private float CalculatePatternX(int type, float start, float end, float t)
    {
        float smoothT = Mathf.SmoothStep(0, 1, t);
        float baseX = Mathf.Lerp(start, end, type == 1 ? t : smoothT);
        
        if (type == 3) baseX += Mathf.Sin(t * Mathf.PI) * availableWidth * 0.2f * Mathf.Sign(end - start);
        if (type == 4) baseX += Mathf.Sin(t * Mathf.PI * 2) * availableWidth * 0.1f;
        
        return baseX;
    }
    
    // Checks for results from completed level and detects newly unlocked levels.
    // Compares unlock state before and after saving results.
    private void CheckForResults()
    {
        if (gameSession == null || !gameSession.hasResults) return;
        
        if (gameSession.selectedLevel != null && gameSession.starsEarned > 0)
        {
            // Capture which levels were unlocked BEFORE saving new results
            HashSet<int> previouslyUnlocked = new HashSet<int>();
            for (int i = 1; i <= totalLevels; i++)
            {
                if (LevelDataManager.Instance.IsLevelUnlocked(i))
                {
                    previouslyUnlocked.Add(i);
                }
            }
            
            // Save the new results
            LevelDataManager.Instance.CompleteLevel(gameSession.selectedLevel.levelNumber, gameSession.starsEarned);
            
            // Find which levels are now unlocked that weren't before
            for (int i = 1; i <= totalLevels; i++)
            {
                if (LevelDataManager.Instance.IsLevelUnlocked(i) && !previouslyUnlocked.Contains(i))
                {
                    newlyUnlockedLevels.Add(i);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[LevelMapController] Level {i} was just unlocked!");
                    }
                }
            }
        }
        
        gameSession.ClearResults();
    }
    
    // Creates object pool for level nodes.
    private void CreatePool()
    {
        int count = Mathf.Min(poolSize, totalLevels);
        for (int i = 0; i < count; i++)
        {
            LevelNode node = Instantiate(nodePrefab, contentArea);
            node.gameObject.SetActive(false);
            nodePool.Add(node);
        }
    }
    
    // Handles mouse wheel and drag input. Ignores input when popup is open.
    private void HandleInput()
    {
        if (levelPopup != null && levelPopup.IsOpen)
        {
            return;
        }
        
        float delta = 0f;
        
        if (Mouse.current != null)
            delta += Mouse.current.scroll.y.ReadValue() * scrollSpeed * Time.deltaTime * 0.01f;
        
        if (Touchscreen.current?.primaryTouch.press.isPressed == true)
        {
            delta += Touchscreen.current.primaryTouch.delta.y.ReadValue() * dragSensitivity;
        }
        else if (Mouse.current?.leftButton.isPressed == true)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (isDragging) delta += (mousePos - lastPointerPos).y * dragSensitivity;
            lastPointerPos = mousePos;
            isDragging = true;
        }
        else
        {
            isDragging = false;
        }
        
        if (Mathf.Abs(delta) > 0.01f) Scroll(delta);
    }
    
    // Scrolls the level map by delta and clamps to bounds.
    public void Scroll(float delta)
    {
        currentScrollY = Mathf.Clamp(currentScrollY + delta, 0, MaxScrollY);
        RefreshNodes();
    }
    
    // Scrolls to center a specific level on screen.
    public void ScrollToLevel(int level)
    {
        level = Mathf.Clamp(level, 1, totalLevels);
        currentScrollY = level == 1 ? 0 : (level - 1) * nodeSpacingY - viewportHeight / 2f + BottomPaddingPx;
        currentScrollY = Mathf.Clamp(currentScrollY, 0, MaxScrollY);
        RefreshNodes();
    }
    
    // Updates which nodes are visible and positions them.
    private void RefreshNodes()
    {
        int bottomLevel = Mathf.Clamp(Mathf.FloorToInt(currentScrollY / nodeSpacingY) - bufferNodes, 1, totalLevels);
        int topLevel = Mathf.Clamp(Mathf.CeilToInt((currentScrollY + viewportHeight) / nodeSpacingY) + bufferNodes, 1, totalLevels);
        
        List<int> toRemove = new List<int>();
        foreach (var kvp in activeNodes)
            if (kvp.Key < bottomLevel || kvp.Key > topLevel) toRemove.Add(kvp.Key);
        foreach (int level in toRemove) ReturnNode(level);
        
        for (int level = bottomLevel; level <= topLevel; level++)
        {
            if (!activeNodes.ContainsKey(level)) SpawnNode(level);
            else PositionNode(activeNodes[level], level);
        }
        
        pathRenderer?.UpdatePath(activeNodes, bottomLevel, topLevel);
    }
    
    // Spawns a node for a specific level.
    // Passes playUnlockAnimation flag if level was just unlocked.
    private void SpawnNode(int level)
    {
        LevelNode node = nodePool.Find(n => !n.gameObject.activeSelf);
        if (node == null) return;
        
        bool shouldPlayUnlockAnim = newlyUnlockedLevels.Contains(level);
        
        node.Setup(
            level, 
            LevelDataManager.Instance.IsLevelUnlocked(level), 
            LevelDataManager.Instance.GetStarsForLevel(level),
            shouldPlayUnlockAnim
        );
        
        PositionNode(node, level);
        node.gameObject.SetActive(true);
        activeNodes[level] = node;
        
        // Clear from set after spawning so animation only plays once
        if (shouldPlayUnlockAnim)
        {
            newlyUnlockedLevels.Remove(level);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[LevelMapController] Spawned level {level} with unlock animation");
            }
        }
    }
    
    // Positions a node based on level number and scroll.
    private void PositionNode(LevelNode node, int level)
    {
        float x = cachedXPositions.GetValueOrDefault(level, 0f);
        float y = -viewportHeight / 2f + BottomPaddingPx + (level - 1) * nodeSpacingY - currentScrollY;
        node.transform.localPosition = new Vector3(x, y, 0);
    }
    
    // Returns a node to the pool.
    private void ReturnNode(int level)
    {
        if (!activeNodes.ContainsKey(level)) return;
        activeNodes[level].gameObject.SetActive(false);
        activeNodes.Remove(level);
    }
    
    // Opens the level popup for the specified level number.
    // Fetches star data from LevelDataManager and passes it to the popup.
    public void OpenLevelPopup(int levelNumber)
    {
        if (levelPopup == null)
        {
            Debug.LogError("[LevelMapController] LevelPopup reference is missing!");
            return;
        }
        
        int starsEarned = LevelDataManager.Instance.GetStarsForLevel(levelNumber);
        levelPopup.Show(levelNumber, starsEarned);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelMapController] Opened popup for level {levelNumber} with {starsEarned} stars");
        }
    }
    
    // Loads a level by number. Sets up GameSession and transitions to game scene.
    public void LoadLevel(int levelNumber)
    {
        if (levelDatabase == null || gameSession == null) return;
        
        LevelConfig config = levelDatabase.GetLevel(levelNumber);
        if (config == null) return;
        
        gameSession.SelectLevel(config);
        SceneManager.LoadScene(gameSceneName);
    }
    
    // Public accessors for path renderer
    public float GetXPositionForLevel(int level) => cachedXPositions.GetValueOrDefault(level, 0f);
    public float GetYPositionForLevel(int level) => -viewportHeight / 2f + BottomPaddingPx + (level - 1) * nodeSpacingY;
    
    // Returns the screen position of a level node.
    // Accounts for current scroll offset and converts to screen space.
    public Vector2 GetLevelScreenPosition(int level)
    {
        float x = cachedXPositions.GetValueOrDefault(level, 0f);
        float y = -viewportHeight / 2f + BottomPaddingPx + (level - 1) * nodeSpacingY - currentScrollY;
        
        // Convert local position to world position
        Vector3 localPos = new Vector3(x, y, 0);
        Vector3 worldPos = contentArea.TransformPoint(localPos);
        
        // Convert world to screen
        Canvas canvas = contentArea.GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        
        return RectTransformUtility.WorldToScreenPoint(cam, worldPos);
    }
}