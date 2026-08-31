using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMERT : MonoBehaviour
{
    // Переменная для хранения начальной позиции объекта
    private Vector3 initialPosition;

    // Вызывается при старте игры
    void Start()
    {
        // Сохраняем начальную позицию объекта
        initialPosition = transform.position;
    }

    // Метод вызывается при столкновении с другим объектом
    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем, с каким объектом произошла коллизия (можно уточнить при необходимости)
        // Например, if (collision.gameObject.CompareTag("YourTag")) { ... }

        // Возвращаем объект на начальную позицию
        transform.position = initialPosition;
    }
}