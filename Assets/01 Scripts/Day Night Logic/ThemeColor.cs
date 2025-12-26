using UnityEngine;

[CreateAssetMenu(fileName = "ThemeColor", menuName = "Theme System/Theme Color")]
public class ThemeColor : ScriptableObject
{
    [Header("Color Variants")]
    public Color dayColor = Color.black;
    public Color nightColor = Color.white;
    
    // Returns the appropriate color based on the current theme mode.
    // Day theme returns dayColor, Night theme returns nightColor.
    public Color GetColor(ThemeMode theme)
    {
        return theme == ThemeMode.Day ? dayColor : nightColor;
    }
}