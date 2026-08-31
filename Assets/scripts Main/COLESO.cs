using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class COLESO : MonoBehaviour
{
    public Transform player; // Ссылка на трансформ персонажа
    public float rotationSpeed = 100f; // Скорость вращения объекта

    private float lastYPosition;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player Transform is not assigned!");
            enabled = false;
            return;
        }

        lastYPosition = player.position.y; // Запоминаем начальное положение по Y
    }

    void Update()
    {
        float currentYPosition = player.position.y;

        // Если персонаж переместился по Y
        if (currentYPosition != lastYPosition)
        {
            // Вычисляем направление перемещения
            float direction = currentYPosition > lastYPosition ? 1f : -1f;

            // Вращаем объект
            transform.Rotate(Vector3.up, direction * rotationSpeed * Time.deltaTime);

            // Обновляем последнее положение по Y
            lastYPosition = currentYPosition;
        }
    }
}
