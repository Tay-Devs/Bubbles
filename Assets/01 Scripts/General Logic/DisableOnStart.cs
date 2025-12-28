using System;
using UnityEngine;

public class DisableOnStart : MonoBehaviour
{
    private void Awake()
    {
        if (Time.frameCount <= 1)
        {
            gameObject.SetActive(false);
        }
    }
}
