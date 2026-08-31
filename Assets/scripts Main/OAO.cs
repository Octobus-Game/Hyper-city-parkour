using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollBPanel : MonoBehaviour
{
    public RectTransform panelContainer; // Контейнер, в котором находятся панели
    public float scrollSpeed = 10f; // Скорость прокрутки

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            Vector3 newPosition = panelContainer.localPosition;
            newPosition.y += scroll * scrollSpeed;
            panelContainer.localPosition = newPosition;
        }
    }
}


