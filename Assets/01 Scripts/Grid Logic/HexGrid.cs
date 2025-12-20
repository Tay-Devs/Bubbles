using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 8;
    public int startingHeight = 10;
    public GameObject bubblePrefab;
    public float rowHeight = 0.9f;
    
    [Header("Grid Positioning")]
    public float bubbleRadius = 0.5f;
    public bool autoPosition = false;
    public bool autoGenerate = true;
    
    [Header("Height Limit")]
    public GridStartHeightLimit heightLimit;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public Action onColorsChanged;
    public Action onGridChanged;
    
    // Grid data
    private List<List<Bubble>> gridData = new List<List<Bubble>>();
    
    // Cached level colors
    private List<BubbleType> levelColors;
    
    // Sub-systems
    public GridMatchSystem MatchSystem { get; private set; }
    public GridRowSystem RowSystem { get; private set; }
    public GridBubbleAttacher BubbleAttacher { get; private set; }
    public ColorRemovalSystem ColorRemoval { get; private set; }
    
    public bool IsDestroying => MatchSystem != null && MatchSystem.IsDestroying;
    
    private static readonly Vector2Int[][] neighborOffsets = {
        new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
        new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(0, 1), new Vector2Int(1, 1) }
    };

    void Awake()
    {
        // Initialize sub-systems
        MatchSystem = GetComponent<GridMatchSystem>();
        RowSystem = GetComponent<GridRowSystem>();
        BubbleAttacher = GetComponent<GridBubbleAttacher>();
        ColorRemoval = GetComponent<ColorRemovalSystem>();
        
        if (MatchSystem == null) MatchSystem = gameObject.AddComponent<GridMatchSystem>();
        if (RowSystem == null) RowSystem = gameObject.AddComponent<GridRowSystem>();
        if (BubbleAttacher == null) BubbleAttacher = gameObject.AddComponent<GridBubbleAttacher>();
        if (ColorRemoval == null) ColorRemoval = gameObject.AddComponent<ColorRemovalSystem>();
        
        // Initialize ColorRemoval with grid reference
        ColorRemoval.Initialize(this);
    }
    
    void Start()
    {
        CacheLevelColors();
        
        if (autoPosition) PositionGrid();
        if (autoGenerate) 
        {
            GenerateGrid();
            InitializeRowSystem();
        }
    }
    
    // Initializes the row system based on win condition.
    private void InitializeRowSystem()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.ActiveWinCondition == WinConditionType.Survival)
        {
            RowSystem.ResetSurvival();
        }
        else
        {
            RowSystem.ResetShots();
        }
        
        // Initialize color tracking after grid is generated
        ColorRemoval.InitializePreviousColors();
    }
    
    private void CacheLevelColors()
    {
        levelColors = new List<BubbleType>();
        
        if (LevelLoader.Instance != null)
        {
            var colors = LevelLoader.Instance.GetAvailableColors();
            if (colors != null && colors.Length > 0)
            {
                levelColors.AddRange(colors);
                Log($"[HexGrid] Using level colors: {string.Join(", ", levelColors)}");
                return;
            }
        }
        
        foreach (BubbleType color in Enum.GetValues(typeof(BubbleType)))
        {
            levelColors.Add(color);
        }
        Log($"[HexGrid] Using fallback (all colors): {string.Join(", ", levelColors)}");
    }
    
    public List<BubbleType> GetLevelColors()
    {
        if (levelColors == null || levelColors.Count == 0)
        {
            CacheLevelColors();
        }
        return levelColors;
    }
    
    // Called by GridCameraFitter after positioning
    public void InitializeGrid()
    {
        CacheLevelColors();
        GenerateGrid();
        InitializeRowSystem();
    }
    
    public void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    public void LogWarning(string msg) { if (enableDebugLogs) Debug.LogWarning(msg); }

    #region Grid Positioning
    
    public float GetGridWorldWidth()
    {
        return (width - 1) + 0.5f + (bubbleRadius * 2);
    }
    
    public void PositionGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        float camLeft = cam.transform.position.x - (cam.orthographicSize * cam.aspect);
        float gridOriginX = camLeft + bubbleRadius;
        float topEdge = cam.transform.position.y + cam.orthographicSize;
        
        transform.position = new Vector3(
            gridOriginX,
            topEdge - 0.5f,
            transform.position.z
        );
        
        Log($"Grid positioned at {transform.position}");
    }
    
    #endregion

    #region Grid Generation
    
    public void GenerateGrid()
    {
        ClearGrid();
        
        List<BubbleType> colors = GetLevelColors();
        
        Log($"[HexGrid] Generating grid with colors: {string.Join(", ", colors)}");
        
        for (int y = 0; y < startingHeight; y++)
        {
            if (heightLimit != null)
            {
                float rowY = transform.position.y - (y * rowHeight) - bubbleRadius;
                
                if (heightLimit.IsBelowLimit(rowY))
                {
                    Log($"Row {y} would be below height limit. Stopping at {y} rows.");
                    break;
                }
            }
            
            List<Bubble> row = CreateRow(y, colors);
            gridData.Add(row);
        }
        
        Log($"Grid generated with {gridData.Count} rows");
    }
    
    public List<Bubble> CreateRow(int y, List<BubbleType> allowedColors = null)
    {
        List<Bubble> row = new List<Bubble>();
        float rowOffset = GetRowXOffset(y);
        
        if (allowedColors == null || allowedColors.Count == 0)
        {
            allowedColors = GetLevelColors();
        }
        
        for (int x = 0; x < width; x++)
        {
            Bubble bubble = Instantiate(bubblePrefab, transform).GetComponent<Bubble>();
            
            BubbleType color = allowedColors[Random.Range(0, allowedColors.Count)];
            
            bubble.SetType(color);
            bubble.isAttached = true;
            bubble.transform.localPosition = new Vector3(x + rowOffset, -y * rowHeight, 0f);
            row.Add(bubble);
        }
        
        return row;
    }

    public void ClearGrid()
    {
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null) Destroy(bubble.gameObject);
        gridData.Clear();
    }
    
    #endregion

    #region Grid Access
    
    public List<List<Bubble>> GetGridData() => gridData;
    public int RowCount => gridData.Count;
    
    public Bubble GetBubbleAt(Vector2Int pos) => GetBubbleAt(pos.x, pos.y);
    public Bubble GetBubbleAt(int x, int y)
    {
        if (y < 0 || y >= gridData.Count) return null;
        if (x < 0 || x >= gridData[y].Count) return null;
        return gridData[y][x];
    }
    
    public void SetBubbleAt(Vector2Int pos, Bubble bubble) => SetBubbleAt(pos.x, pos.y, bubble);
    public void SetBubbleAt(int x, int y, Bubble bubble)
    {
        if (y < 0 || y >= gridData.Count) return;
        EnsureColumnExists(x, y);
        gridData[y][x] = bubble;
    }

    public bool IsEmpty(Vector2Int pos) => IsEmpty(pos.x, pos.y);
    public bool IsEmpty(int x, int y)
    {
        if (y < 0) return false;
        if (y >= gridData.Count) return true;
        if (x < 0 || x >= gridData[y].Count) return true;
        return gridData[y][x] == null;
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }
    
    public void EnsureRowExists(int y) 
    { 
        while (y >= gridData.Count) 
            gridData.Add(new List<Bubble>()); 
    }
    
    public void EnsureColumnExists(int x, int y) 
    { 
        while (x >= gridData[y].Count) 
            gridData[y].Add(null); 
    }
    
    public void InsertRowAtTop(List<Bubble> row)
    {
        gridData.Insert(0, row);
    }
    
    #endregion

    #region Coordinate Conversion
    
    public float GetRowXOffset(int y)
    {
        return (y % 2 == 0) ? 0f : 0.5f;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int y = Mathf.Max(0, Mathf.RoundToInt(-local.y / rowHeight));
        float rowOffset = GetRowXOffset(y);
        return new Vector2Int(Mathf.RoundToInt(local.x - rowOffset), y);
    }

    public Vector3 GridToWorld(Vector2Int pos) => GridToWorld(pos.x, pos.y);
    public Vector3 GridToWorld(int x, int y)
    {
        float rowOffset = GetRowXOffset(y);
        return transform.position + new Vector3(x + rowOffset, -y * rowHeight, 0f);
    }
    
    #endregion

    #region Neighbors
    
    public List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        var neighbors = new List<Vector2Int>();
        foreach (var offset in neighborOffsets[pos.y % 2])
            neighbors.Add(pos + offset);
        return neighbors;
    }
    
    #endregion

    #region Color Tracking
    
    public HashSet<BubbleType> GetAvailableColors()
    {
        var colors = new HashSet<BubbleType>();
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null)
                    colors.Add(bubble.type);
        return colors;
    }
    
    public bool ColorExistsInGrid(BubbleType type)
    {
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null && bubble.type == type)
                    return true;
        return false;
    }
    
    #endregion

    #region Grid State
    
    public bool IsGridEmpty()
    {
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null)
                    return false;
        return true;
    }
    
    public void ResetVisited()
    {
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null) 
                    bubble.visited = false;
    }
    
    #endregion
}