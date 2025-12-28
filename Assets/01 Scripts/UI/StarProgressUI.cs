using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StarProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private LiveStarIndicatorUI starIndicator;
    [SerializeField] private Canvas canvas;
    
    [Header("Flying Star")]
    [SerializeField] private GameObject flyingStarPrefab;
    [SerializeField] private RectTransform flyingStarParent;
    
    [Header("Format Settings")]
    [TextArea]
    [SerializeField] private string timeFormat = "{0:F1} / {1:F1}";
    [TextArea]
    [SerializeField] private string scoreFormat = "{0:N0} / {1:N0}";
    [TextArea]
    [SerializeField] private string rowsFormat = "{0} / {1}";
    
    [Header("Number Animation")]
    [SerializeField] private float numberCountDuration = 0.3f;
    [SerializeField] private Ease numberCountEase = Ease.OutQuad;
    
    [Header("Goal Change Animation")]
    [SerializeField] private float goalPunchScale = 0.2f;
    [SerializeField] private float goalPunchDuration = 0.3f;
    [SerializeField] private int goalPunchVibrato = 5;
    
    [Header("Flying Star Animation")]
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private Ease flyEase = Ease.InOutQuad;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float rotationAmount = 360f;
    [SerializeField] private float delayBetweenStars = 0.15f;
    
    [Header("Lose Animation")]
    [SerializeField] private Vector2 loseTargetOffset = new Vector2(200f, 0f);
    [SerializeField] private float loseFadeDuration = 0.3f;
    
    [Header("Classic Mode Lose Animation")]
    [SerializeField] private float classicGrowScale = 1.5f;
    [SerializeField] private float classicGrowDuration = 0.2f;
    [SerializeField] private float classicSpinAmount = 360f;
    [SerializeField] private float classicMoveDistance = 100f;
    [SerializeField] private float classicFadeDuration = 0.4f;
    [SerializeField] private Ease classicGrowEase = Ease.OutBack;
    [SerializeField] private Ease classicFadeEase = Ease.InQuad;
    
    [Header("Audio")]
    [SerializeField] private SFXData starEarnedSFX;
    [SerializeField] private SFXData starLostSFX;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private LevelConfig currentLevel;
    private WinConditionType winCondition;
    private int currentStarTarget = 3;
    private int pendingAnimations = 0;
    
    // Animated display values
    private float displayedCurrent = 0f;
    private float displayedTarget = 0f;
    private float actualTarget = 0f;
    
    private Tweener currentTween;
    private Tweener targetTween;
    private RectTransform textRectTransform;
    
    void Start()
    {
        if (LevelLoader.Instance != null)
        {
            currentLevel = LevelLoader.Instance.CurrentLevel;
        }
        
        if (GameManager.Instance != null)
        {
            winCondition = GameManager.Instance.ActiveWinCondition;
        }
        
        // Subscribe to star change events
        if (starIndicator != null)
        {
            starIndicator.onStarChanging += OnStarChanging;
            starIndicator.onForceUpdate += OnForceUpdate;
        }
        
        textRectTransform = progressText != null ? progressText.rectTransform : null;
        
        InitializeValues();
        UpdateDisplay();
    }
    
    void OnDestroy()
    {
        if (starIndicator != null)
        {
            starIndicator.onStarChanging -= OnStarChanging;
            starIndicator.onForceUpdate -= OnForceUpdate;
        }
        
        currentTween?.Kill();
        targetTween?.Kill();
    }
    
    // Initializes display values based on current game state.
    private void InitializeValues()
    {
        if (currentLevel == null) return;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                displayedCurrent = 0f;
                displayedTarget = currentLevel.threeStarTime;
                actualTarget = currentLevel.threeStarTime;
                break;
                
            case WinConditionType.ReachTargetScore:
                displayedCurrent = 0f;
                displayedTarget = currentLevel.GetScoreForStars(1);
                actualTarget = currentLevel.GetScoreForStars(1);
                break;
                
            case WinConditionType.Survival:
                displayedCurrent = 0f;
                displayedTarget = currentLevel.oneStarRows;
                actualTarget = currentLevel.oneStarRows;
                break;
        }
    }
    
    void Update()
    {
        if (currentLevel == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        
        UpdateDisplay();
    }
    
    // Updates the progress text based on current win condition.
    private void UpdateDisplay()
    {
        if (progressText == null || currentLevel == null) return;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                UpdateClassicDisplay();
                break;
            case WinConditionType.ReachTargetScore:
                UpdateScoreDisplay();
                break;
            case WinConditionType.Survival:
                UpdateSurvivalDisplay();
                break;
        }
    }
    
    private void UpdateClassicDisplay()
    {
        float currentTime = LevelLoader.Instance != null ? LevelLoader.Instance.ElapsedTime : 0f;
        
        float targetTime;
        int newStarTarget;
        
        if (currentTime < currentLevel.threeStarTime)
        {
            targetTime = currentLevel.threeStarTime;
            newStarTarget = 3;
        }
        else if (currentTime < currentLevel.twoStarTime)
        {
            targetTime = currentLevel.twoStarTime;
            newStarTarget = 2;
        }
        else if (currentTime < currentLevel.oneStarTime)
        {
            targetTime = currentLevel.oneStarTime;
            newStarTarget = 1;
        }
        else
        {
            targetTime = currentLevel.oneStarTime;
            newStarTarget = 0;
        }
        
        // Check if target changed
        if (newStarTarget != currentStarTarget)
        {
            currentStarTarget = newStarTarget;
            AnimateTargetChange(targetTime);
            PlayGoalChangePunch();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[StarProgressUI] Star target changed to {currentStarTarget}");
            }
        }
        
        // Update current value (time updates continuously, no tween needed)
        displayedCurrent = currentTime;
        
        RefreshTextDisplay();
    }
    
    private void UpdateScoreDisplay()
    {
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        
        int oneStarScore = currentLevel.GetScoreForStars(1);
        int twoStarScore = currentLevel.GetScoreForStars(2);
        int threeStarScore = currentLevel.GetScoreForStars(3);
        
        int targetScore;
        int newStarTarget;
        
        if (currentScore < oneStarScore)
        {
            targetScore = oneStarScore;
            newStarTarget = 1;
        }
        else if (currentScore < twoStarScore)
        {
            targetScore = twoStarScore;
            newStarTarget = 2;
        }
        else if (currentScore < threeStarScore)
        {
            targetScore = threeStarScore;
            newStarTarget = 3;
        }
        else
        {
            targetScore = threeStarScore;
            newStarTarget = 3;
        }
        
        // Check if target changed
        if (newStarTarget != currentStarTarget)
        {
            currentStarTarget = newStarTarget;
            AnimateTargetChange(targetScore);
            PlayGoalChangePunch();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[StarProgressUI] Star target changed to {currentStarTarget}");
            }
        }
        
        // Animate current score
        AnimateCurrentValue(currentScore);
        
        RefreshTextDisplay();
    }
    
    private void UpdateSurvivalDisplay()
    {
        int rowsSurvived = GameManager.Instance != null ? GameManager.Instance.GetSurvivalRowsCount() : 0;
        
        int targetRows;
        int newStarTarget;
        
        if (rowsSurvived < currentLevel.oneStarRows)
        {
            targetRows = currentLevel.oneStarRows;
            newStarTarget = 1;
        }
        else if (rowsSurvived < currentLevel.twoStarRows)
        {
            targetRows = currentLevel.twoStarRows;
            newStarTarget = 2;
        }
        else if (rowsSurvived < currentLevel.threeStarRows)
        {
            targetRows = currentLevel.threeStarRows;
            newStarTarget = 3;
        }
        else
        {
            targetRows = currentLevel.threeStarRows;
            newStarTarget = 3;
        }
        
        // Check if target changed
        if (newStarTarget != currentStarTarget)
        {
            currentStarTarget = newStarTarget;
            AnimateTargetChange(targetRows);
            PlayGoalChangePunch();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[StarProgressUI] Star target changed to {currentStarTarget}");
            }
        }
        
        // Animate current rows
        AnimateCurrentValue(rowsSurvived);
        
        RefreshTextDisplay();
    }
    
    // Animates the current value smoothly to the new value.
    private void AnimateCurrentValue(float newValue)
    {
        if (Mathf.Approximately(displayedCurrent, newValue)) return;
        
        currentTween?.Kill();
        currentTween = DOTween.To(
            () => displayedCurrent,
            x => { displayedCurrent = x; RefreshTextDisplay(); },
            newValue,
            numberCountDuration
        ).SetEase(numberCountEase);
    }
    
    // Animates the target value smoothly to the new value.
    private void AnimateTargetChange(float newTarget)
    {
        if (Mathf.Approximately(actualTarget, newTarget)) return;
        
        actualTarget = newTarget;
        
        targetTween?.Kill();
        targetTween = DOTween.To(
            () => displayedTarget,
            x => { displayedTarget = x; RefreshTextDisplay(); },
            newTarget,
            numberCountDuration
        ).SetEase(numberCountEase);
    }
    
    // Plays a punch scale animation on the text when goal changes.
    private void PlayGoalChangePunch()
    {
        if (textRectTransform == null) return;
        
        textRectTransform.DOKill();
        textRectTransform.localScale = Vector3.one;
        textRectTransform.DOPunchScale(Vector3.one * goalPunchScale, goalPunchDuration, goalPunchVibrato, 0.5f);
    }
    
    // Updates the text display with current animated values.
    private void RefreshTextDisplay()
    {
        if (progressText == null) return;
        
        switch (winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                progressText.text = string.Format(timeFormat, displayedCurrent, displayedTarget);
                break;
                
            case WinConditionType.ReachTargetScore:
                progressText.text = string.Format(scoreFormat, Mathf.RoundToInt(displayedCurrent), Mathf.RoundToInt(displayedTarget));
                break;
                
            case WinConditionType.Survival:
                progressText.text = string.Format(rowsFormat, Mathf.RoundToInt(displayedCurrent), Mathf.RoundToInt(displayedTarget));
                break;
        }
    }
    
    // Called when a star is about to change (earned or lost).
    // Routes to appropriate animation based on mode and direction.
    private void OnStarChanging(int starIndex, bool isEarning, Vector3 starWorldPos)
    {
        if (flyingStarPrefab == null || flyingStarParent == null)
        {
            starIndicator?.OnStarAnimationComplete(starIndex, isEarning);
            return;
        }
        
        float delay = pendingAnimations * delayBetweenStars;
        pendingAnimations++;
        
        if (isEarning)
        {
            SpawnEarningStarWithDelay(starIndex, starWorldPos, delay);
        }
        else
        {
            // Classic mode uses a different lose animation
            if (winCondition == WinConditionType.ClearAllBubbles)
            {
                SpawnClassicLosingStarWithDelay(starIndex, starWorldPos, delay);
            }
            else
            {
                SpawnLosingStarWithDelay(starIndex, starWorldPos, delay);
            }
        }
    }
    
    // Called when game ends and animations should be skipped.
    private void OnForceUpdate()
    {
        pendingAnimations = 0;
    }
    
    // Spawns a star that flies FROM the progress text TO the star display.
    private void SpawnEarningStarWithDelay(int starIndex, Vector3 targetWorldPos, float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            SpawnEarningStar(starIndex, targetWorldPos);
        });
    }
    
    private void SpawnEarningStar(int starIndex, Vector3 targetWorldPos)
    {
        GameObject star = Instantiate(flyingStarPrefab, flyingStarParent);
        RectTransform starRect = star.GetComponent<RectTransform>();
        
        if (starRect == null)
        {
            Destroy(star);
            OnAnimationComplete(starIndex, true);
            return;
        }
        
        // Start at progress text position
        Vector2 startLocalPos = GetLocalPosition(progressText.transform.position);
        Vector2 targetLocalPos = GetLocalPosition(targetWorldPos);
        
        // Set initial position and scale
        starRect.anchoredPosition = startLocalPos;
        starRect.localScale = Vector3.one * startScale;
        
        // Play SFX at animation start with combo pitch
        if (starEarnedSFX != null)
        {
            SFXManager.Play(starEarnedSFX, starIndex);
        }
        
        // Create animation sequence
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(starRect.DOAnchorPos(targetLocalPos, flyDuration).SetEase(flyEase));
        sequence.Join(starRect.DOScale(endScale, flyDuration).SetEase(Ease.OutBack));
        sequence.Join(starRect.DORotate(new Vector3(0, 0, rotationAmount), flyDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));
        
        sequence.OnComplete(() =>
        {
            Destroy(star);
            OnAnimationComplete(starIndex, true);
        });
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarProgressUI] Earning star {starIndex} flying to display");
        }
    }
    
    // Delays the Classic mode lose animation spawn.
    private void SpawnClassicLosingStarWithDelay(int starIndex, Vector3 starWorldPos, float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            SpawnClassicLosingStar(starIndex, starWorldPos);
        });
    }
    
    // Spawns earned star at indicator, grows it, swaps indicator sprite, then spins/moves/fades.
    private void SpawnClassicLosingStar(int starIndex, Vector3 starWorldPos)
    {
        if (flyingStarPrefab == null || flyingStarParent == null)
        {
            starIndicator?.OnStarAnimationComplete(starIndex, false);
            return;
        }
        
        GameObject star = Instantiate(flyingStarPrefab, flyingStarParent);
        RectTransform starRect = star.GetComponent<RectTransform>();
        Image starImage = star.GetComponent<Image>();
        
        if (starRect == null)
        {
            Destroy(star);
            starIndicator?.OnStarAnimationComplete(starIndex, false);
            return;
        }
        
        // Match source star size
        RectTransform sourceStarRect = starIndicator.GetStarRectTransform(starIndex);
        if (sourceStarRect != null)
        {
            starRect.sizeDelta = sourceStarRect.sizeDelta;
        }
        
        // Position at the star indicator
        Vector2 localPos = GetLocalPosition(starWorldPos);
        starRect.anchoredPosition = localPos;
        starRect.localScale = Vector3.one;
        starRect.localRotation = Quaternion.identity;
        
        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = star.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = star.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        
        Sequence sequence = DOTween.Sequence();
        
        // Phase 1: Grow to max size
        sequence.Append(starRect.DOScale(classicGrowScale, classicGrowDuration).SetEase(classicGrowEase));
        
        // At max size, swap the indicator sprite to empty
        sequence.AppendCallback(() =>
        {
            starIndicator?.OnStarAnimationComplete(starIndex, false);
        });
        
        // Phase 2: Spin, move right, and fade out
        sequence.Append(starRect.DOAnchorPosX(localPos.x + classicMoveDistance, classicFadeDuration).SetEase(Ease.OutQuad));
        sequence.Join(starRect.DOLocalRotate(new Vector3(0, 0, classicSpinAmount), classicFadeDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        sequence.Join(starRect.DOScale(0f, classicFadeDuration).SetEase(classicFadeEase));
        sequence.Join(canvasGroup.DOFade(0f, classicFadeDuration).SetEase(classicFadeEase));
        
        sequence.OnComplete(() =>
        {
            Destroy(star);
            pendingAnimations = Mathf.Max(0, pendingAnimations - 1);
            
            if (starLostSFX != null)
            {
                SFXManager.Play(starLostSFX);
            }
        });
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarProgressUI] Classic lose - star {starIndex} growing then flying away");
        }
    }
    
    // Spawns a star that flies FROM the star display and fades out.
    private void SpawnLosingStarWithDelay(int starIndex, Vector3 sourceWorldPos, float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            SpawnLosingStar(starIndex, sourceWorldPos);
        });
    }
    
    private void SpawnLosingStar(int starIndex, Vector3 targetWorldPos)
    {
        if (flyingStarPrefab == null || flyingStarParent == null)
        {
            starIndicator?.OnStarAnimationComplete(starIndex, false);
            return;
        }
        
        GameObject star = Instantiate(flyingStarPrefab, flyingStarParent);
        RectTransform starRect = star.GetComponent<RectTransform>();
        Image starImage = star.GetComponent<Image>();
        
        if (starRect == null)
        {
            Destroy(star);
            starIndicator?.OnStarAnimationComplete(starIndex, false);
            return;
        }
        
        // Use unearned star sprite if available
        if (starImage != null && starIndicator != null && starIndicator.starEmpty != null)
        {
            starImage.sprite = starIndicator.starEmpty;
        }
        
        // Start at progress text position (same as earning)
        Vector2 startLocalPos = GetLocalPosition(progressText.transform.position);
        // End at star indicator position (same as earning)
        Vector2 targetLocalPos = GetLocalPosition(targetWorldPos);
        
        starRect.anchoredPosition = startLocalPos;
        starRect.localScale = Vector3.one * startScale;
        
        // Create animation sequence - same as earning
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(starRect.DOAnchorPos(targetLocalPos, flyDuration).SetEase(flyEase));
        sequence.Join(starRect.DOScale(endScale, flyDuration).SetEase(Ease.OutBack));
        sequence.Join(starRect.DORotate(new Vector3(0, 0, rotationAmount), flyDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));
        
        sequence.OnComplete(() =>
        {
            Destroy(star);
            pendingAnimations = Mathf.Max(0, pendingAnimations - 1);
            
            // Update indicator when animation completes
            starIndicator?.OnStarAnimationComplete(starIndex, false);
            
            if (starLostSFX != null)
            {
                SFXManager.Play(starLostSFX);
            }
        });
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarProgressUI] Losing star {starIndex} flying to indicator");
        }
    }
    
    private void OnAnimationComplete(int starIndex, bool wasEarned)
    {
        pendingAnimations = Mathf.Max(0, pendingAnimations - 1);
        
        if (wasEarned)
        {
            starIndicator?.OnStarAnimationComplete(starIndex, wasEarned);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarProgressUI] Animation complete for star {starIndex}");
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
    
    public int GetCurrentStarTarget()
    {
        return currentStarTarget;
    }
}