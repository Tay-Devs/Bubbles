using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoseZoneWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid grid;
    [SerializeField] private GridStartHeightLimit heightLimit;
    [SerializeField] private Image warningImage;

    [Header("Themed Warning Objects")]
    [SerializeField] private GameObject dayWarningObject;
    [SerializeField] private GameObject nightWarningObject;
    [SerializeField] private Animator dayAnimator;
    [SerializeField] private Animator nightAnimator;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool isWarningActive = false;
    private Coroutine fadeRoutine;

    void Start()
    {
        if (grid == null)
            grid = FindFirstObjectByType<HexGrid>();

        if (heightLimit == null)
            heightLimit = FindFirstObjectByType<GridStartHeightLimit>();

        if (grid != null)
        {
            grid.onColorsChanged += CheckBubblePositions;

            if (grid.RowSystem != null)
                grid.RowSystem.onRowSpawned += CheckBubblePositions;
        }

        PlayerController.onBubbleConnected += CheckBubblePositions;

        // Subscribe to theme changes
        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        if (ThemeManager.Instance != null)
        {
            UpdateThemedObjects(ThemeManager.Instance.IsDay());
        }
        else
        {
            // Default to day mode if no ThemeManager
            UpdateThemedObjects(true);
        }

        // Initial hidden state
        if (warningImage != null)
        {
            Color c = warningImage.color;
            c.a = 0f;
            warningImage.color = c;
            warningImage.raycastTarget = false;
        }
    }

    void OnDestroy()
    {
        if (grid != null)
        {
            grid.onColorsChanged -= CheckBubblePositions;

            if (grid.RowSystem != null)
                grid.RowSystem.onRowSpawned -= CheckBubblePositions;
        }

        PlayerController.onBubbleConnected -= CheckBubblePositions;

        ThemeManager.OnThemeChanged -= OnThemeChanged;
    }

    // Called when theme changes. Syncs animator state and swaps visible object.
    private void OnThemeChanged(ThemeMode themeMode)
    {
        bool isDayMode = themeMode == ThemeMode.Day;
        UpdateThemedObjects(isDayMode);

        if (enableDebugLogs)
        {
            Debug.Log($"[LoseZoneWarning] Theme changed to {themeMode}");
        }
    }

    // Enables the correct themed object and syncs animator playback.
    private void UpdateThemedObjects(bool isDayMode)
    {
        if (dayWarningObject == null || nightWarningObject == null)
            return;

        // Get current animator state before switching
        float normalizedTime = 0f;
        int stateHash = 0;
        
        Animator activeAnimator = isDayMode ? nightAnimator : dayAnimator;
        Animator targetAnimator = isDayMode ? dayAnimator : nightAnimator;

        // Capture current playback position from the previously active animator
        if (activeAnimator != null && activeAnimator.gameObject.activeSelf)
        {
            AnimatorStateInfo stateInfo = activeAnimator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = stateInfo.normalizedTime;
            stateHash = stateInfo.fullPathHash;
        }

        // Swap active objects
        dayWarningObject.SetActive(isDayMode);
        nightWarningObject.SetActive(!isDayMode);

        // Sync the new animator to the same frame
        if (targetAnimator != null && stateHash != 0)
        {
            targetAnimator.Play(stateHash, 0, normalizedTime);
        }
    }

    private void CheckBubblePositions()
    {
        if (grid == null || heightLimit == null || warningImage == null)
            return;

        bool anyBubbleBelowLimit = false;

        foreach (var row in grid.GetGridData())
        {
            foreach (var bubble in row)
            {
                if (bubble != null && heightLimit.IsBelowLimit(bubble.transform.position))
                {
                    anyBubbleBelowLimit = true;
                    break;
                }
            }

            if (anyBubbleBelowLimit)
                break;
        }

        if (anyBubbleBelowLimit != isWarningActive)
        {
            isWarningActive = anyBubbleBelowLimit;
            FadeWarning(isWarningActive);

            if (enableDebugLogs)
            {
                Debug.Log($"[LoseZoneWarning] Warning {(isWarningActive ? "FADING IN" : "FADING OUT")}");
            }
        }
    }

    private void FadeWarning(bool show)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeImage(show));
    }

    private IEnumerator FadeImage(bool show)
    {
        Color color = warningImage.color;
        float startAlpha = color.a;
        float targetAlpha = show ? 1f : 0f;
        float time = 0f;

        if (show)
            warningImage.raycastTarget = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            warningImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        warningImage.color = new Color(color.r, color.g, color.b, targetAlpha);

        if (!show)
            warningImage.raycastTarget = false;
    }
}