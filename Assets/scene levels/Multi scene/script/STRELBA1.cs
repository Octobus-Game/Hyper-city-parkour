using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SELVEL8 : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;
    public float cooldownTime = 5f;

    [Header("Orbit Trap Settings")]
    public GameObject orbitTrapPrefab;
    public float orbitTrapSpeed = 15f;
    public float orbitTrapFireRate = 1f;
    public int maxOrbitTraps = 3;
    public float orbitTrapLifetime = 30f;

    [Header("Charged Shot Settings")]
    public bool enableChargedShot = true;
    [Range(0.5f, 5f)] public float chargeTime = 2f;
    [Range(1f, 3f)] public float maxDamageMultiplier = 100f;
    [Range(1f, 3f)] public float maxSpeedMultiplier = 100f;
    public AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

    [Header("Charged Shot Effects")]
    public ParticleSystem chargeEffect;
    public AudioClip chargeSound;
    public AudioClip fullyChargedSound;
    public Color chargeColor = Color.yellow;
    public bool showChargeIndicator = true;

    [Header("Recoil Settings")]
    [Tooltip("Объект, который будет трясти (например, оружие или камера)")]
    public Transform recoilTarget;
    [Tooltip("Включить отдачу по позиции")]
    public bool enablePositionRecoil = true;
    [Tooltip("Включить отдачу по повороту")]
    public bool enableRotationRecoil = true;

    [Header("Recoil — Позиция")]
    [Tooltip("Смещение назад при выстреле")]
    public float recoilKickback = 0.15f;
    [Tooltip("Смещение вверх при выстреле")]
    public float recoilKickUp = 0.05f;
    [Tooltip("Случайное боковое смещение")]
    public float recoilKickSide = 0.02f;
    [Tooltip("Скорость возврата в исходное положение")]
    public float recoilPositionReturnSpeed = 10f;

    [Header("Recoil — Поворот")]
    [Tooltip("Задран ствол вверх (pitch)")]
    public float recoilPitch = 2f;
    [Tooltip("Случайный yaw")]
    public float recoilYaw = 0.5f;
    [Tooltip("Случайный крен (roll)")]
    public float recoilRoll = 0.3f;
    [Tooltip("Скорость возврата поворота")]
    public float recoilRotationReturnSpeed = 8f;

    [Header("Recoil — Заряженный выстрел")]
    [Tooltip("Множитель отдачи для заряженного выстрела")]
    public float chargedRecoilMultiplier = 2.5f;

    [Header("Recoil — Орбитальная ловушка")]
    [Tooltip("Множитель отдачи для орбитальной ловушки")]
    public float orbitTrapRecoilMultiplier = 1.5f;

    [Header("Recoil — Кривые")]
    [Tooltip("Кривая нарастания отдачи")]
    public AnimationCurve recoilKickCurve = AnimationCurve.EaseInOut(0f, 0f, 0.1f, 1f);
    [Tooltip("Кривая затухания отдачи")]
    public AnimationCurve recoilReturnCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    public AudioClip orbitTrapShootSound;
    public AudioSource audioSource;
    public Animator animator;
    public string shootAnimationTrigger = "Shoot";
    public string chargeAnimationTrigger = "Charge";
    public string orbitTrapAnimationTrigger = "OrbitTrap";
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
    private float nextFireTime = 0f;
    private float nextOrbitTrapTime = 0f;
    private bool isCooldown = false;
    private Vector3 originalCameraPosition;
    private bool isShaking = false;

    // Orbit trap tracking
    private List<GameObject> activeOrbitTraps = new List<GameObject>();

    // Charged shot variables
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private float currentDamageMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;
    private bool isFullyCharged = false;
    private AudioSource chargeAudioSource;

    // Animation hashes
    private int shootAnimationHash;
    private int chargeAnimationHash;
    private int orbitTrapAnimationHash;

    // Recoil state
    private Vector3 recoilPositionOffset = Vector3.zero;
    private Vector3 recoilRotationOffset = Vector3.zero;
    private Vector3 currentRecoilVelocityPos = Vector3.zero;
    private Vector3 currentRecoilVelocityRot = Vector3.zero;
    private Vector3 recoilTargetPosition;
    private Quaternion recoilTargetRotation;
    private bool recoilInitialized = false;

    void Awake()
    {
        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            originalCameraPosition = cameraTransform.localPosition;
        }

        if (targetPrefab != null)
            targetName = targetPrefab.name;

        shootAnimationHash = Animator.StringToHash(shootAnimationTrigger);
        chargeAnimationHash = Animator.StringToHash(chargeAnimationTrigger);
        orbitTrapAnimationHash = Animator.StringToHash(orbitTrapAnimationTrigger);
        waitForEndOfFrame = new WaitForEndOfFrame();

        if (chargeSound != null || fullyChargedSound != null)
        {
            GameObject chargeAudioObj = new GameObject("ChargeAudioSource");
            chargeAudioObj.transform.SetParent(transform);
            chargeAudioSource = chargeAudioObj.AddComponent<AudioSource>();
            chargeAudioSource.playOnAwake = false;
        }

        // Инициализация отдачи
        if (recoilTarget != null)
        {
            recoilTargetPosition = recoilTarget.localPosition;
            recoilTargetRotation = recoilTarget.localRotation;
            recoilInitialized = true;
        }
    }

    void Update()
    {
        HandleInput();
        HandleCharging();
        CleanupDestroyedTraps();
        UpdateRecoil();
    }

    // ---------------------------------------------
    //  RECOIL
    // ---------------------------------------------

    /// <summary>
    /// Вызвать отдачу с заданным множителем силы.
    /// </summary>
    void ApplyRecoil(float multiplier = 1f)
    {
        if (!recoilInitialized) return;

        if (enablePositionRecoil)
        {
            // Назад + вверх + случайный бок
            Vector3 kick = new Vector3(
                Random.Range(-recoilKickSide, recoilKickSide) * multiplier,
                recoilKickUp * multiplier,
                -recoilKickback * multiplier
            );
            recoilPositionOffset += kick;
        }

        if (enableRotationRecoil)
        {
            Vector3 rotKick = new Vector3(
                recoilPitch * multiplier,
                Random.Range(-recoilYaw, recoilYaw) * multiplier,
                Random.Range(-recoilRoll, recoilRoll) * multiplier
            );
            recoilRotationOffset += rotKick;
        }
    }

    void UpdateRecoil()
    {
        if (!recoilInitialized) return;

        // Плавно возвращаем позицию к нулю
        recoilPositionOffset = Vector3.SmoothDamp(
            recoilPositionOffset,
            Vector3.zero,
            ref currentRecoilVelocityPos,
            1f / recoilPositionReturnSpeed
        );

        // Плавно возвращаем поворот к нулю
        recoilRotationOffset = Vector3.SmoothDamp(
            recoilRotationOffset,
            Vector3.zero,
            ref currentRecoilVelocityRot,
            1f / recoilRotationReturnSpeed
        );

        // Применяем к recoilTarget
        if (enablePositionRecoil)
            recoilTarget.localPosition = recoilTargetPosition + recoilPositionOffset;

        if (enableRotationRecoil)
            recoilTarget.localRotation = recoilTargetRotation * Quaternion.Euler(recoilRotationOffset);
    }

    // ---------------------------------------------
    //  INPUT
    // ---------------------------------------------

    void HandleInput()
    {
        if (Input.GetButton("Fire1") && CanShoot() && !isCharging)
        {
            Shoot(1f, 1f);
        }

        if (Input.GetButtonDown("Fire2") && CanShootOrbitTrap())
        {
            ShootOrbitTrap();
        }

        if (enableChargedShot)
        {
            if (Input.GetKeyDown(KeyCode.R) && CanShoot())
                StartCharging();
            else if (Input.GetKey(KeyCode.R) && isCharging)
                ContinueCharging();
            else if (Input.GetKeyUp(KeyCode.R) && isCharging)
                FireChargedShot();
        }
    }

    void CleanupDestroyedTraps()
    {
        for (int i = activeOrbitTraps.Count - 1; i >= 0; i--)
        {
            if (activeOrbitTraps[i] == null)
                activeOrbitTraps.RemoveAt(i);
        }
    }

    // ---------------------------------------------
    //  ORBIT TRAP
    // ---------------------------------------------

    bool CanShootOrbitTrap()
    {
        return Time.time >= nextOrbitTrapTime &&
               !isCooldown &&
               orbitTrapPrefab != null &&
               activeOrbitTraps.Count < maxOrbitTraps;
    }

    void ShootOrbitTrap()
    {
        GameObject orbitTrap = Instantiate(orbitTrapPrefab, firePoint.position, firePoint.rotation);
        activeOrbitTraps.Add(orbitTrap);

        if (orbitTrap.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.velocity = firePoint.forward * orbitTrapSpeed;

        Destroy(orbitTrap, orbitTrapLifetime);
        PlayOrbitTrapEffects();

        // Отдача для ловушки
        ApplyRecoil(orbitTrapRecoilMultiplier);

        if (ShouldShakeCamera())
            StartCoroutine(ShakeCamera(1.2f));

        nextOrbitTrapTime = Time.time + orbitTrapFireRate;
        Debug.Log($"Выпущена орбитальная ловушка! Активных ловушек: {activeOrbitTraps.Count}/{maxOrbitTraps}");
    }

    void PlayOrbitTrapEffects()
    {
        muzzleFlash?.Play();

        if (audioSource != null && orbitTrapShootSound != null)
            audioSource.PlayOneShot(orbitTrapShootSound);
        else if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (animator != null)
            animator.SetTrigger(orbitTrapAnimationHash);
    }

    // ---------------------------------------------
    //  CHARGE
    // ---------------------------------------------

    void HandleCharging()
    {
        if (!isCharging) return;

        currentChargeTime += Time.deltaTime;
        float chargeProgress = Mathf.Clamp01(currentChargeTime / chargeTime);
        float curveValue = chargeCurve.Evaluate(chargeProgress);

        currentDamageMultiplier = Mathf.Lerp(1f, maxDamageMultiplier, curveValue);
        currentSpeedMultiplier = Mathf.Lerp(1f, maxSpeedMultiplier, curveValue);

        if (chargeProgress >= 1f && !isFullyCharged)
        {
            isFullyCharged = true;
            OnFullyCharged();
        }

        UpdateChargeEffects(chargeProgress);
    }

    void StartCharging()
    {
        isCharging = true;
        currentChargeTime = 0f;
        currentDamageMultiplier = 1f;
        currentSpeedMultiplier = 1f;
        isFullyCharged = false;

        chargeEffect?.Play();

        if (chargeSound != null && chargeAudioSource != null)
        {
            chargeAudioSource.clip = chargeSound;
            chargeAudioSource.loop = true;
            chargeAudioSource.Play();
        }

        if (animator != null)
            animator.SetTrigger(chargeAnimationHash);

        Debug.Log("Начата зарядка выстрела");
    }

    void ContinueCharging() { }

    void OnFullyCharged()
    {
        if (fullyChargedSound != null && chargeAudioSource != null)
        {
            chargeAudioSource.Stop();
            chargeAudioSource.PlayOneShot(fullyChargedSound);
        }
        Debug.Log($"Зарядка завершена! Урон x{currentDamageMultiplier:F1}, Скорость x{currentSpeedMultiplier:F1}");
    }

    void UpdateChargeEffects(float chargeProgress)
    {
        if (chargeEffect != null)
        {
            var emission = chargeEffect.emission;
            emission.rateOverTime = 10f + chargeProgress * 40f;

            var main = chargeEffect.main;
            main.startColor = Color.Lerp(Color.white, chargeColor, chargeProgress);
        }

        if (chargeAudioSource != null && chargeAudioSource.isPlaying)
        {
            chargeAudioSource.volume = 0.3f + chargeProgress * 0.7f;
            chargeAudioSource.pitch = 1f + chargeProgress * 0.5f;
        }
    }

    void FireChargedShot()
    {
        Shoot(currentDamageMultiplier, currentSpeedMultiplier);
        StopCharging();
        Debug.Log($"Заряженный выстрел! Урон x{currentDamageMultiplier:F1}, Скорость x{currentSpeedMultiplier:F1}");
    }

    void StopCharging()
    {
        isCharging = false;
        currentChargeTime = 0f;
        currentDamageMultiplier = 1f;
        currentSpeedMultiplier = 1f;
        isFullyCharged = false;

        chargeEffect?.Stop();
        chargeAudioSource?.Stop();
    }

    // ---------------------------------------------
    //  SHOOT
    // ---------------------------------------------

    bool CanShoot()
    {
        return Time.time >= nextFireTime && !isCooldown;
    }

    void Shoot(float damageMultiplier = 1f, float speedMultiplier = 1f)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        if (!bullet.CompareTag("Bullet"))
            bullet.tag = "Bullet";

        ApplyBulletModifiers(bullet, damageMultiplier, speedMultiplier);

        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.velocity = firePoint.forward * (bulletSpeed * speedMultiplier);

        PlayEffects();

        // Отдача — заряженный выстрел сильнее
        float recoilMult = (damageMultiplier > 1f || speedMultiplier > 1f)
            ? Mathf.Max(damageMultiplier, speedMultiplier) * chargedRecoilMultiplier / maxDamageMultiplier
            : 1f;

        // Нормализуем чтобы не было безумной отдачи при 100x уроне
        recoilMult = Mathf.Clamp(recoilMult, 1f, chargedRecoilMultiplier);
        ApplyRecoil(recoilMult);

        if (ShouldShakeCamera())
        {
            float shakeMultiplier = Mathf.Max(damageMultiplier, speedMultiplier);
            StartCoroutine(ShakeCamera(shakeMultiplier));
        }

        nextFireTime = Time.time + fireRate;

        if (cooldownTime > 0)
            StartCooldown();
    }

    void ApplyBulletModifiers(GameObject bullet, float damageMultiplier, float speedMultiplier)
    {
        Bulletdamage bulletDamage = bullet.GetComponent<Bulletdamage>();
        if (bulletDamage != null)
        {
            bulletDamage.SetDamage(bulletDamage.Damage * damageMultiplier);
            bulletDamage.SetSpeed(bulletDamage.Speed * speedMultiplier);

            if (damageMultiplier > 1f || speedMultiplier > 1f)
            {
                bullet.transform.localScale *= (1f + (damageMultiplier - 1f) * 0.5f);

                Renderer bulletRenderer = bullet.GetComponent<Renderer>();
                if (bulletRenderer != null)
                    bulletRenderer.material.color = Color.Lerp(Color.white, chargeColor, damageMultiplier - 1f);
            }
        }
    }

    void PlayEffects()
    {
        muzzleFlash?.Play();

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        if (animator != null)
            animator.SetTrigger(shootAnimationHash);
    }

    // ---------------------------------------------
    //  CAMERA SHAKE
    // ---------------------------------------------

    bool ShouldShakeCamera()
    {
        return cameraTransform != null && !isShaking && Random.value <= shakeChance;
    }

    IEnumerator ShakeCamera(float intensityMultiplier = 1f)
    {
        isShaking = true;
        float elapsedTime = 0f;
        float duration = shakeDuration;
        float intensity = shakeIntensity * intensityMultiplier;
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
        if (collision.gameObject.name.Contains(targetName))
        {
            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }
        }
        Destroy(gameObject);
    }

    void OnDisable()
    {
        if (cameraTransform != null && isShaking)
        {
            cameraTransform.localPosition = originalCameraPosition;
            isShaking = false;
        }

        if (isCharging)
            StopCharging();

        // Сброс отдачи
        if (recoilInitialized)
        {
            recoilTarget.localPosition = recoilTargetPosition;
            recoilTarget.localRotation = recoilTargetRotation;
        }
    }

    // ---------------------------------------------
    //  PUBLIC API — ORBIT TRAPS
    // ---------------------------------------------

    public void DestroyAllOrbitTraps()
    {
        foreach (GameObject trap in activeOrbitTraps)
        {
            if (trap != null) Destroy(trap);
        }
        activeOrbitTraps.Clear();
    }

    public void SetMaxOrbitTraps(int maxTraps)
    {
        maxOrbitTraps = Mathf.Max(1, maxTraps);
        while (activeOrbitTraps.Count > maxOrbitTraps)
        {
            GameObject oldest = activeOrbitTraps[0];
            activeOrbitTraps.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }
    }

    public void SetOrbitTrapSpeed(float speed)    => orbitTrapSpeed = Mathf.Max(1f, speed);
    public void SetOrbitTrapFireRate(float rate)  => orbitTrapFireRate = Mathf.Max(0.1f, rate);

    // ---------------------------------------------
    //  PUBLIC API — CHARGED SHOT
    // ---------------------------------------------

    public void SetChargedShotEnabled(bool enabled)
    {
        enableChargedShot = enabled;
        if (!enabled && isCharging) StopCharging();
    }

    public void SetChargeTime(float newChargeTime)          => chargeTime = Mathf.Clamp(newChargeTime, 0.5f, 5f);
    public void SetMaxDamageMultiplier(float multiplier)    => maxDamageMultiplier = Mathf.Clamp(multiplier, 1f, 3f);
    public void SetMaxSpeedMultiplier(float multiplier)     => maxSpeedMultiplier = Mathf.Clamp(multiplier, 1f, 3f);

    // ---------------------------------------------
    //  PUBLIC API — RECOIL
    // ---------------------------------------------

    /// <summary>Включить/выключить отдачу.</summary>
    public void SetRecoilEnabled(bool positionEnabled, bool rotationEnabled)
    {
        enablePositionRecoil = positionEnabled;
        enableRotationRecoil = rotationEnabled;
    }

    /// <summary>Задать силу позиционной отдачи.</summary>
    public void SetPositionRecoil(float kickback, float kickUp, float kickSide)
    {
        recoilKickback = Mathf.Max(0f, kickback);
        recoilKickUp   = Mathf.Max(0f, kickUp);
        recoilKickSide = Mathf.Max(0f, kickSide);
    }

    /// <summary>Задать силу угловой отдачи.</summary>
    public void SetRotationRecoil(float pitch, float yaw, float roll)
    {
        recoilPitch = Mathf.Max(0f, pitch);
        recoilYaw   = Mathf.Max(0f, yaw);
        recoilRoll  = Mathf.Max(0f, roll);
    }

    /// <summary>Задать скорость возврата отдачи.</summary>
    public void SetRecoilReturnSpeed(float positionSpeed, float rotationSpeed)
    {
        recoilPositionReturnSpeed = Mathf.Max(0.1f, positionSpeed);
        recoilRotationReturnSpeed = Mathf.Max(0.1f, rotationSpeed);
    }

    // ---------------------------------------------
    //  GETTERS
    // ---------------------------------------------

    public bool IsCharging             => isCharging;
    public float ChargeProgress        => isCharging ? Mathf.Clamp01(currentChargeTime / chargeTime) : 0f;
    public float CurrentDamageMultiplier => currentDamageMultiplier;
    public float CurrentSpeedMultiplier  => currentSpeedMultiplier;
    public bool IsFullyCharged         => isFullyCharged;
    public int ActiveOrbitTrapsCount   => activeOrbitTraps.Count;
    public int MaxOrbitTraps           => maxOrbitTraps;
    public bool CanShootMoreTraps      => activeOrbitTraps.Count < maxOrbitTraps;
    public Vector3 CurrentRecoilPos    => recoilPositionOffset;
    public Vector3 CurrentRecoilRot    => recoilRotationOffset;

    // ---------------------------------------------
    //  CONTEXT MENU (отладка)
    // ---------------------------------------------

    [ContextMenu("Test Charged Shot")]
    void TestChargedShot()
    {
        Debug.Log(enableChargedShot
            ? $"Charged shot ON. Charge time: {chargeTime}s, Max damage: x{maxDamageMultiplier}, Max speed: x{maxSpeedMultiplier}"
            : "Charged shot is disabled");
    }

    [ContextMenu("Force Full Charge")]
    void ForceFullCharge()
    {
        if (enableChargedShot && !isCharging)
        {
            StartCharging();
            currentChargeTime = chargeTime;
            currentDamageMultiplier = maxDamageMultiplier;
            currentSpeedMultiplier = maxSpeedMultiplier;
            isFullyCharged = true;
        }
    }

    [ContextMenu("Test Orbit Trap")]
    void TestOrbitTrap()
    {
        Debug.Log(orbitTrapPrefab != null
            ? $"Orbit trap ready. Active: {activeOrbitTraps.Count}/{maxOrbitTraps}, Speed: {orbitTrapSpeed}, Rate: {orbitTrapFireRate}s"
            : "Orbit trap prefab is not assigned!");
    }

    [ContextMenu("Force Shoot Orbit Trap")]
    void ForceShootOrbitTrap()
    {
        if (orbitTrapPrefab != null && activeOrbitTraps.Count < maxOrbitTraps)
            ShootOrbitTrap();
    }

    [ContextMenu("Destroy All Orbit Traps")]
    void ContextDestroyAllOrbitTraps()
    {
        DestroyAllOrbitTraps();
        Debug.Log("All orbit traps destroyed!");
    }

    [ContextMenu("Test Recoil")]
    void TestRecoil()
    {
        ApplyRecoil(1f);
        Debug.Log($"Recoil applied. Pos offset: {recoilPositionOffset}, Rot offset: {recoilRotationOffset}");
    }

    [ContextMenu("Test Charged Recoil")]
    void TestChargedRecoil()
    {
        ApplyRecoil(chargedRecoilMultiplier);
        Debug.Log($"Charged recoil applied (x{chargedRecoilMultiplier})");
    }
}