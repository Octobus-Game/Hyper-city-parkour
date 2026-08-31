using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stena01 : MonoBehaviour
{
    public GameObject targetObject; // Объект, который исчезнет
    private ulong pressCount = 0;
    private ulong targetCount = 7625597484987;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetMouseButtonDown(0) || // Левая кнопка мыши
            Input.GetMouseButtonDown(1))   // Правая кнопка мыши
        {
            pressCount++;

            if (pressCount >= targetCount && targetObject != null)
            {
                targetObject.SetActive(false);
                Debug.Log("Объект исчез после " + pressCount + " нажатий.");
            }
        }
    }
}
