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
    // Add more as needed for your bubble types
    
    [Header("Or Use Colors")]
    public bool useColors = true; // If true, changes image color instead of sprite
    public Color redColor = Color.red;
    public Color blueColor = Color.blue;
    public Color greenColor = Color.green;
    public Color yellowColor = Color.yellow;
    public Color purpleColor = new Color(0.5f, 0f, 0.5f);
    
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
        
        if (useColors)
        {
            bubbleImage.color = GetColorForType(type);
        }
        else
        {
            bubbleImage.sprite = GetSpriteForType(type);
        }
    }
    
    // Returns the color for a bubble type.
    Color GetColorForType(BubbleType type)
    {
        switch (type)
        {
            case BubbleType.Red: return redColor;
            case BubbleType.Blue: return blueColor;
            case BubbleType.Green: return greenColor;
            case BubbleType.Yellow: return yellowColor;
            case BubbleType.Purple: return purpleColor;
            default: return Color.white;
        }
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