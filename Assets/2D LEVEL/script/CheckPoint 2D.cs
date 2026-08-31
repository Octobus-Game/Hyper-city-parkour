using UnityEngine;

public class _TeleportTrigger : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private Transform destination;
    [SerializeField] private bool keepVelocity = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (destination == null)
        {
            Debug.LogWarning("Destination не назначен!", this);
            return;
        }

        Teleport(other);
    }

   private void Teleport(Collider2D other)
{
    if (!keepVelocity)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    // Берём позицию destination, но Z оставляем от игрока
    Vector3 newPos = destination.position;
    newPos.z = other.transform.position.z;

    other.transform.position = newPos;
}
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (destination == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(destination.position, 0.3f);
        Gizmos.DrawLine(transform.position, destination.position);
    }
#endif
}