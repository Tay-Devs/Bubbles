using UnityEngine;

[CreateAssetMenu(fileName = "ThemeSprite", menuName = "Theme System/Theme Sprite")]
public class ThemeSprite : ScriptableObject
{
    [Header("Sprite Variants")]
    public Sprite daySprite;
    public Sprite nightSprite;
    
    public Sprite GetSprite(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? daySprite : nightSprite;
    }
}

// Enum for theme modes
public enum ThemeMode
{
    Day = 0,
    Night = 1
}