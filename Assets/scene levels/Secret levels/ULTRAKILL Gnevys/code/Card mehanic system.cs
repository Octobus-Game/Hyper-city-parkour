
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Скрипт для управления стрельбой (повесить на игрока или камеру)
public class ProjectileShooter : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    public GameObject mainProjectilePrefab; // 1.префаб - основной снаряд
    public Transform firePoint; // Точка выстрела
    public float projectileSpeed = 10f;
    public LayerMask shootableLayer = -1; // Слои по которым можно стрелять

    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }

    void Update()
    {
        // Стрельба по правой кнопке мыши
        if (Input.GetMouseButtonDown(1))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // Создаем снаряд
        Vector3 shootDirection = GetShootDirection();
        GameObject projectile = Instantiate(mainProjectilePrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        // Настраиваем компонент снаряда
        MainProjectile projectileScript = projectile.GetComponent<MainProjectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(shootDirection, projectileSpeed);
        }
    }

    Vector3 GetShootDirection()
    {
        Vector3 direction;

        // Raycast от камеры к центру экрана
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, shootableLayer))
        {
            direction = (hit.point - firePoint.position).normalized;
        }
        else
        {
            direction = playerCamera.transform.forward;
        }

        return direction;
    }
}

// Скрипт для первого префаба (основного снаряда)
public class MainProjectile : MonoBehaviour
{
    [Header("Настройки основного снаряда")]
    public GameObject subBulletPrefab; // 2.префаб - вторичная пуля
    public float speed = 10f;
    public float lifetime = 5f;
    public int bulletCount = 3; // Количество пуль при попадании
    public float bulletSpeed = 15f;

    private Vector3 direction;
    private Rigidbody rb;
    private bool hasExploded = false;

    public void Initialize(Vector3 shootDirection, float projectileSpeed)
    {
        direction = shootDirection;
        speed = projectileSpeed;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.velocity = direction * speed;

        // Уничтожаем снаряд через время
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            SpawnSubBullets();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasExploded)
        {
            hasExploded = true;
            SpawnSubBullets();
            Destroy(gameObject);
        }
    }

    void SpawnSubBullets()
    {
        // Находим всех агентов на сцене
        TargetAgent[] agents = FindObjectsOfType<TargetAgent>();

        if (agents.Length == 0) return;

        // Создаем пули
        for (int i = 0; i < bulletCount; i++)
        {
            GameObject bullet = Instantiate(subBulletPrefab, transform.position, Quaternion.identity);
            HomingBullet bulletScript = bullet.GetComponent<HomingBullet>();

            if (bulletScript != null)
            {
                // Находим ближайшего агента для каждой пули
                TargetAgent closestAgent = GetClosestAgent(agents, transform.position);
                if (closestAgent != null)
                {
                    bulletScript.Initialize(closestAgent.transform, bulletSpeed);
                }
            }
        }
    }

    TargetAgent GetClosestAgent(TargetAgent[] agents, Vector3 position)
    {
        TargetAgent closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (TargetAgent agent in agents)
        {
            if (agent != null)
            {
                float distance = Vector3.Distance(position, agent.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = agent;
                }
            }
        }

        return closest;
    }
}

// Скрипт для второго префаба (самонаводящиеся пули)
public class HomingBullet : MonoBehaviour
{
    [Header("Настройки самонаводящейся пули")]
    public float speed = 15f;
    public float lifetime = 10f;
    public float damage = 10f;
    public float trackingStrength = 5f; // Сила наведения на цель

    private Transform target;
    private Rigidbody rb;

    public void Initialize(Transform targetTransform, float bulletSpeed)
    {
        target = targetTransform;
        speed = bulletSpeed;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;

        // Начальное направление к цели
        if (target != null)
        {
            Vector3 initialDirection = (target.position - transform.position).normalized;
            rb.velocity = initialDirection * speed;
        }

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (target != null && rb != null)
        {
            // Направление к цели
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Плавное наведение на цель
            Vector3 currentVelocity = rb.velocity;
            Vector3 desiredVelocity = directionToTarget * speed;
            Vector3 newVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, trackingStrength * Time.fixedDeltaTime);

            rb.velocity = newVelocity;

            // Поворачиваем пулю в направлении движения
            if (newVelocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(newVelocity);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем попадание в агента
        TargetAgent agent = other.GetComponent<TargetAgent>();
        if (agent != null)
        {
            agent.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Уничтожаемся при столкновении с препятствиями (кроме других пуль)
        if (other.gameObject.layer != gameObject.layer && !other.CompareTag("Bullet"))
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TargetAgent agent = collision.gameObject.GetComponent<TargetAgent>();
        if (agent != null)
        {
            agent.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}

// Скрипт для агентов (целей)
public class TargetAgent : MonoBehaviour
{
    [Header("Настройки цели")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Визуальные эффекты")]
    public GameObject deathEffect; // Опциональный эффект смерти
    public Color normalColor = Color.white;
    public Color damageColor = Color.red;

    private Renderer agentRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        agentRenderer = GetComponent<Renderer>();

        // Если нет рендерера, добавляем базовый
        if (agentRenderer == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            agentRenderer = cube.GetComponent<Renderer>();
        }

        if (agentRenderer != null)
        {
            agentRenderer.material.color = normalColor;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Агент {gameObject.name} получил {damage} урона. Осталось здоровья: {currentHealth}");

        // Визуальный эффект получения урона
        if (agentRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        agentRenderer.material.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        agentRenderer.material.color = normalColor;
    }

    void Die()
    {
        Debug.Log($"Агент {gameObject.name} уничтожен!");

        // Создаем эффект смерти если есть
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    // Визуализация агента в редакторе
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Показываем текущее здоровье
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            float healthPercentage = currentHealth / maxHealth;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(2f * healthPercentage, 0.2f, 0.2f));
        }
    }
}