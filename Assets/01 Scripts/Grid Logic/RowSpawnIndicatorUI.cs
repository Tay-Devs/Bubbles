using UnityEngine;
using UnityEngine.UI;

public class RowSpawnProgressUI : MonoBehaviour
{
    [Header("Progress Bar (assign one)")]
    public Slider progressSlider;
    public Image progressFill; // Alternative: Image with Fill type
    
    [Header("Fill Direction")]
    public bool invertFill = false; // True = fills up as danger increases, False = drains down
    
    private HexGrid grid;
    private bool isSurvivalMode;
    
    void Start()
    {
        grid = FindFirstObjectByType<HexGrid>();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onWinConditionSet.AddListener(OnWinConditionSet);
            OnWinConditionSet(GameManager.Instance.ActiveWinCondition);
        }
        
        if (grid != null && grid.RowSystem != null)
        {
            grid.RowSystem.onShotsChanged += OnShotsChanged;
        }
    }
    
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onWinConditionSet.RemoveListener(OnWinConditionSet);
        }
        
        if (grid != null && grid.RowSystem != null)
        {
            grid.RowSystem.onShotsChanged -= OnShotsChanged;
        }
    }
    
    void Update()
    {
        if (!isSurvivalMode) return;
        if (grid == null || grid.RowSystem == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        
        UpdateSurvivalProgress();
    }
    
    // Sets the mode based on win condition type.
    void OnWinConditionSet(WinConditionType condition)
    {
        isSurvivalMode = (condition == WinConditionType.Survival);
        
        if (!isSurvivalMode && grid != null && grid.RowSystem != null)
        {
            OnShotsChanged(grid.RowSystem.CurrentShots, grid.RowSystem.MaxShots);
        }
    }
    
    // Updates progress bar based on survival timer.
    // Fills up as time passes toward next row spawn.
    void UpdateSurvivalProgress()
    {
        float currentInterval = grid.RowSystem.CurrentSurvivalInterval;
        float timer = grid.RowSystem.SurvivalTimer;
        
        float progress = timer / currentInterval;
        SetProgress(progress);
    }
    
    // Updates progress bar based on remaining shots.
    // Called when shot count changes in non-survival modes.
    void OnShotsChanged(int current, int max)
    {
        if (isSurvivalMode) return;
        
        float progress = 1f - ((float)current / max);
        SetProgress(progress);
    }
    
    // Sets the progress value on either Slider or Image fill.
    // Handles invert option for different visual styles.
    void SetProgress(float value)
    {
        float finalValue = invertFill ? 1f - value : value;
        
        if (progressSlider != null)
        {
            progressSlider.value = finalValue;
        }
        
        if (progressFill != null)
        {
            progressFill.fillAmount = finalValue;
        }
    }
}