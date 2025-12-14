using System;
using System.Collections.Generic;
using UnityEngine;

public class ColorRemovalSystem : MonoBehaviour
{
    [Header("Bonus Settings")]
    public int scoreBonus = 1000;
    
    [Header("Animation")]
    public GameObject colorRemovedEffect;
    public float effectDuration = 2f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Events
    public Action<BubbleType> onColorRemoved;
    
    private HexGrid grid;
    private HashSet<BubbleType> previousColors = new HashSet<BubbleType>();
    private int colorsRemovedCount = 0;
    
    public int ColorsRemovedCount => colorsRemovedCount;
    
    // Returns how many rows should spawn per trigger (1 + colors removed).
    public int RowsPerSpawn => 1 + colorsRemovedCount;
    
    // Initializes the system with grid reference.
    public void Initialize(HexGrid hexGrid)
    {
        grid = hexGrid;
        
        if (grid != null)
        {
            grid.onColorsChanged += CheckForRemovedColors;
        }
        
        if (colorRemovedEffect != null)
        {
            colorRemovedEffect.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (grid != null)
        {
            grid.onColorsChanged -= CheckForRemovedColors;
        }
    }
    
    // Call this after grid is generated to store initial colors.
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
        
        Log($"[ColorRemovalSystem] Starting with {previousColors.Count} colors. Rows per spawn: {RowsPerSpawn}");
    }
    
    // Called when grid colors change. Checks if any color was completely removed.
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
    private void OnColorCompletelyRemoved(BubbleType color)
    {
        colorsRemovedCount++;
        
        Log($"[ColorRemovalSystem] Color {color} removed! Total removed: {colorsRemovedCount}, Rows per spawn now: {RowsPerSpawn}");
        
        // Add score bonus
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreBonus);
            Log($"[ColorRemovalSystem] Added {scoreBonus} bonus points");
        }
        
        // Show effect
        if (colorRemovedEffect != null)
        {
            StartCoroutine(ShowEffect());
        }
        
        // Fire event
        onColorRemoved?.Invoke(color);
    }
    
    private System.Collections.IEnumerator ShowEffect()
    {
        colorRemovedEffect.SetActive(true);
        yield return new WaitForSeconds(effectDuration);
        colorRemovedEffect.SetActive(false);
    }
    
    // Resets the system for a new game.
    public void Reset()
    {
        colorsRemovedCount = 0;
        previousColors.Clear();
        
        if (colorRemovedEffect != null)
        {
            colorRemovedEffect.SetActive(false);
        }
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log(msg);
    }
}