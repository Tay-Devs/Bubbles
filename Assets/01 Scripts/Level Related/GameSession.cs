using UnityEngine;

[CreateAssetMenu(fileName = "GameSession", menuName = "Bubble Game/Game Session")]
public class GameSession : ScriptableObject
{
    [Header("Selected Level")]
    public LevelConfig selectedLevel;
    
    [Header("Results (set by game scene)")]
    public bool hasResults;
    public bool levelWon;
    public int starsEarned;
    public int finalScore;
    public float completionTime;
    public int rowsSurvived;
    
    // Clears results before starting a new game.
    public void ClearResults()
    {
        hasResults = false;
        levelWon = false;
        starsEarned = 0;
        finalScore = 0;
        completionTime = 0f;
        rowsSurvived = 0;
    }
    
    // Sets the selected level and clears previous results.
    public void SelectLevel(LevelConfig level)
    {
        selectedLevel = level;
        ClearResults();
        Debug.Log($"[GameSession] Selected level {level.levelNumber}: {level.levelName}");
    }
    
    // Records results after game ends.
    public void SetResults(bool won, int stars, int score, float time, int rows)
    {
        hasResults = true;
        levelWon = won;
        starsEarned = stars;
        finalScore = score;
        completionTime = time;
        rowsSurvived = rows;
        
        Debug.Log($"[GameSession] Results - Won: {won}, Stars: {stars}, Score: {score}, Time: {time:F1}s, Rows: {rows}");
    }
}