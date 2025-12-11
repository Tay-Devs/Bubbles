using System;
using System.Collections.Generic;
using UnityEngine;

public class GridRowSystem : MonoBehaviour
{
    [Header("Shot System (Normal Mode)")]
    public int shotsBeforeNewRow = 5;
    [SerializeField] private int currentShotsRemaining;
    
    [Header("Survival Mode Runtime")]
    [SerializeField] private int survivalRowsSpawned = 0;
    [SerializeField] private float survivalTimer = 0f;
    [SerializeField] private float currentInterval;
    
    // Events
    public Action<int, int> onShotsChanged; // (current, max)
    public Action onRowSpawned;
    public Action<int> onSurvivalRowSpawned; // (totalRowsSpawned)
    public Action<float> onSurvivalIntervalChanged; // (newInterval)
    
    private HexGrid grid;
    
    public int CurrentShots => currentShotsRemaining;
    public int MaxShots => shotsBeforeNewRow;
    public int SurvivalRowsSpawned => survivalRowsSpawned;
    public float CurrentSurvivalInterval => currentInterval;
    public float SurvivalTimer => survivalTimer;
    public bool IsSurvivalMode => GameManager.Instance != null && 
                                   GameManager.Instance.ActiveWinCondition == WinConditionType.Survival;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }
    
    void Update()
    {
        if (!IsSurvivalMode) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        
        UpdateSurvivalTimer();
    }

    #region Survival Mode
    
    // Updates the survival timer and spawns rows when interval is reached.
    // Pauses timer while bubbles are being destroyed (chain reactions).
    private void UpdateSurvivalTimer()
    {
        if (grid.IsDestroying)
        {
            return;
        }
        
        survivalTimer += Time.deltaTime;
        
        if (survivalTimer >= currentInterval)
        {
            survivalTimer = 0f;
            SpawnSurvivalRow();
        }
    }
    
    // Spawns a new row and reduces the interval for next spawn.
    // Interval decreases until it reaches the minimum value.
    private void SpawnSurvivalRow()
    {
        SpawnNewRowAtTop();
        survivalRowsSpawned++;
        
        GameManager.Instance.OnSurvivalRowSpawned(survivalRowsSpawned);
        onSurvivalRowSpawned?.Invoke(survivalRowsSpawned);
        
        // Reduce interval for next row
        float deduction = GameManager.Instance.SurvivalIntervalDeduction;
        float minInterval = GameManager.Instance.SurvivalMinInterval;
        
        float previousInterval = currentInterval;
        currentInterval = Mathf.Max(currentInterval - deduction, minInterval);
        
        if (currentInterval != previousInterval)
        {
            onSurvivalIntervalChanged?.Invoke(currentInterval);
            grid.Log($"Survival interval reduced: {previousInterval:F1}s -> {currentInterval:F1}s");
        }
    }
    
    // Resets survival mode state for a new game.
    public void ResetSurvival()
    {
        survivalRowsSpawned = 0;
        survivalTimer = 0f;
        currentInterval = GameManager.Instance != null 
            ? GameManager.Instance.SurvivalStartingInterval 
            : 10f;
        
        onSurvivalIntervalChanged?.Invoke(currentInterval);
        grid.Log($"Survival reset. Starting interval: {currentInterval}s");
    }
    
    #endregion

    #region Shot Management
    
    public void ResetShots()
    {
        currentShotsRemaining = shotsBeforeNewRow;
        onShotsChanged?.Invoke(currentShotsRemaining, shotsBeforeNewRow);
        grid.Log($"Shots reset to {currentShotsRemaining}");
    }
    
    // Consumes a shot and spawns a new row if shots are depleted.
    // Does nothing in Survival mode since rows are time-based.
    public void ConsumeShot()
    {
        if (IsSurvivalMode)
        {
            grid.Log("Survival mode: Shot not consumed (time-based rows)");
            return;
        }
        
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
    
    // Spawns a new row at the top, pushing existing bubbles down.
    // Handles floating bubble relocation and lose condition check.
    public void SpawnNewRowAtTop()
    {
        grid.Log("Spawning new row at top");
        
        PushGridDown();
        
        var availableColors = new List<BubbleType>(grid.GetAvailableColors());
        if (availableColors.Count == 0)
        {
            foreach (BubbleType color in Enum.GetValues(typeof(BubbleType)))
                availableColors.Add(color);
        }
        
        List<Bubble> newRow = grid.CreateRow(0, availableColors);
        grid.InsertRowAtTop(newRow);
        
        UpdateAllBubblePositions();
        RelocateFloatingBubbles();
        
        grid.MatchSystem.CheckLoseCondition();
        
        grid.onColorsChanged?.Invoke();
        onRowSpawned?.Invoke();
    }
    
    // Moves all existing bubbles down by one row height.
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
    
    // Updates all bubble positions to match their grid coordinates.
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
    
    // Iteratively relocates floating bubbles to valid connected positions.
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
    
    // Finds the nearest empty neighbor position adjacent to connected bubbles.
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
    
    // Finds any empty position adjacent to any connected bubble as a fallback.
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
    
    // Moves a bubble from one grid position to another.
    private void MoveBubbleInGrid(Vector2Int from, Vector2Int to)
    {
        var bubble = grid.GetBubbleAt(from);
        if (bubble == null) return;
        
        if (to.x < 0 || to.x >= grid.width) return;
        
        grid.SetBubbleAt(from, null);
        
        grid.EnsureRowExists(to.y);
        grid.EnsureColumnExists(to.x, to.y);
        
        grid.SetBubbleAt(to, bubble);
        bubble.transform.position = grid.GridToWorld(to);
    }
    
    #endregion
}