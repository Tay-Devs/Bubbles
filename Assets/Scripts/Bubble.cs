using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public BubbleType type = BubbleType.Black;

    void Start()
    {
        SetType(type);
    }

    public void SetType(BubbleType type)
    {
        this.type = type;
        foreach (BubbleType color in Enum.GetValues(typeof(BubbleType)))
        {
            transform.Find(color.ToString()).gameObject.SetActive(color == type);
        }
    }
}


