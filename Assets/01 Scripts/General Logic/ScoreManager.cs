using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    
    [Header("UI")]
    public TMP_Text scoreText;
    [TextArea(2, 4)]
    public string scoreFormat = "Score\n{0:N0}"; // Supports multiline, {0:N0} formats with commas
    
    [Header("Animation")]
    public float countDuration = 0.3f; // How long the number takes to count up
    public Ease countEase = Ease.OutQuad;
    
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
    public Action<int> onBubbleScore; // points from a single bubble pop
    
    private int currentScore;
    private float displayedScore; // For smooth animation
    private Tweener countTween;
    
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
        displayedScore = currentScore;
        UpdateScoreDisplay();
    }
    
    void OnDestroy()
    {
        countTween?.Kill();
    }

    // Calculates points for a single matched bubble based on its index in the match.
    // Index 0-2 get base points, index 3+ get increasing multipliers.
    // Call this for each bubble as it pops to get individual points.
    public int GetMatchBubblePoints(int bubbleIndex)
    {
        float multiplier;
        
        if (bubbleIndex < minMatchCount)
        {
            multiplier = 1f;
        }
        else
        {
            // Bubble 3 (index 3): 1.5x, Bubble 4: 2.0x, etc.
            multiplier = 1f + ((bubbleIndex - minMatchCount + 1) * scalingIncrement);
        }
        
        int points = Mathf.RoundToInt(basePointsPerBubble * multiplier);
        Log($"Match bubble {bubbleIndex + 1}: {basePointsPerBubble} x {multiplier:F1} = {points}");
        return points;
    }

    // Calculates points for a single floating bubble based on its index.
    // Uses exponential growth: base * multiplier^index.
    // Call this for each floating bubble as it pops.
    public int GetFloatingBubblePoints(int bubbleIndex)
    {
        float multiplier = Mathf.Pow(floatingMultiplier, bubbleIndex);
        int points = Mathf.RoundToInt(floatingBasePoints * multiplier);
        Log($"Floating bubble {bubbleIndex + 1}: {floatingBasePoints} x {multiplier:F2} = {points}");
        return points;
    }
    
    // Calculates floating bubble points using a custom base value.
    // Use this to continue scoring from the last matched bubble's points.
    // Formula: customBase * multiplier^(bubbleIndex+1) to apply multiplier immediately.
    public int GetFloatingBubblePointsFromBase(int bubbleIndex, int customBasePoints)
    {
        float multiplier = Mathf.Pow(floatingMultiplier, bubbleIndex + 1);
        int points = Mathf.RoundToInt(customBasePoints * multiplier);
        Log($"Floating bubble {bubbleIndex + 1}: {customBasePoints} x {multiplier:F2} = {points}");
        return points;
    }

    // Adds points and animates the score display using DOTween.
    // The number smoothly counts up to the new total.
    public void AddScore(int points)
    {
        if (points <= 0) return;
        
        currentScore += points;
        onScoreChanged?.Invoke(currentScore, points);
        onBubbleScore?.Invoke(points);
        
        AnimateScoreDisplay();
        
        Log($"Added {points} points, total: {currentScore}");
    }

    // Animates the displayed score counting up to currentScore.
    // Kills any existing tween and starts fresh to handle rapid additions.
    private void AnimateScoreDisplay()
    {
        countTween?.Kill();
        
        countTween = DOTween.To(
            () => displayedScore,
            x => {
                displayedScore = x;
                UpdateScoreDisplay();
            },
            currentScore,
            countDuration
        ).SetEase(countEase);
    }

    // Resets score to zero. Call this when starting a new game.
    public void ResetScore()
    {
        countTween?.Kill();
        currentScore = 0;
        displayedScore = 0;
        onScoreChanged?.Invoke(currentScore, 0);
        UpdateScoreDisplay();
        Log("Score reset");
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, Mathf.RoundToInt(displayedScore));
        }
    }
    
    void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[ScoreManager] {msg}");
    }
}