using UnityEngine;

public class LoseZoneWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid grid;
    [SerializeField] private GridStartHeightLimit heightLimit;
    [SerializeField] private GameObject warningUI;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private bool isWarningActive = false;
    
    void Start()
    {
        if (grid == null)
            grid = FindFirstObjectByType<HexGrid>();
        
        if (heightLimit == null)
            heightLimit = FindFirstObjectByType<GridStartHeightLimit>();
        
        // Subscribe to events that change bubble positions
        if (grid != null)
        {
            grid.onColorsChanged += CheckBubblePositions;
            
            if (grid.RowSystem != null)
            {
                grid.RowSystem.onRowSpawned += CheckBubblePositions;
            }
        }
        
        PlayerController.onBubbleConnected += CheckBubblePositions;
        
        // Set initial state
        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (grid != null)
        {
            grid.onColorsChanged -= CheckBubblePositions;
            
            if (grid.RowSystem != null)
            {
                grid.RowSystem.onRowSpawned -= CheckBubblePositions;
            }
        }
        
        PlayerController.onBubbleConnected -= CheckBubblePositions;
    }
    
    // Checks if any bubble is below the height limit line.
    // Shows warning UI if any bubble is in danger zone, hides if none are.
    private void CheckBubblePositions()
    {
        if (grid == null || heightLimit == null || warningUI == null) return;
        
        bool anyBubbleBelowLimit = false;
        
        foreach (var row in grid.GetGridData())
        {
            foreach (var bubble in row)
            {
                if (bubble != null && heightLimit.IsBelowLimit(bubble.transform.position))
                {
                    anyBubbleBelowLimit = true;
                    break;
                }
            }
            
            if (anyBubbleBelowLimit) break;
        }
        
        // Only update if state changed
        if (anyBubbleBelowLimit != isWarningActive)
        {
            isWarningActive = anyBubbleBelowLimit;
            warningUI.SetActive(isWarningActive);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[LoseZoneWarning] Warning {(isWarningActive ? "SHOWN" : "HIDDEN")}");
            }
        }
    }
}