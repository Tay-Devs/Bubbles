using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int startingWidth = 8;
    public int startingHeight = 10;
    public GameObject bubblePrefab;
    public int minMatchCount = 3;
    
    [Header("Grid Positioning")]
    public float leftOffset = 0.5f; // Distance from left screen edge
    public float topOffset = 0.5f; // Distance from top screen edge
    public bool autoPosition = true; // Auto position on start
    
    [Header("Destruction Animation")]
    public float destructionDelay = 0.15f; // Starting delay between pops
    public float destructionDelayMultiplier = 0.85f; // Multiplier applied each pop (0.85 = 15% faster each time)
    public float destructionDelayLimit = 0.03f; // Minimum delay (speed cap)
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    [Header("Lose Condition")]
    public LoseZone loseZone;
    
    // Event fired when colors change (after destruction)
    public System.Action onColorsChanged;
    
    private List<List<Bubble>> gridData;
    private int xOffset = 0;
    private bool isDestroying = false;
    
    public bool IsDestroying => isDestroying;
    
    // Get all unique colors currently in the grid
    public HashSet<BubbleType> GetAvailableColors()
    {
        var colors = new HashSet<BubbleType>();
        
        if (gridData == null) return colors;
        
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null)
                {
                    colors.Add(bubble.type);
                }
            }
        }
        
        return colors;
    }
    
    // Check if a specific color exists in the grid
    public bool ColorExistsInGrid(BubbleType type)
    {
        if (gridData == null) return false;
        
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null && bubble.type == type)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // Check if the grid is empty (all bubbles cleared)
    public bool IsGridEmpty()
    {
        if (gridData == null) return true;
        
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    // Check win condition - returns true if player won
    private bool CheckWinCondition()
    {
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.IsPlaying) return false;
        
        if (IsGridEmpty())
        {
            Log("All bubbles cleared - Victory!");
            GameManager.Instance.Victory();
            return true;
        }
        
        return false;
    }
    
    // Hex neighbor offsets: [even row, odd row]
    private static readonly Vector2Int[][] neighborOffsets = {
        new[] { new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(-1,-1), new Vector2Int(0,-1), new Vector2Int(-1,1), new Vector2Int(0,1) },
        new[] { new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(1,-1), new Vector2Int(0,1), new Vector2Int(1,1) }
    };

    void Start()
    {
        if (autoPosition) PositionGrid();
        GenerateGrid();
    }
    
    // Position grid based on screen boundaries
    public void PositionGrid()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Get screen bounds in world space
        float screenHeight = 2f * cam.orthographicSize;
        float screenWidth = screenHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;
        
        float leftEdge = camPos.x - screenWidth / 2f;
        float topEdge = camPos.y + screenHeight / 2f;
        
        // Position grid with offsets
        transform.position = new Vector3(
            leftEdge + leftOffset,
            topEdge - topOffset,
            transform.position.z
        );
    }
    
    void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    void LogWarning(string msg) { if (enableDebugLogs) Debug.LogWarning(msg); }

    public void GenerateGrid()
    {
        ClearGrid();
        gridData = new List<List<Bubble>>();
        xOffset = 0;
        
        for (int y = 0; y < startingHeight; y++)
        {
            List<Bubble> row = new List<Bubble>();
            bool isShortRow = y % 2 != 0;
            int colCount = isShortRow ? startingWidth - 1 : startingWidth;
            
            for (int x = 0; x < colCount; x++)
            {
                Bubble bubble = Instantiate(bubblePrefab, transform).GetComponent<Bubble>();
                bubble.SetType((BubbleType)Random.Range(0, Enum.GetValues(typeof(BubbleType)).Length));
                bubble.isAttached = true;
                bubble.transform.localPosition = new Vector3(x + (isShortRow ? 0.5f : 0f), -y * 0.9f, 0f);
                row.Add(bubble);
            }
            gridData.Add(row);
        }
    }

    public void ClearGrid()
    {
        if (gridData == null) return;
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null) Destroy(bubble.gameObject);
        gridData.Clear();
        xOffset = 0;
    }

    // Grid access
    public Bubble GetBubbleAt(Vector2Int pos) => GetBubbleAt(pos.x, pos.y);
    public Bubble GetBubbleAt(int x, int y)
    {
        if (y < 0 || y >= gridData.Count) return null;
        int dataX = x + xOffset;
        if (dataX < 0 || dataX >= gridData[y].Count) return null;
        return gridData[y][dataX];
    }

    public bool IsEmpty(Vector2Int pos) => IsEmpty(pos.x, pos.y);
    public bool IsEmpty(int x, int y)
    {
        if (y < 0) return false;
        if (y >= gridData.Count) return true;
        int dataX = x + xOffset;
        if (dataX < 0 || dataX >= gridData[y].Count) return true;
        return gridData[y][dataX] == null;
    }

    // Coordinate conversion
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int y = Mathf.Max(0, Mathf.RoundToInt(-local.y / 0.9f));
        float offset = (y % 2 != 0) ? 0.5f : 0f;
        return new Vector2Int(Mathf.RoundToInt(local.x - offset), y);
    }

    public Vector3 GridToWorld(Vector2Int pos) => GridToWorld(pos.x, pos.y);
    public Vector3 GridToWorld(int x, int y)
    {
        float offset = (y % 2 != 0) ? 0.5f : 0f;
        return transform.position + new Vector3(x + offset, -y * 0.9f, 0f);
    }

    // Get hex neighbors
    public List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        var neighbors = new List<Vector2Int>();
        foreach (var offset in neighborOffsets[pos.y % 2])
            neighbors.Add(pos + offset);
        return neighbors;
    }

    // Grid expansion
    private void EnsureRowExists(int y) { while (y >= gridData.Count) gridData.Add(new List<Bubble>()); }
    private void EnsureColumnExists(int dataX, int y) { while (dataX >= gridData[y].Count) gridData[y].Add(null); }
    
    private void ExpandLeft(int amount)
    {
        for (int y = 0; y < gridData.Count; y++)
            for (int i = 0; i < amount; i++)
                gridData[y].Insert(0, null);
        xOffset += amount;
        Log($"Expanded grid left by {amount}");
    }

    // Find best empty position for attachment
    public Vector2Int FindAttachPosition(Vector3 worldPos)
    {
        Vector2Int target = WorldToGrid(worldPos);
        if (IsEmpty(target)) return target;
        
        float bestDist = float.MaxValue;
        Vector2Int bestPos = target;
        
        foreach (var neighbor in GetNeighbors(target))
        {
            if (neighbor.y >= 0 && IsEmpty(neighbor))
            {
                float dist = Vector3.Distance(worldPos, GridToWorld(neighbor));
                if (dist < bestDist) { bestDist = dist; bestPos = neighbor; }
            }
        }
        return bestPos;
    }

    // Attach bubble to grid
    public Vector2Int AttachBubble(Bubble bubble, Vector3 worldPos)
    {
        if (bubble.isAttached)
        {
            LogWarning($"Bubble {bubble.type} already attached");
            return new Vector2Int(-1, -1);
        }
        
        Vector2Int pos = FindAttachPosition(worldPos);
        EnsureRowExists(pos.y);
        
        if (pos.x < -xOffset) ExpandLeft((-xOffset) - pos.x);
        
        int dataX = pos.x + xOffset;
        EnsureColumnExists(dataX, pos.y);
        
        bubble.transform.SetParent(transform);
        bubble.transform.position = GridToWorld(pos);
        bubble.isAttached = true;
        gridData[pos.y][dataX] = bubble;
        
        Log($"Attached {bubble.type} at ({pos.x}, {pos.y})");
        
        // Check lose condition immediately after attachment
        if (CheckLoseCondition())
        {
            return new Vector2Int(-1, -1); // Signal game over, skip match checking
        }
        
        return pos;
    }

    // Flood fill - finds connected bubbles, optionally matching a specific color
    private List<Vector2Int> FloodFill(Vector2Int start, BubbleType? matchType = null)
    {
        var result = new List<Vector2Int>();
        var startBubble = GetBubbleAt(start);
        if (startBubble == null) return result;
        
        ResetVisited();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        startBubble.visited = true;
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            
            foreach (var neighbor in GetNeighbors(current))
            {
                var bubble = GetBubbleAt(neighbor);
                if (bubble == null || bubble.visited) continue;
                if (matchType.HasValue && bubble.type != matchType.Value) continue;
                
                bubble.visited = true;
                queue.Enqueue(neighbor);
            }
        }
        return result;
    }

    private void ResetVisited()
    {
        foreach (var row in gridData)
            foreach (var bubble in row)
                if (bubble != null) bubble.visited = false;
    }

    // Check and destroy matches
    public bool CheckAndDestroyMatches(Vector2Int startPos)
    {
        if (startPos.x == -1) return false;
        
        var startBubble = GetBubbleAt(startPos);
        if (startBubble == null) return false;
        
        var matches = FloodFill(startPos, startBubble.type);
        Log($"Found {matches.Count} matching {startBubble.type} bubbles");
        
        if (matches.Count >= minMatchCount)
        {
            StartCoroutine(DestroyBubblesSequentially(matches, true));
            return true;
        }
        
        return false;
    }
    
    // Destroy bubbles one by one with diminishing delay
    private IEnumerator DestroyBubblesSequentially(List<Vector2Int> positions, bool checkFloatingAfter)
    {
        isDestroying = true;
        Log($"Destroying {positions.Count} bubbles sequentially");
        
        float currentDelay = destructionDelay;
        
        foreach (var pos in positions)
        {
            yield return new WaitForSeconds(currentDelay);
            RemoveBubbleAt(pos);
            
            // Apply diminishing delay
            currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
        }
        
        if (checkFloatingAfter)
        {
            yield return StartCoroutine(DestroyFloatingBubblesCoroutine(currentDelay));
        }
        
        isDestroying = false;
        
        // Notify that colors may have changed
        onColorsChanged?.Invoke();
        
        // Check win condition first (if grid empty, player wins)
        if (CheckWinCondition()) yield break;
        
        // Check lose condition after all destruction is complete
        CheckLoseCondition();
    }
    
    // Coroutine version for floating bubbles - continues from current delay
    private IEnumerator DestroyFloatingBubblesCoroutine(float startingDelay)
    {
        var floating = GetFloatingBubbles();
        
        if (floating.Count > 0)
        {
            Log($"Found {floating.Count} floating bubbles");
            float currentDelay = startingDelay;
            
            foreach (var pos in floating)
            {
                yield return new WaitForSeconds(currentDelay);
                RemoveBubbleAt(pos);
                
                // Continue diminishing delay
                currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
            }
        }
    }

    // Get list of floating bubble positions
    private List<Vector2Int> GetFloatingBubbles()
    {
        var floating = new List<Vector2Int>();
        if (gridData.Count == 0) return floating;
        
        // Find all connected to top row
        ResetVisited();
        var connected = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        
        for (int dataX = 0; dataX < gridData[0].Count; dataX++)
        {
            var bubble = gridData[0][dataX];
            if (bubble != null && !bubble.visited)
            {
                var pos = new Vector2Int(dataX - xOffset, 0);
                bubble.visited = true;
                queue.Enqueue(pos);
            }
        }
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            connected.Add(current);
            
            foreach (var neighbor in GetNeighbors(current))
            {
                var bubble = GetBubbleAt(neighbor);
                if (bubble != null && !bubble.visited)
                {
                    bubble.visited = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        // Find unconnected bubbles
        for (int y = 0; y < gridData.Count; y++)
            for (int dataX = 0; dataX < gridData[y].Count; dataX++)
                if (gridData[y][dataX] != null)
                {
                    var pos = new Vector2Int(dataX - xOffset, y);
                    if (!connected.Contains(pos)) floating.Add(pos);
                }
        
        return floating;
    }
    
    // Destroy floating bubbles (public method for external calls)
    public int DestroyFloatingBubbles()
    {
        var floating = GetFloatingBubbles();
        
        if (floating.Count > 0)
        {
            Log($"Found {floating.Count} floating bubbles");
            StartCoroutine(DestroyFloatingBubblesCoroutine(destructionDelay));
        }
        
        return floating.Count;
    }

    // Remove bubble
    public void RemoveBubbleAt(Vector2Int pos) => RemoveBubbleAt(pos.x, pos.y);
    public void RemoveBubbleAt(int x, int y)
    {
        if (y < 0 || y >= gridData.Count) return;
        int dataX = x + xOffset;
        if (dataX < 0 || dataX >= gridData[y].Count) return;
        
        var bubble = gridData[y][dataX];
        if (bubble != null)
        {
            bubble.Explode();
            gridData[y][dataX] = null;
        }
    }
    
    // Check if any bubble is in the lose zone - returns true if game over triggered
    private bool CheckLoseCondition()
    {
        if (loseZone == null || GameManager.Instance == null) return false;
        if (!GameManager.Instance.IsPlaying) return false;
        
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null && loseZone.IsInLoseZone(bubble.transform.position))
                {
                    Log($"Bubble at {bubble.transform.position.y} is in lose zone (line at {loseZone.LoseLineY})");
                    GameManager.Instance.GameOver();
                    return true;
                }
            }
        }
        return false;
    }
}