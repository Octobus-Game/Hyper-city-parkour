// ===== StartTrigger.cs =====

using Mirror;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshCollider))]
public class StartTrigger : MonoBehaviour
{
    [Header("Режим триггера")]
    [Tooltip("Если true — этот триггер ОСТАНАВЛИВАЕТ таймер и записывает финальное время")]
    [SerializeField] private bool isStopTrigger = false;

    [Header("Настройки")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce  = true;

    [Header("UI финального времени (только для Stop триггера)")]
    [Tooltip("TMP текст, куда запишется финальное время при остановке")]
    [SerializeField] private TextMeshProUGUI finalTimeText;

    [Tooltip("Префикс перед финальным временем, например: 'Финиш: '")]
    [SerializeField] private string finalTimePrefix = "Финиш: ";

    private bool _hasTriggered = false;

    // ──────────────────────────────────────────────
    private void Awake()
    {
        var col      = GetComponent<MeshCollider>();
        col.convex   = true;
        col.isTrigger = true;
    }

    // ──────────────────────────────────────────────
    private void OnTriggerExit(Collider other)
    {
        if (triggerOnce && _hasTriggered) return;
        if (!other.CompareTag(playerTag))  return;

        var timer = other.GetComponentInParent<SpeedrunTimer>()
                 ?? other.GetComponent<SpeedrunTimer>();

        if (timer == null)
        {
            Debug.LogWarning($"[StartTrigger] SpeedrunTimer не найден на {other.name}");
            return;
        }

        if (!timer.isLocalPlayer) return;

        // ── STOP ──────────────────────────────────
        if (isStopTrigger)
        {
            if (!timer.IsRunning) return;   // таймер и так не идёт

            timer.CmdStopTimer();

            // Фиксируем финальное время и пишем в TMP
            string finalFormatted = timer.GetFormattedTime();
            WriteFinalTime(finalFormatted);

            Debug.Log($"[StopTrigger]  Таймер остановлен → {finalFormatted}");
        }
        // ── START ─────────────────────────────────
        else
        {
            timer.CmdStartTimer();
            Debug.Log("[StartTrigger] Таймер запущен!");
        }

        _hasTriggered = true;
    }

    // ──────────────────────────────────────────────
    private void WriteFinalTime(string formatted)
    {
        if (finalTimeText == null)
        {
            Debug.LogWarning("[StopTrigger] finalTimeText не назначен в инспекторе!");
            return;
        }

        finalTimeText.text = finalTimePrefix + formatted;
    }

    // ──────────────────────────────────────────────
    public void ResetTrigger() => _hasTriggered = false;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Зелёный = старт, Красный = финиш
        Gizmos.color = isStopTrigger
            ? new Color(1f, 0.1f, 0.1f, 0.30f)
            : new Color(0f, 1f, 0.1f, 0.25f);

        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            Gizmos.DrawMesh(mf.sharedMesh,
                transform.position,
                transform.rotation,
                transform.lossyScale);
    }
#endif
}