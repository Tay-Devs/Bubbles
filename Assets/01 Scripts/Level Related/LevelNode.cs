using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNode : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelNumberText;
    public Button levelButton;
    public Image nodeBackground;
    public GameObject[] starIcons; // Assign 3 star GameObjects
    
    [Header("Lock State")]
    public GameObject lockIcon;
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    private int levelNumber;
    private bool isUnlocked;
    
    public int LevelNumber => levelNumber;
    public Vector3 Position => transform.position;
    
    // Sets up the node with level number and current progress.
    // Updates visuals based on unlock state and stars earned.
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
            levelButton.interactable = unlocked;
            levelButton.onClick.RemoveAllListeners();
            levelButton.onClick.AddListener(OnNodeClicked);
        }
        
        UpdateLockVisual(unlocked);
        UpdateStars(starsEarned);
        UpdateNodeColor(unlocked, starsEarned > 0);
    }
    
    // Shows or hides the lock icon based on unlock state.
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
    
    // Updates star icons to show earned stars.
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
    
    // Changes node background color based on state.
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
    
    // Called when the node button is clicked.
    private void OnNodeClicked()
    {
        if (!isUnlocked) return;
        
        Debug.Log($"[LevelNode] Level {levelNumber} clicked");
        LevelMapController.Instance?.LoadLevel(levelNumber);
    }
}