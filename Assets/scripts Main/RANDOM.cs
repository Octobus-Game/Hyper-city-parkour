using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RANDOM : MonoBehaviour
{
    [Range(0, 100)] public float spawnChance = 25f; // Шанс появления в процентах

    void Start()
    {
        // Генерация случайного числа от 0 до 100
        float randomValue = Random.Range(0f, 100f);

        // Если число больше шанса, уничтожить объект
        if (randomValue > spawnChance)
        {
            Destroy(gameObject);
        }
    }
}
