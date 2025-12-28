using System;
using System.Collections;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public BubbleType type = BubbleType.Blue;
    public Action onTouch;
    public bool visited = false;
    public bool isAttached = false;
    public SFXData popSound;
    
    [SerializeField] private float timeToDestroy = 0.5f;
    
    [Header("Effect")]
    [SerializeField] private BubbleEffectData effectData;
    
    // Callback fired when destruction is complete (after animation)
    public Action onDestroyed;
    
    private static readonly int PopTrigger = Animator.StringToHash("Pop");
    void Start()
    {
        SetType(type);
    }

    // Sets the bubble color by enabling the matching child object.
    // Each color type has a child GameObject that gets toggled.
    public void SetType(BubbleType newType)
    {
        this.type = newType;
        
        // Disable all color children, enable only the selected one
        foreach (BubbleType color in Enum.GetValues(typeof(BubbleType)))
        {
            Transform colorChild = transform.Find(color.ToString());
            if (colorChild != null)
            {
                colorChild.gameObject.SetActive(color == newType);
            }
        }
    }

    // Triggers the pop animation and spawns the color-specific effect.
    // The bubble is destroyed after timeToDestroy.
    public void Explode()
    {
        // Spawn the effect prefab for this color
        SpawnEffect();
        
        // Hide the bubble visual
        SetType(BubbleType.Null);
        
        StartCoroutine(BubbleDestroyDelay());
    }
    
    // Spawns the explosion effect for this bubble's color type.
    // Effect handles its own day/night theme and self-destruction.
    private void SpawnEffect()
    {
        if (effectData == null) return;
        
        GameObject effectPrefab = effectData.GetEffectPrefab(type);
        if (effectPrefab != null)
        {
            BubbleEffect.Spawn(effectPrefab, transform.position);
        }
    }

    // Waits for timeToDestroy then notifies listeners and destroys.
    private IEnumerator BubbleDestroyDelay()
    {
        yield return new WaitForSeconds(timeToDestroy);
        onDestroyed?.Invoke();
        Destroy(gameObject);
    }
}