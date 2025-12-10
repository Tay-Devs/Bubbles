using UnityEngine;
using System;

public enum WinConditionType
{
    ClearAllBubbles,    // Classic mode - pop all bubbles to win
    ReachScore,         // Score attack - reach target score to win
    SurvivalClear       // Survival - rows spawn on timer, clear all to win
}

[CreateAssetMenu(fileName = "New Level", menuName = "Bubble Shooter/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    [TextArea(2, 4)]
    public string levelDescription = "";
    public Sprite levelIcon;
    
    [Header("Win Condition")]
    public WinConditionType winCondition = WinConditionType.ClearAllBubbles;
    
    // === ClearAllBubbles Settings ===
    // (No extra settings needed for basic win)
    
    // === ReachScore Settings ===
    [Tooltip("Target score to win. Only used when winCondition is ReachScore.")]
    public int targetScore = 1000;
    
    // === SurvivalClear Settings ===
    [Tooltip("Seconds between new row spawns.")]
    public float rowSpawnInterval = 10f;
    [Tooltip("If true, spawn interval decreases over time.")]
    public bool accelerateOverTime = false;
    [Tooltip("Minimum spawn interval when accelerating.")]
    public float minSpawnInterval = 3f;
    [Tooltip("How much faster each spawn gets (multiplier).")]
    public float accelerationRate = 0.95f;
    
    // === Star Requirements ===
    
    // ClearAllBubbles & SurvivalClear: Time-based stars (minutes and seconds)
    [Header("Star Requirements - Time Based")]
    public int star1Minutes = 3;
    public int star1Seconds = 0;
    public int star2Minutes = 2;
    public int star2Seconds = 0;
    public int star3Minutes = 1;
    public int star3Seconds = 0;
    
    // ReachScore: Bubble count stars
    [Header("Star Requirements - Bubble Count")]
    public int star1MaxBubbles = 50;
    public int star2MaxBubbles = 35;
    public int star3MaxBubbles = 20;
    
    // === Grid Settings ===
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int startingRows = 6;
    
    [Header("Shot Settings")]
    [Tooltip("Shots before a new row spawns (0 = disabled).")]
    public int shotsBeforeNewRow = 5;
    
    [Header("Bubble Colors")]
    [Tooltip("Which bubble types can appear in this level. Leave empty for all types.")]
    public BubbleType[] allowedBubbleTypes;
    
    [Header("Difficulty")]
    [Range(1, 10)]
    public int difficultyRating = 1;
    
    // Helper to get star time requirement in seconds
    public float GetStarTimeRequirement(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return star1Minutes * 60f + star1Seconds;
            case 2: return star2Minutes * 60f + star2Seconds;
            case 3: return star3Minutes * 60f + star3Seconds;
            default: return 0f;
        }
    }
    
    // Helper to get star bubble requirement
    public int GetStarBubbleRequirement(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return star1MaxBubbles;
            case 2: return star2MaxBubbles;
            case 3: return star3MaxBubbles;
            default: return 0;
        }
    }
    
    // Calculate stars earned based on performance
    // For time-based: pass elapsed time in seconds
    // For bubble-based: pass bubbles used
    public int CalculateStars(float timeOrBubbles)
    {
        if (winCondition == WinConditionType.ReachScore)
        {
            // Bubble count - lower is better
            int bubblesUsed = Mathf.RoundToInt(timeOrBubbles);
            if (bubblesUsed <= star3MaxBubbles) return 3;
            if (bubblesUsed <= star2MaxBubbles) return 2;
            if (bubblesUsed <= star1MaxBubbles) return 1;
            return 0;
        }
        else
        {
            // Time-based - lower is better
            float time = timeOrBubbles;
            if (time <= GetStarTimeRequirement(3)) return 3;
            if (time <= GetStarTimeRequirement(2)) return 2;
            if (time <= GetStarTimeRequirement(1)) return 1;
            return 0;
        }
    }
    
    // Helper to check if a bubble type is allowed in this level
    public bool IsBubbleTypeAllowed(BubbleType type)
    {
        if (allowedBubbleTypes == null || allowedBubbleTypes.Length == 0)
            return true;
            
        foreach (var allowed in allowedBubbleTypes)
        {
            if (allowed == type) return true;
        }
        return false;
    }
    
    // Get allowed types as array
    public BubbleType[] GetAllowedTypes()
    {
        if (allowedBubbleTypes == null || allowedBubbleTypes.Length == 0)
        {
            return (BubbleType[])Enum.GetValues(typeof(BubbleType));
        }
        return allowedBubbleTypes;
    }
}