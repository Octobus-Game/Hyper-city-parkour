using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLAYMODE : MonoBehaviour
{
    public GameObject targetObject;    // Объект, который нужно активировать
    public Camera mainCamera;          // Основная камера
    public Camera targetCamera;        // Камера, на которую нужно переключиться

    private bool isObjectActive = false;

    void Start()
    {
        // Убедимся, что целевой объект выключен в начале, а основная камера включена
        targetObject.SetActive(false);
        mainCamera.enabled = true;
        targetCamera.enabled = false;
    }

    void Update()
    {
        // Проверяем нажатие на кнопку (например, пробел)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Переключаем состояние объекта и камер
            isObjectActive = !isObjectActive;
            targetObject.SetActive(isObjectActive);

            // Переключение камер
            if (isObjectActive)
            {
                mainCamera.enabled = false;
                targetCamera.enabled = true;
            }
            else
            {
                mainCamera.enabled = true;
                targetCamera.enabled = false;
            }
        }
    }
}

