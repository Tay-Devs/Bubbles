using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNumberText;
    public Button levelButton;
    public Image nodeBackground;
    public GameObject[] starIcons;
    
    [Header("Lock State")]
    public GameObject lockIcon;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    private int levelNumber;
    private bool isUnlocked;
    
    public int LevelNumber => levelNumber;
    public Vector3 Position => transform.position;
    
    void Awake()
    {
        // Auto-find button on this GameObject
        if (levelButton == null)
        {
            levelButton = GetComponent<Button>();
        }
        
        // Disable raycast on child elements so they don't block the button
        DisableChildRaycasts();
    }
    
    // Disables Raycast Target on all child graphics to prevent blocking button clicks.
    private void DisableChildRaycasts()
    {
        // Disable on all child Images
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            // Skip if this is the button's target graphic
            if (levelButton != null && img == levelButton.targetGraphic)
            {
                continue;
            }
            img.raycastTarget = false;
        }
        
        // Disable on all child TMP texts
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            txt.raycastTarget = false;
        }
    }
    
    public void Setup(int level, bool unlocked, int starsEarned)
    {
        levelNumber = level;
        isUnlocked = unlocked;
        
        if (levelNumberText != null)
        {
            levelNumberText.text = level.ToString();
        }
        
        if (levelButton != null)
        {
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(OnNodeClicked);
            levelButton.interactable = unlocked;
        }
        else
        {
            Debug.LogWarning($"[LevelNode] Level {level} has no Button component!");
        }
        
        UpdateLockVisual(unlocked);
        UpdateStars(starsEarned);
        UpdateNodeColor(unlocked, starsEarned > 0);
    }
    
    private void UpdateLockVisual(bool unlocked)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(!unlocked);
        }
        
        if (levelNumberText != null)
        {
            levelNumberText.gameObject.SetActive(unlocked);
        }
    }
    
    private void UpdateStars(int starsEarned)
    {
        if (starIcons == null) return;
        
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
            {
                starIcons[i].SetActive(i < starsEarned);
            }
        }
    }
    
    private void UpdateNodeColor(bool unlocked, bool completed)
    {
        if (nodeBackground == null) return;
        
        if (!unlocked)
        {
            nodeBackground.color = lockedColor;
        }
        else if (completed)
        {
            nodeBackground.color = completedColor;
        }
        else
        {
            nodeBackground.color = unlockedColor;
        }
    }
    
    private void OnNodeClicked()
    {
        Debug.Log($"[LevelNode] Button clicked for level {levelNumber}, unlocked: {isUnlocked}");
        
        if (!isUnlocked)
        {
            Debug.Log("[LevelNode] Level is locked, ignoring click");
            return;
        }
        
        if (LevelMapController.Instance == null)
        {
            Debug.LogError("[LevelNode] LevelMapController.Instance is null!");
            return;
        }
        
        LevelMapController.Instance.LoadLevel(levelNumber);
    }
}