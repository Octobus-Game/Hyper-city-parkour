using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Playersviewer : MonoBehaviour
{
    public Text prefabCountText; // Drag your UI Text object here in the inspector
    public string prefabTag = "MyPrefab"; // Tag your prefabs accordingly

    void Update()
    {
        GameObject[] prefabInstances = GameObject.FindGameObjectsWithTag(prefabTag);
        int count = prefabInstances.Length;

        if (prefabCountText != null)
        {
            prefabCountText.text = $"Игроков в игре: {count}";
        }
    }
}
