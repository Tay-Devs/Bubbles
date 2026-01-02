using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Bubble Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber = 1;
    [TextArea]
    public string levelName = "Level 1";
    
    [Header("Level Icon")]
    public ThemeSprite levelIcon; // Theme sprite
    
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 10;
    
    [Header("Bubble Settings")]
    public List<BubbleType> availableColors = new List<BubbleType>();
    
    [Header("Win Condition")]
    public WinConditionType winCondition = WinConditionType.ClearAllBubbles;
    
    [Header("Clear All Settings")]
    [Tooltip("Time in seconds before losing each star. Total time = interval * 3")]
    public float starTimeInterval = 30f;
    
    [Header("Score Settings")]
    public int targetScore = 1000;
    
    [Header("Survival Settings")]
    public float survivalStartingInterval = 10f;
    public float survivalIntervalDeduction = 0.5f;
    public float survivalMinInterval = 2f;
    public int oneStarRows = 5;
    public int twoStarRows = 10;
    public int threeStarRows = 15;
    
    // Calculated time thresholds based on interval
    public float ThreeStarTime => starTimeInterval;
    public float TwoStarTime => starTimeInterval * 2f;
    public float OneStarTime => starTimeInterval * 3f;
    
    public Sprite GetIcon(ThemeMode theme)
    {
        if (levelIcon == null)
        {
            return levelIcon.GetSprite(theme);
        }

        return null;
    }
    
    public Sprite GetCurrentIcon()
    {
        ThemeMode theme = ThemeMode.Day;
        if (ThemeManager.Instance != null)
        {
            theme = ThemeManager.Instance.CurrentTheme;
        }
        
        return levelIcon.GetSprite(theme);
    }
    
    public int CalculateStars(bool won, float completionTime = 0f, int score = 0, int rowsSurvived = 0, bool clearedAllBubbles = false)
    {
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                if (!won) return 0;
                return CalculateClearAllStars(completionTime);
            case WinConditionType.ReachTargetScore:
                if (!won && score < targetScore) return 0;
                return CalculateScoreStars(score);
            case WinConditionType.Survival:
                return CalculateSurvivalStars(rowsSurvived, clearedAllBubbles);
            default:
                return 0;
        }
    }
    
    // Calculates stars based on interval. Lose 1 star per interval elapsed.
    private int CalculateClearAllStars(float completionTime)
    {
        // Count how many intervals have passed (how many stars lost)
        int starsLost = Mathf.FloorToInt(completionTime / starTimeInterval);
        int starsEarned = 3 - starsLost;
        return Mathf.Clamp(starsEarned, 0, 3);
    }
    
    private int CalculateScoreStars(int score)
    {
        if (score >= targetScore) return 3;
        if (score >= Mathf.CeilToInt(targetScore * 2f / 3f)) return 2;
        if (score >= Mathf.CeilToInt(targetScore / 3f)) return 1;
        return 0;
    }
    
    private int CalculateSurvivalStars(int rowsSurvived, bool clearedAllBubbles)
    {
        if (clearedAllBubbles) return 3;
        if (rowsSurvived >= threeStarRows) return 3;
        if (rowsSurvived >= twoStarRows) return 2;
        if (rowsSurvived >= oneStarRows) return 1;
        return 0;
    }
    
    public int GetScoreForStars(int stars)
    {
        switch (stars)
        {
            case 1: return Mathf.CeilToInt(targetScore / 3f);
            case 2: return Mathf.CeilToInt(targetScore * 2f / 3f);
            case 3: return targetScore;
            default: return 0;
        }
    }
    
    // Returns time threshold for given star count using interval.
    public float GetTimeForStars(int stars)
    {
        switch (stars)
        {
            case 1: return OneStarTime;
            case 2: return TwoStarTime;
            case 3: return ThreeStarTime;
            default: return float.MaxValue;
        }
    }
    
    public int GetRowsForStars(int stars)
    {
        switch (stars)
        {
            case 1: return oneStarRows;
            case 2: return twoStarRows;
            case 3: return threeStarRows;
            default: return 0;
        }
    }
}