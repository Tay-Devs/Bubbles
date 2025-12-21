using System;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public BubbleType type = BubbleType.Blue;
    public Action onTouch;
    public bool visited = false;
    public bool isAttached = false;
    public SFXData popSound;

    void Start()
    {
        SetType(type);
    }

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

    public void Explode()
    {
        SFXManager.PlayAtPosition(popSound, transform.position);
        Destroy(gameObject);
    }
}