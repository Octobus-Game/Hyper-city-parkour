using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SettingsManagersave : MonoBehaviour
{
    public static GameSettings CurrentSettings = new GameSettings();
    private static string settingsPath => Path.Combine(Application.persistentDataPath, "settings.json");

    public static void SaveSettings()
    {
        string json = JsonUtility.ToJson(CurrentSettings, true);
        File.WriteAllText(settingsPath, json);
        Debug.Log("Настройки сохранены: " + settingsPath);
    }

    public static void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            string json = File.ReadAllText(settingsPath);
            CurrentSettings = JsonUtility.FromJson<GameSettings>(json);
            Debug.Log("Настройки загружены");
        }
        else
        {
            Debug.Log("Файл настроек не найден. Используются значения по умолчанию.");
        }
    }

    [System.Serializable]
    public class GameSettings
    {
        public float cameraFov = 60f;
        public float cameraFarClip = 500f;
        public bool postProcessingEnabled = true;
    }

    










}
