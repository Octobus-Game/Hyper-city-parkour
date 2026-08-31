using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на префаб пули.
/// Хранит урон и скорость — оба SyncVar, поэтому клиенты
/// получают актуальные значения сразу при спавне объекта.
/// </summary>
public class Mirror_Bullet_Damage : NetworkBehaviour
{
    [SyncVar] private float damage = 10f;
    [SyncVar] private Vector3 moveDirection;
    [SyncVar] private float moveSpeed;

    private Rigidbody rb;
    private bool alreadyHit = false; // защита от двойного срабатывания

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // ── Инициализация на сервере до спавна ──────────────────────────────────
    // Вызывать сразу после Instantiate, ДО NetworkServer.Spawn
    public void ServerInit(float dmg, Vector3 dir, float speed)
    {
        damage        = dmg;
        moveDirection = dir;
        moveSpeed     = speed;
    }

    // ── Применить скорость после того как SyncVar'ы пришли ─────────────────
    public override void OnStartServer()
    {
        ApplyVelocity();
    }

    public override void OnStartClient()
    {
        ApplyVelocity();
    }

    void ApplyVelocity()
    {
        if (rb != null && moveDirection != Vector3.zero)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }

    // ── Публичные геттеры ───────────────────────────────────────────────────
    public float GetDamage() => damage;

    // ── Столкновение обрабатывается ТОЛЬКО на сервере ───────────────────────
    // Коллайдер пули должен быть НЕ триггером, либо используем OnTriggerEnter —
    // зависит от настройки префаба. Здесь оба варианта.

    [ServerCallback]          // выполняется только на сервере, без лишних предупреждений
    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    // ────────────────────────────────────────────────────────────────────────
    private void HandleHit(GameObject target)
    {
        if (alreadyHit) return;   // пуля уже обработана
        alreadyHit = true;

        if (target.CompareTag("Bullet")) return; // пуля в пулю — игнорируем

        Healt_mirror_Player health = target.GetComponent<Healt_mirror_Player>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Уничтожаем пулю только на сервере — Mirror реплицирует это клиентам
        NetworkServer.Destroy(gameObject);
    }
}