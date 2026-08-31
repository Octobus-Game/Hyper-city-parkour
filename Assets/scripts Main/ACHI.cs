using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ACHI : MonoBehaviour
{
    public GameObject panel; // Панель, которую мы будем отображать
    private bool panelShown = false; // Переменная для отслеживания, была ли панель показана

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false); // Сначала панель неактивна
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Убедитесь, что объект имеет тег "Player"
        {
            if (panel != null && !panelShown) // Отобразить панель только если она ещё не была показана
            {
                panel.SetActive(true); // Отобразить панель при входе в триггер
                panelShown = true; // Установить флаг, что панель была показана
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) // Скрыть панель при выходе из триггера
        {
            if (panel != null)
            {
                panel.SetActive(false); // Скрыть панель
            }
        }
    }
}
