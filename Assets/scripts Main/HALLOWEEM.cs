using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class HideObjectsByDate : MonoBehaviour
{
    // Массив объектов, которые нужно скрыть
    public GameObject[] objectsToHide;

    // Дата начала (25 октября)
    private DateTime startDate = new DateTime(DateTime.Now.Year, 10, 25);
    // Дата окончания (31 октября)
    private DateTime endDate = new DateTime(DateTime.Now.Year, 10, 31);

    void Start()
    {
        // Получаем текущую дату
        DateTime currentDate = DateTime.Now;

        // Проверяем, попадает ли текущая дата в диапазон от 25 до 31 октября
        if (currentDate >= startDate && currentDate <= endDate)
        {
            // Объекты видимы
            ShowObjects();
        }
        else if (currentDate > endDate)
        {
            // Объекты скрыты после 31 октября
            HideObjects();
        }
    }

    void ShowObjects()
    {
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(true); // Показать объект
        }
    }

    void HideObjects()
    {
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(false); // Скрыть объект
        }
    }
}




