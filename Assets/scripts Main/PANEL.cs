using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PANEL : MonoBehaviour
{
    public Button showPanelButton; // Кнопка, на которую нажимают
    public GameObject panel;       // Панель, которая будет отображаться

    void Start()
    {
        // Проверяем, что кнопка и панель назначены
        if (showPanelButton != null && panel != null)
        {
            // Скрываем панель при старте
            panel.SetActive(false);

            // Добавляем слушатель на кнопку
            showPanelButton.onClick.AddListener(OnButtonClick);
        }
    }

    void OnButtonClick()
    {
        // Показываем панель при нажатии на кнопку
        panel.SetActive(true);
    }
}
