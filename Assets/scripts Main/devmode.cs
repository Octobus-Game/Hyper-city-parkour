using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class devmode : MonoBehaviour
{
    public bool isDev = false;
    private string filePath = Application.persistentDataPath + "/devmode.asc";

    void Start()
    {
        if (Application.isEditor)
        {
            isDev = true;
            Debug.Log("DEVMODE ENABLED");
        }
        else
        {
            /* тут нужно проверить sha256 файла, но мне щас лень */
        }

        if (isDev = true)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
