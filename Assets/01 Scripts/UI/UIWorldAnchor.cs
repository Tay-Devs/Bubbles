using UnityEngine;
using System;

[DefaultExecutionOrder(50)]
public class UIWorldAnchor : MonoBehaviour
{
    [Header("References")]
    public RectTransform uiAnchor;
    public Camera gameCamera;
    
    [Header("Options")]
    public bool anchorX = true;
    public bool anchorY = true;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    // Event fired after position is applied
    public event Action onPositionApplied;
    
    void Start()
    {
        showDebug = false;
        if (gameCamera == null)
            gameCamera = Camera.main;
        
        // GridCameraFitter will call Refresh() after camera is set up
    }
    
    public void ApplyAnchor()
    {
        if (uiAnchor == null)
        {
            if (showDebug) Debug.LogError($"[UIWorldAnchor] {name}: uiAnchor not assigned!");
            return;
        }
        
        if (gameCamera == null)
        {
            if (showDebug) Debug.LogError($"[UIWorldAnchor] {name}: gameCamera not assigned!");
            return;
        }
        
        // Step 1: Get the UI element's screen position
        Vector2 screenPos = GetScreenPosition();
        
        // Step 2: Convert screen to viewport (0-1 range)
        Vector2 viewportPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );
        
        // Step 3: Convert viewport to world position
        Vector3 worldPos = gameCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, 0f));
        
        // Step 4: Apply to transform (keeping original Z)
        Vector3 finalPos = transform.position;
        if (anchorX) finalPos.x = worldPos.x;
        if (anchorY) finalPos.y = worldPos.y;
        
        if (showDebug)
        {
            Debug.Log($"[UIWorldAnchor] {name}: screen({screenPos.x:F0}, {screenPos.y:F0}) -> viewport({viewportPos.x:F2}, {viewportPos.y:F2}) -> world({worldPos.x:F2}, {worldPos.y:F2})");
            Debug.Log($"[UIWorldAnchor] {name}: Position changed from {transform.position} to {finalPos}");
        }
        
        transform.position = finalPos;
        
        // Notify listeners that position was applied
        if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: Firing onPositionApplied event, listeners: {onPositionApplied?.GetInvocationList()?.Length ?? 0}");
        onPositionApplied?.Invoke();
    }
    
    private Vector2 GetScreenPosition()
    {
        Canvas canvas = uiAnchor.GetComponentInParent<Canvas>();
        
        if (canvas == null)
        {
            if (showDebug) Debug.LogError($"[UIWorldAnchor] {name}: No canvas found!");
            return Vector2.zero;
        }
        
        // Get root canvas
        Canvas rootCanvas = canvas.rootCanvas;
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        
        // Get the UI element's position in canvas space and convert to screen
        Vector3[] corners = new Vector3[4];
        uiAnchor.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        
        Vector2 screenPoint;
        
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay: world corners ARE screen coordinates
            screenPoint = new Vector2(worldCenter.x, worldCenter.y);
            if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: Overlay mode, screenPoint = {screenPoint}");
        }
        else if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // Screen Space Camera: canvas size matches screen size
            // We can calculate screen position from the local position within canvas
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                new Vector2(Screen.width / 2f, Screen.height / 2f), 
                rootCanvas.worldCamera, 
                out Vector2 canvasCenter);
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                Vector2.zero, 
                rootCanvas.worldCamera, 
                out Vector2 canvasOrigin);
            
            // Get local position of our anchor in canvas space
            Vector3 localAnchorPos = canvasRect.InverseTransformPoint(worldCenter);
            
            // Calculate the scale factor (canvas units to screen pixels)
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            float scaleX = Screen.width / canvasWidth;
            float scaleY = Screen.height / canvasHeight;
            
            // Convert local canvas position to screen position
            // Canvas origin is at center, so we need to offset
            screenPoint = new Vector2(
                (localAnchorPos.x + canvasWidth * 0.5f) * scaleX,
                (localAnchorPos.y + canvasHeight * 0.5f) * scaleY
            );
            
            if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: ScreenSpaceCamera mode, localPos = {localAnchorPos}, canvasSize = ({canvasWidth}, {canvasHeight}), scale = ({scaleX}, {scaleY}), screenPoint = {screenPoint}");
        }
        else
        {
            // World Space: use camera projection
            Camera renderCam = rootCanvas.worldCamera ?? gameCamera;
            Vector3 screenPos3D = renderCam.WorldToScreenPoint(worldCenter);
            screenPoint = new Vector2(screenPos3D.x, screenPos3D.y);
            if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: WorldSpace mode, screenPoint = {screenPoint}");
        }
        
        return screenPoint;
    }
    
    // Call this from GridCameraFitter after camera is resized
    public void Refresh()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        ApplyAnchor();
    }
}