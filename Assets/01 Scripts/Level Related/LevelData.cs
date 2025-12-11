using System;
using UnityEngine;

[Serializable]
public class LevelData
{
    public int levelNumber;
    public int starsEarned; // 0-3 stars
    public bool isCompleted;
    
    public LevelData(int level)
    {
        levelNumber = level;
        starsEarned = 0;
        isCompleted = false;
    }
}