using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ENEMYXP : MonoBehaviour
{
    [SerializeField] private GameObject targetPrefab; // Префаб, попадания которого считаются
    private int hitCount = 0;
    private const int maxHits = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(targetPrefab.tag)) // Проверяем, совпадает ли тег объекта с тегом префаба
        {
            hitCount++;
            Debug.Log($"Попадание: {hitCount}/10");

            if (hitCount >= maxHits)
            {
                Destroy(gameObject); // Удаляем объект после 600 попаданий
            }
        }
    }
}
