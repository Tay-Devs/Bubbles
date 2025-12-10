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
    
    public bool IsDestroying => isDestroying;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    #region Match Detection
    
    // Check for matches and destroy them
    public bool CheckAndDestroyMatches(Vector2Int startPos)
    {
        if (startPos.x == -1) return false;
        
        var startBubble = grid.GetBubbleAt(startPos);
        if (startBubble == null) return false;
        
        var matches = FloodFill(startPos, startBubble.type);
        grid.Log($"Found {matches.Count} matching {startBubble.type} bubbles");
        
        if (matches.Count >= minMatchCount)
        {
            // Match found - destroy bubbles with per-bubble scoring
            StartCoroutine(DestroyMatchedBubblesSequentially(matches));
            return true;
        }
        
        // No match - NOW check lose condition since no bubbles will be cleared
        if (CheckLoseCondition())
        {
            return false;
        }
        
        // No match and not lost - consume a shot
        grid.RowSystem.ConsumeShot();
        return false;
    }
    
    // Flood fill to find connected bubbles of the same color
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
    
    // Destroys matched bubbles one at a time, awarding scaled points per bubble.
    // Uses per-bubble scaling: first 3 get base points, then increasing multipliers.
    private IEnumerator DestroyMatchedBubblesSequentially(List<Vector2Int> positions)
    {
        isDestroying = true;
        grid.Log($"Destroying {positions.Count} matched bubbles sequentially");
        
        float currentDelay = destructionDelay;
        int lastPoints = 0;
        
        for (int i = 0; i < positions.Count; i++)
        {
            yield return new WaitForSeconds(currentDelay);
            
            // Get bubble position before destroying it
            Vector3 bubbleWorldPos = grid.GridToWorld(positions[i]);
            
            RemoveBubbleAt(positions[i]);
            
            // Award score and spawn popup
            if (ScoreManager.Instance != null)
            {
                int points = ScoreManager.Instance.GetMatchBubblePoints(i);
                ScoreManager.Instance.AddScore(points);
                SpawnScorePopup(bubbleWorldPos, points, i);
                lastPoints = points;
            }
            
            currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
        }
        
        // Pass match count and last points to continue combo for floating bubbles
        int matchCount = positions.Count;
        yield return StartCoroutine(DestroyFloatingBubblesCoroutine(currentDelay, matchCount, lastPoints));
        
        isDestroying = false;
        
        grid.onColorsChanged?.Invoke();
        
        if (CheckWinCondition()) yield break;
        
        // Check lose condition after destruction - bubbles in lose zone may have been cleared
        CheckLoseCondition();
    }
    
    // Destroys floating bubbles one at a time with multiplicative scoring.
    // Continues combo index and scoring from the matched bubbles that triggered this.
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
                
                // Get bubble position before destroying it
                Vector3 bubbleWorldPos = grid.GridToWorld(floating[i]);
                
                RemoveBubbleAt(floating[i]);
                
                // Award score and spawn popup, continuing combo from match
                if (ScoreManager.Instance != null)
                {
                    int points;
                    if (lastMatchPoints > 0)
                    {
                        // Continue from last match points with multiplicative scaling
                        points = ScoreManager.Instance.GetFloatingBubblePointsFromBase(i, lastMatchPoints);
                    }
                    else
                    {
                        // Fallback to default floating scoring
                        points = ScoreManager.Instance.GetFloatingBubblePoints(i);
                    }
                    
                    ScoreManager.Instance.AddScore(points);
                    
                    // Continue combo index from where match left off
                    int comboIndex = startingComboIndex + i;
                    SpawnScorePopup(bubbleWorldPos, points, comboIndex);
                }
                
                currentDelay = Mathf.Max(currentDelay * destructionDelayMultiplier, destructionDelayLimit);
            }
        }
    }
    
    // Spawns a score popup at the given position showing the points earned.
    // ComboIndex controls text size scaling - higher = bigger text.
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
            // Called standalone, so no combo continuation
            StartCoroutine(DestroyFloatingBubblesCoroutine(destructionDelay, 0, 0));
        }
        
        return floating.Count;
    }
    
    #endregion

    #region Floating Bubbles
    
    // Find all bubbles connected to the top row
    public HashSet<Vector2Int> FindConnectedToTop()
    {
        var connected = new HashSet<Vector2Int>();
        var gridData = grid.GetGridData();
        if (gridData.Count == 0) return connected;
        
        grid.ResetVisited();
        var queue = new Queue<Vector2Int>();
        
        // Start from all bubbles in row 0
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
    
    // Get list of floating bubbles (not connected to top)
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
    
    // Check if position is adjacent to any connected bubble
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
            grid.Log("All bubbles cleared - Victory!");
            GameManager.Instance.Victory();
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