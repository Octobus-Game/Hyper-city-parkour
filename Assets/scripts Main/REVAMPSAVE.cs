using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class REVAMPSAVE : MonoBehaviour
{
    int levelIndex;

    void Start()
    {
        Debug.Log("Просто дебаг-сообщение для того что бы убедится, что скрипт вообще работает");

        string filePath = Path.Combine(Application.dataPath, "progress.txt");

        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("levelIndex"))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int parsedIndex))
                    {
                        levelIndex = parsedIndex;
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Файл progress.txt не найден. Устанавливается levelIndex по умолчанию = 1");
            levelIndex = 1;
        }

        Debug.Log("Загружен levelIndex: " + levelIndex);

        if (levelIndex < 9 && levelIndex >= 1)
        {
            Debug.Log("Все гуд");

            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine("levelIndex = " + levelIndex);
            }
        }
        else
        {
            Debug.Log("Halt.");
        }
    }
}
