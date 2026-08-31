using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Osi : MonoBehaviour
{
    [Header("Position Sliders")]
    public Slider xSlider;
    public Slider ySlider;
    public Slider zSlider;

    [Header("Rotation Sliders")]
    public Slider xRotSlider;
    public Slider yRotSlider;
    public Slider zRotSlider;

    [Header("Scale Sliders")]
    public Slider xScaleSlider;
    public Slider yScaleSlider;
    public Slider zScaleSlider;

    public Transform targetObject; // The object to move

    [Header("Slider Settings")]
    public float minPosValue = -10f;     // Минимальное значение слайдера позиции
    public float maxPosValue = 10f;      // Максимальное значение слайдера позиции
    public float minRotValue = -180f;    // Минимальное значение слайдера поворота
    public float maxRotValue = 180f;     // Максимальное значение слайдера поворота
    public float minScaleValue = 0.1f;   // Минимальное значение слайдера масштаба
    public float maxScaleValue = 3f;     // Максимальное значение слайдера масштаба

    void Start()
    {
        // Настройка диапазона слайдеров
        SetupSliders();

        // Set up listeners for position sliders
        xSlider.onValueChanged.AddListener(OnSliderChanged);
        ySlider.onValueChanged.AddListener(OnSliderChanged);
        zSlider.onValueChanged.AddListener(OnSliderChanged);

        // Set up listeners for rotation sliders
        xRotSlider.onValueChanged.AddListener(OnSliderChanged);
        yRotSlider.onValueChanged.AddListener(OnSliderChanged);
        zRotSlider.onValueChanged.AddListener(OnSliderChanged);

        // Set up listeners for scale sliders
        xScaleSlider.onValueChanged.AddListener(OnSliderChanged);
        yScaleSlider.onValueChanged.AddListener(OnSliderChanged);
        zScaleSlider.onValueChanged.AddListener(OnSliderChanged);

        // Initialize position, rotation and scale
        UpdateObjectTransform();
    }

    void SetupSliders()
    {
        // Настраиваем диапазон для позиционных слайдеров
        if (xSlider != null)
        {
            xSlider.minValue = minPosValue;
            xSlider.maxValue = maxPosValue;
            xSlider.value = 0f;
        }

        if (ySlider != null)
        {
            ySlider.minValue = minPosValue;
            ySlider.maxValue = maxPosValue;
            ySlider.value = 0f;
        }

        if (zSlider != null)
        {
            zSlider.minValue = minPosValue;
            zSlider.maxValue = maxPosValue;
            zSlider.value = 0f;
        }

        // Настраиваем диапазон для слайдеров поворота
        if (xRotSlider != null)
        {
            xRotSlider.minValue = minRotValue;
            xRotSlider.maxValue = maxRotValue;
            xRotSlider.value = 0f;
        }

        if (yRotSlider != null)
        {
            yRotSlider.minValue = minRotValue;
            yRotSlider.maxValue = maxRotValue;
            yRotSlider.value = 0f;
        }

        if (zRotSlider != null)
        {
            zRotSlider.minValue = minRotValue;
            zRotSlider.maxValue = maxRotValue;
            zRotSlider.value = 0f;
        }

        // Настраиваем диапазон для слайдеров масштаба
        if (xScaleSlider != null)
        {
            xScaleSlider.minValue = minScaleValue;
            xScaleSlider.maxValue = maxScaleValue;
            xScaleSlider.value = 1f; // Начальный масштаб 1
        }

        if (yScaleSlider != null)
        {
            yScaleSlider.minValue = minScaleValue;
            yScaleSlider.maxValue = maxScaleValue;
            yScaleSlider.value = 1f;
        }

        if (zScaleSlider != null)
        {
            zScaleSlider.minValue = minScaleValue;
            zScaleSlider.maxValue = maxScaleValue;
            zScaleSlider.value = 1f;
        }
    }

    void OnSliderChanged(float value)
    {
        UpdateObjectTransform();
    }

    void UpdateObjectTransform()
    {
        if (targetObject != null)
        {
            // Обновляем позицию
            Vector3 newPos = new Vector3(xSlider.value, ySlider.value, zSlider.value);
            targetObject.position = newPos;

            // Обновляем поворот
            Vector3 newRot = new Vector3(xRotSlider.value, yRotSlider.value, zRotSlider.value);
            targetObject.rotation = Quaternion.Euler(newRot);

            // Обновляем масштаб
            Vector3 newScale = new Vector3(xScaleSlider.value, yScaleSlider.value, zScaleSlider.value);
            targetObject.localScale = newScale;
        }
    }
}