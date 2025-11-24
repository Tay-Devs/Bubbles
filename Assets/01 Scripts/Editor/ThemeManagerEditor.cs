using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ThemeManager))]
public class ThemeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ThemeManager manager = (ThemeManager)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // Add buttons for editor preview
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Preview Day Theme"))
        {
            manager.SetTheme(ThemeMode.Day);
            manager.UpdateAllThemeables();
        }
        
        if (GUILayout.Button("Preview Night Theme"))
        {
            manager.SetTheme(ThemeMode.Night);
            manager.UpdateAllThemeables();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Refresh All Themeable Objects"))
        {
            manager.UpdateAllThemeables();
        }
    }
}