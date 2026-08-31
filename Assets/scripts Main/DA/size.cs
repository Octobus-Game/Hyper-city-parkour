using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class size : MonoBehaviour
{
    // Увеличение размера в процентах
    [SerializeField] private float sizeIncreasePercentage = 5f;

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект игроком
        if (other.CompareTag("Player"))
        {
            // Получаем Transform игрока
            Transform playerTransform = other.transform;

            // Увеличиваем размер на заданный процент
            Vector3 newScale = playerTransform.localScale * (1 + sizeIncreasePercentage / 100f);
            playerTransform.localScale = newScale;

            Debug.Log("Игрок увеличен на " + sizeIncreasePercentage + "%!");
        }
    }
}
