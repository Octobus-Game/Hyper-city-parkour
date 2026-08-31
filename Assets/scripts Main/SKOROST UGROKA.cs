using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SKOROSTUGROKA : MonoBehaviour
{
    public Rigidbody playerRigidbody; // Ссылка на Rigidbody игрока
    public Text speedText; // Ссылка на UI-текст для отображения скорости

    void Update()
    {
        if (playerRigidbody != null && speedText != null)
        {
            // Рассчитываем скорость в метрах в секунду
            float speedInMetersPerSecond = playerRigidbody.velocity.magnitude;

            // Переводим скорость в км/ч
            float speedInKilometersPerHour = speedInMetersPerSecond * 3.6f;

            // Обновляем текстовое поле
            speedText.text = $"{speedInKilometersPerHour:F1} км/ч";
        }
    }
}
