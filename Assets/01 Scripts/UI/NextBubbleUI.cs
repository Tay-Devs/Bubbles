using UnityEngine;
using UnityEngine.UI;

public class NextBubbleUI : MonoBehaviour
{
    [Header("References")]
    public Image bubbleImage;
    
    [Header("Color Sprites")]
    public Sprite redSprite;
    public Sprite blueSprite;
    public Sprite greenSprite;
    public Sprite yellowSprite;
    public Sprite purpleSprite;
    
    void Start()
    {
        PlayerController.onNextBubbleChanged += OnNextBubbleChanged;
        
        // Get initial value
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            OnNextBubbleChanged(player.NextBubbleType);
        }
    }
    
    void OnDestroy()
    {
        PlayerController.onNextBubbleChanged -= OnNextBubbleChanged;
    }
    
    // Updates the UI to display the next bubble type.
    void OnNextBubbleChanged(BubbleType type)
    {
        if (bubbleImage == null) return;

        bubbleImage.sprite = GetSpriteForType(type);
     
    }
    
    
    // Returns the sprite for a bubble type.
    Sprite GetSpriteForType(BubbleType type)
    {
        switch (type)
        {
            case BubbleType.Red: return redSprite;
            case BubbleType.Blue: return blueSprite;
            case BubbleType.Green: return greenSprite;
            case BubbleType.Yellow: return yellowSprite;
            case BubbleType.Purple: return purpleSprite;
            default: return null;
        }
    }
}