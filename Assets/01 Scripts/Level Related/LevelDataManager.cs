using System.Collections.Generic;
using UnityEngine;

public class LevelDataManager : MonoBehaviour
{
    public static LevelDataManager Instance { get; private set; }
    
    [Header("Unlock Settings")]
    public int freeLevels = 3;
    public int starsPerUnlock = 3;
    
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
        if (clearSaveOnStart)
        {
            // Test data - remove after testing
        }
    }
    
    public int GetStarsRequired(int levelNumber)
    {
        if (levelNumber <= freeLevels) return 0;
        return (levelNumber - freeLevels) * starsPerUnlock;
    }
    
    public bool IsLevelUnlocked(int levelNumber)
    {
        return TotalStars >= GetStarsRequired(levelNumber);
    }
    
    public int GetHighestUnlockedLevel()
    {
        int highest = 1;
        
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
    
    // Returns the first level that has 0 stars (first incomplete level).
    public int GetFirstIncompleteLevel()
    {
        int highestUnlocked = GetHighestUnlockedLevel();
        
        for (int i = 1; i <= highestUnlocked; i++)
        {
            int stars = GetStarsForLevel(i);
            if (stars == 0)
            {
                return i;
            }
        }
        
        // All unlocked levels have stars, return highest unlocked
        return highestUnlocked;
    }
    
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
    
    public LevelData GetLevelData(int levelNumber)
    {
        if (!levelDataDict.ContainsKey(levelNumber))
        {
            levelDataDict[levelNumber] = new LevelData(levelNumber);
        }
        
        return levelDataDict[levelNumber];
    }
    
    public int GetStarsForLevel(int levelNumber)
    {
        if (levelDataDict.ContainsKey(levelNumber))
        {
            return levelDataDict[levelNumber].starsEarned;
        }
        return 0;
    }
    
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
    
    private void CalculateTotalStars()
    {
        TotalStars = 0;
        foreach (var kvp in levelDataDict)
        {
            TotalStars += kvp.Value.starsEarned;
        }
    }
    
    public void SaveProgress()
    {
        var saveData = new LevelSaveData();
        saveData.levels = new List<LevelData>(levelDataDict.Values);
        
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log("[LevelDataManager] Progress saved");
    }
    
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