using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 5f;
    public int damage = 10;
    public LayerMask targetLayers = -1;

    [Header("Effects")]
    public GameObject explosionEffect;

    void Start()
    {
        // Уничтожаем пулю через определенное время
        if (isServer)
        {
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Только сервер обрабатывает коллизии
        if (!isServer) return;

        GameObject hitObject = collision.gameObject;

        // Проверяем, попали ли мы в нужный слой
        if (((1 << hitObject.layer) & targetLayers) != 0)
        {
            // Пытаемся нанести урон
            if (hitObject.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }

            // Воспроизводим эффект попадания на всех клиентах
            if (explosionEffect != null)
            {
                RpcPlayExplosionEffect(collision.contacts[0].point, collision.contacts[0].normal);
            }
        }

        // Уничтожаем пулю
        DestroyBullet();
    }

    [ClientRpc]
    void RpcPlayExplosionEffect(Vector3 position, Vector3 normal)
    {
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, position, Quaternion.LookRotation(normal));

            // Автоматически уничтожаем эффект
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                Destroy(effect, particles.main.duration);
            }
            else
            {
                Destroy(effect, 2f); // Запасной вариант
            }
        }
    }

    void DestroyBullet()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}

// Интерфейс для объектов, которые могут получать урон
public interface IDamageable
{
    void TakeDamage(int damage);
}