using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Триггер для завершения уровня
public class LevelCompleteTrigger : MonoBehaviour
{
    [Header("Настройки триггера")]
    public int levelNumber = 1;

    private LevelProgressManager progressManager;

    void Start()
    {
        progressManager = FindObjectOfType<LevelProgressManager>();
        if (progressManager == null)
        {
            Debug.LogError("LevelProgressManager не найден на сцене!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        if (progressManager != null)
        {
            progressManager.SaveLevelComplete(levelNumber);
            Debug.Log($"Уровень {levelNumber} завершен!");

            // Здесь можно добавить эффекты завершения уровня
            // например, показать UI поздравления
        }
    }
}