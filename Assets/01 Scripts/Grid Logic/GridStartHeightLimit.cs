using UnityEngine;

public class GridStartHeightLimit : MonoBehaviour
{
    [Header("Zone Settings")]
    public float zoneWidth = 10f;
    public float zoneHeight = 0.5f;
    
    [Header("Auto Size")]
    public bool matchCameraWidth = true;
    public Camera gameCamera;
    
    [Header("Gizmo Settings")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange
    public Color gizmoOutlineColor = new Color(1f, 0.5f, 0f, 1f);
    
    private UIWorldAnchor worldAnchor;
    
    // Returns the Y position of the top of the limit zone (where grid should stop)
    public float LimitLineY => transform.position.y + zoneHeight / 2f;
    
    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
        
        // Check if we have a UIWorldAnchor - if so, wait for it
        worldAnchor = GetComponent<UIWorldAnchor>();
        if (worldAnchor != null)
        {
            worldAnchor.onPositionApplied += OnPositionApplied;
        }
        else if (matchCameraWidth)
        {
            UpdateWidth();
        }
    }
    
    void OnDestroy()
    {
        if (worldAnchor != null)
        {
            worldAnchor.onPositionApplied -= OnPositionApplied;
        }
    }
    
    void OnPositionApplied()
    {
        if (matchCameraWidth)
        {
            UpdateWidth();
        }
    }
    
    public void UpdateWidth()
    {
        if (gameCamera == null) return;
        
        float cameraWidth = gameCamera.orthographicSize * 2f * gameCamera.aspect;
        zoneWidth = cameraWidth;
        
        //Debug.Log($"[GridStartHeightLimit] Width updated to {zoneWidth}");
    }
    
    // Check if a world Y position is below the limit (should stop generating)
    public bool IsBelowLimit(float yPos)
    {
        return yPos <= LimitLineY;
    }
    
    // Check if a world position is below the limit
    public bool IsBelowLimit(Vector3 worldPos)
    {
        return worldPos.y <= LimitLineY;
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
        
        // Draw limit line at top of zone
        Vector3 lineStart = new Vector3(center.x - zoneWidth / 2f, LimitLineY, 0f);
        Vector3 lineEnd = new Vector3(center.x + zoneWidth / 2f, LimitLineY, 0f);
        Gizmos.DrawLine(lineStart, lineEnd);
    }
}