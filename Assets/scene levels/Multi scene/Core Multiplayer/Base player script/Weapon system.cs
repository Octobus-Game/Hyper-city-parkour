using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SELVEL10 : NetworkBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;
    public float cooldownTime = 5f;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    public AudioSource audioSource;
    public Animator animator;
    public string shootAnimationTrigger = "Shoot";
    public ParticleSystem hitEffect;

    [Header("Target Settings")]
    public GameObject targetPrefab;
    private string targetName; // Кэшированное имя цели

    [Header("Camera Shake Settings")]
    public Camera playerCamera;
    [Range(0f, 1f)] public float shakeChance = 0.3f;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.2f;

    // Cached components
    private Transform cameraTransform;
    private WaitForEndOfFrame waitForEndOfFrame;

    // State variables
    [SyncVar] private float nextFireTime = 0f;
    [SyncVar] private bool isCooldown = false;
    private Vector3 originalCameraPosition;
    private bool isShaking = false;

    // Hash for animation trigger (более эффективно чем строки)
    private int shootAnimationHash;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // Настраиваем камеру только для локального игрока
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            originalCameraPosition = cameraTransform.localPosition;
        }
    }

    void Awake()
    {
        // Кэшируем компоненты и данные в Awake
        if (targetPrefab != null)
        {
            targetName = targetPrefab.name;
        }

        shootAnimationHash = Animator.StringToHash(shootAnimationTrigger);
        waitForEndOfFrame = new WaitForEndOfFrame();
    }

    void Update()
    {
        // Только локальный игрок может стрелять
        if (!isLocalPlayer) return;

        // Дополнительная проверка на подключение
        if (!NetworkClient.ready) return;

        // Используем GetButtonDown для одиночных выстрелов или GetButton для автоматической стрельбы
        if (Input.GetButton("Fire1") && CanShoot())
        {
            CmdShoot();
        }
    }

    bool CanShoot()
    {
        // Дополнительные проверки для сетевого состояния
        if (!isLocalPlayer || !NetworkClient.ready) return false;

        return Time.time >= nextFireTime && !isCooldown;
    }

    [Command]
    void CmdShoot()
    {
        if (!CanShoot()) return;

        // Создание пули на сервере
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Используем TryGetComponent для лучшей производительности
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.velocity = firePoint.forward * bulletSpeed;
        }

        // Спавним пулю в сети
        NetworkServer.Spawn(bullet);

        // Воспроизводим эффекты на всех клиентах
        RpcPlayEffects();

        // Только для стрелявшего игрока - тряска камеры (с проверкой подключения)
        if (ShouldShakeCamera() && connectionToClient != null)
        {
            TargetShakeCamera();
        }

        nextFireTime = Time.time + fireRate;

        // Запускаем кулдаун только если он больше 0
        if (cooldownTime > 0)
        {
            StartCooldown();
        }
    }

    [ClientRpc]
    void RpcPlayEffects()
    {
        // Объединяем все эффекты в одном методе
        muzzleFlash?.Play();

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (animator != null)
        {
            animator.SetTrigger(shootAnimationHash); // Используем хэш вместо строки
        }
    }

    bool ShouldShakeCamera()
    {
        return cameraTransform != null && !isShaking && Random.value <= shakeChance;
    }

    [TargetRpc]
    void TargetShakeCamera()
    {
        if (isLocalPlayer) // Только для локального игрока
        {
            StartCoroutine(ShakeCamera());
        }
    }

    IEnumerator ShakeCamera()
    {
        isShaking = true;
        float elapsedTime = 0f;

        // Кэшируем значения для избежания повторных вычислений
        float duration = shakeDuration;
        float intensity = shakeIntensity;
        Vector3 basePosition = originalCameraPosition;

        while (elapsedTime < duration)
        {
            // Оптимизированное вычисление смещения
            Vector3 randomOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0f
            );

            cameraTransform.localPosition = basePosition + randomOffset;

            elapsedTime += Time.deltaTime;
            yield return waitForEndOfFrame; // Переиспользуем объект
        }

        // Возвращаем камеру в исходное положение
        cameraTransform.localPosition = basePosition;
        isShaking = false;
    }

    void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), cooldownTime);
    }

    void EndCooldown()
    {
        isCooldown = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Только сервер обрабатывает коллизии
        if (!isServer) return;

        // Используем CompareTag если возможно, иначе кэшированное имя
        if (collision.gameObject.name.Contains(targetName))
        {
            if (hitEffect != null)
            {
                // Создаем эффект попадания на всех клиентах
                RpcPlayHitEffect(collision.contacts[0].point);
            }
        }

        // Уничтожаем объект в сети
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcPlayHitEffect(Vector3 hitPoint)
    {
        if (hitEffect != null)
        {
            // Создаем эффект попадания
            ParticleSystem effect = Instantiate(hitEffect, hitPoint, Quaternion.identity);
            effect.Play();

            // Опционально: уничтожаем эффект после воспроизведения
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

    // Опционально: метод для сброса позиции камеры при отключении
    void OnDisable()
    {
        if (isLocalPlayer && cameraTransform != null && isShaking)
        {
            cameraTransform.localPosition = originalCameraPosition;
            isShaking = false;
        }
    }

    // Дополнительный метод для синхронизации состояния при подключении нового игрока
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Дополнительная инициализация клиента при необходимости
    }

    // Метод для очистки при отключении от сервера
    public override void OnStopClient()
    {
        base.OnStopClient();
        // Очистка ресурсов клиента
    }
}