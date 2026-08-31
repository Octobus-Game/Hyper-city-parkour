using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SCALE : MonoBehaviour
{
    private bool isSelected = false; // Флаг выбора объекта
    public float scaleSpeed = 0.05f; // Скорость изменения масштаба

    void Update()
    {
        // Проверяем клик мыши
        if (Input.GetMouseButtonDown(0)) // ЛКМ
        {
            CheckObjectSelection();
        }

        // Если объект выбран и нажата клавиша S
        if (isSelected && Input.GetKey(KeyCode.S))
        {
            float scaleChange = scaleSpeed * Time.deltaTime * 50; // Изменение масштаба
            transform.localScale += new Vector3(scaleChange, scaleChange, 0);
        }
    }

    void CheckObjectSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform == transform) // Если кликнули по текущему объекту
            {
                isSelected = true;
            }
            else
            {
                isSelected = false;
            }
        }
    }
}
