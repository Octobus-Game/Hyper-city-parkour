// ===== SpeedrunTimer.cs — дополненная версия =====
// Добавлены: IsRunning, GetFormattedTime(), хук OnRunningChanged

using Mirror;
using UnityEngine;
using TMPro;

public class SpeedrunTimer : NetworkBehaviour
{
    [Header("UI текущего времени (обновляется каждый кадр)")]
    [SerializeField] private TextMeshProUGUI timerText;

    [SyncVar(hook = nameof(OnTimeChanged))]
    private double _syncedTime;

    [SyncVar(hook = nameof(OnRunningChanged))]
    private bool _isRunning = false;

    private double _startTime;

    // ── Публичное свойство для чтения флага ───────
    public bool IsRunning => _isRunning;

    // ──────────────────────────────────────────────
    [Command] public void CmdStartTimer()
    {
        if (_isRunning) return;
        _startTime  = NetworkTime.time;
        _syncedTime = 0;
        _isRunning  = true;
    }

    [Command] public void CmdStopTimer()
    {
        if (!_isRunning) return;
        _syncedTime = NetworkTime.time - _startTime; // фиксируем точное время
        _isRunning  = false;
    }

    [Command] public void CmdResetTimer()
    {
        _isRunning  = false;
        _syncedTime = 0;
    }

    // ──────────────────────────────────────────────
    private void Update()
    {
        // Сервер тикает SyncVar
        if (isServer && _isRunning)
            _syncedTime = NetworkTime.time - _startTime;

        // Локальный игрок рендерит без задержки сети
        if (isLocalPlayer && _isRunning)
            UpdateUI(NetworkTime.time - _startTime);
    }

    // ──────────────────────────────────────────────
    //  Хуки SyncVar
    // ──────────────────────────────────────────────
    private void OnTimeChanged(double _, double newTime)
    {
        // Обновляет UI у наблюдателей (не локальный игрок)
        if (!isLocalPlayer) UpdateUI(newTime);
    }

    private void OnRunningChanged(bool _, bool running)
    {
        // Таймер остановился → замораживаем финальное значение в timerText
        if (!running) UpdateUI(_syncedTime);
    }

    // ──────────────────────────────────────────────
    //  Форматирование
    // ──────────────────────────────────────────────

    /// <summary>Возвращает строку "MM:SS.mmm" для текущего/финального времени.</summary>
    public string GetFormattedTime() => Format(_isRunning
        ? NetworkTime.time - _startTime
        : _syncedTime);

    private static string Format(double totalSeconds)
    {
        int min  = (int)(totalSeconds / 60);
        int sec  = (int)(totalSeconds % 60);
        int ms   = (int)((totalSeconds % 1) * 1000);
        return $"{min:00}:{sec:00}.{ms:000}";
    }

    private void UpdateUI(double t)
    {
        if (timerText != null)
            timerText.text = Format(t);
    }
}