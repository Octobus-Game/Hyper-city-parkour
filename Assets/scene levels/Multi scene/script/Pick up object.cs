using UnityEngine;
using Mirror;

/// <summary>
/// Прикрепляется к каждому объекту на слое "Multiplayer-object".
/// Управляет сетевым авторитетом и физикой при подборе / броске.
///
/// Требования к объекту:
///  - NetworkIdentity (с включённым clientAuthority)
///  - Rigidbody (можно без него — скрипт проверяет)
///  - Слой: "Multiplayer-object"
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class PickupObject : NetworkBehaviour
{
    [Header("Hold Settings")]
    [Tooltip("Скорость сетевой синхронизации позиции пока объект держат")]
    public float networkSyncRate = 20f;

    // ──────────────────────────────────────────────
    // Состояние — SyncVar, чтобы все клиенты знали
    // ──────────────────────────────────────────────

    [SyncVar(hook = nameof(OnHeldStateChanged))]
    private bool isBeingHeld = false;

    [SyncVar]
    private Vector3 networkPosition;

    [SyncVar]
    private Quaternion networkRotation;

    // ──────────────────────────────────────────────
    private Rigidbody rb;
    private float syncTimer;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Убеждаемся, что объект на правильном слое
        int correctLayer = LayerMask.NameToLayer("Multiplayer-object");
        if (correctLayer != -1 && gameObject.layer != correctLayer)
        {
            Debug.LogWarning($"[PickupObject] {name}: слой не 'Multiplayer-object'. " +
                             "Исправьте в инспекторе или через скрипт.");
        }
    }

    void Update()
    {
        // Авторитетный клиент (тот, кто держит) отправляет позицию на сервер
        if (isOwned && isBeingHeld)
        {
            syncTimer += Time.deltaTime;
            if (syncTimer >= 1f / networkSyncRate)
            {
                syncTimer = 0f;
                CmdSyncTransform(transform.position, transform.rotation);
            }
        }

        // Остальные клиенты интерполируют к сетевой позиции
        if (!isOwned && isBeingHeld)
        {
            transform.position = Vector3.Lerp(
                transform.position, networkPosition, Time.deltaTime * 20f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, networkRotation, Time.deltaTime * 20f);
        }
    }

    // ──────────────────────────────────────────────
    // Public API (вызывается из PlayerPickup)
    // ──────────────────────────────────────────────

    /// <summary>Включить/выключить режим "в руке" локально.</summary>
    public void SetHeldState(bool held)
    {
        // Физику отключаем сразу локально для отзывчивости;
        // SyncVar обновится через сервер и применит на всех клиентах.
        ApplyPhysics(!held);
    }

    // ──────────────────────────────────────────────
    // Commands (клиент → сервер)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Запросить передачу авторитета этому клиенту.
    /// Вызывается игроком, который хочет подобрать объект.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdRequestAuthority(NetworkIdentity requester)
    {
        // Если объект уже у кого-то — отказываем
        if (isBeingHeld)
        {
            Debug.Log($"[PickupObject] {name} уже держат — авторитет не выдан.");
            return;
        }

        // Снимаем старый авторитет (если есть)
        if (netIdentity.connectionToClient != null)
            netIdentity.RemoveClientAuthority();

        // Выдаём авторитет новому владельцу
        netIdentity.AssignClientAuthority(requester.connectionToClient);

        isBeingHeld = true; // SyncVar — разойдётся на все клиенты

        Debug.Log($"[PickupObject] {name}: авторитет выдан {requester.name}");
    }

    /// <summary>Отпустить объект — снять авторитет и вернуть физику.</summary>
    [Command]
    public void CmdReleaseAuthority()
    {
        isBeingHeld = false; // SyncVar

        if (netIdentity.connectionToClient != null)
            netIdentity.RemoveClientAuthority();

        Debug.Log($"[PickupObject] {name}: авторитет снят.");
    }

    /// <summary>Синхронизировать позицию с сервера на все клиенты.</summary>
    [Command]
    private void CmdSyncTransform(Vector3 pos, Quaternion rot)
    {
        networkPosition = pos;
        networkRotation = rot;

        // Сервер тоже двигает объект (для серверной физики / столкновений)
        transform.position = pos;
        transform.rotation = rot;
    }

    // ──────────────────────────────────────────────
    // SyncVar hooks
    // ──────────────────────────────────────────────

    /// <summary>Вызывается на всех клиентах при изменении isBeingHeld.</summary>
    private void OnHeldStateChanged(bool oldValue, bool newValue)
    {
        // Тот, кто держит, управляет физикой сам через SetHeldState;
        // остальные клиенты реагируют здесь.
        if (!isOwned)
            ApplyPhysics(!newValue);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void ApplyPhysics(bool enable)
    {
        if (rb == null) return;
        rb.isKinematic = !enable;
        rb.useGravity   =  enable;

        if (!enable)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ──────────────────────────────────────────────
    // Editor Gizmos
    // ──────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Зелёный = свободен, красный = держат
        Gizmos.color = isBeingHeld ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = isBeingHeld ? Color.red : Color.green;
        style.fontSize = 10;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f,
            isBeingHeld ? "● HELD" : "○ free",
            style
        );
    }
#endif
}