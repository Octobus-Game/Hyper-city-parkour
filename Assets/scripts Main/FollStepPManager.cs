using UnityEngine;

// Добавьте этот скрипт на объект, который должен оставлять следы (например, игрока)
[RequireComponent(typeof(CharacterController))]
public class FootprintTrigger : MonoBehaviour
{
    [Header("Footprint Settings")]
    public LayerMask footprintSurfaces; // Слой поверхностей, которые могут принимать следы
    public float footprintInterval = 0.5f; // Интервал между следами (в секундах)
    public float minimumVelocity = 0.1f; // Минимальная скорость для создания следа
    public Vector3 footprintOffset = Vector3.zero; // Смещение точки создания следа

    [Header("Alternating Feet")]
    public bool alternateFeet = true; // Чередовать левую/правую ногу
    public float feetSpacing = 0.2f; // Расстояние между ногами

    [Header("Debug")]
    public bool drawDebugGizmos = true;

    private float lastFootprintTime;
    private bool isLeftFoot = true;
    private Rigidbody rb;
    private Vector3 lastPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        lastFootprintTime = -footprintInterval; // Позволяет создать след сразу
    }

    void FixedUpdate()
    {
        // Проверяем скорость движения
        Vector3 velocity = rb != null ? rb.velocity : (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        float speed = velocity.magnitude;

        // Создаем след, если прошло достаточно времени и объект движется
        if (Time.time - lastFootprintTime >= footprintInterval && speed >= minimumVelocity)
        {
            TryCreateFootprint();
        }
    }

    void TryCreateFootprint()
    {
        // Raycast вниз для поиска поверхности
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + footprintOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 2f, footprintSurfaces))
        {
            // Проверяем, есть ли у поверхности FootprintManager
            FootprintManager manager = hit.collider.GetComponent<FootprintManager>();

            if (manager != null)
            {
                // Вычисляем позицию следа с учетом чередования ног
                Vector3 footprintPosition = hit.point;

                if (alternateFeet)
                {
                    // Смещаем след влево или вправо относительно направления движения
                    Vector3 right = Vector3.Cross(hit.normal, transform.forward).normalized;
                    footprintPosition += right * (isLeftFoot ? -feetSpacing : feetSpacing);
                    isLeftFoot = !isLeftFoot;
                }

                // Создаем след
                manager.AddFootprint(footprintPosition, hit.normal);
                lastFootprintTime = Time.time;

                if (drawDebugGizmos)
                {
                    Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.green, 1f);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        // Рисуем луч для отладки
        Vector3 rayOrigin = transform.position + footprintOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * 2f);
        Gizmos.DrawWireSphere(rayOrigin, 0.1f);
    }
}