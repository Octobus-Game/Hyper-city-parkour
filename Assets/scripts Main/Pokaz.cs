using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pokaz : MonoBehaviour
{
    // Укажите объект, который вы хотите отслеживать
    public GameObject targetObject;

    // Цвет для выделения объекта
    public Color highlightColor = Color.green;

    // Оригинальный цвет объекта
    private Color originalColor;

    // Флаг для отображения выделения
    private bool isHighlighted = false;

    private void Start()
    {
        if (targetObject != null)
        {
            // Сохраняем исходный цвет объекта (если есть Renderer)
            Renderer targetRenderer = targetObject.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                originalColor = targetRenderer.material.color;
            }
        }
        else
        {
            Debug.LogError("Target Object не установлен в инспекторе!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Если игрок касается объекта
        if (other.gameObject == targetObject)
        {
            // Выводим координаты объекта в консоль
            Vector3 position = targetObject.transform.position;
            Debug.Log($"Объект находится по координатам: {position}");

            // Подсвечиваем объект
            HighlightObject(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Когда игрок перестает касаться объекта
        if (other.gameObject == targetObject)
        {
            // Убираем подсветку
            HighlightObject(false);
        }
    }

    private void HighlightObject(bool highlight)
    {
        if (targetObject != null)
        {
            Renderer targetRenderer = targetObject.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                if (highlight)
                {
                    targetRenderer.material.color = highlightColor;
                    isHighlighted = true;
                }
                else
                {
                    targetRenderer.material.color = originalColor;
                    isHighlighted = false;
                }
            }
        }
    }
}
