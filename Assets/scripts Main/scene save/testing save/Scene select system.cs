using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectButton : MonoBehaviour
{
    [Header("Настройки кнопки уровня")]
    public int requiredLevel = 1; // Какой уровень должен быть завершен
    public int targetLevel = 2;   // На какой уровень переходить
    public string sceneName = "Level2"; // Имя сцены для загрузки

    [Header("UI Элементы")]
    public Button levelButton;
    public GameObject lockedIcon; // Иконка заблокированного уровня

    private LevelProgressManager progressManager;

    void Start()
    {
        progressManager = FindObjectOfType<LevelProgressManager>();

        if (levelButton == null)
            levelButton = GetComponent<Button>();

        // Настраиваем кнопку при старте
        UpdateButtonState();

        // Добавляем обработчик нажатия
        if (levelButton != null)
        {
            levelButton.onClick.AddListener(OnLevelButtonClick);
        }
    }

    void UpdateButtonState()
    {
        if (progressManager == null) return;

        bool canAccess = progressManager.IsLevelCompleted(requiredLevel);

        // Включаем/отключаем кнопку
        if (levelButton != null)
        {
            levelButton.interactable = canAccess;
        }

        // Показываем/скрываем иконку блокировки
        if (lockedIcon != null)
        {
            lockedIcon.SetActive(!canAccess);
        }

        Debug.Log($"Кнопка уровня {targetLevel}: {(canAccess ? "доступна" : "заблокирована")}");
    }

    void OnLevelButtonClick()
    {
        if (progressManager == null) return;

        if (progressManager.IsLevelCompleted(requiredLevel))
        {
            // Загружаем сцену следующего уровня
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.Log($"Доступ к уровню {targetLevel} заблокирован! Сначала завершите уровень {requiredLevel}");

            // Здесь можно показать сообщение игроку
            ShowAccessDeniedMessage();
        }
    }

    private void ShowAccessDeniedMessage()
    {
        // Можно добавить UI сообщение или звуковой эффект
        Debug.Log("Показать сообщение: 'Завершите предыдущий уровень!'");
    }

    // Метод для обновления состояния кнопки извне
    public void RefreshButtonState()
    {
        UpdateButtonState();
    }
}
