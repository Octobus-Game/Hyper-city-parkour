using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPlayer : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    [Header("Gradient Controls")]
    public Button gradientButton;
    public Slider speedSlider;
    public Text speedText; // Опционально для отображения скорости

    [Header("Material Settings")]
    public Material targetMaterial;

    [Header("Color Preview (Optional)")]
    public Image colorPreview;

    [Header("Gradient Settings")]
    public float gradientSpeed = 1f;
    public bool isGradientActive = false;

    private Color currentColor;
    private float hueValue = 0f;
    private Coroutine gradientCoroutine;

    void Start()
    {
        // Инициализация слайдеров
        InitializeSliders();
        InitializeGradientControls();

        // Получение текущего цвета материала
        if (targetMaterial != null)
        {
            currentColor = targetMaterial.color;
            UpdateSlidersFromColor();
        }
        else
        {
            currentColor = Color.white;
        }

        // Обновление цвета при старте
        UpdateColor();
    }

    void InitializeSliders()
    {
        // Настройка диапазона слайдеров (0-1)
        if (redSlider != null)
        {
            redSlider.minValue = 0f;
            redSlider.maxValue = 1f;
            redSlider.onValueChanged.AddListener(OnRedChanged);
        }

        if (greenSlider != null)
        {
            greenSlider.minValue = 0f;
            greenSlider.maxValue = 1f;
            greenSlider.onValueChanged.AddListener(OnGreenChanged);
        }

        if (blueSlider != null)
        {
            blueSlider.minValue = 0f;
            blueSlider.maxValue = 1f;
            blueSlider.onValueChanged.AddListener(OnBlueChanged);
        }
    }

    void InitializeGradientControls()
    {
        // Настройка кнопки градиента
        if (gradientButton != null)
        {
            gradientButton.onClick.AddListener(ToggleGradient);
            UpdateGradientButtonText();
        }

        // Настройка слайдера скорости
        if (speedSlider != null)
        {
            speedSlider.minValue = 0.1f;
            speedSlider.maxValue = 5f;
            speedSlider.value = gradientSpeed;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        // Обновление текста скорости
        UpdateSpeedText();
    }

    void UpdateSlidersFromColor()
    {
        if (redSlider != null)
            redSlider.value = currentColor.r;
        if (greenSlider != null)
            greenSlider.value = currentColor.g;
        if (blueSlider != null)
            blueSlider.value = currentColor.b;
    }

    public void OnRedChanged(float value)
    {
        if (!isGradientActive)
        {
            currentColor.r = value;
            UpdateColor();
        }
    }

    public void OnGreenChanged(float value)
    {
        if (!isGradientActive)
        {
            currentColor.g = value;
            UpdateColor();
        }
    }

    public void OnBlueChanged(float value)
    {
        if (!isGradientActive)
        {
            currentColor.b = value;
            UpdateColor();
        }
    }

    public void OnSpeedChanged(float value)
    {
        gradientSpeed = value;
        UpdateSpeedText();
    }

    void UpdateSpeedText()
    {
        if (speedText != null)
        {
            speedText.text = $"Скорость: {gradientSpeed:F1}";
        }
    }

    public void ToggleGradient()
    {
        isGradientActive = !isGradientActive;

        if (isGradientActive)
        {
            StartGradient();
        }
        else
        {
            StopGradient();
        }

        UpdateGradientButtonText();
        SetSlidersInteractable(!isGradientActive);
    }

    void StartGradient()
    {
        if (gradientCoroutine != null)
        {
            StopCoroutine(gradientCoroutine);
        }

        gradientCoroutine = StartCoroutine(GradientCoroutine());
    }

    void StopGradient()
    {
        if (gradientCoroutine != null)
        {
            StopCoroutine(gradientCoroutine);
            gradientCoroutine = null;
        }
    }

    IEnumerator GradientCoroutine()
    {
        while (isGradientActive)
        {
            // Плавное изменение Hue от 0 до 1
            hueValue += Time.deltaTime * gradientSpeed * 0.1f;

            if (hueValue >= 1f)
            {
                hueValue = 0f;
            }

            // Конвертация HSV в RGB для получения радужного эффекта
            currentColor = Color.HSVToRGB(hueValue, 1f, 1f);

            // Обновление цвета и слайдеров
            UpdateColor();
            UpdateSlidersFromColor();

            yield return null;
        }
    }

    void UpdateGradientButtonText()
    {
        if (gradientButton != null)
        {
            Text buttonText = gradientButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isGradientActive ? "Остановить градиент" : "Запустить градиент";
            }
        }
    }

    void SetSlidersInteractable(bool interactable)
    {
        if (redSlider != null)
            redSlider.interactable = interactable;
        if (greenSlider != null)
            greenSlider.interactable = interactable;
        if (blueSlider != null)
            blueSlider.interactable = interactable;
    }

    void UpdateColor()
    {
        // Обновление цвета материала
        if (targetMaterial != null)
        {
            targetMaterial.color = currentColor;
        }

        // Обновление превью цвета (если есть)
        if (colorPreview != null)
        {
            colorPreview.color = currentColor;
        }
    }

    // Метод для установки цвета программно
    public void SetColor(Color newColor)
    {
        if (!isGradientActive)
        {
            currentColor = newColor;
            UpdateSlidersFromColor();
            UpdateColor();
        }
    }

    // Метод для получения текущего цвета
    public Color GetCurrentColor()
    {
        return currentColor;
    }

    // Сброс цвета к белому
    public void ResetToWhite()
    {
        if (!isGradientActive)
        {
            SetColor(Color.white);
        }
    }

    // Сброс цвета к черному
    public void ResetToBlack()
    {
        if (!isGradientActive)
        {
            SetColor(Color.black);
        }
    }

    void OnDestroy()
    {
        // Остановка корутины при уничтожении объекта
        if (gradientCoroutine != null)
        {
            StopCoroutine(gradientCoroutine);
        }

        // Очистка слушателей событий
        if (redSlider != null)
            redSlider.onValueChanged.RemoveListener(OnRedChanged);
        if (greenSlider != null)
            greenSlider.onValueChanged.RemoveListener(OnGreenChanged);
        if (blueSlider != null)
            blueSlider.onValueChanged.RemoveListener(OnBlueChanged);
        if (gradientButton != null)
            gradientButton.onClick.RemoveListener(ToggleGradient);
        if (speedSlider != null)
            speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
    }
}
