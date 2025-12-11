using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Bubble Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber = 1;
    public string levelName = "Level 1";
    
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 10;
    
    [Header("Bubble Settings")]
    public List<BubbleType> availableColors = new List<BubbleType>();
    
    [Header("Win Condition")]
    public WinConditionType winCondition = WinConditionType.ClearAllBubbles;
    
    // Clear All Bubbles settings (time-based stars)
    [Header("Clear All Settings")]
    public float threeStarTime = 90f;   // 1.5 minutes
    public float twoStarTime = 150f;    // 2.5 minutes
    public float oneStarTime = 300f;    // 5 minutes
    
    // Score settings
    [Header("Score Settings")]
    public int targetScore = 1000;
    // Stars: 1 star = 1/3, 2 stars = 2/3, 3 stars = full score
    
    // Survival settings
    [Header("Survival Settings")]
    public float survivalStartingInterval = 10f;
    public float survivalIntervalDeduction = 0.5f;
    public float survivalMinInterval = 2f;
    public int oneStarRows = 5;
    public int twoStarRows = 10;
    public int threeStarRows = 15;
    
    // Calculates stars earned based on win condition and performance.
    // Returns 0-3 stars depending on how well the player did.
    public int CalculateStars(float completionTime = 0f, int score = 0, int rowsSurvived = 0, bool clearedAllBubbles = false)
    {
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                return CalculateClearAllStars(completionTime);
                
            case WinConditionType.ReachTargetScore:
                return CalculateScoreStars(score);
                
            case WinConditionType.Survival:
                return CalculateSurvivalStars(rowsSurvived, clearedAllBubbles);
                
            default:
                return 0;
        }
    }
    
    // Calculates stars for Clear All mode based on completion time.
    // Faster completion = more stars.
    private int CalculateClearAllStars(float completionTime)
    {
        if (completionTime <= threeStarTime) return 3;
        if (completionTime <= twoStarTime) return 2;
        if (completionTime <= oneStarTime) return 1;
        return 0;
    }
    
    // Calculates stars for Score mode based on points earned.
    // 3 stars = full target, 2 stars = 2/3, 1 star = 1/3.
    private int CalculateScoreStars(int score)
    {
        if (score >= targetScore) return 3;
        if (score >= Mathf.CeilToInt(targetScore * 2f / 3f)) return 2;
        if (score >= Mathf.CeilToInt(targetScore / 3f)) return 1;
        return 0;
    }
    
    // Calculates stars for Survival mode based on rows survived.
    // Clearing all bubbles grants automatic 3 stars.
    private int CalculateSurvivalStars(int rowsSurvived, bool clearedAllBubbles)
    {
        if (clearedAllBubbles) return 3;
        if (rowsSurvived >= threeStarRows) return 3;
        if (rowsSurvived >= twoStarRows) return 2;
        if (rowsSurvived >= oneStarRows) return 1;
        return 0;
    }
    
    // Returns the score needed for a specific star count.
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
    
    // Returns the time limit for a specific star count.
    public float GetTimeForStars(int stars)
    {
        switch (stars)
        {
            case 1: return oneStarTime;
            case 2: return twoStarTime;
            case 3: return threeStarTime;
            default: return float.MaxValue;
        }
    }
    
    // Returns the rows needed for a specific star count.
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