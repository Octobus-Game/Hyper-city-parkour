using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на КОРЕНЬ игрока - туда же, где NetworkIdentity и Healt_mirror_Player.
///
/// Нужен потому, что [Command] можно объявить только в NetworkBehaviour, а
/// оружие (SELVEL8_Bullet) - обычный MonoBehaviour. Оружие вызывает
/// CmdSpawnBullet на объекте стрелка (у него есть authority), сервер создаёт
/// пулю и спавнит её всем клиентам.
/// </summary>
public class BulletSpawnerMirror : NetworkBehaviour
{
    [Tooltip("Префаб пули: NetworkIdentity + DamageMirror. " +
             "Должен быть в NetworkManager -> Registered Spawnable Prefabs.")]
    [SerializeField] private GameObject bulletPrefab;

    private Healt_mirror_Player _health;

    private void Awake()
    {
        _health = GetComponent<Healt_mirror_Player>();
    }

    [Command]
    public void CmdSpawnBullet(Vector3 origin, Vector3 direction, float damage)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError(
                $"[Server] {name}: в BulletSpawnerMirror не назначен bulletPrefab.", this);
            return;
        }

        // Мёртвый игрок не стреляет.
        if (_health != null && _health.IsDead)
            return;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            origin,
            Quaternion.LookRotation(direction.normalized));

        if (bullet.TryGetComponent(out DamageMirror damageMirror))
            damageMirror.ServerInit(Mathf.Max(0f, damage), netIdentity);

        NetworkServer.Spawn(bullet);
    }
}
