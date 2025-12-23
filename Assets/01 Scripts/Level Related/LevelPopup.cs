using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_Text levelNameText;
    public Button playButton;
    public Button closeButton;
    
    [Header("Star References")]
    public Image[] starImages;
    
    [Header("Star Sprites")]
    public Sprite starEarnedSprite;
    public Sprite starUnearnedSprite;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private int selectedLevelNumber;
    private bool isOpen;
    
    public bool IsOpen => isOpen;
    
    void Awake()
    {
        SetupButtons();
        Hide();
    }
    
    // Subscribes button click events to their respective handlers.
    // Play button loads the selected level, close button hides the popup.
    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
    }
    
    // Opens the popup and displays level name and star progress.
    // Stars earned determines how many gold vs grey stars to show.
    public void Show(int levelNumber, int starsEarned)
    {
        selectedLevelNumber = levelNumber;
        
        if (levelNameText != null)
        {
            levelNameText.text = "Level " + levelNumber;
        }
        
        UpdateStars(starsEarned);
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        
        isOpen = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelPopup] Opened popup for level {levelNumber} with {starsEarned} stars");
        }
    }
    
    // Updates star images based on how many stars the player earned.
    // Earned stars show gold sprite, unearned show grey sprite.
    private void UpdateStars(int starsEarned)
    {
        if (starImages == null) return;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < starsEarned;
            starImages[i].sprite = isEarned ? starEarnedSprite : starUnearnedSprite;
        }
    }
    
    // Hides the popup and resets the selected level.
    public void Hide()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        isOpen = false;
        
        if (enableDebugLogs)
        {
            Debug.Log("[LevelPopup] Popup closed");
        }
    }
    
    // Called when play button is clicked. Loads the selected level through LevelMapController.
    private void OnPlayClicked()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelPopup] Play clicked for level {selectedLevelNumber}");
        }
        
        if (LevelMapController.Instance != null)
        {
            LevelMapController.Instance.LoadLevel(selectedLevelNumber);
        }
        else
        {
            Debug.LogError("[LevelPopup] LevelMapController.Instance is null!");
        }
    }
}