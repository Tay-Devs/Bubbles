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
    public float leftOffset = 0.5f;
    public float topOffset = 0.5f;
    public bool autoPosition = true;
    public RectTransform topUIBoundary; // UI panel that marks the top boundary
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public Action onColorsChanged;
    public Action onGridChanged;
    
    // Grid data
    private List<List<Bubble>> gridData = new List<List<Bubble>>();
    
    // Sub-systems (assigned in Awake)
    public GridMatchSystem MatchSystem { get; private set; }
    public GridRowSystem RowSystem { get; private set; }
    public GridBubbleAttacher BubbleAttacher { get; private set; }
    
    // For external access
    public bool IsDestroying => MatchSystem != null && MatchSystem.IsDestroying;
    
    // Hex neighbor offsets: [even row, odd row]
    private static readonly Vector2Int[][] neighborOffsets = {
        // Even row (no offset) - odd rows are +0.5 right
        new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
        // Odd row (+0.5 offset) - even rows have no offset
        new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(0, 1), new Vector2Int(1, 1) }
    };

    void Awake()
    {
        // Initialize sub-systems
        MatchSystem = GetComponent<GridMatchSystem>();
        RowSystem = GetComponent<GridRowSystem>();
        BubbleAttacher = GetComponent<GridBubbleAttacher>();
        
        if (MatchSystem == null) MatchSystem = gameObject.AddComponent<GridMatchSystem>();
        if (RowSystem == null) RowSystem = gameObject.AddComponent<GridRowSystem>();
        if (BubbleAttacher == null) BubbleAttacher = gameObject.AddComponent<GridBubbleAttacher>();
    }
    
    void Start()
    {
        if (autoPosition) PositionGrid();
        GenerateGrid();
        RowSystem.ResetShots();
    }
    
    // Logging
    public void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    public void LogWarning(string msg) { if (enableDebugLogs) Debug.LogWarning(msg); }

    #region Grid Positioning
    
    public void PositionGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Get safe area in screen coordinates
        Rect safeArea = Screen.safeArea;
        
        // Convert safe area corners to world space
        Vector3 safeBottomLeft = cam.ScreenToWorldPoint(new Vector3(safeArea.x, safeArea.y, 0));
        Vector3 safeTopRight = cam.ScreenToWorldPoint(new Vector3(safeArea.xMax, safeArea.yMax, 0));
        
        float safeLeft = safeBottomLeft.x;
        float topEdge = safeTopRight.y;
        
        // If UI boundary is set, use its bottom edge as the top
        if (topUIBoundary != null)
        {
            Vector3[] corners = new Vector3[4];
            topUIBoundary.GetWorldCorners(corners);
            // corners[0] = bottom-left, corners[1] = top-left, corners[2] = top-right, corners[3] = bottom-right
            // We want the bottom edge of the UI panel
            topEdge = corners[0].y;
        }
        
        // Position grid with offsets
        transform.position = new Vector3(
            safeLeft + leftOffset,
            topEdge - topOffset,
            transform.position.z
        );
    }
    
    #endregion

    #region Grid Generation
    
    public void GenerateGrid()
    {
        ClearGrid();
        
        for (int y = 0; y < startingHeight; y++)
        {
            List<Bubble> row = CreateRow(y);
            gridData.Add(row);
        }
    }
    
    public List<Bubble> CreateRow(int y, List<BubbleType> allowedColors = null)
    {
        List<Bubble> row = new List<Bubble>();
        float rowOffset = GetRowXOffset(y);
        
        for (int x = 0; x < width; x++)
        {
            Bubble bubble = Instantiate(bubblePrefab, transform).GetComponent<Bubble>();
            
            BubbleType color;
            if (allowedColors != null && allowedColors.Count > 0)
                color = allowedColors[Random.Range(0, allowedColors.Count)];
            else
                color = (BubbleType)Random.Range(0, Enum.GetValues(typeof(BubbleType)).Length);
            
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