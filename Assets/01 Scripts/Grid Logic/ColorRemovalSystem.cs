using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ColorRemovalSystem : MonoBehaviour
{
    [Header("Bonus Settings")]
    public int scoreBonus = 1000;
    
    [Header("Day Popup")]
    [SerializeField] private Animator dayPopupAnimator;
    [SerializeField] private CanvasGroup dayPopupCanvasGroup;
    [SerializeField] private TextMeshProUGUI dayBonusText;
    
    [Header("Night Popup")]
    [SerializeField] private Animator nightPopupAnimator;
    [SerializeField] private CanvasGroup nightPopupCanvasGroup;
    [SerializeField] private TextMeshProUGUI nightBonusText;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Events
    public Action<BubbleType> onColorRemoved;
    
    private HexGrid grid;
    private HashSet<BubbleType> previousColors = new HashSet<BubbleType>();
    private int colorsRemovedCount = 0;
    
    private static readonly int PlayTrigger = Animator.StringToHash("Play");
    
    public int ColorsRemovedCount => colorsRemovedCount;
    
    // Returns how many rows should spawn per trigger (1 + colors removed).
    public int RowsPerSpawn => 1 + colorsRemovedCount;
    
    private void Start()
    {
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Set initial visibility
        if (ThemeManager.Instance != null)
        {
            UpdatePopupVisibility(ThemeManager.Instance.CurrentTheme);
        }
    }
    
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
        
        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }
    
    // Called when theme changes. Swaps which popup is visible.
    // Both popups keep playing, only visibility changes.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        UpdatePopupVisibility(newTheme);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorRemovalSystem] Theme changed to {newTheme}, updated popup visibility");
        }
    }
    
    // Sets CanvasGroup alpha to show/hide the correct popup.
    // Uses alpha instead of SetActive so animators keep running.
    private void UpdatePopupVisibility(ThemeMode theme)
    {
        bool isDay = theme == ThemeMode.Day;
        
        if (dayPopupCanvasGroup != null)
        {
            dayPopupCanvasGroup.alpha = isDay ? 1f : 0f;
            dayPopupCanvasGroup.blocksRaycasts = isDay;
        }
        
        if (nightPopupCanvasGroup != null)
        {
            nightPopupCanvasGroup.alpha = isDay ? 0f : 1f;
            nightPopupCanvasGroup.blocksRaycasts = !isDay;
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
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorRemovalSystem] Starting with {previousColors.Count} colors. Rows per spawn: {RowsPerSpawn}");
        }
    }
    
    // Called when grid colors change. Compares current colors against previous
    // to detect if any colors were completely removed from the grid.
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
        
        // Process all removed colors together
        if (removedColors.Count > 0)
        {
            OnColorsCompletelyRemoved(removedColors);
        }
        
        // Update previous colors for next check
        previousColors.Clear();
        foreach (var color in currentColors)
        {
            previousColors.Add(color);
        }
    }
    
    // Called when one or more colors are completely removed from the grid.
    // Calculates combined bonus, updates text, plays animations, and fires events.
    private void OnColorsCompletelyRemoved(List<BubbleType> removedColors)
    {
        int colorCount = removedColors.Count;
        colorsRemovedCount += colorCount;
        
        // Calculate combined bonus
        int totalBonus = scoreBonus * colorCount;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorRemovalSystem] {colorCount} color(s) removed! Total removed: {colorsRemovedCount}, Bonus: +{totalBonus}, Rows per spawn now: {RowsPerSpawn}");
        }
        
        // Add score bonus
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(totalBonus);
        }
        
        // Update bonus text on both popups with singular/plural
        string colorWord = colorCount == 1 ? "Color" : "Colors";
        string bonusString = $"{colorWord} Removed\n+{totalBonus}";
        if (dayBonusText != null) dayBonusText.text = bonusString;
        if (nightBonusText != null) nightBonusText.text = bonusString;
        
        // Play both animations simultaneously (only visible one is seen)
        if (dayPopupAnimator != null) dayPopupAnimator.SetTrigger(PlayTrigger);
        if (nightPopupAnimator != null) nightPopupAnimator.SetTrigger(PlayTrigger);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorRemovalSystem] Triggered popup animations with bonus: {bonusString}");
        }
        
        // Fire event for each removed color
        foreach (var color in removedColors)
        {
            onColorRemoved?.Invoke(color);
        }
    }
    
    // Resets the system for a new game.
    // Clears tracking data so color detection starts fresh.
    public void Reset()
    {
        colorsRemovedCount = 0;
        previousColors.Clear();
    }
}