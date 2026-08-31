using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SKYBOX : MonoBehaviour
{
    [Header("Skybox Settings")]
    [SerializeField] private Material[] skyboxes = new Material[16]; // Element 0-15
    [SerializeField] private Button[] skyboxButtons = new Button[16]; // Element 0-15

    [Header("Lighting Settings")]
    [SerializeField] private Light directionLight;
    [SerializeField] private SkyboxLightingData[] lightingData = new SkyboxLightingData[16]; // Element 0-15

    [Header("Current State")]
    [SerializeField] private int currentSkyboxIndex = 0;

    void Start()
    {
        InitializeButtons();
        InitializeLightingData();
        SetSkybox(currentSkyboxIndex);
    }

    void InitializeButtons()
    {
        // Привязываем события к кнопкам
        for (int i = 0; i < skyboxButtons.Length; i++)
        {
            if (skyboxButtons[i] != null)
            {
                int index = i; // Локальная копия для замыкания
                skyboxButtons[i].onClick.AddListener(() => ChangeSkybox(index));

                // Опционально: устанавливаем текст кнопки
                Text buttonText = skyboxButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Sky {index}";
                }
            }
        }
    }

    void InitializeLightingData()
    {
        // Инициализируем данные освещения значениями по умолчанию, если они не заданы
        for (int i = 0; i < lightingData.Length; i++)
        {
            if (lightingData[i] == null)
            {
                lightingData[i] = new SkyboxLightingData();
            }
        }
    }

    public void ChangeSkybox(int skyboxIndex)
    {
        // Проверяем корректность индекса
        if (skyboxIndex < 0 || skyboxIndex >= skyboxes.Length)
        {
            Debug.LogWarning($"Invalid skybox index: {skyboxIndex}");
            return;
        }

        // Проверяем, что skybox существует
        if (skyboxes[skyboxIndex] == null)
        {
            Debug.LogWarning($"Skybox at index {skyboxIndex} is null!");
            return;
        }

        SetSkybox(skyboxIndex);
    }

    void SetSkybox(int index)
    {
        // Устанавливаем skybox
        RenderSettings.skybox = skyboxes[index];

        // Применяем настройки освещения
        ApplyLightingSettings(index);

        // Обновляем текущий индекс
        currentSkyboxIndex = index;

        

        Debug.Log($"Changed to skybox {index}: {skyboxes[index].name}");
    }

    void ApplyLightingSettings(int index)
    {
        if (directionLight != null && index < lightingData.Length && lightingData[index] != null)
        {
            directionLight.color = lightingData[index].lightColor;
            directionLight.intensity = lightingData[index].lightIntensity;

            // Дополнительные настройки если нужны
            if (lightingData[index].overrideLightRotation)
            {
                directionLight.transform.rotation = Quaternion.Euler(lightingData[index].lightRotation);
            }
        }
    }

    

    // Утилитарные методы
    public void SetSkyboxByIndex(int index)
    {
        ChangeSkybox(index);
    }

    public void NextSkybox()
    {
        int nextIndex = (currentSkyboxIndex + 1) % skyboxes.Length;
        // Найдем следующий непустой skybox
        for (int i = 0; i < skyboxes.Length; i++)
        {
            if (skyboxes[nextIndex] != null)
            {
                ChangeSkybox(nextIndex);
                break;
            }
            nextIndex = (nextIndex + 1) % skyboxes.Length;
        }
    }

    public void PreviousSkybox()
    {
        int prevIndex = (currentSkyboxIndex - 1 + skyboxes.Length) % skyboxes.Length;
        // Найдем предыдущий непустой skybox
        for (int i = 0; i < skyboxes.Length; i++)
        {
            if (skyboxes[prevIndex] != null)
            {
                ChangeSkybox(prevIndex);
                break;
            }
            prevIndex = (prevIndex - 1 + skyboxes.Length) % skyboxes.Length;
        }
    }

    public void SetButtonInteractable(int index, bool interactable)
    {
        if (index >= 0 && index < skyboxButtons.Length && skyboxButtons[index] != null)
        {
            skyboxButtons[index].interactable = interactable;
        }
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < skyboxButtons.Length; i++)
        {
            SetButtonInteractable(i, interactable);
        }
    }

    // Методы для динамической настройки
    public void SetSkyboxMaterial(int index, Material skyboxMaterial)
    {
        if (index >= 0 && index < skyboxes.Length)
        {
            skyboxes[index] = skyboxMaterial;
        }
    }

    public void SetLightingData(int index, Color lightColor, float lightIntensity)
    {
        if (index >= 0 && index < lightingData.Length)
        {
            if (lightingData[index] == null)
                lightingData[index] = new SkyboxLightingData();

            lightingData[index].lightColor = lightColor;
            lightingData[index].lightIntensity = lightIntensity;
        }
    }

    // Геттеры
    public int GetCurrentSkyboxIndex()
    {
        return currentSkyboxIndex;
    }

    public Material GetCurrentSkybox()
    {
        return skyboxes[currentSkyboxIndex];
    }

    public Material GetSkyboxByIndex(int index)
    {
        if (index >= 0 && index < skyboxes.Length)
            return skyboxes[index];
        return null;
    }

    // Метод для получения всех доступных skybox
    public List<int> GetAvailableSkyboxIndices()
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < skyboxes.Length; i++)
        {
            if (skyboxes[i] != null)
            {
                availableIndices.Add(i);
            }
        }
        return availableIndices;
    }
}

// Класс для хранения данных освещения для каждого skybox
[System.Serializable]
public class SkyboxLightingData
{
    [Header("Light Settings")]
    public Color lightColor = Color.white;
    [Range(0f, 8f)]
    public float lightIntensity = 1.0f;

    [Header("Advanced Light Settings")]
    public bool overrideLightRotation = false;
    public Vector3 lightRotation = new Vector3(50f, -30f, 0f);

    [Header("Additional Settings")]
    public bool enableShadows = true;
    public LightShadows shadowType = LightShadows.Soft;

    public SkyboxLightingData()
    {
        lightColor = Color.white;
        lightIntensity = 1.0f;
        overrideLightRotation = false;
        lightRotation = new Vector3(50f, -30f, 0f);
        enableShadows = true;
        shadowType = LightShadows.Soft;
    }

    public SkyboxLightingData(Color color, float intensity)
    {
        lightColor = color;
        lightIntensity = intensity;
        overrideLightRotation = false;
        lightRotation = new Vector3(50f, -30f, 0f);
        enableShadows = true;
        shadowType = LightShadows.Soft;
    }
}