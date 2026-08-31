using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class WebcamManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public Button stopButton;
    public Dropdown webcamDropdown;
    public Dropdown resolutionDropdown;
    public Slider qualitySlider;
    public Text statusText;

    [Header("Settings")]
    public WebcamStreamer webcamStreamer;

    private WebCamDevice[] availableDevices;
    private Vector2Int[] availableResolutions = {
        new Vector2Int(320, 240),
        new Vector2Int(640, 480),
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1080)
    };

    void Start()
    {
        InitializeUI();
        RefreshWebcamList();
    }

    void InitializeUI()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartWebcam);

        if (stopButton != null)
            stopButton.onClick.AddListener(StopWebcam);

        if (webcamDropdown != null)
            webcamDropdown.onValueChanged.AddListener(OnWebcamChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (qualitySlider != null)
            qualitySlider.onValueChanged.AddListener(OnQualityChanged);

        // Настройка слайдера качества
        if (qualitySlider != null)
        {
            qualitySlider.minValue = 1f;
            qualitySlider.maxValue = 30f;
            qualitySlider.value = 10f;
        }
    }

    void RefreshWebcamList()
    {
        availableDevices = WebCamTexture.devices;

        if (webcamDropdown != null)
        {
            webcamDropdown.ClearOptions();

            if (availableDevices.Length == 0)
            {
                webcamDropdown.options.Add(new Dropdown.OptionData("Камеры не найдены"));
                UpdateStatus("Веб-камеры не обнаружены");
                return;
            }

            foreach (var device in availableDevices)
            {
                webcamDropdown.options.Add(new Dropdown.OptionData(device.name));
            }

            webcamDropdown.RefreshShownValue();
            UpdateStatus($"Найдено камер: {availableDevices.Length}");
        }

        // Заполняем список разрешений
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();

            foreach (var resolution in availableResolutions)
            {
                resolutionDropdown.options.Add(new Dropdown.OptionData($"{resolution.x}x{resolution.y}"));
            }

            resolutionDropdown.value = 1; // 640x480 по умолчанию
            resolutionDropdown.RefreshShownValue();
        }
    }

    public void StartWebcam()
    {
        if (webcamStreamer == null)
        {
            UpdateStatus("WebcamStreamer не найден!");
            return;
        }

        if (!webcamStreamer.isLocalPlayer)
        {
            UpdateStatus("Можно запустить только на локальном игроке");
            return;
        }

        if (availableDevices.Length == 0)
        {
            UpdateStatus("Нет доступных камер");
            return;
        }

        // Активируем объект вебкамеры
        if (!webcamStreamer.gameObject.activeInHierarchy)
        {
            webcamStreamer.gameObject.SetActive(true);
            UpdateStatus("Объект вебкамеры активирован");
        }

        // Применяем выбранные настройки
        ApplySettings();

        webcamStreamer.StartWebcam();
        UpdateStatus("Веб-камера запущена");

        UpdateButtonStates(false, true);
    }

    public void StopWebcam()
    {
        if (webcamStreamer == null) return;

        webcamStreamer.StopWebcam();

        // Отключаем объект вебкамеры
        if (webcamStreamer.gameObject.activeInHierarchy)
        {
            webcamStreamer.gameObject.SetActive(false);
            UpdateStatus("Объект вебкамеры отключен");
        }
        else
        {
            UpdateStatus("Веб-камера остановлена");
        }

        UpdateButtonStates(true, false);
    }

    void ApplySettings()
    {
        if (webcamStreamer == null) return;

        // Применяем выбранную камеру
        if (webcamDropdown != null && webcamDropdown.value < availableDevices.Length)
        {
            webcamStreamer.webcamName = availableDevices[webcamDropdown.value].name;
        }

        // Применяем разрешение
        if (resolutionDropdown != null && resolutionDropdown.value < availableResolutions.Length)
        {
            var resolution = availableResolutions[resolutionDropdown.value];
            webcamStreamer.SetWebcamResolution(resolution.x, resolution.y);
        }

        // Применяем частоту обновления сети
        if (qualitySlider != null)
        {
            webcamStreamer.networkUpdateRate = qualitySlider.value;
        }
    }

    void OnWebcamChanged(int index)
    {
        if (index < availableDevices.Length)
        {
            UpdateStatus($"Выбрана камера: {availableDevices[index].name}");
        }
    }

    void OnResolutionChanged(int index)
    {
        if (index < availableResolutions.Length)
        {
            var resolution = availableResolutions[index];
            UpdateStatus($"Выбрано разрешение: {resolution.x}x{resolution.y}");
        }
    }

    void OnQualityChanged(float value)
    {
        UpdateStatus($"Частота обновления: {value:F1} FPS");
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"Статус: {message}";
        }

        Debug.Log($"WebcamManager: {message}");
    }

    void UpdateButtonStates(bool startEnabled, bool stopEnabled)
    {
        if (startButton != null)
            startButton.interactable = startEnabled;

        if (stopButton != null)
            stopButton.interactable = stopEnabled;
    }

    void Update()
    {
        // Обновляем статус в реальном времени
        if (webcamStreamer != null)
        {
            bool isActive = webcamStreamer.IsWebcamActive();
            bool isObjectActive = webcamStreamer.gameObject.activeInHierarchy;

            if (isActive && isObjectActive)
            {
                var resolution = webcamStreamer.GetWebcamResolution();
                if (statusText != null)
                {
                    statusText.text = $"Статус: Активна ({resolution.x}x{resolution.y})";
                }
            }
            else if (!isObjectActive)
            {
                if (statusText != null)
                {
                    statusText.text = "Статус: Объект отключен";
                }
            }
        }
    }

    // Публичные методы для внешнего управления
    public void SetWebcamStreamer(WebcamStreamer streamer)
    {
        webcamStreamer = streamer;
    }

    public bool IsWebcamRunning()
    {
        return webcamStreamer != null && webcamStreamer.IsWebcamActive() && webcamStreamer.gameObject.activeInHierarchy;
    }

    public bool IsWebcamObjectActive()
    {
        return webcamStreamer != null && webcamStreamer.gameObject.activeInHierarchy;
    }

    // Отдельные методы для управления активацией объекта
    public void ActivateWebcamObject()
    {
        if (webcamStreamer != null && !webcamStreamer.gameObject.activeInHierarchy)
        {
            webcamStreamer.gameObject.SetActive(true);
            UpdateStatus("Объект вебкамеры активирован");
        }
    }

    public void DeactivateWebcamObject()
    {
        if (webcamStreamer != null && webcamStreamer.gameObject.activeInHierarchy)
        {
            // Сначала останавливаем камеру, если она работает
            if (webcamStreamer.IsWebcamActive())
            {
                webcamStreamer.StopWebcam();
            }

            webcamStreamer.gameObject.SetActive(false);
            UpdateStatus("Объект вебкамеры отключен");
            UpdateButtonStates(true, false);
        }
    }

    [ContextMenu("Refresh Webcam List")]
    public void RefreshWebcamListManual()
    {
        RefreshWebcamList();
    }
}