using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelMapController : MonoBehaviour
{
    public static LevelMapController Instance { get; private set; }
    
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
    public float bottomBufferScreens = 1f; // Keep nodes for this many screens below visible area
    
    private List<LevelNode> nodePool = new List<LevelNode>();
    private Dictionary<int, LevelNode> activeNodes = new Dictionary<int, LevelNode>();
    
    private float currentScrollY = 0f;
    private float viewportHeight;
    private int lowestVisibleLevel = 1;
    private int highestVisibleLevel = 1;
    
    // Input tracking
    private Vector2 lastPointerPosition;
    private bool isDragging = false;
    
    // Cached random positions for each level
    private Dictionary<int, float> cachedXPositions = new Dictionary<int, float>();
    
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
        InitializePool();
        CalculateViewport();
        ScrollToLevel(LevelDataManager.Instance.GetHighestUnlockedLevel());
        RefreshVisibleNodes();
    }
    
    void Update()
    {
        HandleScrollInput();
    }
    
    // Creates the initial pool of reusable level nodes.
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            LevelNode node = Instantiate(nodePrefab);
            node.transform.SetParent(contentArea, false);
            node.gameObject.SetActive(false);
            nodePool.Add(node);
        }
    }
    
    // Calculates viewport height for culling calculations.
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
    
    // Handles touch and mouse scroll input using new Input System.
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
    
    // Scrolls the map by the given delta and refreshes visible nodes.
    public void Scroll(float delta)
    {
        currentScrollY += delta;
        currentScrollY = Mathf.Max(0, currentScrollY);
        
        RefreshVisibleNodes();
    }
    
    // Instantly scrolls to center a specific level on screen.
    public void ScrollToLevel(int levelNumber)
    {
        currentScrollY = (levelNumber - 1) * nodeSpacingY - (viewportHeight / 2f);
        currentScrollY = Mathf.Max(0, currentScrollY);
        
        RefreshVisibleNodes();
    }
    
    // Calculates which levels should be visible and updates node pool.
    // Uses larger buffer for bottom to keep early levels loaded longer.
    private void RefreshVisibleNodes()
    {
        // Calculate bottom buffer in node count based on screen size
        float bottomBufferPixels = viewportHeight * bottomBufferScreens;
        int bottomBufferNodeCount = Mathf.CeilToInt(bottomBufferPixels / nodeSpacingY);
        
        // Bottom level: keep nodes for extra screens below
        int bottomLevel = Mathf.Max(1, Mathf.FloorToInt(currentScrollY / nodeSpacingY) - bottomBufferNodeCount);
        
        // Top level: standard buffer above visible area
        int topLevel = Mathf.CeilToInt((currentScrollY + viewportHeight) / nodeSpacingY) + topBufferNodes;
        
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
        
        // Add nodes for visible levels
        for (int level = bottomLevel; level <= topLevel; level++)
        {
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
    
    // Spawns or reuses a node for the given level number.
    private void SpawnNodeForLevel(int level)
    {
        LevelNode node = GetNodeFromPool();
        if (node == null) return;
        
        bool unlocked = LevelDataManager.Instance.IsLevelUnlocked(level);
        int stars = LevelDataManager.Instance.GetStarsForLevel(level);
        
        node.Setup(level, unlocked, stars);
        UpdateNodePosition(node, level);
        node.gameObject.SetActive(true);
        
        activeNodes[level] = node;
    }
    
    // Positions a node based on its level number using zigzag pattern.
    private void UpdateNodePosition(LevelNode node, int level)
    {
        float x = GetXPositionForLevel(level);
        float y = GetYPositionForLevel(level) - currentScrollY;
        
        node.transform.localPosition = new Vector3(x, y, 0);
    }
    
    // Returns the X position for a level using cached random zigzag pattern.
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
    
    // Returns the Y position for a level (simple linear spacing).
    public float GetYPositionForLevel(int level)
    {
        return (level - 1) * nodeSpacingY;
    }
    
    // Gets an inactive node from the pool.
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
    
    // Returns a node to the pool for reuse.
    private void ReturnNodeToPool(int level)
    {
        if (activeNodes.ContainsKey(level))
        {
            activeNodes[level].gameObject.SetActive(false);
            activeNodes.Remove(level);
        }
    }
    
    // Loads the game scene with the selected level.
    public void LoadLevel(int levelNumber)
    {
        PlayerPrefs.SetInt("SelectedLevel", levelNumber);
        PlayerPrefs.Save();
        
        Debug.Log($"[LevelMapController] Loading level {levelNumber}");
        SceneManager.LoadScene("GameScene");
    }
}