using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// SCP-173 логика: объект движется к ближайшему игроку,
/// но замирает, если хотя бы один игрок смотрит на него.
/// Прикрепить к объекту вместе с NetworkIdentity + NetworkTransform.
/// Только Server Authority — клиенты только наблюдают.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class ScpEntity : NetworkBehaviour
{
    [Header("Движение")]
    [Tooltip("Скорость приближения к цели (юниты/сек)")]
    public float moveSpeed = 3f;

    [Tooltip("Расстояние, на котором объект останавливается у игрока")]
    public float stopDistance = 1.5f;

    [Header("Детекция взгляда")]
    [Tooltip("Угол полукона «видения» от направления взгляда игрока (градусы)")]
    public float viewAngleThreshold = 30f;

    [Tooltip("Максимальное расстояние, с которого игрок «видит» объект")]
    public float maxViewDistance = 40f;

    [Tooltip("Делать Raycast для проверки стен между игроком и объектом")]
    public bool checkLineOfSight = true;

    // ----------------------------------------------------------------
    // Приватное состояние (только сервер)
    // ----------------------------------------------------------------
    private Transform _target;          // ближайший игрок
    private bool      _isBeingWatched;  // заморожен ли объект

    // ----------------------------------------------------------------
    // Mirror lifecycle
    // ----------------------------------------------------------------

    public override void OnStartServer()
    {
        // Ничего специального не нужно; NetworkTransform будет
        // автоматически транслировать позицию клиентам.
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        UpdateWatchState();
        if (!_isBeingWatched)
            MoveTowardsTarget();
    }

    // ----------------------------------------------------------------
    // Логика взгляда
    // ----------------------------------------------------------------

    /// <summary>
    /// Проверяет всех подключённых игроков. Если хотя бы один
    /// смотрит на объект — isBeingWatched = true.
    /// </summary>
    [Server]
    private void UpdateWatchState()
    {
        _isBeingWatched = false;
        _target = null;
        float closestDist = float.MaxValue;

        // Перебираем все соединения (каждый = один игрок)
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn?.identity == null) continue;

            GameObject playerGo = conn.identity.gameObject;
            ScpPlayerEyes eyes = playerGo.GetComponent<ScpPlayerEyes>();
            if (eyes == null) continue;

            // Является ли этот игрок ближайшим? (выбираем цель движения)
            float dist = Vector3.Distance(transform.position, playerGo.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                _target = playerGo.transform;
            }

            // Смотрит ли этот игрок на нас?
            if (IsPlayerLooking(eyes))
            {
                _isBeingWatched = true;
                // Не прерываем цикл: продолжаем, чтобы найти ближайшего
            }
        }
    }

    /// <summary>
    /// Возвращает true, если глаза <paramref name="eyes"/> направлены
    /// в сторону этого объекта (угол + расстояние + опционально LoS).
    /// </summary>
    [Server]
    private bool IsPlayerLooking(ScpPlayerEyes eyes)
    {
        Vector3 toEntity = transform.position - eyes.EyePosition;
        float distance = toEntity.magnitude;

        if (distance > maxViewDistance)
            return false;

        // Угол между направлением взгляда и вектором к объекту
        float angle = Vector3.Angle(eyes.LookDirection, toEntity);
        if (angle > viewAngleThreshold)
            return false;

        // Проверка прямой видимости (стены)
        if (checkLineOfSight)
        {
            if (Physics.Raycast(eyes.EyePosition, toEntity.normalized, out RaycastHit hit, distance))
            {
                // Если луч ударил не в нас — объект за стеной
                if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
                    return false;
            }
        }

        return true;
    }

    // ----------------------------------------------------------------
    // Движение
    // ----------------------------------------------------------------

    [Server]
    private void MoveTowardsTarget()
    {
        if (_target == null) return;

        float dist = Vector3.Distance(transform.position, _target.position);
        if (dist <= stopDistance) return;

        // Направление к цели (только по плоскости XZ, если нужно)
        Vector3 direction = (_target.position - transform.position).normalized;
        // Раскомментируйте следующую строку, чтобы объект не летал по Y:
        // direction.y = 0f;

        transform.position += direction * moveSpeed * Time.fixedDeltaTime;

        // Смотрим в сторону цели
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    // ----------------------------------------------------------------
    // Gizmos (видно в Editor)
    // ----------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Зона остановки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Зона дальности зрения
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxViewDistance);
    }
#endif
}
