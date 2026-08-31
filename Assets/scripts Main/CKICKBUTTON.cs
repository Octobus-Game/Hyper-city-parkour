using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickAndDisable : MonoBehaviour
{
    public GameObject targetObject; // Объект, который будет отключаться
    public AudioSource disableSound; // Аудиоисточник для звука

    void OnMouseDown()
    {
        // Проверяем, что целевой объект назначен
        if (targetObject != null)
        {
            // Отключаем целевой объект
            targetObject.SetActive(false);
            Debug.Log("Объект отключен");

            // Воспроизводим звук, если аудиоисточник назначен
            if (disableSound != null)
            {
                disableSound.Play();
            }
        }
    }
}