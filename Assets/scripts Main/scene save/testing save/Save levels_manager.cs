using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Класс для управления сохранением прогресса уровней
public class LevelProgressManager : MonoBehaviour
{
    [Header("Настройки файла сохранения")]
    public string saveFileName = "progress.txt";

    private string savePath;
    private Dictionary<int, bool> levelProgress = new Dictionary<int, bool>();

    void Awake()
    {
        // Создаем путь к файлу сохранения в папке Application.persistentDataPath
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        // Загружаем прогресс при запуске
        LoadProgress();
    }

    // Метод для сохранения завершения уровня
    public void SaveLevelComplete(int levelNumber)
    {
        levelProgress[levelNumber] = true;
        SaveProgress();
    }

    // Метод для проверки, завершен ли уровень
    public bool IsLevelCompleted(int levelNumber)
    {
        return levelProgress.ContainsKey(levelNumber) && levelProgress[levelNumber];
    }

    // Сохранение прогресса в файл
    private void SaveProgress()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(savePath))
            {
                foreach (var level in levelProgress)
                {
                    if (level.Value)
                    {
                        writer.WriteLine($"{level.Key} level = true");
                    }
                }
            }
            Debug.Log("Прогресс сохранен в: " + savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка при сохранении прогресса: " + e.Message);
        }
    }

    // Загрузка прогресса из файла
    private void LoadProgress()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("Файл сохранения не найден. Создается новый прогресс.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(savePath);
            levelProgress.Clear();

            foreach (string line in lines)
            {
                if (line.Contains("level = true"))
                {
                    string[] parts = line.Split(' ');
                    if (int.TryParse(parts[0], out int levelNumber))
                    {
                        levelProgress[levelNumber] = true;
                    }
                }
            }
            Debug.Log("Прогресс загружен из: " + savePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Ошибка при загрузке прогресса: " + e.Message);
        }
    }

    // Метод для сброса всего прогресса (для тестирования)
    public void ResetProgress()
    {
        levelProgress.Clear();
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        Debug.Log("Прогресс сброшен");
    }
}