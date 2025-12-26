using UnityEngine;
using TMPro;

public class LevelNumberUI : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text levelText;
    
    [Header("Format")]
    [TextArea(2, 4)]
    public string displayFormat = "Level\n{0}";
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    void Start()
    {
        UpdateDisplay();
    }
    
    // Fetches level number from LevelLoader and updates the text.
    // Uses displayFormat to allow custom formatting (e.g., "Level\n{0}").
    public void UpdateDisplay()
    {
        if (levelText == null) return;
        
        int levelNumber = 1;
        
        if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
        {
            levelNumber = LevelLoader.Instance.CurrentLevel.levelNumber;
        }
        
        levelText.text = string.Format(displayFormat, levelNumber);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[LevelNumberUI] Displaying level {levelNumber}");
        }
    }
}