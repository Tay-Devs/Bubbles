using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StarProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private LiveStarIndicatorUI starIndicator;
    [SerializeField] private Canvas canvas;
    
    [Header("Slider Star Milestones")]
    [SerializeField] private List<RectTransform> starMarkers = new List<RectTransform>();
    [SerializeField] private List<Image> sliderStarImages = new List<Image>();
    [SerializeField] private float starYOffset = 30f;
    
    [Header("Star Sprites (Themeable)")]
    [SerializeField] private ThemeSprite starEarnedTheme;
    [SerializeField] private ThemeSprite starEmptyTheme;
    
    [Header("Flying Star")]
    [SerializeField] private GameObject flyingStarPrefab;
    [SerializeField] private RectTransform flyingStarParent;
    
    [Header("Flying Star Animation")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private Ease flyEase = Ease.InOutQuad;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float rotationAmount = 360f;
    
    [Header("End Level Star Animation")]
    [SerializeField] private float endLevelStarDelay = 0.2f;
    
    [Header("Burn Animation (Classic Mode)")]
    [SerializeField] private float burnGrowScale = 1.5f;
    [SerializeField] private float burnGrowDuration = 0.2f;
    [SerializeField] private float burnSpinAmount = 360f;
    [SerializeField] private float burnMoveDistance = 100f;
    [SerializeField] private float burnFadeDuration = 0.4f;
    [SerializeField] private Ease burnGrowEase = Ease.OutBack;
    [SerializeField] private Ease burnFadeEase = Ease.InQuad;
    
    [Header("Audio")]
    [SerializeField] private SFXData starEarnedSFX;
    [SerializeField] private SFXData starLostSFX;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private LevelConfig currentLevel;
    private WinConditionType winCondition;
    private ThemeMode currentTheme;
    
    // Tracks which milestone stars have been passed/claimed (index 0=first star, 1=second, 2=third)
    private bool[] milestoneReached = new bool[3];
    
    // Normalized threshold positions (0-1 range) for star placement and detection
    private float[] normalizedThresholds = new float[3];
    
    // Max value for normalization
    private float maxValue = 1f;
    
    private bool isGameEnded = false;
    private int pendingAnimations = 0;
    
    void Start()
    {
        CacheReferences();
        CacheCurrentTheme();
        SubscribeToEvents();
        InitializeSlider();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void Update()
    {
        if (currentLevel == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (isGameEnded) return;
        
        UpdateSlider();
    }
    
    // Caches level config and win condition from LevelLoader/GameManager.
    private void CacheReferences()
    {
        if (LevelLoader.Instance != null)
        {
            currentLevel = LevelLoader.Instance.CurrentLevel;
        }
        
        if (GameManager.Instance != null)
        {
            winCondition = GameManager.Instance.ActiveWinCondition;
        }
    }
    
    // Stores the current theme from ThemeManager for sprite lookups.
    private void CacheCurrentTheme()
    {
        if (ThemeManager.Instance != null)
        {
            currentTheme = ThemeManager.Instance.CurrentTheme;
        }
    }
    
    // Subscribes to game events and theme changes for end-of-level handling.
    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onVictory.AddListener(OnGameEnd);
            GameManager.Instance.onGameOver.AddListener(OnGameEnd);
        }
        
        ThemeManager.OnThemeChanged += OnThemeChanged;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onVictory.RemoveListener(OnGameEnd);
            GameManager.Instance.onGameOver.RemoveListener(OnGameEnd);
        }
        
        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }
    
    // Called when theme changes. Updates cached theme and refreshes slider star visuals.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        currentTheme = newTheme;
        UpdateSliderStarVisuals();
        Log($"Theme changed to {newTheme}, updated slider star visuals");
    }
    
    // Returns the correct sprite from a ThemeSprite based on current theme.
    // Returns null if ThemeSprite is not assigned.
    private Sprite GetSpriteFromTheme(ThemeSprite themeSprite)
    {
        if (themeSprite == null) return null;
        return themeSprite.GetSprite(currentTheme);
    }
    
    // Sets up slider range, thresholds, and positions stars automatically.
    private void InitializeSlider()
    {
        if (currentLevel == null || progressSlider == null) return;
        
        // Reset milestone tracking
        for (int i = 0; i < milestoneReached.Length; i++)
        {
            milestoneReached[i] = false;
        }
        isGameEnded = false;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                InitializeClassicMode();
                break;
            case WinConditionType.ReachTargetScore:
                InitializeScoreMode();
                break;
            case WinConditionType.Survival:
                InitializeSurvivalMode();
                break;
        }
        
        // Position stars along slider track based on thresholds
        PositionStarsOnTrack();
        
        // Show all slider stars initially
        UpdateSliderStarVisuals();
        
        Log($"Initialized slider for {winCondition} mode");
    }
    
    // Classic mode: Slider fills from 0 to total time (interval * 3).
    // Stars positioned at equal intervals (33%, 66%, 100%) and burn when passed.
    private void InitializeClassicMode()
    {
        maxValue = currentLevel.OneStarTime;
        
        // Equal spacing for visual alignment with slider dividers
        normalizedThresholds[0] = 1f / 3f;
        normalizedThresholds[1] = 2f / 3f;
        normalizedThresholds[2] = 1f;
        
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;
        
        Log($"Classic: interval={currentLevel.starTimeInterval}s, max={maxValue}s");
    }
    
    // Score mode: Slider fills from 0 to targetScore.
    // Stars positioned at equal intervals (33%, 66%, 100%).
    private void InitializeScoreMode()
    {
        maxValue = currentLevel.targetScore;
        
        // Equal spacing for visual alignment with slider dividers
        normalizedThresholds[0] = 1f / 3f;
        normalizedThresholds[1] = 2f / 3f;
        normalizedThresholds[2] = 1f;
        
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;
        
        Log($"Score: max={maxValue}, thresholds=[{normalizedThresholds[0]:F2}, {normalizedThresholds[1]:F2}, {normalizedThresholds[2]:F2}]");
    }
    
    // Survival mode: Slider fills from 0 to threeStarRows.
    // Stars positioned at equal intervals (33%, 66%, 100%).
    private void InitializeSurvivalMode()
    {
        maxValue = currentLevel.threeStarRows;
        
        // Equal spacing for visual alignment with slider dividers
        normalizedThresholds[0] = 1f / 3f;
        normalizedThresholds[1] = 2f / 3f;
        normalizedThresholds[2] = 1f;
        
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;
        
        Log($"Survival: max={maxValue} rows, thresholds=[{normalizedThresholds[0]:F2}, {normalizedThresholds[1]:F2}, {normalizedThresholds[2]:F2}]");
    }
    
    // Positions star images above the marker transforms using world position.
    // This works regardless of different parent transforms or anchor settings.
    private void PositionStarsOnTrack()
    {
        for (int i = 0; i < sliderStarImages.Count && i < starMarkers.Count; i++)
        {
            if (sliderStarImages[i] == null || starMarkers[i] == null) continue;
            
            RectTransform starRect = sliderStarImages[i].rectTransform;
            RectTransform markerRect = starMarkers[i];
            
            // Get marker's world position
            Vector3 markerWorldPos = markerRect.position;
            
            // Set star to same world position, then offset Y
            starRect.position = new Vector3(markerWorldPos.x, markerWorldPos.y + starYOffset, markerWorldPos.z);
            
            Log($"Star {i} positioned at marker world pos ({markerWorldPos.x:F1}, {markerWorldPos.y:F1})");
        }
    }
    
    // Main update loop that routes to mode-specific slider update.
    private void UpdateSlider()
    {
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                UpdateClassicSlider();
                break;
            case WinConditionType.ReachTargetScore:
                UpdateScoreSlider();
                break;
            case WinConditionType.Survival:
                UpdateSurvivalSlider();
                break;
        }
    }
    
    // Classic mode: Slider fills continuously. Stars burn when their threshold is passed.
    private void UpdateClassicSlider()
    {
        float currentTime = LevelLoader.Instance != null ? LevelLoader.Instance.ElapsedTime : 0f;
        
        // Normalize progress across full time range
        float normalizedProgress = Mathf.Clamp01(currentTime / maxValue);
        progressSlider.value = normalizedProgress;
        
        // Check each star threshold (left to right)
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i] && normalizedProgress >= normalizedThresholds[i])
            {
                milestoneReached[i] = true;
                BurnSliderStar(i);
                Log($"Classic: Star {i} burned at {currentTime:F1}s (threshold: {normalizedThresholds[i] * maxValue:F1}s)");
            }
        }
    }
    
    // Score mode: Slider fills continuously. Stars earned when thresholds reached.
    private void UpdateScoreSlider()
    {
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        
        float normalizedProgress = Mathf.Clamp01(currentScore / maxValue);
        progressSlider.value = normalizedProgress;
        
        // Check star thresholds (left to right)
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i] && normalizedProgress >= normalizedThresholds[i])
            {
                milestoneReached[i] = true;
                EarnSliderStar(i);
                Log($"Score: Star {i} earned at {currentScore} points");
            }
        }
    }
    
    // Survival mode: Slider fills continuously. Stars earned when row thresholds reached.
    private void UpdateSurvivalSlider()
    {
        int rowsSurvived = GameManager.Instance != null ? GameManager.Instance.GetSurvivalRowsCount() : 0;
        
        float normalizedProgress = Mathf.Clamp01(rowsSurvived / maxValue);
        progressSlider.value = normalizedProgress;
        
        // Check star thresholds (left to right)
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i] && normalizedProgress >= normalizedThresholds[i])
            {
                milestoneReached[i] = true;
                EarnSliderStar(i);
                Log($"Survival: Star {i} earned at {rowsSurvived} rows");
            }
        }
    }
    
    // Called when game ends. Forces final update for Score/Survival modes to catch
    // pending milestones, then handles Classic mode star awarding.
    private void OnGameEnd()
    {
        // For Score/Survival: force one final update to catch any pending milestones
        // This handles cases where score/rows change and victory trigger in the same frame
        if (winCondition == WinConditionType.ReachTargetScore || winCondition == WinConditionType.Survival)
        {
            UpdateSlider();
        }
        
        isGameEnded = true;
        
        if (winCondition == WinConditionType.ClearAllBubbles)
        {
            AwardRemainingClassicStars();
        }
        
        Log("Game ended");
    }
    
    // Awards all stars that weren't burned during Classic mode.
    // Stars animate from their slider position to sequential indicator slots (left-to-right).
    private void AwardRemainingClassicStars()
    {
        float delay = 0f;
        int targetSlot = 0;
        
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i])
            {
                int sourceIndex = i;
                int targetIndex = targetSlot;
                DOVirtual.DelayedCall(delay, () =>
                {
                    FlyStarToIndicator(sourceIndex, targetIndex);
                });
                targetSlot++;
                delay += endLevelStarDelay;
            }
        }
        
        int starsToAward = CountRemainingStars();
        Log($"Awarding {starsToAward} remaining stars to indicator");
    }
    
    // Triggers earning animations for bonus stars when grid is cleared in Survival mode.
    // Awards all remaining unearned stars with visual feedback.
    public void TriggerBonusStarsOnClear()
    {
        if (winCondition != WinConditionType.Survival) return;
        
        float delay = 0f;
        int targetSlot = 0;
        
        // Count how many stars already earned to determine starting target slot
        for (int i = 0; i < 3; i++)
        {
            if (milestoneReached[i]) targetSlot++;
        }
        
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i])
            {
                milestoneReached[i] = true;
                int sourceIndex = i;
                int targetIndex = targetSlot;
                
                DOVirtual.DelayedCall(delay, () =>
                {
                    FlyStarToIndicator(sourceIndex, targetIndex);
                });
                targetSlot++;
                delay += endLevelStarDelay;
            }
        }
        
        Log($"Bonus stars triggered on clear");
    }
    
    // Counts how many stars haven't been burned/passed yet.
    private int CountRemainingStars()
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            if (!milestoneReached[i]) count++;
        }
        return count;
    }
    
    // Burns a star on the slider (Classic mode) - grows, spins, and fades away.
    // Uses theme-appropriate earned sprite for the flying star.
    private void BurnSliderStar(int starIndex)
    {
        if (starIndex < 0 || starIndex >= sliderStarImages.Count) return;
        
        Image sliderStar = sliderStarImages[starIndex];
        if (sliderStar == null) return;
        
        if (flyingStarPrefab == null || flyingStarParent == null)
        {
            sliderStar.gameObject.SetActive(false);
            return;
        }
        
        // Spawn flying star at slider star position
        GameObject flyingStar = Instantiate(flyingStarPrefab, flyingStarParent);
        RectTransform starRect = flyingStar.GetComponent<RectTransform>();
        Image starImage = flyingStar.GetComponent<Image>();
        
        if (starRect == null)
        {
            Destroy(flyingStar);
            sliderStar.gameObject.SetActive(false);
            return;
        }
        
        // Match source star size and sprite
        RectTransform sourceRect = sliderStar.rectTransform;
        starRect.sizeDelta = sourceRect.sizeDelta;
        
        Sprite earnedSprite = GetSpriteFromTheme(starEarnedTheme);
        if (starImage != null && earnedSprite != null)
        {
            starImage.sprite = earnedSprite;
        }
        
        // Position at slider star location
        Vector2 localPos = GetLocalPosition(sliderStar.transform.position);
        starRect.anchoredPosition = localPos;
        starRect.localScale = Vector3.one;
        starRect.localRotation = Quaternion.identity;
        
        // Hide original slider star immediately
        sliderStar.gameObject.SetActive(false);
        
        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = flyingStar.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = flyingStar.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        
        pendingAnimations++;
        
        // Animation sequence: grow -> spin/move/fade
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(starRect.DOScale(burnGrowScale, burnGrowDuration).SetEase(burnGrowEase));
        sequence.Append(starRect.DOAnchorPosX(localPos.x + burnMoveDistance, burnFadeDuration).SetEase(Ease.OutQuad));
        sequence.Join(starRect.DOLocalRotate(new Vector3(0, 0, burnSpinAmount), burnFadeDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        sequence.Join(starRect.DOScale(0f, burnFadeDuration).SetEase(burnFadeEase));
        sequence.Join(canvasGroup.DOFade(0f, burnFadeDuration).SetEase(burnFadeEase));
        
        sequence.OnComplete(() =>
        {
            Destroy(flyingStar);
            pendingAnimations--;
            
            if (starLostSFX != null)
            {
                SFXManager.Play(starLostSFX);
            }
        });
        
        Log($"Burning slider star {starIndex}");
    }
    
    // Earns a star from the slider (Score/Survival mode) - flies to the indicator.
    private void EarnSliderStar(int starIndex)
    {
        if (starIndex < 0 || starIndex >= sliderStarImages.Count) return;
        
        Image sliderStar = sliderStarImages[starIndex];
        if (sliderStar == null) return;
        
        // Hide slider star
        sliderStar.gameObject.SetActive(false);
        
        // Fly to indicator (same index for Score/Survival since stars are earned in order)
        FlyStarToIndicator(starIndex, starIndex);
    }
    
    // Spawns a flying star from the slider position to the indicator position.
    // Uses theme-appropriate earned sprite for the animation.
    private void FlyStarToIndicator(int sourceStarIndex, int targetIndicatorIndex)
    {
        if (sourceStarIndex < 0 || sourceStarIndex >= sliderStarImages.Count) return;
        if (starIndicator == null) return;
        
        Image sliderStar = sliderStarImages[sourceStarIndex];
        Vector3 sourcePos = sliderStar != null ? sliderStar.transform.position : transform.position;
        Vector3 targetPos = starIndicator.GetStarWorldPosition(targetIndicatorIndex);
        
        if (flyingStarPrefab == null || flyingStarParent == null)
        {
            starIndicator.OnStarAnimationComplete(targetIndicatorIndex, true);
            return;
        }
        
        // Hide slider star if still visible
        if (sliderStar != null)
        {
            sliderStar.gameObject.SetActive(false);
        }
        
        // Spawn flying star
        GameObject flyingStar = Instantiate(flyingStarPrefab, flyingStarParent);
        RectTransform starRect = flyingStar.GetComponent<RectTransform>();
        Image starImage = flyingStar.GetComponent<Image>();
        
        if (starRect == null)
        {
            Destroy(flyingStar);
            starIndicator.OnStarAnimationComplete(targetIndicatorIndex, true);
            return;
        }
        
        // Match source star size and sprite
        if (sliderStar != null)
        {
            starRect.sizeDelta = sliderStar.rectTransform.sizeDelta;
        }
        
        Sprite earnedSprite = GetSpriteFromTheme(starEarnedTheme);
        if (starImage != null && earnedSprite != null)
        {
            starImage.sprite = earnedSprite;
        }
        
        // Position at source
        Vector2 startLocalPos = GetLocalPosition(sourcePos);
        Vector2 targetLocalPos = GetLocalPosition(targetPos);
        
        starRect.anchoredPosition = startLocalPos;
        starRect.localScale = Vector3.one * startScale;
        
        pendingAnimations++;
        
        // Play SFX with combo pitch
        if (starEarnedSFX != null)
        {
            SFXManager.PlayAtPosition(starEarnedSFX,Vector3.zero, targetIndicatorIndex);
        }
        
        // Animation sequence
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(starRect.DOAnchorPos(targetLocalPos, flyDuration).SetEase(flyEase));
        sequence.Join(starRect.DOScale(endScale, flyDuration).SetEase(Ease.OutBack));
        sequence.Join(starRect.DORotate(new Vector3(0, 0, rotationAmount), flyDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        
        int capturedTargetIndex = targetIndicatorIndex;
        sequence.OnComplete(() =>
        {
            Destroy(flyingStar);
            pendingAnimations--;
            starIndicator.OnStarAnimationComplete(capturedTargetIndex, true);
        });
        
        Log($"Flying star from slider {sourceStarIndex} to indicator {targetIndicatorIndex}");
    }
    
    // Updates the visual state of all slider star images using theme-appropriate sprites.
    private void UpdateSliderStarVisuals()
    {
        Sprite earnedSprite = GetSpriteFromTheme(starEarnedTheme);
        
        for (int i = 0; i < sliderStarImages.Count; i++)
        {
            if (sliderStarImages[i] == null) continue;
            
            bool isAvailable = !milestoneReached[i];
            sliderStarImages[i].gameObject.SetActive(isAvailable);
            
            if (isAvailable && earnedSprite != null)
            {
                sliderStarImages[i].sprite = earnedSprite;
            }
        }
    }
    
    // Converts world position to local position within flying star parent.
    private Vector2 GetLocalPosition(Vector3 worldPos)
    {
        if (flyingStarParent == null || canvas == null) return Vector2.zero;
        
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            flyingStarParent,
            screenPos,
            cam,
            out Vector2 localPos
        );
        
        return localPos;
    }
    
    // Returns the number of stars that will be awarded.
    public int GetCurrentStarCount()
    {
        if (winCondition == WinConditionType.ClearAllBubbles)
        {
            return CountRemainingStars();
        }
        else
        {
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                if (milestoneReached[i]) count++;
            }
            return count;
        }
    }
    
    // Resets the slider for a new game.
    public void Reset()
    {
        CacheReferences();
        CacheCurrentTheme();
        InitializeSlider();
    }
    
    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[StarProgressUI] {msg}");
    }
}