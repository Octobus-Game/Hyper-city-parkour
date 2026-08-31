using UnityEngine;

/// <summary>
/// Прикрепляется к игроку (или любому объекту, который может телепортироваться).
/// Отслеживает позицию предыдущего кадра и запрашивает телепортацию у порталов.
/// </summary>
public class PortalTeleporter : MonoBehaviour
{
    [Header("Компоненты")]
    public CharacterController characterController; // Если используется CharacterController
    public Rigidbody            rb;                 // Если используется Rigidbody

    // Позиция в предыдущем кадре (для определения пересечения плоскости)
    private Vector3 _previousPosition;

    // Флаг: только что телепортировались (защита от двойной телепортации)
    private bool _teleportedThisFrame;

    void Start()
    {
        _previousPosition = transform.position;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        _teleportedThisFrame = false;
    }

    /// <summary>
    /// Вызывается из PortalManager.
    /// Проверяет каждый портал и выполняет телепортацию при необходимости.
    /// </summary>
    public void CheckPortals(NonEuclideanPortal[] portals)
    {
        if (_teleportedThisFrame) return;

        foreach (var portal in portals)
        {
            if (portal == null || portal.linkedPortal == null) continue;

            if (portal.ShouldTeleport(transform, _previousPosition))
            {
                Teleport(portal);
                break; // Только один портал за кадр
            }
        }

        _previousPosition = transform.position;
    }

    // ── Приватные методы ──────────────────────────────────────────────

    private void Teleport(NonEuclideanPortal portal)
    {
        var (newPos, newRot) = portal.GetTeleportTransform(transform);

        // Рассчитываем новое направление скорости (если есть Rigidbody)
        Vector3 newVelocity = Vector3.zero;
        if (rb != null)
        {
            // Трансформируем вектор скорости через матрицу поворота порталов
            Matrix4x4 m = portal.linkedPortal.transform.localToWorldMatrix
                        * Matrix4x4.Rotate(Quaternion.Euler(0, 180f, 0))
                        * portal.transform.worldToLocalMatrix;

#if UNITY_6000_0_OR_NEWER
            newVelocity = m.MultiplyVector(rb.linearVelocity);
#else
            newVelocity = m.MultiplyVector(rb.velocity);
#endif
        }

        // --- Применяем позицию ---
        if (characterController != null && characterController.enabled)
        {
            // CharacterController нельзя переместить напрямую без отключения
            characterController.enabled = false;
            transform.position = newPos;
            transform.rotation = newRot;
            characterController.enabled = true;
        }
        else
        {
            transform.position = newPos;
            transform.rotation = newRot;
        }

        // --- Применяем скорость ---
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = newVelocity;
#else
            rb.velocity = newVelocity;
#endif
        }

        _previousPosition    = newPos;
        _teleportedThisFrame = true;

        Debug.Log($"[Portal] {gameObject.name} телепортирован через {portal.name} → {portal.linkedPortal.name}");
    }
}