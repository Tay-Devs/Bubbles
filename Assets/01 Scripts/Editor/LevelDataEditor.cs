using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    // Level Info
    SerializedProperty levelName;
    SerializedProperty levelDescription;
    SerializedProperty levelIcon;
    
    // Win Condition
    SerializedProperty winCondition;
    
    // ReachScore settings
    SerializedProperty targetScore;
    
    // SurvivalClear settings
    SerializedProperty rowSpawnInterval;
    SerializedProperty accelerateOverTime;
    SerializedProperty minSpawnInterval;
    SerializedProperty accelerationRate;
    
    // Star requirements - Time based
    SerializedProperty star1Minutes;
    SerializedProperty star1Seconds;
    SerializedProperty star2Minutes;
    SerializedProperty star2Seconds;
    SerializedProperty star3Minutes;
    SerializedProperty star3Seconds;
    
    // Star requirements - Bubble count
    SerializedProperty star1MaxBubbles;
    SerializedProperty star2MaxBubbles;
    SerializedProperty star3MaxBubbles;
    
    // Grid settings
    SerializedProperty gridWidth;
    SerializedProperty startingRows;
    SerializedProperty shotsBeforeNewRow;
    SerializedProperty allowedBubbleTypes;
    SerializedProperty difficultyRating;

    void OnEnable()
    {
        levelName = serializedObject.FindProperty("levelName");
        levelDescription = serializedObject.FindProperty("levelDescription");
        levelIcon = serializedObject.FindProperty("levelIcon");
        
        winCondition = serializedObject.FindProperty("winCondition");
        
        targetScore = serializedObject.FindProperty("targetScore");
        
        rowSpawnInterval = serializedObject.FindProperty("rowSpawnInterval");
        accelerateOverTime = serializedObject.FindProperty("accelerateOverTime");
        minSpawnInterval = serializedObject.FindProperty("minSpawnInterval");
        accelerationRate = serializedObject.FindProperty("accelerationRate");
        
        star1Minutes = serializedObject.FindProperty("star1Minutes");
        star1Seconds = serializedObject.FindProperty("star1Seconds");
        star2Minutes = serializedObject.FindProperty("star2Minutes");
        star2Seconds = serializedObject.FindProperty("star2Seconds");
        star3Minutes = serializedObject.FindProperty("star3Minutes");
        star3Seconds = serializedObject.FindProperty("star3Seconds");
        
        star1MaxBubbles = serializedObject.FindProperty("star1MaxBubbles");
        star2MaxBubbles = serializedObject.FindProperty("star2MaxBubbles");
        star3MaxBubbles = serializedObject.FindProperty("star3MaxBubbles");
        
        gridWidth = serializedObject.FindProperty("gridWidth");
        startingRows = serializedObject.FindProperty("startingRows");
        shotsBeforeNewRow = serializedObject.FindProperty("shotsBeforeNewRow");
        allowedBubbleTypes = serializedObject.FindProperty("allowedBubbleTypes");
        difficultyRating = serializedObject.FindProperty("difficultyRating");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        WinConditionType currentWinCondition = (WinConditionType)winCondition.enumValueIndex;
        
        // === Level Info ===
        EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(levelName);
        EditorGUILayout.PropertyField(levelDescription);
        EditorGUILayout.PropertyField(levelIcon);
        
        EditorGUILayout.Space(10);
        
        // === Win Condition ===
        EditorGUILayout.LabelField("Win Condition", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(winCondition);
        
        EditorGUILayout.Space(5);
        
        // Show settings based on win condition type
        switch (currentWinCondition)
        {
            case WinConditionType.ClearAllBubbles:
                DrawClearAllBubblesSettings();
                break;
                
            case WinConditionType.ReachScore:
                DrawReachScoreSettings();
                break;
                
            case WinConditionType.SurvivalClear:
                DrawSurvivalClearSettings();
                break;
        }
        
        EditorGUILayout.Space(10);
        
        // === Grid Settings ===
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(gridWidth);
        EditorGUILayout.PropertyField(startingRows);
        
        if (currentWinCondition != WinConditionType.SurvivalClear)
        {
            EditorGUILayout.PropertyField(shotsBeforeNewRow);
        }
        
        EditorGUILayout.Space(10);
        
        // === Bubble Colors ===
        EditorGUILayout.LabelField("Bubble Colors", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(allowedBubbleTypes, true);
        
        EditorGUILayout.Space(10);
        
        // === Difficulty ===
        EditorGUILayout.LabelField("Difficulty", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(difficultyRating);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawClearAllBubblesSettings()
    {
        EditorGUILayout.HelpBox("Win by clearing all bubbles from the grid.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Star Requirements (Time)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Complete the level within the time limit to earn stars.", MessageType.None);
        
        EditorGUILayout.Space(5);
        
        DrawTimeStarRow("★ 1 Star", star1Minutes, star1Seconds);
        DrawTimeStarRow("★★ 2 Stars", star2Minutes, star2Seconds);
        DrawTimeStarRow("★★★ 3 Stars", star3Minutes, star3Seconds);
    }
    
    private void DrawReachScoreSettings()
    {
        EditorGUILayout.HelpBox("Win by reaching the target score.", MessageType.Info);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(targetScore);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Star Requirements (Bubble Count)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Reach the target score using fewer bubbles to earn more stars.", MessageType.None);
        
        EditorGUILayout.Space(5);
        
        DrawBubbleStarRow("★ 1 Star", star1MaxBubbles);
        DrawBubbleStarRow("★★ 2 Stars", star2MaxBubbles);
        DrawBubbleStarRow("★★★ 3 Stars", star3MaxBubbles);
    }
    
    private void DrawSurvivalClearSettings()
    {
        EditorGUILayout.HelpBox("New rows spawn on a timer. Clear all bubbles to win.", MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Row Spawn Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rowSpawnInterval, new GUIContent("Spawn Interval (sec)"));
        EditorGUILayout.PropertyField(accelerateOverTime);
        
        if (accelerateOverTime.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minSpawnInterval, new GUIContent("Min Interval"));
            EditorGUILayout.PropertyField(accelerationRate, new GUIContent("Acceleration Rate"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("Star Requirements (Time)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Complete the level within the time limit to earn stars.", MessageType.None);
        
        EditorGUILayout.Space(5);
        
        DrawTimeStarRow("★ 1 Star", star1Minutes, star1Seconds);
        DrawTimeStarRow("★★ 2 Stars", star2Minutes, star2Seconds);
        DrawTimeStarRow("★★★ 3 Stars", star3Minutes, star3Seconds);
    }
    
    // Draws a single time-based star requirement row with proper layout
    private void DrawTimeStarRow(string label, SerializedProperty minutes, SerializedProperty seconds)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        
        GUILayout.FlexibleSpace();
        
        minutes.intValue = EditorGUILayout.IntField(minutes.intValue, GUILayout.Width(40));
        EditorGUILayout.LabelField("min", GUILayout.Width(30));
        
        seconds.intValue = EditorGUILayout.IntField(seconds.intValue, GUILayout.Width(40));
        EditorGUILayout.LabelField("sec", GUILayout.Width(30));
        
        EditorGUILayout.EndHorizontal();
    }
    
    // Draws a single bubble count star requirement row with proper layout
    private void DrawBubbleStarRow(string label, SerializedProperty bubbles)
    {
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        
        GUILayout.FlexibleSpace();
        
        bubbles.intValue = EditorGUILayout.IntField(bubbles.intValue, GUILayout.Width(60));
        EditorGUILayout.LabelField("bubbles or less", GUILayout.Width(100));
        
        EditorGUILayout.EndHorizontal();
    }
}