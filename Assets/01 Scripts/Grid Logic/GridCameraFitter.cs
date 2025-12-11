using UnityEngine;
using UnityEngine.UI;

// Runs before HexGrid (-100) but after LevelLoader (-200)
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
    public float topOffset = 0.5f; // Distance below UI boundary
    
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
        if (targetCamera == null || grid == null)
        {
            Debug.LogError("[GridCameraFitter] Missing camera or grid!");
            return;
        }
        
        Debug.Log($"[GridCameraFitter] Fitting camera for grid width: {grid.width}");
        
        // Step 1: Get UI boundary position as a screen ratio BEFORE changing camera
        float uiBoundaryScreenRatio = GetUIBoundaryScreenRatio();
        
        // Step 2: Calculate and set camera size to fit grid width
        // Grid width: columns 0 to (width-1), plus 0.5 offset for odd rows, plus bubble radius on each side
        float gridWorldWidth = (grid.width - 1) + 0.5f + (bubbleRadius * 2) + (horizontalPadding * 2);
        float screenAspect = (float)Screen.width / Screen.height;
        float requiredOrthoSize = gridWorldWidth / (2f * screenAspect);
        
        targetCamera.orthographicSize = requiredOrthoSize;
        
        // Step 3: Convert screen ratio back to world position with new camera size
        float camY = targetCamera.transform.position.y;
        float camTop = camY + requiredOrthoSize;
        float camBottom = camY - requiredOrthoSize;
        float camHeight = requiredOrthoSize * 2f;
        
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
        float camLeft = targetCamera.transform.position.x - (requiredOrthoSize * screenAspect);
        float gridOriginX = camLeft + bubbleRadius + horizontalPadding;
        float gridOriginY = gridTopY - topOffset - bubbleRadius;
        
        grid.transform.position = new Vector3(
            gridOriginX,
            gridOriginY,
            grid.transform.position.z
        );
        
        // Prevent HexGrid from repositioning itself
        grid.autoPosition = false;
        grid.autoGenerate = false;
        
        // Refresh UI anchors
        RefreshAllUIAnchors();
        
        Debug.Log($"[GridCameraFitter] Camera ortho: {requiredOrthoSize}, Grid at: {grid.transform.position}");
    }
    
    private bool needsAnchorRefresh = false;
    
    void Update()
    {
        if (needsAnchorRefresh)
        {
            needsAnchorRefresh = false;
            DoRefreshAnchors();
        }
    }
    
    private void RefreshAllUIAnchors()
    {
        Canvas.ForceUpdateCanvases();
        needsAnchorRefresh = true;
    }
    
    private void DoRefreshAnchors()
    {
        var anchors = FindObjectsByType<UIWorldAnchor>(FindObjectsSortMode.None);
        Debug.Log($"[GridCameraFitter] Refreshing {anchors.Length} UI world anchors");
        foreach (var anchor in anchors)
        {
            anchor.Refresh();
        }
        
        // Initialize grid after anchors are positioned
        if (grid != null && !grid.autoGenerate)
        {
            Debug.Log("[GridCameraFitter] Initializing grid");
            grid.InitializeGrid();
        }
    }
    
    // Get the bottom edge of the UI boundary as a ratio of screen height
    private float GetUIBoundaryScreenRatio()
    {
        if (topUIBoundary == null) return -1f;
        
        Canvas canvas = topUIBoundary.GetComponentInParent<Canvas>();
        if (canvas == null) return -1f;
        
        Vector3[] corners = new Vector3[4];
        topUIBoundary.GetWorldCorners(corners);
        
        // corners[0] = bottom-left
        Vector3 bottomLeft = corners[0];
        float screenY;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenY = bottomLeft.y;
        }
        else
        {
            Camera canvasCam = canvas.worldCamera != null ? canvas.worldCamera : targetCamera;
            screenY = canvasCam.WorldToScreenPoint(bottomLeft).y;
        }
        
        return screenY / Screen.height;
    }
    
    public void RefreshFit()
    {
        FitAndPositionGrid();
    }
}