using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO; 

public class SaveInit : MonoBehaviour
{
    public Button[] buttons;
    private int levelIsUnlocked = 1;

    private string progressFilePath;

    void Start()
    {
        progressFilePath = Path.Combine(Application.persistentDataPath, "SAVEDATA.txt");
        Debug.Log(progressFilePath);

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
                        levelIsUnlocked = parsedIndex;
                        Debug.Log("Прогресс загружен: levelIsUnlocked = " + levelIsUnlocked);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Файл прогресса не найден, используется значение по умолчанию");
        }

        // Отключаем все кнопки
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }

        // Включаем только доступные кнопки
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