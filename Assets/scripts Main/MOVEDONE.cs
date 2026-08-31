using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MoveUpDown : MonoBehaviour
{
    public float speed = 2.0f;  // Скорость движения
    public float height = 1.0f; // Высота амплитуды движения

    private Vector3 startPos;

    void Start()
    {
        // Запоминаем начальную позицию объекта
        startPos = transform.position;
    }

    void Update()
    {
        // Вычисляем новую позицию по оси Y с использованием функции синуса для плавного движения вверх и вниз
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;

        // Устанавливаем новую позицию объекта
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}

