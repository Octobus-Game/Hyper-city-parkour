using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SELVEL801 : NetworkBehaviour
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
    private string targetName;

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

    // Hash for animation trigger
    private int shootAnimationHash;

    void Awake()
    {
        // Кэшируем компоненты только для локального игрока
        if (isLocalPlayer && playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            originalCameraPosition = cameraTransform.localPosition;
        }

        if (targetPrefab != null)
        {
            targetName = targetPrefab.name;
        }

        shootAnimationHash = Animator.StringToHash(shootAnimationTrigger);
        waitForEndOfFrame = new WaitForEndOfFrame();
    }

    void Update()
    {
        // Обрабатываем ввод только для локального игрока
        if (!isLocalPlayer) return;

        // Добавляем отладочное сообщение
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Client: Fire1 pressed");
        }

        if (Input.GetButton("Fire1") && CanShoot())
        {
            Debug.Log("Client: Calling CmdShoot");
            // Отправляем команду на сервер
            CmdShoot();
        }
    }

    bool CanShoot()
    {
        return NetworkTime.time >= nextFireTime && !isCooldown;
    }

    [Command]
    void CmdShoot()
    {
        // Проверяем возможность стрельбы на сервере
        if (!CanShoot()) return;

        // Добавляем отладочное сообщение
        Debug.Log("Server: Shooting bullet");

        // Проверяем наличие префаба
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab is null!");
            return;
        }

        // Создаем пулю на сервере
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Проверяем наличие NetworkIdentity
        if (bullet.GetComponent<NetworkIdentity>() == null)
        {
            Debug.LogError("Bullet prefab doesn't have NetworkIdentity component!");
            Destroy(bullet);
            return;
        }

        // Спавним пулю в сети
        NetworkServer.Spawn(bullet);

        // Устанавливаем скорость пули
        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.velocity = firePoint.forward * bulletSpeed;
        }

        // Устанавливаем имя цели для пули
        if (bullet.TryGetComponent<NetworkBullet>(out NetworkBullet networkBullet))
        {
            networkBullet.SetTargetName(targetName);
        }

        // Устанавливаем время следующего выстрела
        nextFireTime = (float)NetworkTime.time + fireRate;

        // Запускаем кулдаун
        if (cooldownTime > 0)
        {
            StartCooldown();
        }

        // Вызываем эффекты на всех клиентах
        RpcPlayShootEffects();
    }

    [ClientRpc]
    void RpcPlayShootEffects()
    {
        PlayEffects();

        // Тряска камеры только для владельца
        if (isLocalPlayer && ShouldShakeCamera())
        {
            StartCoroutine(ShakeCamera());
        }
    }

    void PlayEffects()
    {
        muzzleFlash?.Play();

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (animator != null)
        {
            animator.SetTrigger(shootAnimationHash);
        }
    }

    bool ShouldShakeCamera()
    {
        return cameraTransform != null && !isShaking && Random.value <= shakeChance;
    }

    IEnumerator ShakeCamera()
    {
        isShaking = true;
        float elapsedTime = 0f;

        float duration = shakeDuration;
        float intensity = shakeIntensity;
        Vector3 basePosition = originalCameraPosition;

        while (elapsedTime < duration)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0f
            );

            cameraTransform.localPosition = basePosition + randomOffset;

            elapsedTime += Time.deltaTime;
            yield return waitForEndOfFrame;
        }

        cameraTransform.localPosition = basePosition;
        isShaking = false;
    }

    [Server]
    void StartCooldown()
    {
        isCooldown = true;
        Invoke(nameof(EndCooldown), cooldownTime);
    }

    [Server]
    void EndCooldown()
    {
        isCooldown = false;
    }

    void OnDisable()
    {
        if (isLocalPlayer && cameraTransform != null && isShaking)
        {
            cameraTransform.localPosition = originalCameraPosition;
            isShaking = false;
        }
    }
}

// Отдельный компонент для пули
public class NetworkBullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    public ParticleSystem hitEffect;
    public float lifetime = 5f;

    private string targetName;

    void Start()
    {
        // Уничтожаем пулю через определенное время
        if (isServer)
        {
            Invoke(nameof(DestroyBullet), lifetime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Обрабатываем коллизию только на сервере
        if (!isServer) return;

        // Проверяем попадание по цели
        if (!string.IsNullOrEmpty(targetName) && collision.gameObject.name.Contains(targetName))
        {
            // Вызываем эффект попадания на всех клиентах
            RpcPlayHitEffect(collision.contacts[0].point);
        }

        // Уничтожаем пулю
        DestroyBullet();
    }

    [ClientRpc]
    void RpcPlayHitEffect(Vector3 hitPoint)
    {
        if (hitEffect != null)
        {
            ParticleSystem effect = Instantiate(hitEffect, hitPoint, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

    [Server]
    void DestroyBullet()
    {
        NetworkServer.Destroy(gameObject);
    }

    public void SetTargetName(string name)
    {
        targetName = name;
    }
}