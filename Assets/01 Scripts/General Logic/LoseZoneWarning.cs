using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoseZoneWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid grid;
    [SerializeField] private GridStartHeightLimit heightLimit;
    [SerializeField] private Image warningImage;

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
