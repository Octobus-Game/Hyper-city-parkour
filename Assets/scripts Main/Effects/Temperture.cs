using UnityEngine;

[ExecuteInEditMode]
public class HeatEffectController : MonoBehaviour
{
    [Header("Heat Settings")]
    [Range(0f, 1f)]
    public float temperature = 0.5f;

    [Range(0f, 2f)]
    public float heatIntensity = 1f;

    [Header("Animation")]
    public bool animateTemperature = false;
    public AnimationCurve temperatureCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float animationSpeed = 1f;

    [Header("Environmental Factors")]
    public bool useTimeOfDay = true;
    public AnimationCurve dayTemperatureCurve = AnimationCurve.Linear(0, 0.2f, 1, 0.8f);

    [Header("Materials")]
    public Material heatPostProcessMaterial;
    public Material[] heatHazeMaterials;

    private Camera cam;
    private float animationTime;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        UpdateTemperature();
        ApplyHeatEffect();
    }

    void UpdateTemperature()
    {
        float finalTemperature = temperature;

        // Анимация температуры
        if (animateTemperature)
        {
            animationTime += Time.deltaTime * animationSpeed;
            float animatedValue = temperatureCurve.Evaluate((animationTime % 1f));
            finalTemperature *= animatedValue;
        }

        // Влияние времени суток
        if (useTimeOfDay)
        {
            float timeOfDay = (Time.time % 86400f) / 86400f; // 24 часа в секундах
            float dayMultiplier = dayTemperatureCurve.Evaluate(timeOfDay);
            finalTemperature *= dayMultiplier;
        }

        // Применяем к материалам
        ApplyTemperatureToMaterials(finalTemperature);
    }

    void ApplyTemperatureToMaterials(float temp)
    {
        // Применяем к пост-процесс материалу
        if (heatPostProcessMaterial != null)
        {
            heatPostProcessMaterial.SetFloat("_Temperature", temp);
        }

        // Применяем к материалам heat haze
        foreach (Material mat in heatHazeMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat("_Temperature", temp);
                mat.SetFloat("_HeatIntensity", heatIntensity);
            }
        }
    }

    void ApplyHeatEffect()
    {
        // Здесь можно добавить дополнительные эффекты:
        // - Изменение FOV камеры
        // - Вибрация камеры
        // - Звуковые эффекты

        if (cam != null && temperature > 0.7f)
        {
            // Легкая вибрация при сильной жаре
            float shake = (temperature - 0.7f) * 0.1f;
            Vector3 shakeOffset = new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake),
                0
            );
            transform.localPosition = shakeOffset;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (heatPostProcessMaterial != null && temperature > 0.1f)
        {
            Graphics.Blit(source, destination, heatPostProcessMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    // Публичные методы для управления из других скриптов
    public void SetTemperature(float temp)
    {
        temperature = Mathf.Clamp01(temp);
    }

    public void IncreaseHeat(float amount)
    {
        temperature = Mathf.Clamp01(temperature + amount);
    }

    public void DecreaseHeat(float amount)
    {
        temperature = Mathf.Clamp01(temperature - amount);
    }
}