using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollButtons : MonoBehaviour
{
    public Button[] buttons; // Массив кнопок, которые будут двигаться
    public GameObject[] Ogject; // Массив объектов, которые также будут двигаться
    public float scrollSpeed = 10f; // Скорость прокрутки

    [Header("Границы прокрутки")]
    public float minY = -500f; // Минимальная граница по Y
    public float maxY = 500f;  // Максимальная граница по Y

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // Прокрутка кнопок
            foreach (Button button in buttons)
            {
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                Vector3 newPosition = rectTransform.localPosition;
                newPosition.y += scroll * scrollSpeed;

                // Применяем границы
                newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

                rectTransform.localPosition = newPosition;
            }

            // Прокрутка объектов
            foreach (GameObject obj in Ogject)
            {
                if (obj != null) // Проверяем, что объект не null
                {
                    // Пробуем получить RectTransform (для UI элементов)
                    RectTransform rectTransform = obj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Vector3 newPosition = rectTransform.localPosition;
                        newPosition.y += scroll * scrollSpeed;

                        // Применяем границы
                        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

                        rectTransform.localPosition = newPosition;
                    }
                    else
                    {
                        // Если это обычный GameObject (не UI), используем Transform
                        Transform objTransform = obj.transform;
                        Vector3 newPosition = objTransform.localPosition;
                        newPosition.y += scroll * scrollSpeed;

                        // Применяем границы
                        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

                        objTransform.localPosition = newPosition;
                    }
                }
            }
        }
    }
}