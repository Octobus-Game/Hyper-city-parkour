using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class HAROR : MonoBehaviour
{
    private bool isFollowing = false;  // Флаг, чтобы определить следит ли кнопка за мышью
    private RectTransform buttonRect;  // RectTransform кнопки

    void Start()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (isFollowing)
        {
            // Получаем позицию мыши и преобразуем её в координаты UI
            Vector2 mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                buttonRect.parent as RectTransform,
                Input.mousePosition,
                Camera.main,
                out mousePosition
            );

            // Перемещаем кнопку за мышью
            buttonRect.anchoredPosition = mousePosition;
        }
    }

    // Когда происходит нажатие на кнопку
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isFollowing = true;  // Начинаем следить за позицией мыши
        }
    }

    // Когда отпускается кнопка мыши
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isFollowing = false;  // Прекращаем следить за позицией мыши
        }
    }
}
