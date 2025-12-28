using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BubbleEffectData", menuName = "Bubble Game/Bubble Effect Data")]
public class BubbleEffectData : ScriptableObject
{
    [SerializeField] private List<BubbleEffectEntry> effects = new List<BubbleEffectEntry>();
    
    private Dictionary<BubbleType, GameObject> effectLookup;
    
    // Returns the effect prefab for the given bubble type.
    // Returns null if no effect is assigned for that type.
    public GameObject GetEffectPrefab(BubbleType type)
    {
        BuildLookup();
        
        if (effectLookup.TryGetValue(type, out GameObject prefab))
        {
            return prefab;
        }
        
        return null;
    }
    
    // Builds the dictionary lookup from the list for fast access.
    private void BuildLookup()
    {
        if (effectLookup != null) return;
        
        effectLookup = new Dictionary<BubbleType, GameObject>();
        
        foreach (var entry in effects)
        {
            if (entry.effectPrefab != null && !effectLookup.ContainsKey(entry.bubbleType))
            {
                effectLookup.Add(entry.bubbleType, entry.effectPrefab);
            }
        }
    }
    
    // Clears the lookup cache. Call this if effects list changes at runtime.
    public void ClearCache()
    {
        effectLookup = null;
    }
    
    private void OnValidate()
    {
        effectLookup = null;
    }
}

[Serializable]
public class BubbleEffectEntry
{
    public BubbleType bubbleType;
    public GameObject effectPrefab;
}