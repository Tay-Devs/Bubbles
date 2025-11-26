using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public BubbleType type = BubbleType.Black;
    public Action onTouch;
    public bool visited = false;

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

    public void Explode()
    {
        Destroy(gameObject);
    }
    private void OnMouseDown()
    {
        Debug.Log("Bubble Clicked");
        onTouch?.Invoke();
    }
}


