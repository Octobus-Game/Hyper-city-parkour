using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followplayer : MonoBehaviour
{
    public Transform player;        // Переменная для ссылки на игрока
    public float followSpeed = 5f;  // Скорость следования за игроком
    public float stopDistance = 2f; // Расстояние, на котором ИИ остановится

    void Update()
    {
        // Проверяем расстояние между ИИ и игроком
        float distance = Vector3.Distance(transform.position, player.position);

        // Если расстояние больше stopDistance, продолжаем двигаться к игроку
        if (distance > stopDistance)
        {
            // Рассчитываем направление к игроку
            Vector3 direction = (player.position - transform.position).normalized;

            // Двигаем ИИ в направлении игрока с заданной скоростью
            transform.position += direction * followSpeed * Time.deltaTime;

            // Поворачиваем ИИ в сторону игрока
            transform.LookAt(player);
        }
    }
}
