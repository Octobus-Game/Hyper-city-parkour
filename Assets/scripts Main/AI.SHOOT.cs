using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AI_Enemy : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float detectionAngle = 90f; // Угол обзора
    [SerializeField] private LayerMask obstacleLayerMask = -1; // Слои препятствий
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private string targetTag = "Player"; // Поиск цели по тегу

    [Header("Shooting Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootForce = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private bool useGravityCompensation = true;
    [SerializeField] private float projectileLifetime = 5f;

    [Header("Accuracy Settings")]
    [SerializeField] private float accuracy = 0.9f; // 0 = неточный, 1 = идеальный
    [SerializeField] private float maxSpread = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private bool rotateToTarget = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private Transform rotationPivot; // Если нужно поворачивать отдельную часть

    [Header("Advanced Settings")]
    [SerializeField] private bool predictTargetMovement = true;
    [SerializeField] private float predictionAccuracy = 0.8f;
    [SerializeField] private bool burstFire = false;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;

    [Header("Audio & Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Events")]
    public UnityEvent OnShoot;
    public UnityEvent OnReload;
    public UnityEvent OnTargetDetected;
    public UnityEvent OnTargetLost;

    [Header("Movement Settings")]
    [SerializeField] private bool chaseTarget = true;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stopDistance = 5f; // Дистанция остановки

    private UnityEngine.AI.NavMeshAgent navAgent;

    void Start()
    {
        // ... ваш существующий код Start() ...

        // Добавьте это в конец метода Start():
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = chaseSpeed;
            navAgent.stoppingDistance = stopDistance;
        }
    }

    void Update()
    {
        if (!isReloading)
        {
            UpdateTargetTracking();

            // Добавьте преследование
            if (chaseTarget && target != null && navAgent != null)
            {
                ChaseTarget();
            }

            DetectAndShoot();
        }

        if (rotateToTarget && target != null && targetInSight)
        {
            RotateToTarget();
        }
    }

    void ChaseTarget()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > stopDistance)
        {
            navAgent.SetDestination(target.position);
        }
        else
        {
            navAgent.ResetPath(); // Останавливаемся на нужной дистанции
        }
    }



    // Приватные переменные
    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;
    private bool targetInSight;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private Rigidbody targetRigidbody;
    private Coroutine burstCoroutine;

    // Кеширование компонентов
    private Camera detectionCamera;
    private List<GameObject> projectilePool = new List<GameObject>();
    private int poolSize = 20;

    

   

    void UpdateTargetTracking()
    {
        if (target == null) return;

        // Обновление скорости цели для предсказания
        if (predictTargetMovement && targetRigidbody != null)
        {
            targetVelocity = targetRigidbody.velocity;
        }
        else if (predictTargetMovement)
        {
            // Альтернативный способ вычисления скорости
            Vector3 currentPosition = target.position;
            targetVelocity = (currentPosition - lastTargetPosition) / Time.deltaTime;
            lastTargetPosition = currentPosition;
        }
    }

    void DetectAndShoot()
    {
        if (target == null)
            return;

        bool wasInSight = targetInSight;
        targetInSight = IsTargetInSight();

        // События обнаружения/потери цели
        if (targetInSight && !wasInSight)
            OnTargetDetected.Invoke();
        else if (!targetInSight && wasInSight)
            OnTargetLost.Invoke();

        if (targetInSight && Time.time >= nextFireTime && currentAmmo > 0)
        {
            if (burstFire)
            {
                if (burstCoroutine == null)
                    burstCoroutine = StartCoroutine(BurstFire());
            }
            else
            {
                Shoot();
            }
        }

        // Автоматическая перезарядка
        if (currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    bool IsTargetInSight()
    {
        if (target == null) return false;

        float distance = Vector3.Distance(transform.position, target.position);

        // Проверка расстояния
        if (distance > detectionRadius)
            return false;

        // Проверка угла обзора
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle > detectionAngle / 2f)
            return false;

        // Проверка прямой видимости
        if (useLineOfSight)
        {
            if (Physics.Raycast(transform.position, directionToTarget, distance, obstacleLayerMask))
                return false;
        }

        return true;
    }

    void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null)
            return;

        Vector3 shootDirection = CalculateShootDirection();

        // Получение снаряда из пула
        GameObject projectile = GetPooledProjectile();
        if (projectile != null)
        {
            projectile.transform.position = shootPoint.position;
            projectile.transform.rotation = Quaternion.LookRotation(shootDirection);
            projectile.SetActive(true);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.velocity = shootDirection * shootForce;
            }

            // Автоматическое уничтожение снаряда
            StartCoroutine(ReturnProjectileToPool(projectile, projectileLifetime));
        }

        // Эффекты выстрела
        PlayShootEffects();

        // Обновление боеприпасов и времени
        currentAmmo--;
        nextFireTime = Time.time + 1f / fireRate;

        OnShoot.Invoke();
    }

    Vector3 CalculateShootDirection()
    {
        Vector3 targetPosition = target.position;

        // Предсказание движения цели
        if (predictTargetMovement)
        {
            float timeToTarget = Vector3.Distance(shootPoint.position, targetPosition) / shootForce;
            targetPosition += targetVelocity * timeToTarget * predictionAccuracy;
        }

        Vector3 direction = (targetPosition - shootPoint.position).normalized;

        // Компенсация гравитации
        if (useGravityCompensation)
        {
            direction = CalculateBallisticTrajectory(shootPoint.position, targetPosition);
        }

        // Добавление разброса
        if (accuracy < 1f)
        {
            float spread = maxSpread * (1f - accuracy);
            direction += Random.insideUnitSphere * spread * 0.1f;
            direction.Normalize();
        }

        return direction;
    }

    Vector3 CalculateBallisticTrajectory(Vector3 startPos, Vector3 targetPos)
    {
        Vector3 direction = targetPos - startPos;
        float distance = direction.magnitude;
        direction.Normalize();

        float gravity = Physics.gravity.magnitude;
        float time = distance / shootForce;
        float verticalOffset = 0.5f * gravity * time * time;

        direction.y += verticalOffset / distance;
        return direction.normalized;
    }

    IEnumerator BurstFire()
    {
        for (int i = 0; i < burstCount && currentAmmo > 0; i++)
        {
            Shoot();
            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstDelay);
        }

        nextFireTime = Time.time + 1f / fireRate;
        burstCoroutine = null;
    }

    IEnumerator Reload()
    {
        isReloading = true;
        OnReload.Invoke();

        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void RotateToTarget()
    {
        Vector3 direction = (target.position - rotationPivot.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rotationPivot.rotation = Quaternion.Slerp(rotationPivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void PlayShootEffects()
    {
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (muzzleFlash != null)
            muzzleFlash.Play();
    }

    void InitializeProjectilePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.SetActive(false);
            projectilePool.Add(projectile);
        }
    }

    GameObject GetPooledProjectile()
    {
        foreach (GameObject projectile in projectilePool)
        {
            if (!projectile.activeInHierarchy)
                return projectile;
        }
        return null;
    }

    IEnumerator ReturnProjectileToPool(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (projectile != null)
            projectile.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = targetInSight ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Угол обзора
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle / 2, 0) * transform.forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle / 2, 0) * transform.forward * detectionRadius;

        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);

        // Линия к цели
        if (target != null)
        {
            Gizmos.color = targetInSight ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }

    // Публичные методы для внешнего управления
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            targetRigidbody = target.GetComponent<Rigidbody>();
    }

    public void ForceReload()
    {
        if (!isReloading)
            StartCoroutine(Reload());
    }

    public bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public bool IsReloading()
    {
        return isReloading;
    }
}