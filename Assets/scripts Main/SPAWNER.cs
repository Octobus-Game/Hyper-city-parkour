using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPAWNER : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;  // Префаб ИИ (врага)
    public Transform spawnPoint;    // Точка спауна врагов
    public float initialSpawnDelay = 2f;  // Начальная задержка спауна
    public float spawnRateIncreaseInterval = 60f;  // Интервал для увеличения скорости спауна (1 минута)
    public float spawnRateMultiplier = 0.9f;  // Коэффициент увеличения скорости спауна (уменьшение задержки)

    [Header("Movement Settings")]
    public bool enableMovement = true;  // Включить/выключить движение
    public float moveSpeed = 5f;  // Скорость движения по X
    public float moveDirection = -1f;  // Направление движения (-1 = влево, 1 = вправо)
    public float destroyPositionX = -10f;  // Позиция X, при которой объект уничтожается

    private float currentSpawnDelay;  // Текущая задержка между спаунами
    private List<GameObject> spawnedEnemies = new List<GameObject>();  // Список заспауненных врагов

    void Start()
    {
        currentSpawnDelay = initialSpawnDelay;
        StartCoroutine(SpawnEnemies());
        StartCoroutine(IncreaseSpawnRateOverTime());

        if (enableMovement)
        {
            StartCoroutine(MoveEnemies());
        }
    }

    // Корутина для спауна врагов
    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(currentSpawnDelay);
            SpawnEnemy();
        }
    }

    // Метод для спауна врага
    void SpawnEnemy()
    {
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedEnemies.Add(newEnemy);
    }

    // Корутина для движения врагов по оси X
    IEnumerator MoveEnemies()
    {
        while (true)
        {
            // Перемещаем всех заспауненных врагов
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] != null)
                {
                    // Двигаем врага по X
                    Vector3 currentPos = spawnedEnemies[i].transform.position;
                    currentPos.x += moveSpeed * moveDirection * Time.deltaTime;
                    spawnedEnemies[i].transform.position = currentPos;

                    // Проверяем, нужно ли уничтожить врага
                    if ((moveDirection < 0 && currentPos.x <= destroyPositionX) ||
                        (moveDirection > 0 && currentPos.x >= destroyPositionX))
                    {
                        Destroy(spawnedEnemies[i]);
                        spawnedEnemies.RemoveAt(i);
                    }
                }
                else
                {
                    // Удаляем null ссылки из списка
                    spawnedEnemies.RemoveAt(i);
                }
            }

            yield return null;  // Ждем следующий кадр
        }
    }

    // Корутина для увеличения скорости спауна каждые 60 секунд
    IEnumerator IncreaseSpawnRateOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnRateIncreaseInterval);
            currentSpawnDelay *= spawnRateMultiplier;  // Уменьшаем задержку между спаунами
            currentSpawnDelay = Mathf.Max(currentSpawnDelay, 0.1f);  // Ограничиваем минимальную задержку
            Debug.Log("Увеличена скорость спауна. Текущая задержка: " + currentSpawnDelay);
        }
    }

    // Метод для изменения направления движения во время игры
    public void SetMoveDirection(float direction)
    {
        moveDirection = direction;
    }

    // Метод для изменения скорости движения во время игры
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    // Метод для включения/выключения движения
    public void ToggleMovement(bool enable)
    {
        enableMovement = enable;
        if (enable && !IsInvoking("MoveEnemies"))
        {
            StartCoroutine(MoveEnemies());
        }
    }

    void OnDestroy()
    {
        // Очищаем список при уничтожении спаунера
        spawnedEnemies.Clear();
    }
}