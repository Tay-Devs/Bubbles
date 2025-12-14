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
    public float threeStarTime = 90f;
    public float twoStarTime = 150f;
    public float oneStarTime = 300f;
    
    // Score settings
    [Header("Score Settings")]
    public int targetScore = 1000;
    
    // Survival settings
    [Header("Survival Settings")]
    public float survivalStartingInterval = 10f;
    public float survivalIntervalDeduction = 0.5f;
    public float survivalMinInterval = 2f;
    public int oneStarRows = 5;
    public int twoStarRows = 10;
    public int threeStarRows = 15;
    
    // Calculates stars earned based on win condition and performance.
    // For ClearAllBubbles: must win to get stars.
    // For ReachTargetScore: must reach target to get stars.
    // For Survival: can earn stars even on loss based on rows survived.
    public int CalculateStars(bool won, float completionTime = 0f, int score = 0, int rowsSurvived = 0, bool clearedAllBubbles = false)
    {
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                // Must win to get stars in this mode
                if (!won) return 0;
                return CalculateClearAllStars(completionTime);
                
            case WinConditionType.ReachTargetScore:
                // Must reach target score to get stars
                if (!won && score < targetScore) return 0;
                return CalculateScoreStars(score);
                
            case WinConditionType.Survival:
                // Can earn stars based on rows survived even if lost
                return CalculateSurvivalStars(rowsSurvived, clearedAllBubbles);
                
            default:
                return 0;
        }
    }
    
    // Calculates stars for Clear All mode based on completion time.
    private int CalculateClearAllStars(float completionTime)
    {
        if (completionTime <= threeStarTime) return 3;
        if (completionTime <= twoStarTime) return 2;
        if (completionTime <= oneStarTime) return 1;
        return 0;
    }
    
    // Calculates stars for Score mode based on points earned.
    private int CalculateScoreStars(int score)
    {
        if (score >= targetScore) return 3;
        if (score >= Mathf.CeilToInt(targetScore * 2f / 3f)) return 2;
        if (score >= Mathf.CeilToInt(targetScore / 3f)) return 1;
        return 0;
    }
    
    // Calculates stars for Survival mode based on rows survived.
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