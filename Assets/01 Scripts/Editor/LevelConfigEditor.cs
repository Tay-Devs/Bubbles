using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelConfig))]
public class LevelConfigEditor : Editor
{
    // Level Info
    private SerializedProperty levelNumber;
    private SerializedProperty levelName;
    private SerializedProperty levelIcon; // Add this
    
    // Grid
    private SerializedProperty gridWidth;
    private SerializedProperty gridHeight;
    
    // Bubbles
    private SerializedProperty availableColors;
    
    // Win Condition
    private SerializedProperty winCondition;
    
    // Clear All
    private SerializedProperty threeStarTime;
    private SerializedProperty twoStarTime;
    private SerializedProperty oneStarTime;
    
    // Score
    private SerializedProperty targetScore;
    
    // Survival
    private SerializedProperty survivalStartingInterval;
    private SerializedProperty survivalIntervalDeduction;
    private SerializedProperty survivalMinInterval;
    private SerializedProperty oneStarRows;
    private SerializedProperty twoStarRows;
    private SerializedProperty threeStarRows;
    
    private void OnEnable()
    {
        // Level Info
        levelNumber = serializedObject.FindProperty("levelNumber");
        levelName = serializedObject.FindProperty("levelName");
        levelIcon = serializedObject.FindProperty("levelIcon");
        
        // Grid
        gridWidth = serializedObject.FindProperty("gridWidth");
        gridHeight = serializedObject.FindProperty("gridHeight");
        
        // Bubbles
        availableColors = serializedObject.FindProperty("availableColors");
        
        // Win Condition
        winCondition = serializedObject.FindProperty("winCondition");
        
        // Clear All
        threeStarTime = serializedObject.FindProperty("threeStarTime");
        twoStarTime = serializedObject.FindProperty("twoStarTime");
        oneStarTime = serializedObject.FindProperty("oneStarTime");
        
        // Score
        targetScore = serializedObject.FindProperty("targetScore");
        
        // Survival
        survivalStartingInterval = serializedObject.FindProperty("survivalStartingInterval");
        survivalIntervalDeduction = serializedObject.FindProperty("survivalIntervalDeduction");
        survivalMinInterval = serializedObject.FindProperty("survivalMinInterval");
        oneStarRows = serializedObject.FindProperty("oneStarRows");
        twoStarRows = serializedObject.FindProperty("twoStarRows");
        threeStarRows = serializedObject.FindProperty("threeStarRows");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Level Info Section
        EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelNumber);
        EditorGUILayout.PropertyField(levelName);
        EditorGUILayout.PropertyField(levelIcon, new GUIContent("Level Icon (Theme)"));
        
        EditorGUILayout.Space(10);
        
        // Grid Section
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gridWidth);
        EditorGUILayout.PropertyField(gridHeight);
        
        EditorGUILayout.Space(10);
        
        // Bubble Section
        EditorGUILayout.LabelField("Bubble Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(availableColors, true);
        
        EditorGUILayout.Space(10);
        
        // Win Condition Section
        EditorGUILayout.LabelField("Win Condition", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(winCondition);
        
        EditorGUILayout.Space(10);
        
        // Show relevant settings based on win condition
        WinConditionType condition = (WinConditionType)winCondition.enumValueIndex;
        
        switch (condition)
        {
            case WinConditionType.ClearAllBubbles:
                DrawClearAllSettings();
                break;
                
            case WinConditionType.ReachTargetScore:
                DrawScoreSettings();
                break;
                
            case WinConditionType.Survival:
                DrawSurvivalSettings();
                break;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawClearAllSettings()
    {
        EditorGUILayout.LabelField("Time-Based Stars", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("★★★ Three Stars", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(threeStarTime, new GUIContent("Complete Under (seconds)"));
        EditorGUILayout.LabelField($"= {threeStarTime.floatValue / 60f:F1} minutes", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("★★ Two Stars", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(twoStarTime, new GUIContent("Complete Under (seconds)"));
        EditorGUILayout.LabelField($"= {twoStarTime.floatValue / 60f:F1} minutes", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("★ One Star", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(oneStarTime, new GUIContent("Complete Under (seconds)"));
        EditorGUILayout.LabelField($"= {oneStarTime.floatValue / 60f:F1} minutes", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawScoreSettings()
    {
        EditorGUILayout.LabelField("Score-Based Stars", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.PropertyField(targetScore, new GUIContent("Target Score (3 Stars)"));
        
        EditorGUILayout.Space(5);
        
        int target = targetScore.intValue;
        int twoStar = Mathf.CeilToInt(target * 2f / 3f);
        int oneStar = Mathf.CeilToInt(target / 3f);
        
        EditorGUILayout.LabelField("Star Thresholds:", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"★★★ Three Stars: {target:N0} points", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"★★ Two Stars: {twoStar:N0} points", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"★ One Star: {oneStar:N0} points", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawSurvivalSettings()
    {
        EditorGUILayout.LabelField("Survival Settings", EditorStyles.boldLabel);
        
        // Row spawn timing
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Row Spawn Timing", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(survivalStartingInterval, new GUIContent("Starting Interval (sec)"));
        EditorGUILayout.PropertyField(survivalIntervalDeduction, new GUIContent("Deduction Per Row (sec)"));
        EditorGUILayout.PropertyField(survivalMinInterval, new GUIContent("Minimum Interval (sec)"));
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Star requirements
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Rows Survived For Stars", EditorStyles.miniLabel);
        
        EditorGUILayout.PropertyField(oneStarRows, new GUIContent("★ One Star (rows)"));
        EditorGUILayout.PropertyField(twoStarRows, new GUIContent("★★ Two Stars (rows)"));
        EditorGUILayout.PropertyField(threeStarRows, new GUIContent("★★★ Three Stars (rows)"));
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Note: Clearing all bubbles = automatic 3 stars", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }
}