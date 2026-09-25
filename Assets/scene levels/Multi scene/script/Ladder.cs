using UnityEngine;
using Mirror;

namespace mirrorplayer
{
    /// <summary>
    /// Лестница для playermirror (CharacterController + Mirror).
    ///
    /// Логика:
    ///  1) На объекте лестницы висит Box Collider (Is Trigger = true, добавляется
    ///     автоматически через [RequireComponent]).
    ///  2) Когда ЛОКАЛЬНЫЙ игрок касается триггера и нажимает Interact Key (по
    ///     умолчанию E) — он "прикрепляется" к лестнице и телепортируется
    ///     к точке Point Ladder Start.
    ///  3) Пока игрок прикреплён:
    ///       W (Climb Up Key)   — едет к Point Ladder End   (условно "вверх")
    ///       S (Climb Down Key) — едет к Point Ladder Start (условно "вниз")
    ///     Скорость перемещения задаётся полем Climb Speed в инспекторе.
    ///  4) Повторное нажатие Interact Key отцепляет игрока и возвращает ему
    ///     обычное управление.
    ///
    ///  Поворот камеры и тела игрока (мышь / Rotation Y) во время лазания
    ///  остаётся свободным — скрипт трогает только canMove, а не canLook.
    ///
    /// Скрипт работает только с локальным игроком (player.isLocalPlayer),
    /// т.к. в Mirror движение каждый клиент применяет только к своей копии
    /// (см. playermirror.HandleMovement — там та же проверка). Позиция затем
    /// расходится по сети штатным способом (NetworkTransform на префабе игрока).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Ladder : MonoBehaviour
    {
        [Header("Ladder Points")]
        [Tooltip("Нижняя точка лестницы — сюда игрок телепортируется при прикреплении (E)")]
        public Transform pointLadderStart;

        [Tooltip("Верхняя точка лестницы — сюда едет игрок по W")]
        public Transform pointLadderEnd;

        [Header("Climb Settings")]
        [Tooltip("Скорость перемещения по лестнице, м/с")]
        public float climbSpeed = 3.0f;

        [Tooltip("Клавиша прикрепления/отцепления от лестницы")]
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("Клавиша движения к Point Ladder End (вверх)")]
        public KeyCode climbUpKey = KeyCode.W;

        [Tooltip("Клавиша движения к Point Ladder Start (вниз)")]
        public KeyCode climbDownKey = KeyCode.S;

        [Header("Auto-Detach (опционально)")]
        [Tooltip("Автоматически отцеплять игрока, когда он доезжает до верхней точки")]
        public bool detachAtTop = false;

        [Tooltip("Автоматически отцеплять игрока, когда он доезжает до нижней точки")]
        public bool detachAtBottom = false;

        [Header("Debug")]
        public bool showDebugLog = false;

        // ─── Runtime state ─────────────────────────────────────────────
        private playermirror occupant;               // локальный игрок, стоящий в зоне триггера
        private CharacterController occupantController;
        private bool isPlayerInZone = false;
        private bool isAttached = false;

        // 0 = Point Ladder Start, 1 = Point Ladder End
        private float climbProgress = 0f;
        private float ladderLength = 0f;

        // ═══════════════════════════════════════════════════════════════
        void Awake()
        {
            var col = GetComponent<BoxCollider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[{gameObject.name}] Ladder: Box Collider был не Trigger — включил автоматически.");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Trigger zone
        // ═══════════════════════════════════════════════════════════════

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<playermirror>();
            if (player == null || !player.isLocalPlayer) return; // чужих игроков не трогаем

            occupant = player;
            occupantController = player.GetComponent<CharacterController>();
            isPlayerInZone = true;

            if (showDebugLog) Debug.Log($"[{gameObject.name}] Ladder: игрок вошёл в зону.");
        }

        void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<playermirror>();
            if (player == null || player != occupant) return;

            isPlayerInZone = false;

            // Если игрок вышел из зоны, не отцепившись явно (например, столкновение
            // вытолкнуло его) — принудительно отцепляем, чтобы не оставить его
            // "залипшим" без возможности управлять персонажем.
            if (isAttached) DetachFromLadder();

            occupant = null;
            occupantController = null;

            if (showDebugLog) Debug.Log($"[{gameObject.name}] Ladder: игрок вышел из зоны.");
        }

        // ═══════════════════════════════════════════════════════════════
        void Update()
        {
            if (occupant == null) return;

            if (!isAttached)
            {
                if (isPlayerInZone && Input.GetKeyDown(interactKey))
                    AttachToLadder();
            }
            else
            {
                if (Input.GetKeyDown(interactKey))
                {
                    DetachFromLadder();
                    return;
                }

                HandleClimbing();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Attach / Detach
        // ═══════════════════════════════════════════════════════════════

        void AttachToLadder()
        {
            if (pointLadderStart == null || pointLadderEnd == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Ladder: не назначены Point Ladder Start / Point Ladder End в инспекторе!");
                return;
            }

            ladderLength = Vector3.Distance(pointLadderStart.position, pointLadderEnd.position);
            if (ladderLength < 0.0001f)
            {
                Debug.LogWarning($"[{gameObject.name}] Ladder: Point Ladder Start и Point Ladder End совпадают.");
                return;
            }

            isAttached = true;
            climbProgress = 0f;

            // Отключаем обычное управление ходьбой/прыжком/гравитацией (canMove) и
            // его CharacterController, чтобы напрямую и предсказуемо задавать позицию
            // вдоль лестницы. canLook НЕ трогаем — требует playermirror.cs с полем
            // canLook (см. HandleCameraRotation/HandleCameraTilt), иначе игрок сможет
            // свободно осматриваться и вращаться по Y, пока висит на лестнице.
            occupant.SetCanMove(false);
            if (occupantController != null) occupantController.enabled = false;

            ApplyClimbPosition();

            if (showDebugLog) Debug.Log($"[{gameObject.name}] Ladder: игрок прикреплён (E).");
        }

        void DetachFromLadder()
        {
            isAttached = false;

            if (occupantController != null) occupantController.enabled = true;
            if (occupant != null) occupant.SetCanMove(true);

            if (showDebugLog) Debug.Log($"[{gameObject.name}] Ladder: игрок отцеплён.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Climbing
        // ═══════════════════════════════════════════════════════════════

        void HandleClimbing()
        {
            float input = 0f;
            if (Input.GetKey(climbUpKey))   input += 1f; // W — к Point Ladder End
            if (Input.GetKey(climbDownKey)) input -= 1f; // S — к Point Ladder Start

            if (Mathf.Abs(input) > 0.0001f)
            {
                climbProgress += (input * climbSpeed * Time.deltaTime) / ladderLength;
                climbProgress = Mathf.Clamp01(climbProgress);
                ApplyClimbPosition();
            }

            if (detachAtTop && climbProgress >= 1f && input > 0f)
                DetachFromLadder();
            else if (detachAtBottom && climbProgress <= 0f && input < 0f)
                DetachFromLadder();
        }

        void ApplyClimbPosition()
        {
            occupant.transform.position = Vector3.Lerp(
                pointLadderStart.position, pointLadderEnd.position, climbProgress);
        }

        // ═══════════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════════

        public bool IsAttached => isAttached;

        // ═══════════════════════════════════════════════════════════════
        void OnDrawGizmosSelected()
        {
            if (pointLadderStart == null || pointLadderEnd == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointLadderStart.position, pointLadderEnd.position);
            Gizmos.DrawSphere(pointLadderStart.position, 0.15f);
            Gizmos.DrawSphere(pointLadderEnd.position, 0.15f);
        }
    }
}