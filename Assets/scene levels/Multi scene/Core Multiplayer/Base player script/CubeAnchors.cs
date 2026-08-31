using UnityEngine;

/// <summary>
/// Висит на префабе куба. Хранит 4 точки соединения (Left/Right/Up/Down) —
/// именно в них и только в них могут появляться соседние кубы.
/// Это полностью исключает пересечение/наложение кубов друг на друга.
///
/// НАСТРОЙКА В РЕДАКТОРЕ:
/// Создайте внутри префаба куба 4 пустых child-объекта (Empty GameObject)
/// и расположите их ровно на серединах нужных граней, на расстоянии
/// половины размера куба от центра. Для куба размером 1x1x1:
///   PointLeft  -> локальная позиция (-0.5, 0, 0)
///   PointRight -> локальная позиция ( 0.5, 0, 0)
///   PointUp    -> локальная позиция ( 0,  0.5, 0)
///   PointDown  -> локальная позиция ( 0, -0.5, 0)
/// Затем перетащите их в соответствующие поля этого компонента.
/// </summary>
public class CubeAnchors : MonoBehaviour
{
    [Header("Точки соединения (Empty child-объекты)")]
    public Transform pointLeft;
    public Transform pointRight;
    public Transform pointUp;
    public Transform pointDown;

    [Header("Проверка занятости точки")]
    [SerializeField] private float occupiedCheckRadius = 0.1f;

    private Transform[] AllPoints => new[] { pointLeft, pointRight, pointUp, pointDown };

    /// <summary>
    /// Возвращает ближайшую к worldPoint СВОБОДНУЮ точку соединения.
    /// Если все точки заняты — возвращает null.
    /// </summary>
    public Transform GetNearestFreePoint(Vector3 worldPoint)
    {
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Transform point in AllPoints)
        {
            if (point == null || IsPointOccupied(point)) continue;

            float dist = Vector3.Distance(point.position, worldPoint);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = point;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Физически проверяет, не стоит ли уже куб в данной точке.
    /// </summary>
    public bool IsPointOccupied(Transform point)
    {
        if (point == null) return true;

        Collider[] hits = Physics.OverlapSphere(point.position, occupiedCheckRadius);
        foreach (var hit in hits)
        {
            if (hit.GetComponentInParent<CubeAnchors>() != null)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (Transform point in AllPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, occupiedCheckRadius);
        }
    }
}
