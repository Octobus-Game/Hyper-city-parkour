using UnityEngine;
using TMPro;

/// <summary>
/// Секундомер с автозапуском по триггерам.
/// ──────────────────────────────────────────────────────────────
/// ОБЯЗАТЕЛЬНО: прикрепи этот скрипт на объект Player
/// (там уже есть CharacterController — он работает как Rigidbody
///  и позволяет получать OnTriggerEnter / OnTriggerExit).
///
/// Настрой триггер-зоны:
///   • Стартовая зона  → тег  "Start time"  (Is Trigger = true)
///   • Финишная зона   → тег  "End time"    (Is Trigger = true)
/// ──────────────────────────────────────────────────────────────
/// </summary>
public class Stopwatch : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Кнопки (необязательно — ручное управление)")]
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private UnityEngine.UI.Button stopButton;
    [SerializeField] private UnityEngine.UI.Button resetButton;

    [Header("Триггеры")]
    [Tooltip("Тег стартовой зоны. Таймер запускается когда игрок ВЫХОДИТ из неё.")]
    [SerializeField] private string startTriggerTag = "Start time";

    [Tooltip("Тег финишной зоны. Таймер останавливается когда игрок КАСАЕТСЯ её.")]
    [SerializeField] private string endTriggerTag = "End time";

    [Tooltip("Сбрасывать таймер при выходе из стартовой зоны (рекомендуется).")]
    [SerializeField] private bool resetOnStart = true;

    // ─── Состояние ─────────────────────────────────────────────
    private float elapsedTime = 0f;
    private bool  isRunning   = false;

    // ═══════════════════════════════════════════════════════════
    //  UNITY CALLBACKS
    // ═══════════════════════════════════════════════════════════

    private void Start()
    {
        // Привязка кнопок (если назначены в инспекторе)
        if (startButton != null) startButton.onClick.AddListener(StartTimer);
        if (stopButton  != null) stopButton.onClick.AddListener(StopTimer);
        if (resetButton != null) resetButton.onClick.AddListener(ResetTimer);

        UpdateDisplay();
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateDisplay();
    }

    // ═══════════════════════════════════════════════════════════
    //  ТРИГГЕРЫ
    // ═══════════════════════════════════════════════════════════

    /// <summary>Игрок ВЫХОДИТ из зоны → если тег "Start time" — запустить таймер.</summary>
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(startTriggerTag)) return;

        if (resetOnStart)
            elapsedTime = 0f;   // сброс перед стартом

        StartTimer();
        Debug.Log("[Stopwatch] Старт! Игрок вышел из зоны: " + startTriggerTag);
    }

    /// <summary>Игрок КАСАЕТСЯ зоны → если тег "End time" — остановить таймер.</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(endTriggerTag)) return;

        StopTimer();
        Debug.Log("[Stopwatch] Финиш! Результат: " + timerText.text);
    }

    // ═══════════════════════════════════════════════════════════
    //  УПРАВЛЕНИЕ (ручное + вызывается из триггеров)
    // ═══════════════════════════════════════════════════════════

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        isRunning   = false;
        elapsedTime = 0f;
        UpdateDisplay();
    }

    /// <summary>Старт / Стоп одной кнопкой.</summary>
    public void ToggleTimer()
    {
        isRunning = !isRunning;
    }

    // ═══════════════════════════════════════════════════════════
    //  ОТОБРАЖЕНИЕ
    // ═══════════════════════════════════════════════════════════

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        int minutes      = (int)(elapsedTime / 60f);
        int seconds      = (int)(elapsedTime % 60f);
        int milliseconds = (int)((elapsedTime * 1000f) % 1000f);

        // Формат: 00:00:000
        timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }
}