using System;
using System.Collections.Generic;
using UnityEngine;

public class GridRowSystem : MonoBehaviour
{
    [Header("Shot System")]
    public int shotsBeforeNewRow = 5;
    [SerializeField] private int currentShotsRemaining;
    
    // Events
    public Action<int, int> onShotsChanged; // (current, max)
    public Action onRowSpawned;
    
    private HexGrid grid;
    
    public int CurrentShots => currentShotsRemaining;
    public int MaxShots => shotsBeforeNewRow;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    #region Shot Management
    
    public void ResetShots()
    {
        currentShotsRemaining = shotsBeforeNewRow;
        onShotsChanged?.Invoke(currentShotsRemaining, shotsBeforeNewRow);
        grid.Log($"Shots reset to {currentShotsRemaining}");
    }
    
    public void ConsumeShot()
    {
        currentShotsRemaining--;
        onShotsChanged?.Invoke(currentShotsRemaining, shotsBeforeNewRow);
        grid.Log($"Shot consumed, {currentShotsRemaining} remaining");
        
        if (currentShotsRemaining <= 0)
        {
            SpawnNewRowAtTop();
            ResetShots();
        }
    }
    
    #endregion

    #region Row Spawning
    
    public void SpawnNewRowAtTop()
    {
        grid.Log("Spawning new row at top");
        
        // Push all existing bubbles down
        PushGridDown();
        
        // Get available colors
        var availableColors = new List<BubbleType>(grid.GetAvailableColors());
        if (availableColors.Count == 0)
        {
            foreach (BubbleType color in Enum.GetValues(typeof(BubbleType)))
                availableColors.Add(color);
        }
        
        // Create new row at position 0
        List<Bubble> newRow = grid.CreateRow(0, availableColors);
        grid.InsertRowAtTop(newRow);
        
        // Update all bubble positions
        UpdateAllBubblePositions();
        
        // Relocate any floating bubbles
        RelocateFloatingBubbles();
        
        // Check lose condition
        grid.MatchSystem.CheckLoseCondition();
        
        // Notify
        grid.onColorsChanged?.Invoke();
        onRowSpawned?.Invoke();
    }
    
    private void PushGridDown()
    {
        foreach (var row in grid.GetGridData())
        {
            foreach (var bubble in row)
            {
                if (bubble != null)
                {
                    bubble.transform.localPosition += new Vector3(0, -grid.rowHeight, 0);
                }
            }
        }
    }
    
    private void UpdateAllBubblePositions()
    {
        var gridData = grid.GetGridData();
        
        for (int y = 0; y < gridData.Count; y++)
        {
            float rowOffset = grid.GetRowXOffset(y);
            
            for (int x = 0; x < gridData[y].Count; x++)
            {
                var bubble = gridData[y][x];
                if (bubble != null)
                {
                    bubble.transform.localPosition = new Vector3(x + rowOffset, -y * grid.rowHeight, 0f);
                }
            }
        }
    }
    
    #endregion

    #region Floating Bubble Relocation
    
    private void RelocateFloatingBubbles()
    {
        int maxIterations = 100;
        int iterations = 0;
        
        while (iterations < maxIterations)
        {
            iterations++;
            
            var floating = grid.MatchSystem.GetFloatingBubbles();
            if (floating.Count == 0) break;
            
            var connected = grid.MatchSystem.FindConnectedToTop();
            bool movedAny = false;
            
            foreach (var floatPos in floating)
            {
                var bubble = grid.GetBubbleAt(floatPos);
                if (bubble == null) continue;
                
                Vector2Int? newPos = FindValidRelocationPosition(floatPos, connected);
                
                if (newPos.HasValue)
                {
                    MoveBubbleInGrid(floatPos, newPos.Value);
                    grid.Log($"Relocated bubble from {floatPos} to {newPos.Value}");
                    movedAny = true;
                    break;
                }
            }
            
            // If no moves possible, try force relocation
            if (!movedAny && floating.Count > 0)
            {
                foreach (var floatPos in floating)
                {
                    var bubble = grid.GetBubbleAt(floatPos);
                    if (bubble == null) continue;
                    
                    Vector2Int? forcePos = ForceRelocationPosition(floatPos, connected);
                    if (forcePos.HasValue)
                    {
                        MoveBubbleInGrid(floatPos, forcePos.Value);
                        grid.Log($"Force relocated bubble from {floatPos} to {forcePos.Value}");
                        movedAny = true;
                        break;
                    }
                }
            }
            
            if (!movedAny) break;
        }
    }
    
    private Vector2Int? FindValidRelocationPosition(Vector2Int fromPos, HashSet<Vector2Int> connected)
    {
        var neighbors = grid.GetNeighbors(fromPos);
        float bestDist = float.MaxValue;
        Vector2Int? bestPos = null;
        
        foreach (var neighbor in neighbors)
        {
            if (neighbor.y < 0) continue;
            if (neighbor.x < 0 || neighbor.x >= grid.width) continue;
            
            if (grid.IsEmpty(neighbor) && grid.MatchSystem.IsAdjacentToConnected(neighbor, connected))
            {
                float dist = Vector2Int.Distance(fromPos, neighbor);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = neighbor;
                }
            }
        }
        
        return bestPos;
    }
    
    private Vector2Int? ForceRelocationPosition(Vector2Int fromPos, HashSet<Vector2Int> connected)
    {
        float bestDist = float.MaxValue;
        Vector2Int? bestPos = null;
        
        foreach (var connectedPos in connected)
        {
            foreach (var neighbor in grid.GetNeighbors(connectedPos))
            {
                if (neighbor.y < 0) continue;
                if (neighbor.x < 0 || neighbor.x >= grid.width) continue;
                
                if (grid.IsEmpty(neighbor))
                {
                    float dist = Vector2Int.Distance(fromPos, neighbor);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPos = neighbor;
                    }
                }
            }
        }
        
        return bestPos;
    }
    
    private void MoveBubbleInGrid(Vector2Int from, Vector2Int to)
    {
        var bubble = grid.GetBubbleAt(from);
        if (bubble == null) return;
        
        if (to.x < 0 || to.x >= grid.width) return;
        
        // Remove from old position
        grid.SetBubbleAt(from, null);
        
        // Ensure new position exists
        grid.EnsureRowExists(to.y);
        grid.EnsureColumnExists(to.x, to.y);
        
        // Add to new position
        grid.SetBubbleAt(to, bubble);
        bubble.transform.position = grid.GridToWorld(to);
    }
    
    #endregion
}