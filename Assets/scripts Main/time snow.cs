using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class TimerTriggerSystem : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float _maxTime = 30f; // Максимальное время таймера
    [SerializeField] private float _restoreRate = 5f; // Скорость восстановления времени в секунду
    [SerializeField] private float _restoreDelay = 0.5f; // Задержка перед началом восстановления

    [Header("UI Elements")]
    [SerializeField] private Text _timerText; // Текст для отображения времени
    [SerializeField] private Image _timerImage; // Image для визуального отображения (Fill Type: Horizontal)
    [SerializeField] private GameObject _warningUI; // UI предупреждения
    [SerializeField] private float _warningThreshold = 10f; // Порог для показа предупреждения

    [Header("Timer Bar Colors")]
    [SerializeField] private Color _normalColor = Color.green; // Обычный цвет
    [SerializeField] private Color _warningColor = Color.yellow; // Цвет предупреждения
    [SerializeField] private Color _dangerColor = Color.red; // Опасный цвет
    [SerializeField] private float _dangerThreshold = 5f; // Порог для красного цвета

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _warningSound;
    [SerializeField] private AudioClip _tickSound;

    [Header("Player Detection")]
    [SerializeField] private string _playerTag = "Player"; // Тег игрока

    private float _currentTime;
    private bool _isPlayerInside = false;
    private bool _isTimerRunning = true;
    private bool _isWarningActive = false;
    private Coroutine _restoreCoroutine;
    private Coroutine _warningCoroutine;

    private void Start()
    {
        // Инициализация таймера
        _currentTime = _maxTime;

        // Настройка UI
        if (_timerImage != null)
        {
            _timerImage.fillAmount = 1f; // Полный заполнение в начале
            _timerImage.color = _normalColor; // Устанавливаем обычный цвет

            // Проверяем что Image настроен правильно
            if (_timerImage.type != Image.Type.Filled)
            {
                Debug.LogWarning("TimerTriggerSystem: Image должен быть типа 'Filled' для корректного отображения!");
            }
        }

        if (_warningUI != null)
        {
            _warningUI.SetActive(false);
        }

        // Запуск таймера
        StartCoroutine(TimerCountdown());
    }

    private void Update()
    {
        UpdateUI();
    }

    private IEnumerator TimerCountdown()
    {
        while (_isTimerRunning && _currentTime > 0)
        {
            // Уменьшаем время только если игрок не в триггере
            if (!_isPlayerInside)
            {
                _currentTime -= Time.deltaTime;

                // Проверяем предупреждение
                CheckWarning();

                // Воспроизводим звук тика при малом времени
                if (_currentTime <= 5f && _currentTime > 0)
                {
                    PlayTickSound();
                }
            }

            yield return null;
        }

        // Время вышло - перезапускаем сцену
        if (_currentTime <= 0)
        {
            RestartScene();
        }
    }

    private void CheckWarning()
    {
        if (_currentTime <= _warningThreshold && !_isWarningActive)
        {
            _isWarningActive = true;

            if (_warningUI != null)
            {
                _warningUI.SetActive(true);
            }

            if (_warningCoroutine == null)
            {
                _warningCoroutine = StartCoroutine(WarningBlink());
            }

            PlayWarningSound();
        }
        else if (_currentTime > _warningThreshold && _isWarningActive)
        {
            _isWarningActive = false;

            if (_warningUI != null)
            {
                _warningUI.SetActive(false);
            }

            if (_warningCoroutine != null)
            {
                StopCoroutine(_warningCoroutine);
                _warningCoroutine = null;
            }
        }
    }

    private IEnumerator WarningBlink()
    {
        while (_isWarningActive)
        {
            if (_warningUI != null)
            {
                _warningUI.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                _warningUI.SetActive(false);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void UpdateUI()
    {
        // Обновляем текст таймера
        if (_timerText != null)
        {
            int minutes = Mathf.FloorToInt(_currentTime / 60);
            int seconds = Mathf.FloorToInt(_currentTime % 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // Обновляем Image заполнение
        if (_timerImage != null)
        {
            float fillAmount = _currentTime / _maxTime;
            _timerImage.fillAmount = fillAmount;

            // Меняем цвет в зависимости от оставшегося времени
            UpdateTimerColor(fillAmount);
        }
    }

    private void UpdateTimerColor(float fillAmount)
    {
        if (_timerImage == null) return;

        float timeLeft = _currentTime;

        if (timeLeft <= _dangerThreshold)
        {
            // Красный цвет для критического времени
            _timerImage.color = _dangerColor;
        }
        else if (timeLeft <= _warningThreshold)
        {
            // Желтый цвет для предупреждения
            _timerImage.color = _warningColor;
        }
        else
        {
            // Зеленый цвет для нормального времени
            _timerImage.color = _normalColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag))
        {
            _isPlayerInside = true;

            // Останавливаем восстановление если оно было активно
            if (_restoreCoroutine != null)
            {
                StopCoroutine(_restoreCoroutine);
            }

            // Начинаем восстановление времени с задержкой
            _restoreCoroutine = StartCoroutine(RestoreTimer());

            Debug.Log("Игрок вошел в триггер - начинается восстановление времени");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_playerTag))
        {
            _isPlayerInside = false;

            // Останавливаем восстановление времени
            if (_restoreCoroutine != null)
            {
                StopCoroutine(_restoreCoroutine);
                _restoreCoroutine = null;
            }

            Debug.Log("Игрок покинул триггер - восстановление времени остановлено");
        }
    }

    private IEnumerator RestoreTimer()
    {
        // Ждем перед началом восстановления
        yield return new WaitForSeconds(_restoreDelay);

        // Постепенно восстанавливаем время
        while (_isPlayerInside && _currentTime < _maxTime)
        {
            _currentTime += _restoreRate * Time.deltaTime;

            // Ограничиваем максимальным временем
            if (_currentTime > _maxTime)
            {
                _currentTime = _maxTime;
            }

            yield return null;
        }

        _restoreCoroutine = null;
    }

    private void PlayWarningSound()
    {
        if (_audioSource != null && _warningSound != null)
        {
            _audioSource.PlayOneShot(_warningSound);
        }
    }

    private void PlayTickSound()
    {
        if (_audioSource != null && _tickSound != null && !_audioSource.isPlaying)
        {
            _audioSource.PlayOneShot(_tickSound);
        }
    }

    private void RestartScene()
    {
        Debug.Log("Время вышло! Перезапуск сцены...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Публичные методы для внешнего управления
    public void PauseTimer()
    {
        _isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        _isTimerRunning = true;
        if (_currentTime > 0)
        {
            StartCoroutine(TimerCountdown());
        }
    }

    public void AddTime(float timeToAdd)
    {
        _currentTime += timeToAdd;
        if (_currentTime > _maxTime)
        {
            _currentTime = _maxTime;
        }
    }

    public void SetMaxTime(float newMaxTime)
    {
        _maxTime = newMaxTime;
        // Image не нужно обновлять максимальное значение, так как используется fillAmount (0-1)
    }

    public float GetCurrentTime()
    {
        return _currentTime;
    }

    public float GetTimePercentage()
    {
        return _currentTime / _maxTime;
    }

    public bool IsPlayerInside()
    {
        return _isPlayerInside;
    }

    // Методы для настройки в инспекторе
    [ContextMenu("Reset Timer")]
    public void ResetTimer()
    {
        _currentTime = _maxTime;
        _isWarningActive = false;

        if (_warningUI != null)
        {
            _warningUI.SetActive(false);
        }

        if (_timerImage != null)
        {
            _timerImage.fillAmount = 1f;
            _timerImage.color = _normalColor;
        }

        if (_warningCoroutine != null)
        {
            StopCoroutine(_warningCoroutine);
            _warningCoroutine = null;
        }
    }

    [ContextMenu("Force Restart Scene")]
    public void ForceRestartScene()
    {
        RestartScene();
    }
}
