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
    public bool showDebug = true;
    
    // Event fired after position is applied
    public event Action onPositionApplied;
    
    void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        // Small delay to ensure camera is set up
        Invoke(nameof(ApplyAnchor), 0.05f);
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
        
        // Get the center of the RectTransform in screen coordinates
        Vector3[] corners = new Vector3[4];
        uiAnchor.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay: corners are already screen coordinates
            if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: Overlay mode, center = {center}");
            return new Vector2(center.x, center.y);
        }
        else
        {
            // Camera/World: need to convert world position to screen
            Camera renderCam = canvas.worldCamera;
            if (renderCam == null)
            {
                if (showDebug) Debug.LogWarning($"[UIWorldAnchor] {name}: Canvas has no render camera, using main camera");
                renderCam = gameCamera;
            }
            
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCam, center);
            if (showDebug) Debug.Log($"[UIWorldAnchor] {name}: Camera mode, center = {center}, screenPoint = {screenPoint}");
            return screenPoint;
        }
    }
    
    // Call this from GridCameraFitter after camera is resized
    public void Refresh()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        ApplyAnchor();
    }
}