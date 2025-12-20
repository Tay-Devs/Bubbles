using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Bubble Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public LevelConfig[] levels;
    
    // Returns level config by level number (1-indexed).
    public LevelConfig GetLevel(int levelNumber)
    {
        if (levelNumber > 0 && levelNumber <= levels.Length)
        {
            return levels[levelNumber - 1];
        }
        return null;
    }
    
    // Returns total number of levels.
    public int LevelCount => levels != null ? levels.Length : 0;
}