using System;
using UnityEngine;

public class DisableOnStart : MonoBehaviour
{
    private void Awake()
    {
        if (Time.timeSinceLevelLoad < 0.1f)
        {
            print(Time.timeSinceLevelLoad);
            gameObject.SetActive(false);
        }
    }
}
