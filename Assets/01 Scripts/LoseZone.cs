using UnityEngine;

public class LoseZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public float zoneWidth = 10f;
    public float zoneHeight = 0.5f;
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    public Color gizmoOutlineColor = Color.red;
    
    // Returns the Y position of the top of the lose zone
    public float LoseLineY => transform.position.y + zoneHeight / 2f;
    
    // Check if a world position is in the lose zone
    public bool IsInLoseZone(Vector3 worldPos)
    {
        return worldPos.y <= LoseLineY;
    }
    
    // Check if a Y position is in the lose zone
    public bool IsInLoseZone(float yPos)
    {
        return yPos <= LoseLineY;
    }
    
    void OnDrawGizmos()
    {
        DrawZoneGizmos();
    }
    
    void OnDrawGizmosSelected()
    {
        DrawZoneGizmos();
    }
    
    void DrawZoneGizmos()
    {
        Vector3 center = transform.position;
        Vector3 size = new Vector3(zoneWidth, zoneHeight, 0.1f);
        
        // Draw filled zone
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, size);
        
        // Draw outline
        Gizmos.color = gizmoOutlineColor;
        Gizmos.DrawWireCube(center, size);
        
        // Draw lose line at top of zone
        Vector3 lineStart = new Vector3(center.x - zoneWidth / 2f, LoseLineY, 0f);
        Vector3 lineEnd = new Vector3(center.x + zoneWidth / 2f, LoseLineY, 0f);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}