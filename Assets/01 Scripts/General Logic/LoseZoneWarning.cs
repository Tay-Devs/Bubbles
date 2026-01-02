using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoseZoneWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid grid;
    [SerializeField] private GridStartHeightLimit heightLimit;

    [Header("Themed Warning Objects")]
    [SerializeField] private GameObject dayWarningObject;
    [SerializeField] private GameObject nightWarningObject;
    [SerializeField] private Animator dayAnimator;
    [SerializeField] private Animator nightAnimator;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private Image dayWarningImage;
    private Image nightWarningImage;
    private bool isWarningActive = false;
    private Coroutine fadeRoutine;
    private float currentAlpha = 0f;

    void Start()
    {
        if (grid == null)
            grid = FindFirstObjectByType<HexGrid>();

        if (heightLimit == null)
            heightLimit = FindFirstObjectByType<GridStartHeightLimit>();

        // Cache both Image components from the themed objects
        if (dayWarningObject != null)
            dayWarningImage = dayWarningObject.GetComponent<Image>();
        
        if (nightWarningObject != null)
            nightWarningImage = nightWarningObject.GetComponent<Image>();

        if (grid != null)
        {
            grid.onColorsChanged += CheckBubblePositions;

            if (grid.RowSystem != null)
                grid.RowSystem.onRowSpawned += CheckBubblePositions;
        }

        PlayerController.onBubbleConnected += CheckBubblePositions;

        ThemeManager.OnThemeChanged += OnThemeChanged;
        
        if (ThemeManager.Instance != null)
        {
            UpdateThemedObjects(ThemeManager.Instance.IsDay());
        }
        else
        {
            UpdateThemedObjects(true);
        }

        // Initialize both images to hidden state
        SetImageAlpha(dayWarningImage, 0f);
        SetImageAlpha(nightWarningImage, 0f);
        currentAlpha = 0f;
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

    // Called when theme changes. Syncs alpha to the newly active image and swaps visible object.
    private void OnThemeChanged(ThemeMode themeMode)
    {
        bool isDayMode = themeMode == ThemeMode.Day;
        UpdateThemedObjects(isDayMode);

        // Sync alpha to the newly active image so it matches the current fade state
        Image activeImage = isDayMode ? dayWarningImage : nightWarningImage;
        SetImageAlpha(activeImage, currentAlpha);

        if (enableDebugLogs)
        {
            Debug.Log($"[LoseZoneWarning] Theme changed to {themeMode}, synced alpha: {currentAlpha}");
        }
    }

    // Enables the correct themed object and syncs animator playback position.
    private void UpdateThemedObjects(bool isDayMode)
    {
        if (dayWarningObject == null || nightWarningObject == null)
            return;

        float normalizedTime = 0f;
        int stateHash = 0;
        
        Animator activeAnimator = isDayMode ? nightAnimator : dayAnimator;
        Animator targetAnimator = isDayMode ? dayAnimator : nightAnimator;

        if (activeAnimator != null && activeAnimator.gameObject.activeSelf)
        {
            AnimatorStateInfo stateInfo = activeAnimator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = stateInfo.normalizedTime;
            stateHash = stateInfo.fullPathHash;
        }

        dayWarningObject.SetActive(isDayMode);
        nightWarningObject.SetActive(!isDayMode);

        if (targetAnimator != null && stateHash != 0)
        {
            targetAnimator.Play(stateHash, 0, normalizedTime);
        }
    }

    // Sets the alpha and raycast state for an Image component.
    // Tracks currentAlpha so both themed images can stay synchronized.
    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        
        Color c = image.color;
        c.a = alpha;
        image.color = c;
        image.raycastTarget = alpha > 0f;
    }

    private void CheckBubblePositions()
    {
        if (grid == null || heightLimit == null)
            return;

        if (dayWarningImage == null && nightWarningImage == null)
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

        fadeRoutine = StartCoroutine(FadeImages(show));
    }

    // Fades the currently active themed image and updates currentAlpha.
    // Both images share the same alpha state so theme switching stays seamless.
    private IEnumerator FadeImages(bool show)
    {
        float startAlpha = currentAlpha;
        float targetAlpha = show ? 1f : 0f;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            
            // Only update the currently active image
            Image activeImage = GetActiveImage();
            SetImageAlpha(activeImage, currentAlpha);
            
            yield return null;
        }

        currentAlpha = targetAlpha;
        Image finalActiveImage = GetActiveImage();
        SetImageAlpha(finalActiveImage, currentAlpha);
    }

    // Returns the Image component of whichever themed object is currently active.
    // Falls back to day image if neither object is active.
    private Image GetActiveImage()
    {
        if (dayWarningObject != null && dayWarningObject.activeSelf)
            return dayWarningImage;
        
        if (nightWarningObject != null && nightWarningObject.activeSelf)
            return nightWarningImage;
        
        return dayWarningImage;
    }
}