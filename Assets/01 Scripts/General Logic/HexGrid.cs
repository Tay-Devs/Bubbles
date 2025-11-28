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
    
    [Header("Destruction Animation")]
    public float destructionDelay = 0.1f; // Delay between each bubble pop
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private List<List<Bubble>> gridData;
    private int xOffset = 0;
    
    // Hex neighbor offsets: [even row, odd row]
    private static readonly Vector2Int[][] neighborOffsets = {
        new[] { new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(-1,-1), new Vector2Int(0,-1), new Vector2Int(-1,1), new Vector2Int(0,1) },
        new[] { new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(1,-1), new Vector2Int(0,1), new Vector2Int(1,1) }
    };

    void Start() => GenerateGrid();
    
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
    
    // Destroy bubbles one by one with delay
    private IEnumerator DestroyBubblesSequentially(List<Vector2Int> positions, bool checkFloatingAfter)
    {
        Log($"Destroying {positions.Count} bubbles sequentially");
        
        foreach (var pos in positions)
        {
            yield return new WaitForSeconds(destructionDelay);
            RemoveBubbleAt(pos);
        }
        
        if (checkFloatingAfter)
        {
            DestroyFloatingBubbles();
        }
    }

    // Destroy floating bubbles
    public int DestroyFloatingBubbles()
    {
        if (gridData.Count == 0) return 0;
        
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
        var floating = new List<Vector2Int>();
        for (int y = 0; y < gridData.Count; y++)
            for (int dataX = 0; dataX < gridData[y].Count; dataX++)
                if (gridData[y][dataX] != null)
                {
                    var pos = new Vector2Int(dataX - xOffset, y);
                    if (!connected.Contains(pos)) floating.Add(pos);
                }
        
        if (floating.Count > 0)
        {
            Log($"Found {floating.Count} floating bubbles");
            StartCoroutine(DestroyBubblesSequentially(floating, false));
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
}