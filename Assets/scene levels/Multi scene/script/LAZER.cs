using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LAZER : MonoBehaviour
{
    public GameObject objectToSpawn; // Объект, который будет появляться
    public Transform spawnPoint;    // Точка появления объекта

    private bool canSpawn = true;   // Флаг, разрешающий создание объекта
    private bool timerRunning = false; // Флаг, сигнализирующий о работе таймера

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canSpawn && !timerRunning)
        {
            StartCoroutine(SpawnObjectWithCooldown());
        }
    }

    private IEnumerator SpawnObjectWithCooldown()
    {
        // Создаём объект
        Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation);

        // Запускаем таймер на 5 секунд
        timerRunning = true;
        yield return new WaitForSeconds(5f);

        // После таймера блокируем спавн на 10 секунд
        canSpawn = false;
        timerRunning = false;
        yield return new WaitForSeconds(10f);

        // Разрешаем спавн снова
        canSpawn = true;
    }
}
