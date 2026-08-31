using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class LevelManager : MonoBehaviour
{
    public Button[] buttons;
    private int levelIsUnlocked = 1;
    private string progressFilePath;

    void Start()
    {
        progressFilePath = Path.Combine(Application.persistentDataPath, "progress.txt");
        Debug.Log(progressFilePath);
        Debug.Log("Бтв оно работает");

        if (File.Exists(progressFilePath))
        {
            string[] lines = File.ReadAllLines(progressFilePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("levelIndex = "))
                {
                    string value = line.Substring("levelIndex = ".Length);
                    if (int.TryParse(value, out int parsedIndex))
                    {
                        // Ограничиваем уровень от 1 до 10
                        levelIsUnlocked = Mathf.Clamp(parsedIndex, 1, 9);
                        Debug.Log("Прогресс загружен: levelIsUnlocked = " + levelIsUnlocked);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Файл прогресса не найден, используется значение по умолчанию");
        }


        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }


        int safeUnlockLimit = Mathf.Min(levelIsUnlocked, buttons.Length);
        for (int i = 0; i < safeUnlockLimit; i++)
        {
            buttons[i].interactable = true;
        }
    }

    public void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene(levelIndex);
    }
}