using System;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    
    [Header("UI")]
    public TMP_Text scoreText;
    public string scoreFormat = "{0:N0}"; // Formats with commas (1,000)
    
    [Header("Match Scoring (Per-Bubble Scaling)")]
    public int basePointsPerBubble = 10;
    public int minMatchCount = 3; // Bubbles up to this count get base points
    public float scalingIncrement = 0.5f; // Each bubble beyond min adds this to multiplier
    
    [Header("Floating Scoring (Multiplicative)")]
    public int floatingBasePoints = 15;
    public float floatingMultiplier = 1.5f; // Each successive bubble multiplies by this
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public Action<int, int> onScoreChanged; // (newScore, pointsAdded)
    public Action<int> onMatchScore; // points from a match
    public Action<int> onFloatingScore; // points from floating bubbles
    
    private int currentScore;
    public int CurrentScore => currentScore;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        UpdateScoreDisplay();
    }

    // Calculates and adds score for matched bubbles using per-bubble scaling.
    // Bubbles 1 to minMatch get base points, each additional bubble gets an increasing multiplier.
    // Returns total points awarded for this match.
    public int AddMatchScore(int bubbleCount)
    {
        if (bubbleCount <= 0) return 0;
        
        int totalPoints = 0;
        
        for (int i = 0; i < bubbleCount; i++)
        {
            float multiplier;
            
            if (i < minMatchCount)
            {
                // First N bubbles get base multiplier
                multiplier = 1f;
            }
            else
            {
                // Each bubble beyond min gets increasing multiplier
                // Bubble 4: 1.5, Bubble 5: 2.0, Bubble 6: 2.5, etc.
                multiplier = 1f + ((i - minMatchCount + 1) * scalingIncrement);
            }
            
            int bubblePoints = Mathf.RoundToInt(basePointsPerBubble * multiplier);
            totalPoints += bubblePoints;
            
            Log($"Bubble {i + 1}: {basePointsPerBubble} x {multiplier:F1} = {bubblePoints}");
        }
        
        AddScore(totalPoints);
        onMatchScore?.Invoke(totalPoints);
        
        Log($"Match total: {bubbleCount} bubbles = {totalPoints} points");
        return totalPoints;
    }

    // Calculates and adds score for floating bubbles using multiplicative scaling.
    // Each successive bubble is worth exponentially more (base * multiplier^index).
    // Returns total points awarded for floating bubbles.
    public int AddFloatingScore(int bubbleCount)
    {
        if (bubbleCount <= 0) return 0;
        
        int totalPoints = 0;
        
        for (int i = 0; i < bubbleCount; i++)
        {
            // Exponential growth: base * multiplier^i
            // Bubble 1: 15, Bubble 2: 22, Bubble 3: 33, etc. (with 1.5x multiplier)
            float multiplier = Mathf.Pow(floatingMultiplier, i);
            int bubblePoints = Mathf.RoundToInt(floatingBasePoints * multiplier);
            totalPoints += bubblePoints;
            
            Log($"Floating {i + 1}: {floatingBasePoints} x {multiplier:F2} = {bubblePoints}");
        }
        
        AddScore(totalPoints);
        onFloatingScore?.Invoke(totalPoints);
        
        Log($"Floating total: {bubbleCount} bubbles = {totalPoints} points");
        return totalPoints;
    }

    // Adds points directly to score and updates display.
    // Use AddMatchScore or AddFloatingScore for gameplay scoring.
    public void AddScore(int points)
    {
        currentScore += points;
        onScoreChanged?.Invoke(currentScore, points);
        UpdateScoreDisplay();
    }

    // Resets score to zero. Call this when starting a new game.
    public void ResetScore()
    {
        currentScore = 0;
        onScoreChanged?.Invoke(currentScore, 0);
        UpdateScoreDisplay();
        Log("Score reset");
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, currentScore);
        }
    }
    
    void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[ScoreManager] {msg}");
    }
}