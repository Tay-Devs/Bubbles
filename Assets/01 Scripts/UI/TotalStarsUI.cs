using UnityEngine;
using TMPro;

public class TotalStarsUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text starsText;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    void Start()
    {
        UpdateDisplay();
    }
    
    void OnEnable()
    {
        UpdateDisplay();
    }
    
    // Fetches total stars from LevelDataManager and updates the text.
    // Called on Start and OnEnable to ensure display is current.
    public void UpdateDisplay()
    {
        if (starsText == null) return;
        
        int totalStars = 0;
        
        if (LevelDataManager.Instance != null)
        {
            totalStars = LevelDataManager.Instance.TotalStars;
        }
        
        starsText.text = totalStars.ToString();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TotalStarsUI] Displaying {totalStars} total stars");
        }
    }
}