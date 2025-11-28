using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexGrid : MonoBehaviour
{
    [Header("Starting Grid Size")]
    public int startingWidth = 8;
    public int startingHeight = 10;
    public GameObject bubblePrefab;
    
    [Header("Match Settings")]
    public int minMatchCount = 3; // Minimum bubbles needed to pop
    
    private List<List<Bubble>> gridData;
    
    // Tracks how many columns were added to the left
    // Grid data index 0 corresponds to world x = -xOffset
    private int xOffset = 0;
    
    // Neighbor offsets for hex grid (odd-r offset coordinates)
    // Even rows (y % 2 == 0) - no horizontal offset
    private static readonly Vector2Int[] evenRowNeighbors = new Vector2Int[]
    {
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0),   // Right
        new Vector2Int(-1, -1), // Top-Left
        new Vector2Int(0, -1),  // Top-Right
        new Vector2Int(-1, 1),  // Bottom-Left
        new Vector2Int(0, 1)    // Bottom-Right
    };
    
    // Odd rows (y % 2 != 0) - shifted right by 0.5
    private static readonly Vector2Int[] oddRowNeighbors = new Vector2Int[]
    {
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Top-Left
        new Vector2Int(1, -1),  // Top-Right
        new Vector2Int(0, 1),   // Bottom-Left
        new Vector2Int(1, 1)    // Bottom-Right
    };

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        ClearGrid();
        gridData = new List<List<Bubble>>();
        xOffset = 0;
        
        for (int y = 0; y < startingHeight; y++)
        {
            List<Bubble> row = CreateRow(y, startingWidth);
            gridData.Add(row);
        }
    }

    private List<Bubble> CreateRow(int y, int rowWidth)
    {
        List<Bubble> row = new List<Bubble>();
        bool isShortRow = y % 2 != 0;
        int columnCount = isShortRow ? rowWidth - 1 : rowWidth;
        
        for (int x = 0; x < columnCount; x++)
        {
            GameObject go = Instantiate(bubblePrefab, transform);
            Bubble bubble = go.GetComponent<Bubble>();
            
            BubbleType randomType = (BubbleType)Random.Range(0, Enum.GetValues(typeof(BubbleType)).Length);
            bubble.SetType(randomType);
            bubble.isAttached = true; // Mark as part of grid
            
            float offset = isShortRow ? 0.5f : 0f;
            bubble.transform.localPosition = new Vector3(x + offset, -y * 0.9f, 0f);
            
            row.Add(bubble);
        }
        
        return row;
    }

    public Bubble GetBubbleAt(int x, int y)
    {
        if (y < 0 || y >= gridData.Count)
            return null;
        
        int dataX = x + xOffset;
        if (dataX < 0 || dataX >= gridData[y].Count)
            return null;
        
        return gridData[y][dataX];
    }

    public Bubble GetBubbleAt(Vector2Int pos)
    {
        return GetBubbleAt(pos.x, pos.y);
    }
    
    // Check if a grid position is empty (no bubble or out of current bounds)
    public bool IsPositionEmpty(int x, int y)
    {
        if (y < 0) return false; // Don't allow above row 0
        
        // If row doesn't exist yet, it's empty
        if (y >= gridData.Count) return true;
        
        int dataX = x + xOffset;
        
        // If column doesn't exist in this row, it's empty
        if (dataX < 0 || dataX >= gridData[y].Count) return true;
        
        // Check if there's a bubble there
        return gridData[y][dataX] == null;
    }
    
    public bool IsPositionEmpty(Vector2Int pos)
    {
        return IsPositionEmpty(pos.x, pos.y);
    }

    public void ClearGrid()
    {
        if (gridData == null) return;
        
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null)
                    Destroy(bubble.gameObject);
            }
        }
        gridData.Clear();
        xOffset = 0;
    }

    // Convert world position to grid coordinates (can be negative)
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        
        int y = Mathf.RoundToInt(-localPos.y / 0.9f);
        y = Mathf.Max(0, y);
        
        bool isShortRow = y % 2 != 0;
        float offset = isShortRow ? 0.5f : 0f;
        int x = Mathf.RoundToInt(localPos.x - offset);
        
        return new Vector2Int(x, y);
    }

    // Convert grid coordinates to world position
    public Vector3 GridToWorldPosition(int x, int y)
    {
        bool isShortRow = y % 2 != 0;
        float offset = isShortRow ? 0.5f : 0f;
        return transform.position + new Vector3(x + offset, -y * 0.9f, 0f);
    }

    // Expand grid rows if needed
    private void EnsureRowExists(int y)
    {
        while (y >= gridData.Count)
        {
            gridData.Add(new List<Bubble>());
        }
    }

    // Expand grid to the left if x is negative
    private void ExpandLeft(int amount)
    {
        for (int y = 0; y < gridData.Count; y++)
        {
            for (int i = 0; i < amount; i++)
            {
                gridData[y].Insert(0, null);
            }
        }
        xOffset += amount;
        Debug.Log($"Expanded grid left by {amount}, new xOffset: {xOffset}");
    }

    // Expand a specific row to fit x position (on the right side)
    private void EnsureColumnExists(int dataX, int y)
    {
        while (dataX >= gridData[y].Count)
        {
            gridData[y].Add(null);
        }
    }
    
    // Get all neighbor positions for a hex grid cell
    public List<Vector2Int> GetNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] offsets = (y % 2 == 0) ? evenRowNeighbors : oddRowNeighbors;
        
        foreach (var offset in offsets)
        {
            neighbors.Add(new Vector2Int(x + offset.x, y + offset.y));
        }
        
        return neighbors;
    }
    
    public List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return GetNeighbors(pos.x, pos.y);
    }
    
    // Find the best empty position to attach a bubble given a world position
    // Returns the nearest empty grid position, checking the target cell first, then neighbors
    public Vector2Int FindAttachPosition(Vector3 worldPos)
    {
        Vector2Int targetPos = WorldToGridPosition(worldPos);
        
        // If target position is empty, use it
        if (IsPositionEmpty(targetPos))
        {
            return targetPos;
        }
        
        // Target is occupied, find nearest empty neighbor
        List<Vector2Int> neighbors = GetNeighbors(targetPos);
        
        float bestDistance = float.MaxValue;
        Vector2Int bestPos = targetPos;
        bool foundEmpty = false;
        
        foreach (var neighborPos in neighbors)
        {
            if (neighborPos.y < 0) continue; // Skip positions above the grid
            
            if (IsPositionEmpty(neighborPos))
            {
                Vector3 neighborWorld = GridToWorldPosition(neighborPos.x, neighborPos.y);
                float distance = Vector3.Distance(worldPos, neighborWorld);
                
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPos = neighborPos;
                    foundEmpty = true;
                }
            }
        }
        
        if (!foundEmpty)
        {
            Debug.LogWarning($"No empty position found near {targetPos}, using target anyway");
        }
        
        return bestPos;
    }

    // Attach a bubble to the grid at nearest empty slot
    // Returns the grid position where it was attached
    public Vector2Int AttachBubble(Bubble bubble, Vector3 worldPos)
    {
        // Safety check - don't attach if already attached
        if (bubble.isAttached)
        {
            Debug.LogWarning($"Bubble {bubble.type} is already attached, skipping");
            return new Vector2Int(-1, -1);
        }
        
        // Find the best empty position
        Vector2Int gridPos = FindAttachPosition(worldPos);
        
        // Expand rows if needed
        EnsureRowExists(gridPos.y);
        
        // Handle left expansion if x is negative
        if (gridPos.x < -xOffset)
        {
            int expandAmount = (-xOffset) - gridPos.x;
            ExpandLeft(expandAmount);
        }
        
        // Convert to data index
        int dataX = gridPos.x + xOffset;
        
        // Expand right if needed
        EnsureColumnExists(dataX, gridPos.y);
        
        // Set position and parent
        bubble.transform.SetParent(transform);
        bubble.transform.position = GridToWorldPosition(gridPos.x, gridPos.y);
        
        // Mark as attached
        bubble.isAttached = true;
        
        // Add to grid
        gridData[gridPos.y][dataX] = bubble;
        
        Debug.Log($"Attached {bubble.type} bubble at grid ({gridPos.x}, {gridPos.y})");
        
        return gridPos;
    }
    
    // Flood fill to find all connected bubbles of the same color
    // Returns list of grid positions with matching bubbles
    public List<Vector2Int> FloodFillSameColor(Vector2Int startPos)
    {
        List<Vector2Int> matchedPositions = new List<Vector2Int>();
        
        Bubble startBubble = GetBubbleAt(startPos);
        if (startBubble == null)
        {
            Debug.LogWarning($"FloodFill: No bubble at start position {startPos}");
            return matchedPositions;
        }
        
        BubbleType targetType = startBubble.type;
        Debug.Log($"FloodFill: Starting from {startPos}, looking for {targetType} bubbles");
        
        // Reset visited flags for all bubbles
        ResetVisitedFlags();
        
        // BFS queue
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);
        startBubble.visited = true;
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            matchedPositions.Add(current);
            
            // Check all neighbors
            List<Vector2Int> neighbors = GetNeighbors(current);
            foreach (var neighborPos in neighbors)
            {
                Bubble neighbor = GetBubbleAt(neighborPos);
                
                // Skip if no bubble, already visited, or different color
                if (neighbor == null) continue;
                if (neighbor.visited) continue;
                if (neighbor.type != targetType) continue;
                
                neighbor.visited = true;
                queue.Enqueue(neighborPos);
            }
        }
        
        return matchedPositions;
    }
    
    // Reset all visited flags
    private void ResetVisitedFlags()
    {
        foreach (var row in gridData)
        {
            foreach (var bubble in row)
            {
                if (bubble != null)
                    bubble.visited = false;
            }
        }
    }
    
    // Check for matches and destroy if enough connected
    // Returns true if bubbles were destroyed
    public bool CheckAndDestroyMatches(Vector2Int startPos)
    {
        // Check for invalid position (returned when bubble was already attached)
        if (startPos.x == -1 && startPos.y == -1)
        {
            Debug.LogWarning("CheckAndDestroyMatches called with invalid position, skipping");
            return false;
        }
        
        List<Vector2Int> matches = FloodFillSameColor(startPos);
        
        Debug.Log($"Found {matches.Count} matching bubbles");
        
        if (matches.Count >= minMatchCount)
        {
            // Destroy all matched bubbles
            foreach (var pos in matches)
            {
                RemoveBubbleAt(pos);
            }
            
            Debug.Log($"Destroyed {matches.Count} bubbles!");
            return true;
        }
        
        return false;
    }
    
    // Remove a bubble from the grid and destroy it
    public void RemoveBubbleAt(Vector2Int pos)
    {
        RemoveBubbleAt(pos.x, pos.y);
    }
    
    public void RemoveBubbleAt(int x, int y)
    {
        if (y < 0 || y >= gridData.Count) return;
        
        int dataX = x + xOffset;
        if (dataX < 0 || dataX >= gridData[y].Count) return;
        
        Bubble bubble = gridData[y][dataX];
        if (bubble != null)
        {
            bubble.Explode();
            gridData[y][dataX] = null;
        }
    }
}