using UnityEngine;
using TMPro;
using DG.Tweening;

public class TotalStarsUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text starsText;
    
    [Header("Animation")]
    [SerializeField] private float punchScale = 0.2f;
    [SerializeField] private float punchDuration = 0.15f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private RectTransform rectTransform;
    
    public TMP_Text StarsText => starsText;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        UpdateDisplay();
    }
    
    // Fetches total stars from LevelDataManager and updates the text.
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
    
    // Sets the display to a specific value (used by StarCollectionUI).
    public void SetDisplayValue(int value)
    {
        if (starsText == null) return;
        starsText.text = value.ToString();
    }
    
    // Increments the displayed value by 1 with a punch animation.
    public void IncrementWithPunch()
    {
        if (starsText == null) return;
        
        // Parse current value and increment
        if (int.TryParse(starsText.text, out int current))
        {
            starsText.text = (current + 1).ToString();
        }
        
        // Punch animation
        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;
            rectTransform.DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.5f);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TotalStarsUI] Incremented to {starsText.text}");
        }
    }
}