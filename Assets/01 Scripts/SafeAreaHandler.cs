using UnityEngine;

public class SafeAreaHandler : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogs = false;
    
    private RectTransform rectTransform;
    private Rect lastSafeArea;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    // Converts the screen's safe area to normalized anchor values and applies them.
    // Crucially resets offsetMin/offsetMax to zero so the panel exactly matches the safe zone.
    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        
        // This is the key fix - reset offsets to zero
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        if (enableDebugLogs)
        {
            Debug.Log($"[SafeAreaHandler] Safe area: {safeArea}, Anchors: {anchorMin} to {anchorMax}");
        }
    }
}