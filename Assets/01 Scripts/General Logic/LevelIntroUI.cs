using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;

public class LevelIntroUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject introPanel;
    public RectTransform popupBox;
    public Image levelIconImage;
    public TMP_Text levelNameText;
    public TMP_Text winConditionText;
    
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
    public bool enableDebugLogs = false;
    
    private bool isDismissed = false;
    private float showTimer = 0f;
    private LevelConfig currentConfig;
    private CanvasGroup canvasGroup;
    private bool isInitialized = false;
    
    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
    }

    void Start()
    {
        if (useFade && popupBox != null)
        {
            canvasGroup = popupBox.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popupBox.gameObject.AddComponent<CanvasGroup>();
        }
        
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        if (startButton != null)
            startButton.onClick.AddListener(DismissIntro);
        
        TryLoadLevelInfo();
        
        if (!isInitialized)
        {
            Log("[LevelIntroUI] Waiting for LevelLoader...");
            Invoke(nameof(RetryLoadLevelInfo), 0.1f);
        }
        
        ShowPopup();
    }
    
    void OnDestroy()
    {
        ThemeManager.OnThemeChanged -= OnThemeChanged;
        if (popupBox != null) popupBox.DOKill();
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
            if (WasTapOrClickPressed())
            {
                DismissIntro();
            }
        }
    }
    
    // Checks for tap or click using new Input System.
    private bool WasTapOrClickPressed()
    {
        // Check mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        
        // Check touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;
        
        return false;
    }
    
    private void Log(string message)
    {
        if (enableDebugLogs) Debug.Log(message);
    }
    
    private void RetryLoadLevelInfo()
    {
        if (isInitialized) return;
        
        Log("[LevelIntroUI] Retrying level info load...");
        TryLoadLevelInfo();
        
        if (!isInitialized)
            Debug.LogWarning("[LevelIntroUI] Failed to load level info after retry!");
    }
    
    private void TryLoadLevelInfo()
    {
        Log("[LevelIntroUI] TryLoadLevelInfo called");
        
        if (LevelLoader.Instance == null)
        {
            Log("[LevelIntroUI] LevelLoader.Instance is NULL");
            return;
        }
        
        if (LevelLoader.Instance.CurrentLevel == null)
        {
            Log("[LevelIntroUI] LevelLoader.CurrentLevel is NULL");
            return;
        }
        
        currentConfig = LevelLoader.Instance.CurrentLevel;
        Log($"[LevelIntroUI] Loaded config: {currentConfig.levelName} (Level {currentConfig.levelNumber})");
        
        if (levelNameText != null)
        {
            levelNameText.text = currentConfig.levelName;
            Log($"[LevelIntroUI] Set level name text: {currentConfig.levelName}");
        }
        
        if (winConditionText != null)
        {
            winConditionText.text = GetWinConditionDescription(currentConfig);
            Log("[LevelIntroUI] Set win condition text");
        }
        
        ThemeMode currentTheme = ThemeManager.Instance != null 
            ? ThemeManager.Instance.CurrentTheme 
            : ThemeMode.Day;
        
        Log($"[LevelIntroUI] Current theme: {currentTheme}");
        UpdateIcon(currentTheme);
        
        isInitialized = true;
        Log("[LevelIntroUI] Initialization complete!");
    }
    
    private void OnThemeChanged(ThemeMode newTheme)
    {
        Log($"[LevelIntroUI] Theme changed to: {newTheme}");
        UpdateIcon(newTheme);
    }
    
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
            Log("[LevelIntroUI] currentConfig.levelIcon is NULL - not assigned in LevelConfig!");
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
        return config.winCondition switch
        {
            WinConditionType.ClearAllBubbles => "Clear all bubbles!",
            WinConditionType.ReachTargetScore => $"Reach {config.targetScore} points!",
            WinConditionType.Survival => "Survive as long as you can!",
            _ => ""
        };
    }
    
    private void ShowPopup()
    {
        if (introPanel != null) introPanel.SetActive(true);
        if (popupBox == null) return;
        
        popupBox.DOKill();
        
        if (useScale) popupBox.localScale = Vector3.one * startScale;
        if (useFade && canvasGroup != null) canvasGroup.alpha = 0f;
        
        Sequence showSequence = DOTween.Sequence();
        
        if (useScale)
            showSequence.Join(popupBox.DOScale(1f, showDuration).SetEase(showEase));
        
        if (useFade && canvasGroup != null)
            showSequence.Join(canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
        
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
            hideSequence.Join(popupBox.DOScale(startScale, hideDuration).SetEase(hideEase));
        
        if (useFade && canvasGroup != null)
            hideSequence.Join(canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
        
        hideSequence.SetUpdate(true);
        hideSequence.OnComplete(OnHideComplete);
    }
    
    private void OnHideComplete()
    {
        if (introPanel != null) introPanel.SetActive(false);
        GameManager.Instance?.StartGame();
    }
}