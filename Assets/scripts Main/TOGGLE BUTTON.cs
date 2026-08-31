using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TOGGLEBUTTON : MonoBehaviour
{
    public GameObject panel; // Ссылка на панель, которую нужно скрывать/показывать

    void Start()
    {
        // Проверяем, что панель установлена
        if (panel != null)
        {
            panel.SetActive(false); // Скрываем панель при старте
        }
    }

    void Update()
    {
        // Проверяем, нажата ли клавиша Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Переключаем видимость панели
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }
}

