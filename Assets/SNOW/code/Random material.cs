using UnityEngine;

public class RandomMaterialColor : MonoBehaviour
{
    [Header("Настройки цвета")]
    [Tooltip("Включать ли случайность для альфа-канала")]
    public bool randomizeAlpha = false;
    [Tooltip("Минимальное значение альфа-канала (если включена случайность)")]
    [Range(0f, 1f)]
    public float minAlpha = 1f;

    [Header("Настройки Emission")]
    [Tooltip("Включить emission для материала")]
    public bool enableEmission = false;
    [Tooltip("Использовать случайный цвет для emission")]
    public bool randomEmissionColor = true;
    [Tooltip("Пользовательский цвет emission (если randomEmissionColor выключен)")]
    [ColorUsage(true, true)]
    public Color customEmissionColor = Color.white;
    [Tooltip("Интенсивность emission")]
    [Range(0f, 5f)]
    public float emissionIntensity = 1f;

    private Renderer objectRenderer;

    void Start()
    {
        // Получаем компонент Renderer
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError($"Renderer не найден на объекте {gameObject.name}!");
            return;
        }

        // Устанавливаем случайный цвет
        SetRandomColor();
    }

    void SetRandomColor()
    {
        // Генерируем случайные значения RGB
        float red = Random.Range(0f, 1f);
        float green = Random.Range(0f, 1f);
        float blue = Random.Range(0f, 1f);

        // Определяем альфа-канал
        float alpha = randomizeAlpha ? Random.Range(minAlpha, 1f) : 1f;

        // Создаем новый цвет
        Color randomColor = new Color(red, green, blue, alpha);

        // Применяем цвет к материалу
        objectRenderer.material.color = randomColor;

        // Настраиваем emission
        if (enableEmission)
        {
            objectRenderer.material.EnableKeyword("_EMISSION");

            Color emissionColor;
            if (randomEmissionColor)
            {
                // Используем случайный цвет для emission
                emissionColor = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f)
                ) * emissionIntensity;
            }
            else
            {
                // Используем пользовательский цвет
                emissionColor = customEmissionColor * emissionIntensity;
            }

            objectRenderer.material.SetColor("_EmissionColor", emissionColor);
        }
        else
        {
            objectRenderer.material.DisableKeyword("_EMISSION");
        }

        // Выводим информацию в консоль
        Debug.Log($"Установлен случайный цвет для {gameObject.name}: {randomColor}");
        if (enableEmission)
        {
            Debug.Log($"Emission включен с интенсивностью: {emissionIntensity}");
        }
    }

    // Метод для изменения цвета во время выполнения (опционально)
    [ContextMenu("Установить новый случайный цвет")]
    public void SetNewRandomColor()
    {
        if (objectRenderer != null)
        {
            SetRandomColor();
        }
    }
}