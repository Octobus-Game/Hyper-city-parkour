using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Дополнительный класс для отладки и тестирования
public class ProgressDebugger : MonoBehaviour
{
    [Header("Отладка")]
    public KeyCode checkProgressKey = KeyCode.P;
    public KeyCode resetProgressKey = KeyCode.R;

    private LevelProgressManager progressManager;

    void Start()
    {
        progressManager = FindObjectOfType<LevelProgressManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(checkProgressKey))
        {
            CheckAllProgress();
        }

        if (Input.GetKeyDown(resetProgressKey))
        {
            if (progressManager != null)
            {
                progressManager.ResetProgress();
            }
        }
    }

    private void CheckAllProgress()
    {
        if (progressManager == null) return;

        Debug.Log("=== ПРОВЕРКА ПРОГРЕССА ===");
        for (int i = 1; i <= 10; i++)
        {
            bool completed = progressManager.IsLevelCompleted(i);
            Debug.Log($"Уровень {i}: {(completed ? "Завершен" : "Не завершен")}");
        }
        Debug.Log("========================");
    }
}

