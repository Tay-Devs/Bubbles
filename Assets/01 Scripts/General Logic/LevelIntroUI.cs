using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LevelIntroUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject introPanel;
    public RectTransform popupBox;
    public Image levelIconImage;
    public TMP_Text levelNameText; // Optional - if you have separate text
    public TMP_Text winConditionText; // Optional - if you have separate text
    
    [Header("Dismiss Settings")]
    public Button startButton;
    public bool tapAnywhereToDismiss = true;
    public float autoDismissTime = 0f;
    
    [Header("Animation Settings")]
    public float showDuration = 0.4f;
    public float hideDuration = 0.3f;
    public Ease showEase = Ease.OutBack;
    public Ease hideEase = Ease.InBack;
    
    [Header("Animation Type")]
    public bool useScale = true;
    public bool useFade = true;
    public float startScale = 0.5f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private bool isDismissed = false;
    private float showTimer = 0f;
    private LevelConfig currentConfig;
    private CanvasGroup canvasGroup;
    private bool isInitialized = false;
    
    void Start()
    {
        // Get or add CanvasGroup for fade
        if (useFade && popupBox != null)
        {
            canvasGroup = popupBox.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = popupBox.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        // Setup button
        if (startButton != null)
        {
            startButton.onClick.AddListener(DismissIntro);
        }
        
        // Try to load level info immediately
        TryLoadLevelInfo();
        
        // If not loaded, try again next frame (in case of execution order issues)
        if (!isInitialized)
        {
            Log("[LevelIntroUI] Waiting for LevelLoader...");
            Invoke(nameof(RetryLoadLevelInfo), 0.1f);
        }
        
        // Show panel with animation
        ShowPopup();
    }
    
    void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= OnThemeChanged;
        
        if (popupBox != null)
        {
            popupBox.DOKill();
        }
    }
    
    void Update()
    {
        if (isDismissed) return;
        
        showTimer += Time.deltaTime;
        
        if (autoDismissTime > 0 && showTimer >= autoDismissTime)
        {
            DismissIntro();
            return;
        }
        
        if (tapAnywhereToDismiss && showTimer > 0.5f)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                DismissIntro();
            }
        }
    }
    
    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }
    
    // Retry loading if first attempt failed.
    private void RetryLoadLevelInfo()
    {
        if (isInitialized) return;
        
        Log("[LevelIntroUI] Retrying level info load...");
        TryLoadLevelInfo();
        
        if (!isInitialized)
        {
            Debug.LogWarning("[LevelIntroUI] Failed to load level info after retry!");
        }
    }
    
    // Attempts to load level info from LevelLoader.
    private void TryLoadLevelInfo()
    {
        Log("[LevelIntroUI] TryLoadLevelInfo called");
        
        // Check LevelLoader
        if (LevelLoader.Instance == null)
        {
            Log("[LevelIntroUI] LevelLoader.Instance is NULL");
            return;
        }
        
        Log("[LevelIntroUI] LevelLoader.Instance found");
        
        // Check CurrentLevel
        if (LevelLoader.Instance.CurrentLevel == null)
        {
            Log("[LevelIntroUI] LevelLoader.CurrentLevel is NULL");
            return;
        }
        
        currentConfig = LevelLoader.Instance.CurrentLevel;
        Log($"[LevelIntroUI] Loaded config: {currentConfig.levelName} (Level {currentConfig.levelNumber})");
        
        // Set level name text if available
        if (levelNameText != null)
        {
            levelNameText.text = currentConfig.levelName;
            Log($"[LevelIntroUI] Set level name text: {currentConfig.levelName}");
        }
        
        // Set win condition text if available
        if (winConditionText != null)
        {
            winConditionText.text = GetWinConditionDescription(currentConfig);
            Log($"[LevelIntroUI] Set win condition text");
        }
        
        // Set level icon
        ThemeMode currentTheme = ThemeMode.Day;
        if (ThemeManager.Instance != null)
        {
            currentTheme = ThemeManager.Instance.CurrentTheme;
            Log($"[LevelIntroUI] Current theme: {currentTheme}");
        }
        else
        {
            Log("[LevelIntroUI] ThemeManager.Instance is NULL, using Day theme");
        }
        
        UpdateIcon(currentTheme);
        
        isInitialized = true;
        Log("[LevelIntroUI] Initialization complete!");
    }
    
    // Called when theme changes.
    private void OnThemeChanged(ThemeMode newTheme)
    {
        Log($"[LevelIntroUI] Theme changed to: {newTheme}");
        UpdateIcon(newTheme);
    }
    
    // Updates the icon based on current theme.
    private void UpdateIcon(ThemeMode theme)
    {
        Log($"[LevelIntroUI] UpdateIcon called with theme: {theme}");
        
        if (levelIconImage == null)
        {
            Log("[LevelIntroUI] levelIconImage is NULL - not assigned in Inspector!");
            return;
        }
        
        if (currentConfig == null)
        {
            Log("[LevelIntroUI] currentConfig is NULL");
            return;
        }
        
        if (currentConfig.levelIcon == null)
        {
            Log("[LevelIntroUI] currentConfig.levelIcon (ThemeSprite) is NULL - not assigned in LevelConfig!");
            levelIconImage.gameObject.SetActive(false);
            return;
        }
        
        Sprite iconSprite = currentConfig.levelIcon.GetSprite(theme);
        
        if (iconSprite == null)
        {
            Log($"[LevelIntroUI] GetSprite returned NULL for theme {theme}");
            levelIconImage.gameObject.SetActive(false);
            return;
        }
        
        Log($"[LevelIntroUI] Setting icon sprite: {iconSprite.name}");
        levelIconImage.sprite = iconSprite;
        levelIconImage.gameObject.SetActive(true);
    }
    
    private string GetWinConditionDescription(LevelConfig config)
    {
        switch (config.winCondition)
        {
            case WinConditionType.ClearAllBubbles:
                return "Clear all bubbles!";
            case WinConditionType.ReachTargetScore:
                return $"Reach {config.targetScore} points!";
            case WinConditionType.Survival:
                return "Survive as long as you can!";
            default:
                return "";
        }
    }
    
    private void ShowPopup()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(true);
        }
        
        if (popupBox == null) return;
        
        popupBox.DOKill();
        
        if (useScale)
        {
            popupBox.localScale = Vector3.one * startScale;
        }
        
        if (useFade && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        Sequence showSequence = DOTween.Sequence();
        
        if (useScale)
        {
            showSequence.Join(popupBox.DOScale(1f, showDuration).SetEase(showEase));
        }
        
        if (useFade && canvasGroup != null)
        {
            showSequence.Join(canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
        }
        
        showSequence.SetUpdate(true);
    }
    
    public void DismissIntro()
    {
        if (isDismissed) return;
        isDismissed = true;
        
        Log("[LevelIntroUI] Intro dismissed - starting game");
        
        HidePopup();
    }
    
    private void HidePopup()
    {
        if (popupBox == null)
        {
            OnHideComplete();
            return;
        }
        
        popupBox.DOKill();
        
        Sequence hideSequence = DOTween.Sequence();
        
        if (useScale)
        {
            hideSequence.Join(popupBox.DOScale(startScale, hideDuration).SetEase(hideEase));
        }
        
        if (useFade && canvasGroup != null)
        {
            hideSequence.Join(canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
        }
        
        hideSequence.SetUpdate(true);
        hideSequence.OnComplete(OnHideComplete);
    }
    
    private void OnHideComplete()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }
}