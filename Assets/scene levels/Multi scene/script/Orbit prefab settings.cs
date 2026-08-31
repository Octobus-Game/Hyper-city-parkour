
using UnityEngine;

public class SimpleOrbitSettings : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Range(0.5f, 10f)]
    public float orbitRadius = 2f;
    [Range(10f, 360f)]
    public float orbitSpeed = 90f;
    [Range(1f, 30f)]
    public float orbitDuration = 10f;

    [Header("Release Settings")]
    [Range(5f, 50f)]
    public float releaseForce = 15f;
    public bool randomReleaseDirection = true;

    [Header("Visual Settings")]
    public Color orbitColor = Color.cyan;

    [Header("Info")]
    [SerializeField, ReadOnly] private int orbitingBulletsCount = 0;
    [SerializeField, ReadOnly] private bool hasOrbitingBullets = false;

    private SimpleOrbitTrap orbitTrap;

    void Awake()
    {
        orbitTrap = GetComponent<SimpleOrbitTrap>();
        if (orbitTrap == null)
        {
            Debug.LogWarning("SimpleOrbitSettings: Не найден компонент SimpleOrbitTrap!");
        }
    }

    void Start()
    {
        ApplySettings();
    }

    void Update()
    {
        // Обновляем информационные поля
        if (orbitTrap != null)
        {
            orbitingBulletsCount = orbitTrap.OrbitingBulletsCount;
            hasOrbitingBullets = orbitTrap.HasOrbitingBullets;
        }
    }

    public void ApplySettings()
    {
        if (orbitTrap == null) return;

        orbitTrap.SetOrbitRadius(orbitRadius);
        orbitTrap.SetOrbitSpeed(orbitSpeed);
        orbitTrap.SetOrbitDuration(orbitDuration);
        orbitTrap.releaseForce = releaseForce;
        orbitTrap.randomReleaseDirection = randomReleaseDirection;
        orbitTrap.orbitColor = orbitColor;

        Debug.Log("Настройки орбитальной ловушки обновлены");
    }

    // Публичные методы для внешнего управления
    public void SetOrbitRadius(float radius)
    {
        orbitRadius = Mathf.Clamp(radius, 0.5f, 10f);
        if (orbitTrap != null)
            orbitTrap.SetOrbitRadius(orbitRadius);
    }

    public void SetOrbitSpeed(float speed)
    {
        orbitSpeed = Mathf.Clamp(speed, 10f, 360f);
        if (orbitTrap != null)
            orbitTrap.SetOrbitSpeed(orbitSpeed);
    }

    public void SetOrbitDuration(float duration)
    {
        orbitDuration = Mathf.Clamp(duration, 1f, 30f);
        if (orbitTrap != null)
            orbitTrap.SetOrbitDuration(orbitDuration);
    }

    public void SetReleaseForce(float force)
    {
        releaseForce = Mathf.Clamp(force, 5f, 50f);
        if (orbitTrap != null)
            orbitTrap.releaseForce = releaseForce;
    }

    public void SetOrbitColor(Color color)
    {
        orbitColor = color;
        if (orbitTrap != null)
            orbitTrap.orbitColor = orbitColor;
    }

    public void ToggleRandomReleaseDirection()
    {
        randomReleaseDirection = !randomReleaseDirection;
        if (orbitTrap != null)
            orbitTrap.randomReleaseDirection = randomReleaseDirection;
    }

    public void ReleaseAllBullets()
    {
        if (orbitTrap != null)
            orbitTrap.ReleaseAllBullets();
    }

    // Геттеры для получения информации
    public int OrbitingBulletsCount => orbitingBulletsCount;
    public bool HasOrbitingBullets => hasOrbitingBullets;
    public float CurrentOrbitRadius => orbitRadius;
    public float CurrentOrbitSpeed => orbitSpeed;
    public float CurrentOrbitDuration => orbitDuration;
    public float CurrentReleaseForce => releaseForce;
    public Color CurrentOrbitColor => orbitColor;
    public bool IsRandomReleaseDirection => randomReleaseDirection;

    // Методы для UI
    public void OnOrbitRadiusChanged(float value)
    {
        SetOrbitRadius(value);
    }

    public void OnOrbitSpeedChanged(float value)
    {
        SetOrbitSpeed(value);
    }

    public void OnOrbitDurationChanged(float value)
    {
        SetOrbitDuration(value);
    }

    public void OnReleaseForceChanged(float value)
    {
        SetReleaseForce(value);
    }

    // Отладочные методы
    [ContextMenu("Apply All Settings")]
    void ContextApplySettings()
    {
        ApplySettings();
    }

    [ContextMenu("Reset to Default")]
    void ResetToDefault()
    {
        orbitRadius = 2f;
        orbitSpeed = 90f;
        orbitDuration = 10f;
        releaseForce = 15f;
        randomReleaseDirection = true;
        orbitColor = Color.cyan;
        ApplySettings();
        Debug.Log("Настройки сброшены к значениям по умолчанию");
    }

    [ContextMenu("Show Status")]
    void ShowStatus()
    {
        if (orbitTrap != null)
        {
            Debug.Log($"Орбитальная ловушка статус:");
            Debug.Log($"- Радиус орбиты: {orbitRadius}");
            Debug.Log($"- Скорость орбиты: {orbitSpeed} град/сек");
            Debug.Log($"- Длительность орбиты: {orbitDuration} сек");
            Debug.Log($"- Сила освобождения: {releaseForce}");
            Debug.Log($"- Случайное направление: {randomReleaseDirection}");
            Debug.Log($"- Цвет орбиты: {orbitColor}");
            Debug.Log($"- Пуль в орбите: {orbitingBulletsCount}");
        }
        else
        {
            Debug.LogError("SimpleOrbitTrap компонент не найден!");
        }
    }

    [ContextMenu("Force Release All")]
    void ContextReleaseAll()
    {
        ReleaseAllBullets();
        Debug.Log("Все пули принудительно освобождены из орбиты");
    }

    // Автоматическое применение настроек при изменении в инспекторе
    void OnValidate()
    {
        if (Application.isPlaying && orbitTrap != null)
        {
            ApplySettings();
        }
    }
}

// Атрибут для создания поля только для чтения в инспекторе
public class ReadOnlyAttribute : PropertyAttribute { }