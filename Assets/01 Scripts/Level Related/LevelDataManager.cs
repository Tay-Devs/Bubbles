using System.Collections.Generic;
using UnityEngine;

public class LevelDataManager : MonoBehaviour
{
    public static LevelDataManager Instance { get; private set; }
    
    [Header("Unlock Settings")]
    public int freeLevels = 3; // Levels 1-3 are free
    public int starsPerUnlock = 3; // Stars needed per level after free levels
    
    [Header("Debug")]
    public bool clearSaveOnStart = false;
    
    private Dictionary<int, LevelData> levelDataDict = new Dictionary<int, LevelData>();
    private const string SAVE_KEY = "LevelProgress";
    
    public int TotalStars { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (clearSaveOnStart)
        {
            ClearSave();
        }
        
        LoadProgress();
        CalculateTotalStars();
    }
    void Start()
    {
       
        // Add test data - remove this after testing!
        if (clearSaveOnStart)
        {
           
            ClearSave();
            // Simulate completing some levels
            CompleteLevel(1, 3);
            CompleteLevel(2, 2);
            CompleteLevel(3, 3);
            CompleteLevel(4, 1);
        }
    }
    // Returns the star requirement for a specific level.
    // First N levels are free, then increases by starsPerUnlock each level.
    public int GetStarsRequired(int levelNumber)
    {
        if (levelNumber <= freeLevels) return 0;
        
        return (levelNumber - freeLevels) * starsPerUnlock;
    }
    
    // Checks if a level is unlocked based on total stars earned.
    public bool IsLevelUnlocked(int levelNumber)
    {
        return TotalStars >= GetStarsRequired(levelNumber);
    }
    
    // Returns the highest level number that the player has unlocked.
    public int GetHighestUnlockedLevel()
    {
        int highest = 1;
        
        // Check up to a reasonable max
        for (int i = 1; i <= 1000; i++)
        {
            if (IsLevelUnlocked(i))
            {
                highest = i;
            }
            else
            {
                break;
            }
        }
        
        return highest;
    }
    
    // Returns the highest level the player has completed.
    public int GetHighestCompletedLevel()
    {
        int highest = 0;
        
        foreach (var kvp in levelDataDict)
        {
            if (kvp.Value.isCompleted && kvp.Key > highest)
            {
                highest = kvp.Key;
            }
        }
        
        return highest;
    }
    
    // Gets level data, creating new entry if it doesn't exist.
    public LevelData GetLevelData(int levelNumber)
    {
        if (!levelDataDict.ContainsKey(levelNumber))
        {
            levelDataDict[levelNumber] = new LevelData(levelNumber);
        }
        
        return levelDataDict[levelNumber];
    }
    
    // Returns stars earned on a specific level.
    public int GetStarsForLevel(int levelNumber)
    {
        if (levelDataDict.ContainsKey(levelNumber))
        {
            return levelDataDict[levelNumber].starsEarned;
        }
        return 0;
    }
    
    // Completes a level with the given star count.
    // Only updates if new stars are higher than previous.
    public void CompleteLevel(int levelNumber, int stars)
    {
        var data = GetLevelData(levelNumber);
        data.isCompleted = true;
        
        if (stars > data.starsEarned)
        {
            data.starsEarned = Mathf.Clamp(stars, 0, 3);
        }
        
        CalculateTotalStars();
        SaveProgress();
        
        Debug.Log($"[LevelDataManager] Level {levelNumber} completed with {stars} stars. Total: {TotalStars}");
    }
    
    // Recalculates total stars from all completed levels.
    private void CalculateTotalStars()
    {
        TotalStars = 0;
        foreach (var kvp in levelDataDict)
        {
            TotalStars += kvp.Value.starsEarned;
        }
    }
    
    // Saves progress to PlayerPrefs as JSON.
    public void SaveProgress()
    {
        var saveData = new LevelSaveData();
        saveData.levels = new List<LevelData>(levelDataDict.Values);
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log("[LevelDataManager] Progress saved");
    }
    
    // Loads progress from PlayerPrefs.
    public void LoadProgress()
    {
        levelDataDict.Clear();
        
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            var saveData = JsonUtility.FromJson<LevelSaveData>(json);
            
            if (saveData != null && saveData.levels != null)
            {
                foreach (var level in saveData.levels)
                {
                    levelDataDict[level.levelNumber] = level;
                }
            }
            
            Debug.Log($"[LevelDataManager] Loaded {levelDataDict.Count} levels");
        }
    }
    
    // Clears all saved progress.
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        levelDataDict.Clear();
        TotalStars = 0;
        Debug.Log("[LevelDataManager] Save cleared");
    }
    
    [System.Serializable]
    private class LevelSaveData
    {
        public List<LevelData> levels;
    }
}