using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTimer : MonoBehaviour
{
    // Название следующей сцены
    [SerializeField] private string nextSceneName;
    // Длительность таймера в секундах
    [SerializeField] private float timerDuration = 8f;
    // Текущий оставшийся таймер
    private float currentTime;

    private void Start()
    {
        // Инициализируем таймер
        currentTime = timerDuration;
    }

    private void Update()
    {
        // Обновляем таймер каждый кадр
        currentTime -= Time.deltaTime;

        // Проверяем, если таймер завершён
        if (currentTime <= 0f)
        {
            // Переходим на следующую сцену
            SceneManager.LoadScene(nextSceneName);
        }
    }
}