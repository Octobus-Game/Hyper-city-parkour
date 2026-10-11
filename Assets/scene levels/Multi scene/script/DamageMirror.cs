using Mirror;
using UnityEngine;

/// <summary>
/// Скрипт на префабе пули (bullet).
///
/// Префаб пули: NetworkIdentity + этот скрипт. Rigidbody и NetworkTransform
/// НЕ нужны - пуля летит по прямой сама, одинаково на сервере и у клиентов
/// (направление берётся из поворота при спавне). Коллайдер тоже не нужен;
/// если он есть - не ставьте ему тег "Damage" (иначе Healt_mirror_Player
/// снимет ещё и свой damageAmount по триггеру).
///
/// Префаб обязательно добавить в NetworkManager -> Registered Spawnable Prefabs.
///
/// Попадание считается только на сервере. Если пуля попала в игрока
/// (Healt_mirror_Player) - вызывается ServerApplyDamage, который отнимает HP,
/// обрабатывает смерть и "Вас убил [Ник]".
/// </summary>
[DisallowMultipleComponent]
public class DamageMirror : NetworkBehaviour
{
    [Header("Полёт")]
    [Tooltip("Скорость пули, м/с.")]
    [SerializeField] private float speed = 80f;

    [Tooltip("Через сколько секунд пуля исчезнет, если ни во что не попала.")]
    [SerializeField] private float lifetime = 5f;

    [Tooltip("Слои, в которые может попасть пуля. Триггеры игнорируются.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Урон")]
    [Tooltip("Урон по умолчанию. Если пулю создаёт BulletSpawnerMirror, " +
             "значение заменяется уроном оружия (SELVEL8_Bullet).")]
    [SerializeField] private float damage = 25f;

    [Tooltip("Сила удара по Rigidbody, если пуля попала не в игрока.")]
    [SerializeField] private float impactForce = 30f;

    [Header("Эффекты попадания (необязательно, играются у всех клиентов)")]
    [Tooltip("Частицы при попадании в игрока.")]
    [SerializeField] private ParticleSystem bloodEffect;

    [Tooltip("Частицы при попадании во всё остальное.")]
    [SerializeField] private ParticleSystem impactEffect;

    [Tooltip("Звук попадания в игрока.")]
    [SerializeField] private AudioClip hitSound;

    [Tooltip("Звук попадания во всё остальное.")]
    [SerializeField] private AudioClip impactSound;

    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;

    [Tooltip("Декаль-дырка от пули (создаётся в точке попадания).")]
    [SerializeField] private GameObject bulletHoleDecal;

    [SerializeField] private float decalLifetime = 10f;

    // Только сервер: кто стрелял и не закончила ли пуля свой полёт.
    private NetworkIdentity _owner;
    private bool _finished;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    // ========================================================================
    // INIT (вызывает BulletSpawnerMirror на сервере ДО NetworkServer.Spawn)
    // ========================================================================

    [Server]
    public void ServerInit(float newDamage, NetworkIdentity owner)
    {
        damage = newDamage;
        _owner = owner;
    }

    public override void OnStartServer()
    {
        // Страховка: пуля, никуда не попавшая, живёт не дольше lifetime.
        Invoke(nameof(ServerDestroySelf), lifetime);
    }

    // ========================================================================
    // ПОЛЁТ
    // ========================================================================

    private void Update()
    {
        float distance = speed * Time.deltaTime;
        Vector3 direction = transform.forward;
        Vector3 from = transform.position;

        // Попадания проверяет только сервер (на хосте isServer тоже true).
        // Проверяется отрезок "прошлая позиция -> новая", поэтому быстрая
        // пуля не пролетает сквозь тонкие стены и игроков.
        if (isServer && !_finished && ServerCheckHit(from, direction, distance))
            return;

        transform.position = from + direction * distance;
    }

    [Server]
    private bool ServerCheckHit(Vector3 from, Vector3 direction, float distance)
    {
        int count = Physics.RaycastNonAlloc(
            from, direction, HitBuffer, distance, hitLayers,
            QueryTriggerInteraction.Ignore);

        int best = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit h = HitBuffer[i];

            if (h.distance >= bestDistance)
                continue;

            // Стрелок не попадает сам в себя.
            NetworkIdentity id = h.collider.GetComponentInParent<NetworkIdentity>();
            if (id != null && id == _owner)
                continue;

            // Другие пули игнорируем.
            if (h.collider.GetComponentInParent<DamageMirror>() != null)
                continue;

            // Мёртвого игрока пролетаем насквозь (на выделенном сервере его
            // коллайдеры могут оставаться включёнными).
            Healt_mirror_Player target = h.collider.GetComponentInParent<Healt_mirror_Player>();
            if (target != null && target.IsDead)
                continue;

            best = i;
            bestDistance = h.distance;
        }

        if (best < 0)
            return false;

        ServerOnHit(HitBuffer[best]);
        return true;
    }

    [Server]
    private void ServerOnHit(RaycastHit hit)
    {
        _finished = true;

        Healt_mirror_Player target = hit.collider.GetComponentInParent<Healt_mirror_Player>();
        bool hitPlayer = target != null;

        if (hitPlayer)
        {
            // Здесь пуля и отнимает HP у Healt_mirror_Player.
            target.ServerApplyDamage(damage, _owner);
        }
        else if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForceAtPosition(
                -hit.normal * impactForce, hit.point, ForceMode.Impulse);
        }

        // RPC и уничтожение идут по одному каналу по порядку, поэтому клиенты
        // успевают получить RpcOnHit до того, как пуля у них исчезнет.
        RpcOnHit(hit.point, hit.normal, hitPlayer);
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void ServerDestroySelf()
    {
        if (_finished)
            return;

        _finished = true;
        NetworkServer.Destroy(gameObject);
    }

    // ========================================================================
    // ЭФФЕКТЫ ПОПАДАНИЯ (у всех клиентов)
    // ========================================================================

    [ClientRpc]
    private void RpcOnHit(Vector3 point, Vector3 normal, bool hitPlayer)
    {
        SpawnParticle(hitPlayer ? bloodEffect : impactEffect, point, normal);

        AudioClip clip = hitPlayer ? hitSound : impactSound;
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, point, soundVolume);

        if (bulletHoleDecal != null)
        {
            GameObject decal = Instantiate(
                bulletHoleDecal,
                point + normal * 0.01f,
                Quaternion.LookRotation(-normal));

            Destroy(decal, decalLifetime);
        }
    }

    private static void SpawnParticle(ParticleSystem prefab, Vector3 point, Vector3 normal)
    {
        if (prefab == null)
            return;

        ParticleSystem ps = Instantiate(
            prefab,
            point,
            Quaternion.LookRotation(normal));

        ps.Play();

        Destroy(
            ps.gameObject,
            ps.main.duration + ps.main.startLifetime.constantMax);
    }
}
