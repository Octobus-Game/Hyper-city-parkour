using System;
using System.IO;
using UnityEngine;

public class ScreenshotManager : MonoBehaviour
{
    private string screenshotFolder = "Screenshots";

    void Update()
    {
        // Проверяем нажатие клавиши F2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {
        // Создаём путь к папке Screenshots
        string folderPath = Path.Combine(Application.persistentDataPath, screenshotFolder);

        // Создаём папку, если её не существует
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Генерируем уникальное имя файла с датой и временем
        string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        // Делаем скриншот
        ScreenCapture.CaptureScreenshot(fullPath);

        Debug.Log($"Скриншот сохранён: {fullPath}");
    }
}