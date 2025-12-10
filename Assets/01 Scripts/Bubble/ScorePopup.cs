using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScorePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float popInDuration = 0.15f;
    public float holdDuration = 0.5f;
    public float popOutDuration = 0.2f;
    public float overshootScale = 1.3f;
    public float floatUpDistance = 0.3f;
    
    [Header("Easing")]
    public Ease popInEase = Ease.OutBack;
    public Ease popOutEase = Ease.InBack;
    
    [Header("Combo Text Scaling")]
    public float startingFontSize = 3f;
    public float maxFontSize = 8f;
    public int stepsToMaxSize = 10;
    
    private TMP_Text textComponent;
    private Sequence animSequence;

    void Awake()
    {
        textComponent = GetComponentInChildren<TMP_Text>();
        
        if (textComponent != null)
        {
            textComponent.enableWordWrapping = false;
            textComponent.overflowMode = TextOverflowModes.Overflow;
        }
    }
    
    void OnDestroy()
    {
        animSequence?.Kill();
    }

    // Initializes the popup with score value and combo index, then plays animation.
    // ComboIndex determines text size scaling - higher index = bigger text.
    public void Show(int points, int comboIndex = 0)
    {
        if (textComponent != null)
        {
            textComponent.text = $"+{points}";
            ApplyComboScaling(comboIndex);
        }
        
        PlayPopAnimation();
    }
    
    // Scales font size from startingFontSize to maxFontSize based on combo index.
    // Linearly interpolates over stepsToMaxSize bubbles, then caps at max.
    private void ApplyComboScaling(int comboIndex)
    {
        if (!textComponent.enableAutoSizing) return;
        
        float progress = Mathf.Clamp01((float)comboIndex / stepsToMaxSize);
        float newSize = Mathf.Lerp(startingFontSize, maxFontSize, progress);
        
        textComponent.fontSizeMin = newSize;
    }

    // Creates a DOTween sequence: scale up with overshoot, hold, then scale down.
    // Adds a subtle float-up movement and fades out at the end.
    private void PlayPopAnimation()
    {
        transform.localScale = Vector3.zero;
        
        animSequence = DOTween.Sequence();
        
        animSequence.Append(
            transform.DOScale(overshootScale, popInDuration * 0.6f)
                .SetEase(Ease.OutQuad)
        );
        
        animSequence.Append(
            transform.DOScale(1f, popInDuration * 0.4f)
                .SetEase(Ease.OutBack)
        );
        
        animSequence.Join(
            transform.DOMove(transform.position + Vector3.up * floatUpDistance, holdDuration + popInDuration)
                .SetEase(Ease.OutQuad)
        );
        
        animSequence.AppendInterval(holdDuration);
        
        animSequence.Append(
            transform.DOScale(0f, popOutDuration)
                .SetEase(popOutEase)
        );
        
        if (textComponent != null)
        {
            animSequence.Join(
                textComponent.DOFade(0f, popOutDuration)
            );
        }
        
        animSequence.OnComplete(() => Destroy(gameObject));
    }

    // Static helper to spawn a popup at a world position with combo scaling.
    // ComboIndex determines text size - pass the bubble's index in the destruction sequence.
    public static ScorePopup Create(GameObject prefab, Vector3 position, int points, int comboIndex = 0)
    {
        if (prefab == null) return null;
        
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        ScorePopup popup = instance.GetComponent<ScorePopup>();
        
        if (popup != null)
        {
            popup.Show(points, comboIndex);
        }
        
        return popup;
    }
}