using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using System.IO;

namespace Setting_save_System
{
    public class CameraSettingsManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Camera targetCamera;

        [Header("FOV Settings")]
        public Slider fovSlider;
        public TextMeshProUGUI fovText;
        public float minFov = 30f;
        public float maxFov = 100f;

        [Header("Far Clip Settings")]
        public Slider farSlider;
        public TextMeshProUGUI farText;
        public float minFar = 50f;
        public float maxFar = 1000f;

        [Header("Post Processing Settings")]
        public Toggle postProcessingToggle;
        public PostProcessVolume postProcessVolume;

        // Настройки по умолчанию
        private float defaultFov = 60f;
        private float defaultFar = 500f;
        private bool defaultPostProcessing = true;

        // Путь к файлу настроек
        private string settingsPath;

        void Start()
        {
            // Создаем путь к файлу настроек
            settingsPath = Path.Combine(Application.dataPath.Replace("_Data", "") + "_Data", "CameraSettings.json");

            // Если камера не задана, используем основную
            if (targetCamera == null)
                targetCamera = Camera.main;

            // Загружаем настройки
            LoadSettings();

            // Инициализируем FOV слайдер
            InitializeFovSlider();

            // Инициализируем Far Clip слайдер
            InitializeFarSlider();

            // Инициализируем Toggle для Post Processing
            InitializePostProcessingToggle();
        }

        void InitializeFovSlider()
        {
            if (fovSlider != null && targetCamera != null)
            {
                fovSlider.minValue = minFov;
                fovSlider.maxValue = maxFov;
                fovSlider.value = targetCamera.fieldOfView;
                fovSlider.onValueChanged.AddListener(OnFovChanged);
                UpdateFovText(fovSlider.value);
            }
        }

        void InitializeFarSlider()
        {
            if (farSlider != null && targetCamera != null)
            {
                farSlider.minValue = minFar;
                farSlider.maxValue = maxFar;
                farSlider.value = targetCamera.farClipPlane;
                farSlider.onValueChanged.AddListener(OnFarChanged);
                UpdateFarText(farSlider.value);
            }
        }

        void InitializePostProcessingToggle()
        {
            if (postProcessingToggle != null && postProcessVolume != null)
            {
                postProcessingToggle.isOn = postProcessVolume.enabled;
                postProcessingToggle.onValueChanged.AddListener(OnPostProcessingToggleChanged);
            }
        }

        void OnFovChanged(float value)
        {
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = value;
                UpdateFovText(value);
                SaveSettings();
            }
        }

        void OnFarChanged(float value)
        {
            if (targetCamera != null)
            {
                targetCamera.farClipPlane = value;
                UpdateFarText(value);
                SaveSettings();
            }
        }

        void OnPostProcessingToggleChanged(bool isOn)
        {
            if (postProcessVolume != null)
            {
                postProcessVolume.enabled = isOn;
                SaveSettings();
            }
        }

        void UpdateFovText(float value)
        {
            if (fovText != null)
                fovText.text = Mathf.RoundToInt(value).ToString();
        }

        void UpdateFarText(float value)
        {
            if (farText != null)
                farText.text = Mathf.RoundToInt(value).ToString();
        }

        // Класс для сериализации настроек
        [System.Serializable]
        public class CameraSettings
        {
            public float fov;
            public float farClipPlane;
            public bool postProcessingEnabled;

            public CameraSettings(float fov, float farClipPlane, bool postProcessingEnabled)
            {
                this.fov = fov;
                this.farClipPlane = farClipPlane;
                this.postProcessingEnabled = postProcessingEnabled;
            }
        }

        // Сохранение настроек
        void SaveSettings()
        {
            try
            {
                CameraSettings settings = new CameraSettings(
                    targetCamera != null ? targetCamera.fieldOfView : defaultFov,
                    targetCamera != null ? targetCamera.farClipPlane : defaultFar,
                    postProcessVolume != null ? postProcessVolume.enabled : defaultPostProcessing
                );

                string json = JsonUtility.ToJson(settings, true);

                // Создаем директорию если она не существует
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                File.WriteAllText(settingsPath, json);
                Debug.Log("Настройки камеры сохранены: " + settingsPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ошибка при сохранении настроек камеры: " + e.Message);
            }
        }

        // Загрузка настроек
        void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    CameraSettings settings = JsonUtility.FromJson<CameraSettings>(json);

                    // Применяем загруженные настройки
                    if (targetCamera != null)
                    {
                        targetCamera.fieldOfView = Mathf.Clamp(settings.fov, minFov, maxFov);
                        targetCamera.farClipPlane = Mathf.Clamp(settings.farClipPlane, minFar, maxFar);
                    }

                    if (postProcessVolume != null)
                    {
                        postProcessVolume.enabled = settings.postProcessingEnabled;
                    }

                    Debug.Log("Настройки камеры загружены: " + settingsPath);
                }
                else
                {
                    // Если файл не существует, применяем настройки по умолчанию
                    ApplyDefaultSettings();
                    Debug.Log("Файл настроек не найден, применены настройки по умолчанию");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Ошибка при загрузке настроек камеры: " + e.Message);
                ApplyDefaultSettings();
            }
        }

        // Применение настроек по умолчанию
        void ApplyDefaultSettings()
        {
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = defaultFov;
                targetCamera.farClipPlane = defaultFar;
            }

            if (postProcessVolume != null)
            {
                postProcessVolume.enabled = defaultPostProcessing;
            }
        }

        // Сброс настроек к значениям по умолчанию (можно вызвать из UI)
        public void ResetToDefault()
        {
            ApplyDefaultSettings();

            // Обновляем UI элементы
            if (fovSlider != null)
            {
                fovSlider.value = defaultFov;
            }

            if (farSlider != null)
            {
                farSlider.value = defaultFar;
            }

            if (postProcessingToggle != null)
            {
                postProcessingToggle.isOn = defaultPostProcessing;
            }

            SaveSettings();
            Debug.Log("Настройки камеры сброшены к значениям по умолчанию");
        }

        void OnDestroy()
        {
            // Убираем слушатели при уничтожении объекта
            if (fovSlider != null)
                fovSlider.onValueChanged.RemoveListener(OnFovChanged);

            if (farSlider != null)
                farSlider.onValueChanged.RemoveListener(OnFarChanged);

            if (postProcessingToggle != null)
                postProcessingToggle.onValueChanged.RemoveListener(OnPostProcessingToggleChanged);
        }
    }
}
