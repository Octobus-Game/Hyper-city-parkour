using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HAPPYTIME : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    void Start()
    {
        DateTime now = DateTime.Now;
        if (now.Month == 5 && now.Day == 26)
        {
            if (targetObject != null)
                targetObject.SetActive(true);
        }
    }
}
