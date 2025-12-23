using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNumberText;
    public Button levelButton;
    public Image nodeBackground;
    
    [Header("Star References")]
    public Image[] starImages;
    
    [Header("Star Sprites")]
    public Sprite starEarnedSprite;
    public Sprite starUnearnedSprite;
    
    [Header("Or Use Colors (if no sprites)")]
    public bool useColors = false;
    public Color starEarnedColor = Color.yellow;
    public Color starUnearnedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Header("Lock State")]
    public GameObject lockIcon;
    public GameObject starsContainer;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    [Header("Level Number Display")]
    public Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedTextColor = Color.white;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private int levelNumber;
    private bool isUnlocked;
    
    public int LevelNumber => levelNumber;
    public Vector3 Position => transform.position;
    
    void Awake()
    {
        if (levelButton == null)
        {
            levelButton = GetComponent<Button>();
        }
        
        DisableChildRaycasts();
    }
    
    private void DisableChildRaycasts()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (levelButton != null && img == levelButton.targetGraphic)
            {
                continue;
            }
            img.raycastTarget = false;
        }
        
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
        
        UpdateLockVisual(unlocked);
        UpdateLevelNumberColor(unlocked);
        UpdateStars(starsEarned, unlocked);
        UpdateNodeColor(unlocked, starsEarned > 0);
    }
    
    private void UpdateLockVisual(bool unlocked)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(!unlocked);
        }
    }
    
    // Updates level number text color based on lock state.
    private void UpdateLevelNumberColor(bool unlocked)
    {
        if (levelNumberText == null) return;
        
        levelNumberText.color = unlocked ? unlockedTextColor : lockedTextColor;
    }
    
    private void UpdateStars(int starsEarned, bool unlocked)
    {
        if (starsContainer != null)
        {
            starsContainer.SetActive(unlocked);
        }
        
        if (starImages == null) return;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            starImages[i].gameObject.SetActive(true);
            
            bool isEarned = i < starsEarned;
            
            if (useColors)
            {
                starImages[i].color = isEarned ? starEarnedColor : starUnearnedColor;
            }
            else
            {
                starImages[i].sprite = isEarned ? starEarnedSprite : starUnearnedSprite;
                starImages[i].color = Color.white;
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
    
    // Called when the node is clicked. Opens the level popup instead of loading directly.
    // Only responds if the level is unlocked.
    private void OnNodeClicked()
    {
        if (!isUnlocked)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[LevelNode] Level is locked, ignoring click");
            }
            return;
        }
        
        if (LevelMapController.Instance == null)
        {
            Debug.LogError("[LevelNode] LevelMapController.Instance is null!");
            return;
        }
        
        LevelMapController.Instance.OpenLevelPopup(levelNumber);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelNode] Opening popup for level {levelNumber}");
        }
    }
}