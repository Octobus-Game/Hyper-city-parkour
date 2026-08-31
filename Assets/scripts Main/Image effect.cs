using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Imageeffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerTriggerSystem _timerSystem;
    [SerializeField] private Image _timerImage;
    [SerializeField] private Image _backgroundImage; // Фон для таймера
    [SerializeField] private Text _timerText;

    [Header("Animation Settings")]
    [SerializeField] private bool _enablePulseEffect = true;
    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _pulseIntensity = 0.3f;
    [SerializeField] private float _pulseTriggerTime = 10f; // Время когда начинается пульсация

    [Header("Color Animation")]
    [SerializeField] private bool _enableColorTransition = true;
    [SerializeField] private float _colorTransitionSpeed = 2f;
    [SerializeField] private AnimationCurve _colorTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shake Effect")]
    [SerializeField] private bool _enableShakeEffect = true;
    [SerializeField] private float _shakeIntensity = 5f;
    [SerializeField] private float _shakeSpeed = 10f;
    [SerializeField] private float _shakeTriggerTime = 5f;

    [Header("Gradient Colors")]
    [SerializeField] private Gradient _timerGradient = new Gradient();

    private Vector3 _originalPosition;
    private Color _originalColor;
    private float _pulseTimer = 0f;
    private bool _isShaking = false;

    private void Start()
    {
        // Автоматически найти компоненты если не назначены
        if (_timerSystem == null)
            _timerSystem = FindObjectOfType<TimerTriggerSystem>();

        if (_timerImage == null)
            _timerImage = GetComponent<Image>();

        // Сохраняем изначальные значения
        if (_timerImage != null)
        {
            _originalPosition = _timerImage.transform.localPosition;
            _originalColor = _timerImage.color;
        }

        // Настраиваем градиент по умолчанию если он пустой
        SetupDefaultGradient();
    }

    private void Update()
    {
        if (_timerSystem == null || _timerImage == null) return;

        float currentTime = _timerSystem.GetCurrentTime();
        float timePercentage = _timerSystem.GetTimePercentage();

        // Обновляем заполнение
        _timerImage.fillAmount = timePercentage;

        // Применяем цветовые эффекты
        if (_enableColorTransition)
        {
            ApplyGradientColor(timePercentage);
        }

        // Применяем эффект пульсации
        if (_enablePulseEffect && currentTime <= _pulseTriggerTime)
        {
            ApplyPulseEffect();
        }

        // Применяем эффект тряски
        if (_enableShakeEffect && currentTime <= _shakeTriggerTime)
        {
            ApplyShakeEffect();
        }
        else if (_isShaking)
        {
            // Возвращаем в исходное положение
            _timerImage.transform.localPosition = _originalPosition;
            _isShaking = false;
        }
    }

    private void SetupDefaultGradient()
    {
        if (_timerGradient.colorKeys.Length == 0)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[4];
            colorKeys[0].color = Color.red;
            colorKeys[0].time = 0f;
            colorKeys[1].color = Color.yellow;
            colorKeys[1].time = 0.33f;
            colorKeys[2].color = Color.green;
            colorKeys[2].time = 0.66f;
            colorKeys[3].color = Color.cyan;
            colorKeys[3].time = 1f;

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0f;
            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 1f;

            _timerGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void ApplyGradientColor(float timePercentage)
    {
        Color gradientColor = _timerGradient.Evaluate(timePercentage);

        if (_enableColorTransition)
        {
            // Плавный переход цвета
            float lerpValue = _colorTransitionCurve.Evaluate(timePercentage);
            _timerImage.color = Color.Lerp(_timerImage.color, gradientColor,
                Time.deltaTime * _colorTransitionSpeed);
        }
        else
        {
            _timerImage.color = gradientColor;
        }
    }

    private void ApplyPulseEffect()
    {
        _pulseTimer += Time.deltaTime * _pulseSpeed;
        float pulseValue = Mathf.Sin(_pulseTimer) * _pulseIntensity;

        // Применяем пульсацию к прозрачности
        Color currentColor = _timerImage.color;
        currentColor.a = Mathf.Clamp01(_originalColor.a + pulseValue);
        _timerImage.color = currentColor;

        // Применяем пульсацию к масштабу
        Vector3 scale = Vector3.one + Vector3.one * pulseValue * 0.1f;
        _timerImage.transform.localScale = scale;
    }

    private void ApplyShakeEffect()
    {
        if (!_isShaking)
        {
            _isShaking = true;
        }

        // Создаем случайное смещение
        float shakeX = Random.Range(-_shakeIntensity, _shakeIntensity);
        float shakeY = Random.Range(-_shakeIntensity, _shakeIntensity);

        Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0);

        // Применяем тряску с учетом времени
        float shakeAmount = Mathf.Sin(Time.time * _shakeSpeed) * 0.5f + 0.5f;
        _timerImage.transform.localPosition = _originalPosition + shakeOffset * shakeAmount;
    }

    // Методы для настройки эффектов
    public void SetPulseEffect(bool enable)
    {
        _enablePulseEffect = enable;
        if (!enable)
        {
            _timerImage.transform.localScale = Vector3.one;
            Color color = _timerImage.color;
            color.a = _originalColor.a;
            _timerImage.color = color;
        }
    }

    public void SetShakeEffect(bool enable)
    {
        _enableShakeEffect = enable;
        if (!enable && _isShaking)
        {
            _timerImage.transform.localPosition = _originalPosition;
            _isShaking = false;
        }
    }

    public void SetColorTransition(bool enable)
    {
        _enableColorTransition = enable;
        if (!enable)
        {
            _timerImage.color = _originalColor;
        }
    }

    // Методы для изменения параметров во время выполнения
    public void SetPulseSpeed(float speed)
    {
        _pulseSpeed = speed;
    }

    public void SetShakeIntensity(float intensity)
    {
        _shakeIntensity = intensity;
    }

    public void SetCustomGradient(Gradient gradient)
    {
        _timerGradient = gradient;
    }

    // Сброс всех эффектов
    public void ResetEffects()
    {
        _timerImage.transform.localPosition = _originalPosition;
        _timerImage.transform.localScale = Vector3.one;
        _timerImage.color = _originalColor;
        _isShaking = false;
        _pulseTimer = 0f;
    }
}
