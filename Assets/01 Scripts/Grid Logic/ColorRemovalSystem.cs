using System;
using System.Collections.Generic;
using UnityEngine;

public class ColorRemovalSystem : MonoBehaviour
{
    [Header("Bonus Settings")]
    public int scoreBonus = 1000;
    
    [Header("Animation")]
    public Animator colorRemovedAnimator;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public Action<BubbleType> onColorRemoved;
    
    private HexGrid grid;
    private HashSet<BubbleType> previousColors = new HashSet<BubbleType>();
    private int colorsRemovedCount = 0;
    
    private static readonly int PlayTrigger = Animator.StringToHash("Play");
    
    public int ColorsRemovedCount => colorsRemovedCount;
    
    // Returns how many rows should spawn per trigger (1 + colors removed).
    public int RowsPerSpawn => 1 + colorsRemovedCount;
    
    // Initializes the system with grid reference and subscribes to color change events.
    // Call this after the grid is ready.
    public void Initialize(HexGrid hexGrid)
    {
        grid = hexGrid;
        
        if (grid != null)
        {
            grid.onColorsChanged += CheckForRemovedColors;
        }
    }
    
    void OnDestroy()
    {
        if (grid != null)
        {
            grid.onColorsChanged -= CheckForRemovedColors;
        }
    }
    
    // Stores initial colors from the grid. Call after grid is generated.
    // Resets the removed count to zero for a fresh start.
    public void InitializePreviousColors()
    {
        previousColors.Clear();
        colorsRemovedCount = 0;
        
        if (grid != null)
        {
            var colors = grid.GetAvailableColors();
            foreach (var color in colors)
            {
                previousColors.Add(color);
            }
        }
        
        Log($"Starting with {previousColors.Count} colors. Rows per spawn: {RowsPerSpawn}");
    }
    
    // Called when grid colors change. Compares current colors against previous
    // to detect if any color was completely removed from the grid.
    private void CheckForRemovedColors()
    {
        if (grid == null) return;
        
        var currentColors = grid.GetAvailableColors();
        
        // Find removed colors
        List<BubbleType> removedColors = new List<BubbleType>();
        foreach (var prevColor in previousColors)
        {
            if (!currentColors.Contains(prevColor))
            {
                removedColors.Add(prevColor);
            }
        }
        
        // Process each removed color
        foreach (var removedColor in removedColors)
        {
            OnColorCompletelyRemoved(removedColor);
        }
        
        // Update previous colors for next check
        previousColors.Clear();
        foreach (var color in currentColors)
        {
            previousColors.Add(color);
        }
    }
    
    // Called when a color is completely removed from the grid.
    // Awards bonus score, plays animation, and fires event.
    private void OnColorCompletelyRemoved(BubbleType color)
    {
        colorsRemovedCount++;
        
        Log($"Color {color} removed! Total removed: {colorsRemovedCount}, Rows per spawn now: {RowsPerSpawn}");
        
        // Add score bonus
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreBonus);
            Log($"Added {scoreBonus} bonus points");
        }
        
        // Play animation
        if (colorRemovedAnimator != null)
        {
            colorRemovedAnimator.SetTrigger(PlayTrigger);
            Log("Triggered color removed animation");
        }
        
        // Fire event
        onColorRemoved?.Invoke(color);
    }
    
    // Resets the system for a new game.
    // Clears tracking data so color detection starts fresh.
    public void Reset()
    {
        colorsRemovedCount = 0;
        previousColors.Clear();
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[ColorRemovalSystem] {msg}");
    }
}