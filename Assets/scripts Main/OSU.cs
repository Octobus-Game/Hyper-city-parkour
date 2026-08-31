using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OSU : MonoBehaviour
{
    public GameObject[] objects; // Массив объектов для управления
    public Button toggleButton; // Кнопка для включения/выключения гизмосов

    private bool gizmosActive = false; // Флаг, активны ли гизмосы

    void Start()
    {
        toggleButton.onClick.AddListener(ToggleGizmos);
    }

    void ToggleGizmos()
    {
        gizmosActive = !gizmosActive;

        foreach (var obj in objects)
        {
            var manipulator = obj.GetComponent<Manipulator>();
            if (manipulator == null)
            {
                manipulator = obj.AddComponent<Manipulator>();
            }

            manipulator.SetGizmosActive(gizmosActive);
        }
    }
}

public class Manipulator : MonoBehaviour
{
    public bool gizmosActive = false; // Флаг, активны ли гизмосы
    private Vector3 offset; // Сдвиг при перемещении объекта
    private Transform selectedAxis; // Выбранная ось

    void OnDrawGizmos()
    {
        if (!gizmosActive) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.forward);
    }

    void OnMouseDown()
    {
        if (!gizmosActive) return;

        // Определение выбранной оси по клику мыши
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            selectedAxis = hit.transform;
            offset = transform.position - hit.point;
        }
    }

    void OnMouseDrag()
    {
        if (!gizmosActive || selectedAxis == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(selectedAxis.up, transform.position);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            transform.position = hitPoint + offset;
        }
    }

    public void SetGizmosActive(bool active)
    {
        gizmosActive = active;
    }
}

