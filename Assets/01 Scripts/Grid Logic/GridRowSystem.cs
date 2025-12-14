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
    [SerializeField] private bool rowPending = false;
    
    [Header("Multi-Row Spawn")]
    public float multiRowDelay = 0.2f; // Delay between rows when spawning multiple
    
    // Events
    public Action<int, int> onShotsChanged;
    public Action onRowSpawned;
    public Action<int> onSurvivalRowSpawned;
    public Action<float> onSurvivalIntervalChanged;
    
    private HexGrid grid;
    private PlayerController playerController;
    
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
    
    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        PlayerController.onBubbleConnected += OnBubbleConnected;
    }
    
    void OnDestroy()
    {
        PlayerController.onBubbleConnected -= OnBubbleConnected;
    }
    
    void Update()
    {
        if (!IsSurvivalMode) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        
        UpdateSurvivalTimer();
    }
    
    // Returns how many rows to spawn per trigger.
    private int GetRowsToSpawn()
    {
        if (grid.ColorRemoval != null)
        {
            return grid.ColorRemoval.RowsPerSpawn;
        }
        return 1;
    }

    #region Survival Mode
    
    private void UpdateSurvivalTimer()
    {
        if (grid.IsDestroying) return;
        if (rowPending) return;
        
        survivalTimer += Time.deltaTime;
        
        if (survivalTimer >= currentInterval)
        {
            bool bubbleFlying = playerController != null && playerController.IsBubbleFlying;
            
            if (bubbleFlying)
            {
                rowPending = true;
                grid.Log("[GridRowSystem] Row spawn pending - waiting for bubble to connect");
            }
            else
            {
                TriggerSurvivalSpawn();
                survivalTimer = 0f;
            }
        }
    }
    
    private void OnBubbleConnected()
    {
        if (!IsSurvivalMode) return;
        if (!rowPending) return;
        
        grid.Log("[GridRowSystem] Bubble connected - spawning pending row");
        
        TriggerSurvivalSpawn();
        
        survivalTimer = 0f;
        rowPending = false;
    }
    
    private void TriggerSurvivalSpawn()
    {
        int rowsToSpawn = GetRowsToSpawn();
        
        grid.Log($"[GridRowSystem] Survival trigger - spawning {rowsToSpawn} row(s)");
        
        if (rowsToSpawn == 1)
        {
            SpawnSurvivalRow();
        }
        else
        {
            StartCoroutine(SpawnMultipleSurvivalRows(rowsToSpawn));
        }
    }
    
    private System.Collections.IEnumerator SpawnMultipleSurvivalRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSurvivalRow();
            
            if (i < count - 1)
            {
                yield return new WaitForSeconds(multiRowDelay);
            }
        }
    }
    
    private void SpawnSurvivalRow()
    {
        SpawnNewRowAtTop();
        survivalRowsSpawned++;
        
        GameManager.Instance.OnSurvivalRowSpawned(survivalRowsSpawned);
        onSurvivalRowSpawned?.Invoke(survivalRowsSpawned);
        
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
    
    public void ResetSurvival()
    {
        survivalRowsSpawned = 0;
        survivalTimer = 0f;
        rowPending = false;
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
            TriggerShotSpawn();
            ResetShots();
        }
    }
    
    private void TriggerShotSpawn()
    {
        int rowsToSpawn = GetRowsToSpawn();
        
        grid.Log($"[GridRowSystem] Shot trigger - spawning {rowsToSpawn} row(s)");
        
        if (rowsToSpawn == 1)
        {
            SpawnNewRowAtTop();
        }
        else
        {
            StartCoroutine(SpawnMultipleRows(rowsToSpawn));
        }
    }
    
    private System.Collections.IEnumerator SpawnMultipleRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnNewRowAtTop();
            
            if (i < count - 1)
            {
                yield return new WaitForSeconds(multiRowDelay);
            }
        }
    }
    
    #endregion

    #region Row Spawning
    
    public void SpawnNewRowAtTop()
    {
        grid.Log("Spawning new row at top");
        
        PushGridDown();
        
        var gridColors = grid.GetAvailableColors();
        var levelColors = grid.GetLevelColors();
        var availableColors = new List<BubbleType>();
        
        foreach (var color in gridColors)
        {
            if (levelColors.Contains(color))
            {
                availableColors.Add(color);
            }
        }
        
        if (availableColors.Count == 0)
        {
            availableColors.AddRange(levelColors);
            grid.Log("[GridRowSystem] No colors in grid, using all level colors");
        }
        
        List<Bubble> newRow = grid.CreateRow(0, availableColors);
        grid.InsertRowAtTop(newRow);
        
        UpdateAllBubblePositions();
        RelocateFloatingBubbles();
        
        grid.MatchSystem.CheckLoseCondition();
        
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
        
        grid.SetBubbleAt(from, null);
        
        grid.EnsureRowExists(to.y);
        grid.EnsureColumnExists(to.x, to.y);
        
        grid.SetBubbleAt(to, bubble);
        bubble.transform.position = grid.GridToWorld(to);
    }
    
    #endregion
}