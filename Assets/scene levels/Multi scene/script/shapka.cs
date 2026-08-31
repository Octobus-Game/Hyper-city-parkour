using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Для работы с UI компонентами
public class shapka : MonoBehaviour
{
    // Ссылка на объект, который нужно сделать видимым
    public GameObject targetObject;

    // Ссылка на кнопку
    public Button button;

    void Start()
    {
        // Убедимся, что объект и кнопка назначены
        if (targetObject == null)
        {
            Debug.LogError("Target object is not assigned!");
            return;
        }

        if (button == null)
        {
            Debug.LogError("Button is not assigned!");
            return;
        }

        // Подписываемся на событие нажатия кнопки
        button.onClick.AddListener(MakeObjectVisible);

        // Скрываем объект изначально
        targetObject.SetActive(false);
    }

    void MakeObjectVisible()
    {
        // Делаем объект видимым
        targetObject.SetActive(true);
    }
}
