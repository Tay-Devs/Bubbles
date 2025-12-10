using UnityEngine;

public class UICeilingCollider : MonoBehaviour
{
    [Header("References")]
    public RectTransform uiElement; // The UI element to use as ceiling
    public Camera gameCamera;
    
    [Header("Collider Settings")]
    public float colliderHeight = 0.1f;
    public bool matchCameraWidth = true;
    
    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);
    
    private BoxCollider2D boxCollider;
    private Canvas canvas;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }
    
    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        if (uiElement != null)
            canvas = uiElement.GetComponentInParent<Canvas>();
            
        UpdateCollider();
    }
    
    void LateUpdate()
    {
        UpdateCollider();
    }

    // Converts the UI element's bottom edge to world position and updates collider.
    // Collider is centered on the bottom edge of the UI element.
    public void UpdateCollider()
    {
        if (uiElement == null || gameCamera == null) return;
        
        // Get the bottom edge of the UI element in world space
        Vector3 worldPos = GetUIBottomEdgeWorldPosition();
        
        // Position this object at the UI bottom edge
        transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        
        // Update collider size
        float width = GetColliderWidth();
        boxCollider.size = new Vector2(width, colliderHeight);
        boxCollider.offset = Vector2.zero;
    }
    
    // Gets the world position of the UI element's bottom edge center.
    // Handles both overlay and camera-space canvases.
    private Vector3 GetUIBottomEdgeWorldPosition()
    {
        Vector3[] corners = new Vector3[4];
        uiElement.GetWorldCorners(corners);
        // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
        
        // Get bottom center
        Vector3 bottomLeft = corners[0];
        Vector3 bottomRight = corners[3];
        Vector3 bottomCenter = (bottomLeft + bottomRight) / 2f;
        
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Convert screen position to world position
            return gameCamera.ScreenToWorldPoint(new Vector3(bottomCenter.x, bottomCenter.y, gameCamera.nearClipPlane));
        }
        else
        {
            return bottomCenter;
        }
    }
    
    // Returns collider width based on settings.
    // Either matches full camera width or the UI element width.
    private float GetColliderWidth()
    {
        if (matchCameraWidth && gameCamera != null)
        {
            return gameCamera.orthographicSize * 2f * gameCamera.aspect;
        }
        
        // Use UI element width converted to world units
        Vector3[] corners = new Vector3[4];
        uiElement.GetWorldCorners(corners);
        
        Vector3 bottomLeft = corners[0];
        Vector3 bottomRight = corners[3];
        
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 worldLeft = gameCamera.ScreenToWorldPoint(new Vector3(bottomLeft.x, bottomLeft.y, 0));
            Vector3 worldRight = gameCamera.ScreenToWorldPoint(new Vector3(bottomRight.x, bottomRight.y, 0));
            return Vector3.Distance(worldLeft, worldRight);
        }
        
        return Vector3.Distance(bottomLeft, bottomRight);
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        
        Gizmos.color = gizmoColor;
        Vector3 center = transform.position + (Vector3)col.offset;
        Vector3 size = new Vector3(col.size.x, col.size.y, 0.1f);
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}