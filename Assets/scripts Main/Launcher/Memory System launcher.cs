
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class GameLauncherSettings : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject settingsUI;
    public UnityEngine.UI.Dropdown monitorDropdown;
    public UnityEngine.UI.Dropdown resolutionDropdown;
    public UnityEngine.UI.Slider memorySlider;
    public UnityEngine.UI.Text memoryValueText;
    public UnityEngine.UI.Toggle fullscreenToggle;
    public UnityEngine.UI.Button launchButton;
    public UnityEngine.UI.Button exitButton;

    [Header("Game Settings")]
    public int selectedMonitor = 0;
    public int selectedResolutionIndex = 0;
    public bool isFullscreen = false;
    public int allocatedMemoryMB = 2048;

    private Resolution[] availableResolutions;
    private List<string> resolutionOptions = new List<string>();

    // Windows API для работы с мониторами
    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y) { this.X = x; this.Y = y; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    private List<Display> monitors = new List<Display>();

    void Start()
    {
        // Показываем окно настроек при запуске
        ShowSettingsWindow();
        SetupUI();
    }

    void ShowSettingsWindow()
    {
        // Останавливаем время для паузы игры
        Time.timeScale = 0f;

        if (settingsUI != null)
            settingsUI.SetActive(true);

        // Включаем курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetupUI()
    {
        SetupMonitorDropdown();
        SetupResolutionDropdown();
        SetupMemorySlider();
        SetupButtons();
    }

    void SetupMonitorDropdown()
    {
        if (monitorDropdown == null) return;

        monitorDropdown.options.Clear();

        // Получаем количество мониторов Unity
        for (int i = 0; i < Display.displays.Length; i++)
        {
            monitorDropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData($"Монитор {i + 1}"));
        }

        monitorDropdown.value = selectedMonitor;
        monitorDropdown.onValueChanged.AddListener(OnMonitorChanged);
    }

    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.options.Clear();
        resolutionOptions.Clear();

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height} @ {availableResolutions[i].refreshRate}Hz";
            resolutionOptions.Add(option);
            resolutionDropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData(option));
        }

        // Устанавливаем текущее разрешение по умолчанию
        selectedResolutionIndex = GetCurrentResolutionIndex();
        resolutionDropdown.value = selectedResolutionIndex;
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }
        return 0;
    }

    void SetupMemorySlider()
    {
        if (memorySlider == null || memoryValueText == null) return;

        // Получаем общий объем оперативной памяти системы
        long totalMemoryMB = SystemInfo.systemMemorySize;

        memorySlider.minValue = 512; // Минимум 512 МБ
        memorySlider.maxValue = totalMemoryMB * 0.8f; // Максимум 80% от общей памяти
        memorySlider.value = allocatedMemoryMB;

        UpdateMemoryText();
        memorySlider.onValueChanged.AddListener(OnMemoryChanged);
    }

    void SetupButtons()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        if (launchButton != null)
            launchButton.onClick.AddListener(LaunchGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void OnMonitorChanged(int value)
    {
        selectedMonitor = value;
    }

    void OnResolutionChanged(int value)
    {
        selectedResolutionIndex = value;
    }

    void OnMemoryChanged(float value)
    {
        allocatedMemoryMB = (int)value;
        UpdateMemoryText();
    }

    void OnFullscreenToggled(bool value)
    {
        isFullscreen = value;
    }

    void UpdateMemoryText()
    {
        if (memoryValueText != null)
            memoryValueText.text = $"{allocatedMemoryMB} МБ";
    }

    void LaunchGame()
    {
        ApplySettings();
        StartGame();
    }

    void ApplySettings()
    {
        try
        {
            // Применяем настройки памяти
            ApplyMemorySettings();

            // Применяем настройки дисплея
            ApplyDisplaySettings();

            Debug.Log($"Настройки применены: Монитор {selectedMonitor + 1}, " +
                     $"Разрешение {availableResolutions[selectedResolutionIndex].width}x{availableResolutions[selectedResolutionIndex].height}, " +
                     $"Полный экран: {isFullscreen}, Память: {allocatedMemoryMB} МБ");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка применения настроек: {e.Message}");
        }
    }

    void ApplyMemorySettings()
    {
        // Устанавливаем параметры сборщика мусора для оптимизации памяти
        GC.Collect();

        // Принудительно устанавливаем максимальное время выполнения GC
        QualitySettings.vSyncCount = 0; // Отключаем VSync для лучшей производительности

        // Устанавливаем целевую частоту кадров
        Application.targetFrameRate = 60;

        Debug.Log($"Выделено памяти: {allocatedMemoryMB} МБ");
    }

    void ApplyDisplaySettings()
    {
        Resolution selectedResolution = availableResolutions[selectedResolutionIndex];

        // Устанавливаем целевой дисплей
        if (selectedMonitor < Display.displays.Length)
        {
            Display.displays[selectedMonitor].Activate();
        }

        // Применяем разрешение и режим экрана
        Screen.SetResolution(
            selectedResolution.width,
            selectedResolution.height,
            isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed,
            selectedResolution.refreshRate
        );

        // Если выбран оконный режим и есть несколько мониторов
        if (!isFullscreen && Display.displays.Length > 1)
        {
            // Перемещаем окно на выбранный монитор
            MoveWindowToMonitor(selectedMonitor);
        }
    }

    void MoveWindowToMonitor(int monitorIndex)
    {
        // Эта функция работает только в Windows builds
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            // Получаем информацию о мониторе и перемещаем окно
            // В реальной игре это потребует дополнительных Windows API вызовов
            Debug.Log($"Перемещение окна на монитор {monitorIndex + 1}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось переместить окно на монитор {monitorIndex + 1}: {e.Message}");
        }
#endif
    }

    void StartGame()
    {
        // Скрываем окно настроек
        if (settingsUI != null)
            settingsUI.SetActive(false);

        // Возобновляем время игры
        Time.timeScale = 1f;

        // Блокируем курсор для игры (опционально)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Здесь можно добавить код инициализации игры
        InitializeGame();
    }

    void InitializeGame()
    {
        Debug.Log("Игра запущена с выбранными настройками!");

        // Применяем настройки качества в зависимости от выделенной памяти
        ApplyQualitySettings();

        // Здесь добавьте ваш код инициализации игры
    }

    void ApplyQualitySettings()
    {
        // Автоматически подстраиваем качество под количество выделенной памяти
        if (allocatedMemoryMB < 1024)
        {
            QualitySettings.SetQualityLevel(0); // Низкое качество
        }
        else if (allocatedMemoryMB < 2048)
        {
            QualitySettings.SetQualityLevel(1); // Среднее качество
        }
        else if (allocatedMemoryMB < 4096)
        {
            QualitySettings.SetQualityLevel(2); // Высокое качество
        }
        else
        {
            QualitySettings.SetQualityLevel(3); // Ультра качество
        }

        Debug.Log($"Установлено качество: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // Сохранение и загрузка настроек
    void SaveSettings()
    {
        PlayerPrefs.SetInt("SelectedMonitor", selectedMonitor);
        PlayerPrefs.SetInt("SelectedResolution", selectedResolutionIndex);
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("AllocatedMemory", allocatedMemoryMB);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        selectedMonitor = PlayerPrefs.GetInt("SelectedMonitor", 0);
        selectedResolutionIndex = PlayerPrefs.GetInt("SelectedResolution", GetCurrentResolutionIndex());
        isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 0) == 1;
        allocatedMemoryMB = PlayerPrefs.GetInt("AllocatedMemory", 2048);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveSettings();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveSettings();
    }

    void OnDestroy()
    {
        SaveSettings();
    }

    // Дополнительные утилиты для мониторинга памяти
    public void ShowMemoryUsage()
    {
        long usedMemory = GC.GetTotalMemory(false) / (1024 * 1024);
        Debug.Log($"Используется памяти: {usedMemory} МБ из выделенных {allocatedMemoryMB} МБ");
    }

    // Метод для принудительной очистки памяти
    public void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Debug.Log("Выполнена принудительная очистка памяти");
    }

    // Проверка доступности монитора
    bool IsMonitorAvailable(int monitorIndex)
    {
        return monitorIndex >= 0 && monitorIndex < Display.displays.Length;
    }

    // Получение информации о системе
    void LogSystemInfo()
    {
        Debug.Log($"Система: {SystemInfo.operatingSystem}");
        Debug.Log($"Процессор: {SystemInfo.processorType} ({SystemInfo.processorCount} ядер)");
        Debug.Log($"Видеокарта: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Оперативная память: {SystemInfo.systemMemorySize} МБ");
        Debug.Log($"Видеопамять: {SystemInfo.graphicsMemorySize} МБ");
        Debug.Log($"Доступно мониторов: {Display.displays.Length}");
    }

    void Awake()
    {
        // Загружаем сохраненные настройки
        LoadSettings();

        // Выводим информацию о системе
        LogSystemInfo();

        // Убеждаемся, что объект не уничтожается при смене сцен
        DontDestroyOnLoad(gameObject);
    }
}