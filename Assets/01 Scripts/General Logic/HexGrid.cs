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
    
    private List<List<Bubble>> gridData;
    
    // Tracks how many columns were added to the left
    // Grid data index 0 corresponds to world x = -xOffset
    private int xOffset = 0;

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

    // Attach a bubble to the grid at nearest empty slot
    public void AttachBubble(Bubble bubble, Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        
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
        
        // Add to grid
        gridData[gridPos.y][dataX] = bubble;
        
        Debug.Log($"Attached bubble at grid ({gridPos.x}, {gridPos.y}), data index ({dataX}, {gridPos.y})");
    }
}