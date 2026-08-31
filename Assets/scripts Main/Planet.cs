using UnityEngine;

/// <summary>
/// Прикрепи этот компонент к объекту-планете (Sphere с SphereCollider).
/// Определяет силу гравитации и радиус притяжения планеты.
///
/// НАСТРОЙКА:
/// 1. Добавь SphereCollider к объекту-планете.
/// 2. Добавь этот компонент к тому же объекту.
/// 3. На игроке (CharacterMovement0) поставь галку «Planet Gravity Enabled».
/// 4. Отрегулируй Gravity Strength и Gravity Influence Radius под размер планеты.
/// </summary>
public class Planet : MonoBehaviour
{
    [Tooltip("Сила притяжения планеты (рекомендуется: 12–20 для игровых масштабов)")]
    public float gravityStrength = 14f;

    [Tooltip("Дополнительный радиус зоны притяжения за пределами коллайдера (в Unity-метрах)")]
    public float gravityInfluenceRadius = 25f;

    private SphereCollider _sphereCollider;

    private void Awake()
    {
        _sphereCollider = GetComponent<SphereCollider>();

        if (_sphereCollider == null)
            Debug.LogWarning($"[Planet] На объекте '{name}' не найден SphereCollider! " +
                             "Радиус влияния будет вычислен по scale объекта.");
    }

    /// <summary>
    /// Полный радиус зоны гравитационного влияния с учётом масштаба объекта.
    /// </summary>
    public float GetInfluenceRadius()
    {
        float maxScale = Mathf.Max(
            transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z
        );

        float colliderRadius = _sphereCollider != null
            ? _sphereCollider.radius * maxScale
            : maxScale * 0.5f;

        return colliderRadius + gravityInfluenceRadius;
    }

    /// <summary>
    /// Возвращает направление «вниз» для заданной точки в пространстве
    /// (вектор от точки к центру планеты).
    /// </summary>
    public Vector3 GetGravityDirection(Vector3 fromPoint)
    {
        return (transform.position - fromPoint).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        // Радиус поверхности планеты (зелёный)
        float maxScale = Mathf.Max(
            transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z
        );
        var col = GetComponent<SphereCollider>();
        float surfaceRadius = col != null ? col.radius * maxScale : maxScale * 0.5f;

        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.3f);
        Gizmos.DrawSphere(transform.position, surfaceRadius);

        // Радиус зоны притяжения (голубой, полупрозрачный)
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.12f);
        Gizmos.DrawSphere(transform.position, GetInfluenceRadius());
    }
}
