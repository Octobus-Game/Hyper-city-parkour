using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Отслеживает рывок, двойной прыжок и скорость — начисляет очки.
/// actionScoreText отображает прокручивающийся список последних действий
/// с плавным угасанием каждой записи.
/// </summary>
public class ScoreMovement : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════

    [Header("Ссылка на контроллер игрока")]
    [SerializeField] private CharacterMovement0 player;

    [Header("TMP Тексты")]
    [Tooltip("Список действий со счётом (Double Jump +200, Dash +120, ...)")]
    [SerializeField] private TMP_Text actionScoreText;

    [Tooltip("Общий накопленный счёт")]
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Настройки списка")]
    [Tooltip("Сколько секунд запись остаётся в списке")]
    [SerializeField] private float entryLifetime = 3f;

    [Tooltip("За сколько секунд до исчезновения запись начинает угасать (0 = мгновенно)")]
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Tooltip("Максимальное число строк в списке")]
    [SerializeField] private int maxListEntries = 6;

    [Header("Конвертация скорости")]
    [Tooltip("1 = нативные юниты Unity (m/s). 3.6 = перевод в km/h")]
    [SerializeField] private float speedConversionFactor = 1f;

    // ═══════════════════════════════════════════════════════════════
    //  КОНСТАНТЫ ОЧКОВ
    // ═══════════════════════════════════════════════════════════════

    private const int SCORE_DOUBLE_JUMP = 200;
    private const int SCORE_DASH        = 120;
    private const int SCORE_SPEED_40    =  30;
    private const int SCORE_SPEED_60    =  70;
    private const int SCORE_SPEED_90    = 130;

    private const float SPEED_THRESHOLD_40 = 40f;
    private const float SPEED_THRESHOLD_60 = 60f;
    private const float SPEED_THRESHOLD_90 = 90f;

    // ═══════════════════════════════════════════════════════════════
    //  ЗАПИСЬ СПИСКА
    // ═══════════════════════════════════════════════════════════════

    private struct ScoreEntry
    {
        public string label;      // "Double Jump", "Dash", "90 km/h" …
        public int    points;     // +200, +120 …
        public float  expireTime; // Time.time + entryLifetime
    }

    // ═══════════════════════════════════════════════════════════════
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ═══════════════════════════════════════════════════════════════

    private int  _totalScore          = 0;
    private bool _prevIsDashing       = false;
    private bool _prevHasDoubleJumped = false;
    private bool _awarded40           = false;
    private bool _awarded60           = false;
    private bool _awarded90           = false;

    private FieldInfo _fieldIsDashing;
    private FieldInfo _fieldHasDoubleJumped;

    private readonly List<ScoreEntry> _entries = new List<ScoreEntry>();
    private readonly StringBuilder   _sb       = new StringBuilder();

    // ═══════════════════════════════════════════════════════════════
    //  UNITY CALLBACKS
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (player == null)
            player = GetComponent<CharacterMovement0>();

        if (player == null)
        {
            Debug.LogError("[ScoreMovement] CharacterMovement0 не найден! Назначь ссылку в инспекторе.");
            enabled = false;
            return;
        }

        var type  = typeof(CharacterMovement0);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        _fieldIsDashing       = type.GetField("_isDashing",       flags);
        _fieldHasDoubleJumped = type.GetField("_hasDoubleJumped", flags);

        if (_fieldIsDashing       == null) Debug.LogWarning("[ScoreMovement] Поле '_isDashing' не найдено.");
        if (_fieldHasDoubleJumped == null) Debug.LogWarning("[ScoreMovement] Поле '_hasDoubleJumped' не найдено.");

        RefreshUI();
    }

    private void Update()
    {
        if (player == null) return;

        CheckDash();
        CheckDoubleJump();
        CheckSpeed();

        // Удаляем истёкшие записи и перерисовываем (каждый кадр — для плавного fade)
        PurgeExpired();
        RebuildList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ПРОВЕРКИ
    // ═══════════════════════════════════════════════════════════════

    private void CheckDash()
    {
        if (_fieldIsDashing == null) return;
        bool isDashing = (bool)_fieldIsDashing.GetValue(player);
        if (isDashing && !_prevIsDashing)
            AwardScore(SCORE_DASH, "Dash");
        _prevIsDashing = isDashing;
    }

    private void CheckDoubleJump()
    {
        if (_fieldHasDoubleJumped == null) return;
        bool hasDoubleJumped = (bool)_fieldHasDoubleJumped.GetValue(player);
        if (hasDoubleJumped && !_prevHasDoubleJumped)
            AwardScore(SCORE_DOUBLE_JUMP, "Double Jump");
        _prevHasDoubleJumped = hasDoubleJumped;
    }

    private void CheckSpeed()
    {
        CharacterController cc = player._characterController;
        if (cc == null) return;

        float speed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude
                      * speedConversionFactor;

        // Сброс флагов при замедлении
        if      (speed < SPEED_THRESHOLD_40) { _awarded40 = false; _awarded60 = false; _awarded90 = false; }
        else if (speed < SPEED_THRESHOLD_60) {                      _awarded60 = false; _awarded90 = false; }
        else if (speed < SPEED_THRESHOLD_90) {                                           _awarded90 = false; }

        // Выдача наград по убыванию приоритета
        if      (speed >= SPEED_THRESHOLD_90 && !_awarded90) { _awarded90 = true; AwardScore(SCORE_SPEED_90, "90 km/h"); }
        else if (speed >= SPEED_THRESHOLD_60 && !_awarded60) { _awarded60 = true; AwardScore(SCORE_SPEED_60, "60 km/h"); }
        else if (speed >= SPEED_THRESHOLD_40 && !_awarded40) { _awarded40 = true; AwardScore(SCORE_SPEED_40, "40 km/h"); }
    }

    // ═══════════════════════════════════════════════════════════════
    //  НАЧИСЛЕНИЕ ОЧКОВ
    // ═══════════════════════════════════════════════════════════════

    private void AwardScore(int points, string label)
    {
        _totalScore += points;
        Debug.Log($"[ScoreMovement] +{points} ({label}) | Итого: {_totalScore}");

        // Добавляем запись в список (новые — снизу)
        _entries.Add(new ScoreEntry
        {
            label      = label,
            points     = points,
            expireTime = Time.time + entryLifetime
        });

        // Обрезаем лишнее сверху
        if (_entries.Count > maxListEntries)
            _entries.RemoveAt(0);

        RefreshTotalText();
        // Список перерисуется в Update() уже в этом же кадре
    }

    // ═══════════════════════════════════════════════════════════════
    //  УПРАВЛЕНИЕ СПИСКОМ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Удаляет записи с истёкшим временем жизни.</summary>
    private void PurgeExpired()
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _entries[i].expireTime)
                _entries.RemoveAt(i);
        }
    }

    /// <summary>
    /// Пересобирает текст из списка.
    /// Каждая строка выглядит так:  Double Jump +200
    /// Записи, близкие к удалению, плавно угасают через тег &lt;alpha=#XX&gt;.
    /// </summary>
    private void RebuildList()
    {
        if (actionScoreText == null) return;

        if (_entries.Count == 0)
        {
            actionScoreText.gameObject.SetActive(false);
            return;
        }

        actionScoreText.gameObject.SetActive(true);
        _sb.Clear();

        for (int i = 0; i < _entries.Count; i++)
        {
            ScoreEntry e = _entries[i];

            // ── Fade-out ──────────────────────────────────────────
            float timeLeft = e.expireTime - Time.time;
            float alpha    = (fadeOutDuration > 0f && timeLeft < fadeOutDuration)
                             ? Mathf.Clamp01(timeLeft / fadeOutDuration)
                             : 1f;
            byte  a8       = (byte)(alpha * 255f);   // 0xFF = полностью видимо

            // ── Строка: "Double Jump +200" ────────────────────────
            _sb.Append($"<alpha=#{a8:X2}>{e.label} +{e.points}");

            if (i < _entries.Count - 1)
                _sb.AppendLine();
        }

        actionScoreText.text = _sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  UI
    // ═══════════════════════════════════════════════════════════════

    private void RefreshTotalText()
    {
        if (totalScoreText != null)
            totalScoreText.text = _totalScore.ToString();
    }

    private void RefreshUI()
    {
        if (actionScoreText != null)
        {
            actionScoreText.text = "";
            actionScoreText.gameObject.SetActive(false);
        }
        RefreshTotalText();
    }

    // ═══════════════════════════════════════════════════════════════
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Сбросить счёт и очистить список (например, при рестарте).</summary>
    public void ResetScore()
    {
        _totalScore = 0;
        _entries.Clear();
        RefreshUI();
    }

    /// <summary>Получить текущий счёт.</summary>
    public int GetScore() => _totalScore;

    /// <summary>
    /// Начислить очки извне (например, из TeleportTriggerCrystal).
    /// </summary>
    /// <param name="points">Количество очков.</param>
    /// <param name="label">Название действия, отображаемое в списке.</param>
    public void AddScore(int points, string label) => AwardScore(points, label);
}