using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OROBOROS : MonoBehaviour
{
    private AGENT agentScript;
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private Animator animator;

    [Header("Fly Mode Settings")]
    [Tooltip("Может ли агент летать")]
    public bool canFly = true;

    [Tooltip("Шанс активации полёта (0-1)")]
    [Range(0f, 1f)]
    public float flyChance = 0.15f;

    [Tooltip("Продолжительность полёта в секундах")]
    public float flyDuration = 5f;

    [Tooltip("Время восстановления после полёта")]
    public float flyCooldown = 20f;

    [Tooltip("Интервал проверки активации полёта")]
    public float flyCheckInterval = 2f;

    [Tooltip("Минимальное расстояние до цели для активации")]
    public float minFlyTriggerDistance = 10f;

    [Tooltip("Максимальное расстояние до цели для активации")]
    public float maxFlyTriggerDistance = 30f;

    [Header("Fly Movement Settings")]
    [Tooltip("Скорость полёта")]
    public float flySpeed = 8f;

    [Tooltip("Высота полёта над землёй")]
    public float flyHeight = 5f;

    [Tooltip("Скорость подъёма/спуска")]
    public float verticalSpeed = 3f;

    [Tooltip("Скорость поворота в полёте")]
    public float flyRotationSpeed = 3f;

    [Header("Animation Settings")]
    [Tooltip("Название анимации полёта")]
    public string flyAnimationName = "Flymode";

    [Header("Effects")]
    [Tooltip("Эффект частиц при взлёте")]
    public ParticleSystem takeoffEffect;

    [Tooltip("Эффект частиц во время полёта")]
    public ParticleSystem flyingEffect;

    [Tooltip("Эффект частиц при посадке")]
    public ParticleSystem landingEffect;

    [Tooltip("Звук взлёта")]
    public AudioClip takeoffSound;

    [Tooltip("Звук полёта (ループ)")]
    public AudioClip flyingSound;

    [Tooltip("Звук посадки")]
    public AudioClip landingSound;

    private AudioSource audioSource;

    // Внутренние переменные
    private bool isFlying = false;
    private float lastFlyTime = 0f;
    private float lastFlyCheckTime = 0f;
    private float flyEndTime = 0f;
    private Vector3 flyTargetPosition;
    private float currentFlyHeight = 0f;
    private bool isTakingOff = false;
    private bool isLanding = false;

    private void Start()
    {
        agentScript = GetComponent<AGENT>();
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!canFly || agentScript == null) return;

        // Проверка условий для активации полёта
        if (!isFlying && !isTakingOff && !isLanding)
        {
            CheckFlyActivation();
        }

        // Управление полётом
        if (isFlying)
        {
            UpdateFlyMovement();
            CheckFlyDuration();
        }
    }

    private void CheckFlyActivation()
    {
        // Проверяем, не занят ли агент другими действиями
        if (IsAgentBusy()) return;

        // Проверяем кулдаун
        if (Time.time < lastFlyTime + flyCooldown) return;

        // Проверяем интервал проверки
        if (Time.time < lastFlyCheckTime + flyCheckInterval) return;

        lastFlyCheckTime = Time.time;

        // Проверяем наличие цели
        if (agentScript.player == null) return;

        // Проверяем расстояние до цели
        float distanceToTarget = Vector3.Distance(transform.position, agentScript.player.transform.position);
        
        if (distanceToTarget < minFlyTriggerDistance || distanceToTarget > maxFlyTriggerDistance)
            return;

        // Проверяем шанс активации
        if (Random.Range(0f, 1f) <= flyChance)
        {
            StartFlyMode();
        }
    }

    private bool IsAgentBusy()
    {
        // Используем рефлексию для проверки приватных полей AGENT
        var agentType = agentScript.GetType();
        var isDashingField = agentType.GetField("isDashing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isJumpingField = agentType.GetField("isJumping", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isShootingField = agentType.GetField("isShooting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isMeleeAttackingField = agentType.GetField("isMeleeAttacking", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool isDashing = isDashingField != null && (bool)isDashingField.GetValue(agentScript);
        bool isJumping = isJumpingField != null && (bool)isJumpingField.GetValue(agentScript);
        bool isShooting = isShootingField != null && (bool)isShootingField.GetValue(agentScript);
        bool isMeleeAttacking = isMeleeAttackingField != null && (bool)isMeleeAttackingField.GetValue(agentScript);

        return isDashing || isJumping || isShooting || isMeleeAttacking;
    }

    private void StartFlyMode()
    {
        StartCoroutine(FlyModeCoroutine());
    }

    private IEnumerator FlyModeCoroutine()
    {
        // Взлёт
        yield return StartCoroutine(TakeOff());

        // Полёт
        isFlying = true;
        lastFlyTime = Time.time;
        flyEndTime = Time.time + flyDuration;

        // Эффекты полёта
        if (flyingEffect != null)
        {
            flyingEffect.Play();
        }

        if (flyingSound != null && audioSource != null)
        {
            audioSource.clip = flyingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Ожидание окончания полёта
        while (Time.time < flyEndTime)
        {
            yield return null;
        }

        // Посадка
        yield return StartCoroutine(Landing());

        isFlying = false;
    }

    private IEnumerator TakeOff()
    {
        isTakingOff = true;

        // Отключаем NavMeshAgent
        if (navAgent != null)
        {
            navAgent.enabled = false;
        }

        // Отключаем гравитацию
        if (rb != null)
        {
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
        }

        // Воспроизводим анимацию
        if (animator != null && !string.IsNullOrEmpty(flyAnimationName))
        {
            animator.Play(flyAnimationName);
        }

        // Эффекты взлёта
        if (takeoffEffect != null)
        {
            ParticleSystem effect = Instantiate(takeoffEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 3f);
        }

        if (takeoffSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(takeoffSound);
        }

        // Плавный подъём
        float takeoffTime = 0f;
        float takeoffDuration = 1.5f;
        Vector3 startPosition = transform.position;

        while (takeoffTime < takeoffDuration)
        {
            takeoffTime += Time.deltaTime;
            float progress = takeoffTime / takeoffDuration;
            
            currentFlyHeight = Mathf.Lerp(0f, flyHeight, progress);
            Vector3 targetPos = new Vector3(startPosition.x, startPosition.y + currentFlyHeight, startPosition.z);
            
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * verticalSpeed);
            
            yield return null;
        }

        isTakingOff = false;
    }

    private void UpdateFlyMovement()
    {
        if (agentScript.player == null) return;

        // Целевая позиция на высоте
        Vector3 targetGroundPos = agentScript.player.transform.position;
        flyTargetPosition = new Vector3(targetGroundPos.x, targetGroundPos.y + flyHeight, targetGroundPos.z);

        // Движение к цели
        Vector3 direction = (flyTargetPosition - transform.position).normalized;
        Vector3 moveVector = direction * flySpeed * Time.deltaTime;
        
        if (rb != null)
        {
            rb.MovePosition(transform.position + moveVector);
        }
        else
        {
            transform.position += moveVector;
        }

        // Поворот к цели
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * flyRotationSpeed);
        }
    }

    private void CheckFlyDuration()
    {
        if (Time.time >= flyEndTime)
        {
            // Время полёта истекло, но не прерываем - это делается в корутине
        }
    }

    private IEnumerator Landing()
    {
        isLanding = true;

        // Останавливаем эффекты полёта
        if (flyingEffect != null)
        {
            flyingEffect.Stop();
        }

        if (audioSource != null && audioSource.isPlaying && audioSource.clip == flyingSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        // Эффект посадки
        if (landingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(landingSound);
        }

        // Плавное снижение
        float landingTime = 0f;
        float landingDuration = 1.0f;

        RaycastHit hit;
        float groundY = transform.position.y;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f, agentScript.groundLayer))
        {
            groundY = hit.point.y;
        }

        while (landingTime < landingDuration)
        {
            landingTime += Time.deltaTime;
            float progress = landingTime / landingDuration;

            float targetY = Mathf.Lerp(transform.position.y, groundY, progress);
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

            yield return null;
        }

        // Эффект при посадке
        if (landingEffect != null)
        {
            ParticleSystem effect = Instantiate(landingEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 3f);
        }

        // Включаем NavMeshAgent обратно
        if (rb != null)
        {
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
        }

        if (navAgent != null)
        {
            navAgent.enabled = true;

            // Находим ближайшую точку на NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(transform.position, out navHit, 5f, NavMesh.AllAreas))
            {
                navAgent.Warp(navHit.position);
            }
        }

        isLanding = false;
    }

    // Публичный метод для принудительной активации полёта (опционально)
    public void ForceFly()
    {
        if (!isFlying && !isTakingOff && !isLanding && canFly)
        {
            StartFlyMode();
        }
    }

    // Публичный метод для досрочной посадки (опционально)
    public void ForceLanding()
    {
        if (isFlying)
        {
            flyEndTime = Time.time;
        }
    }

    // Геттер для проверки состояния полёта (для других скриптов)
    public bool IsFlying()
    {
        return isFlying || isTakingOff || isLanding;
    }

    private void OnDrawGizmosSelected()
    {
        if (!canFly) return;

        // Визуализация высоты полёта
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * flyHeight, 1f);

        // Визуализация дистанции активации
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minFlyTriggerDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxFlyTriggerDistance);
    }
}