using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strelba : MonoBehaviour
{

    public float moveSpeed = 5f; // Скорость движения объекта
    private Camera mainCamera;

    void Start()
    {
        // Получаем ссылку на основную камеру
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Проверяем, нажата ли левая кнопка мыши
        if (Input.GetMouseButton(0))
        {
            // Получаем направление движения на основе направления камеры
            Vector3 direction = mainCamera.transform.forward;
            direction.y = 0; // Убираем влияние вертикального наклона камеры
            direction.Normalize();

            // Двигаем объект в направлении камеры
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}
