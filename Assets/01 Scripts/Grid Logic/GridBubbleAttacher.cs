using UnityEngine;

public class GridBubbleAttacher : MonoBehaviour
{
    private HexGrid grid;

    void Awake()
    {
        grid = GetComponent<HexGrid>();
    }

    // Attach a bubble to the grid and check for matches
    public Vector2Int AttachBubble(Bubble bubble, Vector3 worldPos)
    {
        if (bubble.isAttached)
        {
            grid.LogWarning($"Bubble {bubble.type} already attached");
            return new Vector2Int(-1, -1);
        }
        
        Vector2Int pos = FindAttachPosition(worldPos);
        grid.EnsureRowExists(pos.y);
        grid.EnsureColumnExists(pos.x, pos.y);
        
        bubble.transform.SetParent(grid.transform);
        bubble.transform.position = grid.GridToWorld(pos);
        bubble.isAttached = true;
        grid.SetBubbleAt(pos, bubble);
        
        grid.Log($"Attached {bubble.type} at ({pos.x}, {pos.y})");
        
        // Check lose condition immediately after attachment
        if (grid.MatchSystem.CheckLoseCondition())
        {
            return new Vector2Int(-1, -1);
        }
        
        return pos;
    }
    
    // Find best empty position for attachment (constrained to grid width)
    public Vector2Int FindAttachPosition(Vector3 worldPos)
    {
        Vector2Int target = grid.WorldToGrid(worldPos);
        
        // Clamp to valid grid bounds
        target.x = Mathf.Clamp(target.x, 0, grid.width - 1);
        
        if (grid.IsEmpty(target)) return target;
        
        float bestDist = float.MaxValue;
        Vector2Int bestPos = target;
        
        foreach (var neighbor in grid.GetNeighbors(target))
        {
            // Skip positions outside valid grid bounds
            if (neighbor.x < 0 || neighbor.x >= grid.width) continue;
            if (neighbor.y < 0) continue;
            
            if (grid.IsEmpty(neighbor))
            {
                float dist = Vector3.Distance(worldPos, grid.GridToWorld(neighbor));
                if (dist < bestDist) 
                { 
                    bestDist = dist; 
                    bestPos = neighbor; 
                }
            }
        }
        return bestPos;
    }
    
    // Attach and check matches in one call
    public void AttachAndCheckMatches(Bubble bubble, Vector3 worldPos)
    {
        Vector2Int pos = AttachBubble(bubble, worldPos);
        if (pos.x != -1)
        {
            grid.MatchSystem.CheckAndDestroyMatches(pos);
        }
    }
}