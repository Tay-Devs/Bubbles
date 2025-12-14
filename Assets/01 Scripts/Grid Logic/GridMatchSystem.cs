using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMatchSystem : MonoBehaviour
{
    [Header("Match Settings")]
    public int minMatchCount = 3;
    
    [Header("Destruction Animation")]
    public float destructionDelay = 0.15f;
    public float destructionDelayMultiplier = 0.85f;
    public float destructionDelayLimit = 0.03f;
    
    [Header("Lose Condition")]
    public LoseZone loseZone;
    
    [Header("Score Popup")]
    public GameObject scorePopupPrefab;
    
    private HexGrid grid;
    private bool isDestroying = false;
    private bool hadMatchThisShot = false; // Track if current shot made a match
    
    public bool IsDestroying => isDestroying;
    public bool HadMatchThisShot => hadMatchThisShot;
    
    // Event fired when destruction sequence completes
    public Action onDestructionComplete;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    #region Match Detection
    
    // Checks for color matches at position and starts destruction if found.
    // Returns true if matches were found and destruction started.
    public bool CheckAndDestroyMatches(Vector2Int startPos)
    {
        if (startPos.x == -1) return false;
        
        var startBubble = grid.GetBubbleAt(startPos);
        if (startBubble == null) return false;
        
        var matches = FloodFill(startPos, startBubble.type);
        grid.Log($"Found {matches.Count} matching {startBubble.type} bubbles");
        
        if (matches.Count >= minMatchCount)
        {
            hadMatchThisShot = true;
            StartCoroutine(DestroyMatchedBubblesSequentially(matches));
            return true;
        }
        
        // No match - mark that this shot didn't have a match
        hadMatchThisShot = false;
        
        if (CheckLoseCondition())
        {
            return false;
        }
        
        grid.RowSystem.ConsumeShot();
        return false;
    }
    
    // Flood fills from start position to find all connected bubbles of same type.
    public List<Vector2Int> FloodFill(Vector2Int start, BubbleType? matchType = null)
    {
        var result = new List<Vector2Int>();
        var startBubble = grid.GetBubbleAt(start);
        if (startBubble == null) return result;
        
        grid.ResetVisited();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        startBubble.visited = true;
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            
            foreach (var neighbor in grid.GetNeighbors(current))
            {
                var bubble = grid.GetBubbleAt(neighbor);
                if (bubble == null || bubble.visited) continue;
                if (matchType.HasValue && bubble.type != matchType.Value) continue;
                
                bubble.visited = true;
                queue.Enqueue(neighbor);
            }
        }
        return result;
    }
    
    #endregion

    #region Destruction
    
    // Destroys matched bubbles sequentially with scoring and popups.
    // After destruction completes, fires onDestructionComplete event.
    private IEnumerator DestroyMatchedBubblesSequentially(List<Vector2Int> positions)
    {
        isDestroying = true;
        grid.Log($"Destroying {positions.Count} matched bubbles sequentially");
        
        float currentDelay = destructionDelay;
        int lastPoints = 0;
        
        for (int i = 0; i < positions.Count; i++)
        {
            yield return new WaitForSeconds(currentDelay);
            
            Vector3 bubbleWorldPos = grid.GridToWorld(positions[i]);
            RemoveBubbleAt(positions[i]);
            
            if (ScoreManager.Instance != null)
            {
                int points = ScoreManager.Instance.GetMatchBubblePoints(i);
                ScoreManager.Instance.AddScore(points);
                SpawnScorePopup(bubbleWorldPos, points, i);
                lastPoints = points;
            }
            
            currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
        }
        
        int matchCount = positions.Count;
        yield return StartCoroutine(DestroyFloatingBubblesCoroutine(currentDelay, matchCount, lastPoints));
        
        isDestroying = false;
        hadMatchThisShot = false;
        
        grid.onColorsChanged?.Invoke();
        
        // Check win conditions
        if (CheckWinCondition())
        {
            onDestructionComplete?.Invoke();
            yield break;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.CheckScoreVictory())
        {
            onDestructionComplete?.Invoke();
            yield break;
        }
        
        if (CheckLoseCondition())
        {
            onDestructionComplete?.Invoke();
            yield break;
        }
        
        // Consume shot after destruction (for non-survival modes)
        grid.RowSystem.ConsumeShot();
        
        // Notify that destruction is complete (for pending row spawns)
        onDestructionComplete?.Invoke();
    }
    
    // Destroys floating bubbles with combo scoring continuation.
    private IEnumerator DestroyFloatingBubblesCoroutine(float startingDelay, int startingComboIndex = 0, int lastMatchPoints = 0)
    {
        var floating = GetFloatingBubbles();
        
        if (floating.Count > 0)
        {
            grid.Log($"Found {floating.Count} floating bubbles, continuing from combo index {startingComboIndex}");
            
            float currentDelay = startingDelay;
            
            for (int i = 0; i < floating.Count; i++)
            {
                yield return new WaitForSeconds(currentDelay);
                
                Vector3 bubbleWorldPos = grid.GridToWorld(floating[i]);
                RemoveBubbleAt(floating[i]);
                
                if (ScoreManager.Instance != null)
                {
                    int points;
                    if (lastMatchPoints > 0)
                    {
                        points = ScoreManager.Instance.GetFloatingBubblePointsFromBase(i, lastMatchPoints);
                    }
                    else
                    {
                        points = ScoreManager.Instance.GetFloatingBubblePoints(i);
                    }
                    
                    ScoreManager.Instance.AddScore(points);
                    
                    int comboIndex = startingComboIndex + i;
                    SpawnScorePopup(bubbleWorldPos, points, comboIndex);
                }
                
                currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
            }
        }
    }
    
    private void SpawnScorePopup(Vector3 position, int points, int comboIndex = 0)
    {
        if (scorePopupPrefab == null) return;
        ScorePopup.Create(scorePopupPrefab, position, points, comboIndex);
    }
    
    public void RemoveBubbleAt(Vector2Int pos) => RemoveBubbleAt(pos.x, pos.y);
    public void RemoveBubbleAt(int x, int y)
    {
        var bubble = grid.GetBubbleAt(x, y);
        if (bubble != null)
        {
            bubble.Explode();
            grid.SetBubbleAt(x, y, null);
        }
    }
    
    public int DestroyFloatingBubbles()
    {
        var floating = GetFloatingBubbles();
        
        if (floating.Count > 0)
        {
            grid.Log($"Found {floating.Count} floating bubbles");
            StartCoroutine(DestroyFloatingBubblesCoroutine(destructionDelay, 0, 0));
        }
        
        return floating.Count;
    }
    
    #endregion

    #region Floating Bubbles
    
    public HashSet<Vector2Int> FindConnectedToTop()
    {
        var connected = new HashSet<Vector2Int>();
        var gridData = grid.GetGridData();
        if (gridData.Count == 0) return connected;
        
        grid.ResetVisited();
        var queue = new Queue<Vector2Int>();
        
        for (int x = 0; x < gridData[0].Count; x++)
        {
            var bubble = gridData[0][x];
            if (bubble != null && !bubble.visited)
            {
                bubble.visited = true;
                queue.Enqueue(new Vector2Int(x, 0));
            }
        }
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            connected.Add(current);
            
            foreach (var neighbor in grid.GetNeighbors(current))
            {
                var bubble = grid.GetBubbleAt(neighbor);
                if (bubble != null && !bubble.visited)
                {
                    bubble.visited = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        
        return connected;
    }
    
    public List<Vector2Int> GetFloatingBubbles()
    {
        var floating = new List<Vector2Int>();
        var gridData = grid.GetGridData();
        if (gridData.Count == 0) return floating;
        
        var connected = FindConnectedToTop();
        
        for (int y = 0; y < gridData.Count; y++)
        {
            for (int x = 0; x < gridData[y].Count; x++)
            {
                if (gridData[y][x] != null)
                {
                    var pos = new Vector2Int(x, y);
                    if (!connected.Contains(pos))
                        floating.Add(pos);
                }
            }
        }
        
        return floating;
    }
    
    public bool IsAdjacentToConnected(Vector2Int pos, HashSet<Vector2Int> connected)
    {
        foreach (var neighbor in grid.GetNeighbors(pos))
        {
            if (connected.Contains(neighbor))
                return true;
        }
        return false;
    }
    
    #endregion

    #region Win/Lose Conditions
    
    private bool CheckWinCondition()
    {
        if (GameManager.Instance == null) return false;
        if (!GameManager.Instance.IsPlaying) return false;
        
        if (grid.IsGridEmpty())
        {
            grid.Log("All bubbles cleared - notifying GameManager");
            GameManager.Instance.OnAllBubblesCleared();
            return true;
        }
        
        return false;
    }
    
    public bool CheckLoseCondition()
    {
        if (loseZone == null || GameManager.Instance == null) return false;
        if (!GameManager.Instance.IsPlaying) return false;
        
        foreach (var row in grid.GetGridData())
        {
            foreach (var bubble in row)
            {
                if (bubble != null && loseZone.IsInLoseZone(bubble.transform.position))
                {
                    grid.Log($"Bubble at {bubble.transform.position.y} is in lose zone");
                    GameManager.Instance.GameOver();
                    return true;
                }
            }
        }
        return false;
    }
    
    #endregion
}