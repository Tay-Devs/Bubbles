using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class ResultsUI : MonoBehaviour
{
    [Header("Text References")]
    public TMP_Text resultText;
    public TMP_Text levelNameText;
    public TMP_Text scoreText;
    
    [Header("Result Text")]
    public string passText = "Pass";
    public string failText = "Fail";
    
    [Header("Star References")]
    public Image[] starImages;
    
    [Header("Star Sprites")]
    public Sprite starEarnedSprite;
    
    [Header("Unearned Star Theme Sprites")]
    [SerializeField] private Sprite unearnedStarDaySprite;
    [SerializeField] private Sprite unearnedStarNightSprite;
    
    [Header("Data References")]
    public GameSession gameSession;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private RectTransform rt;
    private int currentStarsEarned;
    private ThemeMode currentTheme;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
    }
    
    void Start()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged += OnThemeChanged;
            
            if (ThemeManager.Instance != null)
            {
                currentTheme = ThemeManager.Instance.CurrentTheme;
            }
        }
    }
    
    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ThemeManager.OnThemeChanged -= OnThemeChanged;
        }
    }
    
    // Called when theme changes. Updates unearned star sprites.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        currentTheme = newTheme;
        UpdateStars(currentStarsEarned);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ResultsUI] Theme changed to {newTheme}, updated stars");
        }
    }
    
    // Returns the appropriate unearned star sprite for the given theme.
    private Sprite GetUnearnedSpriteForTheme(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? unearnedStarDaySprite : unearnedStarNightSprite;
    }
    
    void OnEnable()
    {
        if (rt == null)
        {
            rt = GetComponent<RectTransform>();
        }
       
        rt.anchoredPosition = Vector2.zero;
        
        if (Application.isPlaying)
        {
            // Get current theme
            if (ThemeManager.Instance != null)
            {
                currentTheme = ThemeManager.Instance.CurrentTheme;
            }
            
            UpdateDisplay();
        }
    }
    
    // Fetches level data from GameSession and updates all UI elements.
    // Called automatically when the results panel is enabled.
    private void UpdateDisplay()
    {
        if (gameSession == null)
        {
            Debug.LogError("[ResultsUI] GameSession reference is missing!");
            return;
        }
        
        int levelNumber = 0;
        int score = gameSession.finalScore;
        int starsEarned = gameSession.starsEarned;
        bool won = gameSession.levelWon;
        
        if (gameSession.selectedLevel != null)
        {
            levelNumber = gameSession.selectedLevel.levelNumber;
        }
        else if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
        {
            levelNumber = LevelLoader.Instance.CurrentLevel.levelNumber;
        }
        
        UpdateResultText(won);
        UpdateLevelName(levelNumber);
        UpdateScore(score);
        UpdateStars(starsEarned);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ResultsUI] Displaying - Result: {(won ? "Pass" : "Fail")}, Level: {levelNumber}, Score: {score}, Stars: {starsEarned}");
        }
    }
    
    // Sets the result text to Pass or Fail based on win condition.
    private void UpdateResultText(bool won)
    {
        if (resultText == null) return;
        
        resultText.text = won ? passText : failText;
    }
    
    // Sets the level name text using "Level" + number format.
    private void UpdateLevelName(int levelNumber)
    {
        if (levelNameText == null) return;
        
        levelNameText.text = "Level " + levelNumber;
    }
    
    // Sets the score text with label and value on separate lines.
    private void UpdateScore(int score)
    {
        if (scoreText == null) return;
        
        scoreText.text = "Score:\n" + score;
    }
    
    // Updates star images based on how many stars were earned.
    // Earned stars show gold sprite, unearned use day/night themed sprite.
    private void UpdateStars(int starsEarned)
    {
        currentStarsEarned = starsEarned;
        
        if (starImages == null) return;
        
        Sprite unearnedSprite = GetUnearnedSpriteForTheme(currentTheme);
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < starsEarned;
            starImages[i].sprite = isEarned ? starEarnedSprite : unearnedSprite;
        }
    }
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UpdatePreview();
        }
    }
    
    // Updates the star preview in editor based on ThemeManager's current theme.
    // Allows seeing theme changes without entering play mode.
    public void UpdatePreview()
    {
        if (starImages == null) return;
        
        ThemeManager manager = FindObjectOfType<ThemeManager>();
        ThemeMode previewTheme = manager != null ? manager.CurrentTheme : ThemeMode.Day;
        
        Sprite unearnedSprite = previewTheme == ThemeMode.Day ? unearnedStarDaySprite : unearnedStarNightSprite;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            
            bool isEarned = i < currentStarsEarned;
            if (!isEarned && unearnedSprite != null)
            {
                starImages[i].sprite = unearnedSprite;
            }
        }
    }
}