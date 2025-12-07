using UnityEngine;

// Runs before HexGrid (-100) to set camera size before grid generates
[DefaultExecutionOrder(-100)]
public class GridCameraFitter : MonoBehaviour
{
    [Header("References")]
    public HexGrid grid;
    public Camera targetCamera;
    public RectTransform topUIBoundary; // UI panel marking where grid should start below
    
    [Header("Fit Settings")]
    public float bubbleRadius = 0.5f;
    public float horizontalPadding = 0f;
    
    [Header("Vertical Settings")]
    public float topOffset = 0.5f; // Distance below UI boundary (or top of screen if no UI)
    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
            
        if (grid == null)
            grid = FindFirstObjectByType<HexGrid>();
            
        FitAndPositionGrid();
    }
    
    public void FitAndPositionGrid()
    {
        if (targetCamera == null || grid == null) return;
        
        // Step 1: Get UI boundary position as a screen ratio BEFORE changing camera
        float uiBoundaryScreenRatio = GetUIBoundaryScreenRatio();
        
        // Step 2: Calculate and set camera size to fit grid width
        float gridWorldWidth = (grid.width - 1) + 0.5f + (bubbleRadius * 2) + (horizontalPadding * 2);
        float screenAspect = (float)Screen.width / Screen.height;
        float requiredOrthoSize = gridWorldWidth / (2f * screenAspect);
        
        targetCamera.orthographicSize = requiredOrthoSize;
        
        // Step 3: Now convert the screen ratio back to world position with new camera size
        float camY = targetCamera.transform.position.y;
        float camTop = camY + requiredOrthoSize;
        float camBottom = camY - requiredOrthoSize;
        float camHeight = requiredOrthoSize * 2f;
        
        // UI boundary in world space (ratio 1 = top, ratio 0 = bottom)
        float gridTopY;
        if (uiBoundaryScreenRatio > 0)
        {
            // There's a UI boundary - position grid below it
            gridTopY = camBottom + (camHeight * uiBoundaryScreenRatio);
        }
        else
        {
            // No UI boundary - use top of screen
            gridTopY = camTop;
        }
        
        // Step 4: Position the grid
        // Grid origin is where bubble CENTERS start, so subtract bubble radius
        // to ensure the top edge of bubbles is below the UI boundary
        float camLeft = targetCamera.transform.position.x - (requiredOrthoSize * screenAspect);
        float gridOriginX = camLeft + bubbleRadius + horizontalPadding;
        float gridOriginY = gridTopY - topOffset - bubbleRadius;
        
        grid.transform.position = new Vector3(
            gridOriginX,
            gridOriginY,
            grid.transform.position.z
        );
        
        // Force HexGrid to not reposition itself
        grid.autoPosition = false;
        
        Debug.Log($"Camera ortho: {requiredOrthoSize}, camY: {camY}, camTop: {camTop}, camBottom: {camBottom}");
        Debug.Log($"UI ratio: {uiBoundaryScreenRatio}, gridTopY: {gridTopY}, topOffset: {topOffset}, bubbleRadius: {bubbleRadius}");
        Debug.Log($"Final gridOriginY: {gridOriginY}, Grid at: {grid.transform.position}");
    }
    
    // Get the bottom edge of the UI boundary as a ratio of screen height (0 = bottom, 1 = top)
    private float GetUIBoundaryScreenRatio()
    {
        if (topUIBoundary == null) return -1f;
        
        Canvas canvas = topUIBoundary.GetComponentInParent<Canvas>();
        if (canvas == null) return -1f;
        
        Vector3[] corners = new Vector3[4];
        topUIBoundary.GetWorldCorners(corners);
        
        // corners[0] = bottom-left, we want its Y position
        Vector3 bottomLeft = corners[0];
        
        float screenY;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For overlay canvas, GetWorldCorners already returns screen coordinates
            screenY = bottomLeft.y;
        }
        else
        {
            // For camera or world space canvas, convert to screen coordinates
            Camera canvasCam = canvas.worldCamera != null ? canvas.worldCamera : targetCamera;
            screenY = canvasCam.WorldToScreenPoint(bottomLeft).y;
        }
        
        Debug.Log($"UI boundary screenY: {screenY}, Screen.height: {Screen.height}, ratio: {screenY / Screen.height}");
        
        return screenY / Screen.height;
    }
    
    public void RefreshFit()
    {
        FitAndPositionGrid();
    }
}