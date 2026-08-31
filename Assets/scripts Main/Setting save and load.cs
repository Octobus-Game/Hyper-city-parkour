using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Setting_save_and_load : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Notify Text (необязательно)")]
    [SerializeField] private TextMeshProUGUI notifyText;
    [SerializeField] private float notifyDuration = 2f;

    // Путь к файлу: папка с игрой / Settings / settings.config
    private string SettingsPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Settings", "settings.config"
    );

    [Serializable]
    private class SettingsData
    {
        public float musicVolume        = 1f;
        public float soundVolume        = 1f;
        public float brightness         = 0f;
        public float fpsLimit           = 60f;
        public float cameraFarClip      = 1000f;
        public int   vsync              = 1;
        public int   postProcessing     = 1;
        public int   resolutionIndex    = 0;
        public float cameraOrthoSize    = 60f;
    }

    private void Start()
    {
        resetButton.onClick.AddListener(ResetSettings);
        saveButton .onClick.AddListener(SaveSettings);
        loadButton .onClick.AddListener(LoadSettings);

        if (notifyText != null)
            notifyText.gameObject.SetActive(false);
    }

    // ── 1. Сброс ─────────────────────────────────────────────────────
    private void ResetSettings()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Применяем дефолты сразу
        ApplyData(new SettingsData());

        Notify("Настройки сброшены");
    }

    // ── 2. Сохранение в .config ──────────────────────────────────────
    private void SaveSettings()
    {
        SettingsData data = new SettingsData
        {
            musicVolume     = PlayerPrefs.GetFloat("MusicVolume",           1f),
            soundVolume     = PlayerPrefs.GetFloat("SoundVolume",           1f),
            brightness      = PlayerPrefs.GetFloat("ExposureCompensation",  0f),
            fpsLimit        = PlayerPrefs.GetFloat("TargetFPS",             60f),
            cameraFarClip   = PlayerPrefs.GetFloat("CameraFarClipPlane",    1000f),
            vsync           = PlayerPrefs.GetInt  ("Vsync",                 1),
            postProcessing  = PlayerPrefs.GetInt  ("PostProcessingEnabled", 1),
            resolutionIndex = PlayerPrefs.GetInt  ("ResolutionIndex",       0),
            cameraOrthoSize = PlayerPrefs.GetFloat("CameraOrthographicSize",60f),
        };

        try
        {
            string dir = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SettingsPath, json);

            Notify("Сохранено: " + SettingsPath);
            Debug.Log("[Settings] Сохранено в " + SettingsPath);
        }
        catch (Exception e)
        {
            Notify("Ошибка сохранения!");
            Debug.LogError("[Settings] Ошибка сохранения: " + e.Message);
        }
    }

    // ── 3. Загрузка из .config ───────────────────────────────────────
    private void LoadSettings()
    {
        if (!File.Exists(SettingsPath))
        {
            Notify("Файл не найден");
            Debug.LogWarning("[Settings] Файл не найден: " + SettingsPath);
            return;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath);
            SettingsData data = JsonUtility.FromJson<SettingsData>(json);

            // Записываем в PlayerPrefs
            PlayerPrefs.SetFloat("MusicVolume",            data.musicVolume);
            PlayerPrefs.SetFloat("SoundVolume",            data.soundVolume);
            PlayerPrefs.SetFloat("ExposureCompensation",   data.brightness);
            PlayerPrefs.SetFloat("TargetFPS",              data.fpsLimit);
            PlayerPrefs.SetFloat("CameraFarClipPlane",     data.cameraFarClip);
            PlayerPrefs.SetInt  ("Vsync",                  data.vsync);
            PlayerPrefs.SetInt  ("PostProcessingEnabled",  data.postProcessing);
            PlayerPrefs.SetInt  ("ResolutionIndex",        data.resolutionIndex);
            PlayerPrefs.SetFloat("CameraOrthographicSize", data.cameraOrthoSize);
            PlayerPrefs.Save();

            // Применяем сразу
            ApplyData(data);

            Notify("Настройки загружены");
            Debug.Log("[Settings] Загружено из " + SettingsPath);
        }
        catch (Exception e)
        {
            Notify("Ошибка загрузки!");
            Debug.LogError("[Settings] Ошибка загрузки: " + e.Message);
        }
    }

    // ── Применение настроек в реальном времени ───────────────────────
    private void ApplyData(SettingsData data)
    {
        // FPS
        Application.targetFrameRate = data.fpsLimit >= 355f ? -1 : Mathf.RoundToInt(data.fpsLimit);

        // VSync
        QualitySettings.vSyncCount = data.vsync;

        // Камера
        if (Camera.main != null)
        {
            Camera.main.farClipPlane      = data.cameraFarClip;
            Camera.main.orthographicSize  = data.cameraOrthoSize;
        }

        // Post Processing
        var volumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
        foreach (var v in volumes)
            v.enabled = data.postProcessing == 1;

        // Звук
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var s in sources)
            s.volume = data.soundVolume;
    }

    // ── Уведомление ──────────────────────────────────────────────────
    private void Notify(string message)
    {
        if (notifyText == null) return;

        StopAllCoroutines();
        notifyText.text = message;
        notifyText.gameObject.SetActive(true);
        StartCoroutine(HideNotify());
    }

    private System.Collections.IEnumerator HideNotify()
    {
        yield return new WaitForSeconds(notifyDuration);
        if (notifyText != null)
            notifyText.gameObject.SetActive(false);
    }
}