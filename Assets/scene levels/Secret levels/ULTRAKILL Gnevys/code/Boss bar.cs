using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class BossHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool invulnerable = false;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathAnimationName = "Death";
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private bool waitForDeathAnimation = true;

    [Header("UI Components")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text healthText; // Необязательно для отображения цифр

    [Header("Visual Effects")]
    [SerializeField] private float smoothTransitionSpeed = 2f;
    [SerializeField] private bool useColorTransition = true;
    [SerializeField] private Gradient healthColorGradient;

    [Header("Shake Effect")]
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private bool shakeOnDamage = true;

    [Header("Damage Multiplier Trigger")]
    [SerializeField] private bool useDamageMultiplierTrigger = true;
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private LayerMask bulletLayers = -1;
    [SerializeField] private string[] bulletTags = { "Bullet", "Projectile" };
    [SerializeField] private GameObject criticalHitEffect;
    [SerializeField] private AudioClip criticalHitSound;
    [SerializeField] private Color criticalDamageColor = Color.red;
    [SerializeField] private float criticalShakeIntensity = 15f;
    [SerializeField] private bool showDebugInfo = true;

    [Header("Death Effects")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private bool disableScriptsOnDeath = true;
    [SerializeField] private bool disableColliderOnDeath = true;
    [SerializeField] private bool disableRigidbodyOnDeath = true;

    [Header("Events")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent<float> OnCriticalDamageTaken; // Событие для критического урона
    public UnityEvent<GameObject> OnBulletHit; // Событие для попадания пули

    private float currentHealth;
    private float targetFillAmount;
    private Coroutine healthTransitionCoroutine;
    private Coroutine shakeCoroutine;
    private Vector3 originalHealthBarPosition;
    private AudioSource audioSource;
    private bool isDead = false;

    // Свойства для внешнего доступа
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0 && !isDead;
    public float DamageMultiplier => damageMultiplier;

    void Start()
    {
        InitializeHealth();
        StoreOriginalPosition();
        InitializeComponents();
        SetupTrigger();
    }

    void InitializeHealth()
    {
        currentHealth = maxHealth;
        targetFillAmount = 1f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = targetFillAmount;
            UpdateHealthBarColor();
        }

        UpdateHealthText();
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    void StoreOriginalPosition()
    {
        if (healthBarFill != null)
        {
            originalHealthBarPosition = healthBarFill.transform.position;
        }
    }

    void InitializeComponents()
    {
        // Получаем или добавляем AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (criticalHitSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Получаем Animator если не назначен
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void SetupTrigger()
    {
        if (!useDamageMultiplierTrigger) return;

        // Проверяем наличие коллайдера
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            // Добавляем коллайдер, если его нет
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("Добавлен BoxCollider для триггера урона");
        }

        // Настраиваем как триггер
        triggerCollider.isTrigger = true;

        if (showDebugInfo)
        {
            Debug.Log($"Триггер удвоения урона настроен. Множитель: {damageMultiplier}x");
        }
    }

    // Обработка попадания пули в триггер
    void OnTriggerEnter(Collider other)
    {
        if (!useDamageMultiplierTrigger || !IsAlive) return;

        // Проверяем, является ли объект пулей
        if (!IsBullet(other.gameObject)) return;

        // Получаем компонент урона пули
        Bulletdamage bulletDamage = other.GetComponent<Bulletdamage>();
        if (bulletDamage == null) return;

        // Проверяем, не попала ли пуля уже
        if (bulletDamage.HasHit) return;

        // Вычисляем урон с множителем
        float originalDamage = bulletDamage.Damage;
        float multipliedDamage = originalDamage * damageMultiplier;

        // Наносим урон напрямую, минуя обычную систему
        TakeCriticalDamage(multipliedDamage, bulletDamage.gameObject);

        // Вызываем событие попадания пули
        OnBulletHit?.Invoke(bulletDamage.gameObject);

        if (showDebugInfo)
        {
            Debug.Log($"Пуля попала в триггер! Урон: {originalDamage} -> {multipliedDamage} (x{damageMultiplier})");
        }
    }

    bool IsBullet(GameObject obj)
    {
        // Проверяем слой
        if (!IsInLayerMask(obj.layer, bulletLayers)) return false;

        // Проверяем тег
        if (bulletTags.Length == 0) return true;

        foreach (string tag in bulletTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // Метод для нанесения критического урона
    void TakeCriticalDamage(float damage, GameObject bullet)
    {
        if (invulnerable || !IsAlive || damage <= 0) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        float actualDamage = previousHealth - currentHealth;

        UpdateHealthBar();

        // Специальная тряска для критического урона
        if (shakeOnDamage && actualDamage > 0)
        {
            ShakeHealthBar(criticalShakeIntensity);
        }

        // Создаем эффект критического попадания
        CreateCriticalHitEffect(bullet);

        // Вызываем события
        OnCriticalDamageTaken?.Invoke(actualDamage);
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(HealthPercentage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void CreateCriticalHitEffect(GameObject bullet)
    {
        // Создаем визуальный эффект критического попадания
        if (criticalHitEffect != null)
        {
            Vector3 effectPosition = bullet != null ? bullet.transform.position : transform.position;
            GameObject effect = Instantiate(criticalHitEffect, effectPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Воспроизводим звук критического попадания
        if (criticalHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(criticalHitSound);
        }

        // Временно меняем цвет полоски здоровья
        StartCoroutine(FlashCriticalColor());
    }

    IEnumerator FlashCriticalColor()
    {
        if (healthBarFill == null) yield break;

        Color originalColor = healthBarFill.color;
        float flashDuration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime * 8f, 1f); // Частота мигания
            healthBarFill.color = Color.Lerp(originalColor, criticalDamageColor, t);
            yield return null;
        }

        // Возвращаем исходный цвет
        UpdateHealthBarColor();
    }

    public void TakeDamage(float damage)
    {
        if (invulnerable || !IsAlive || damage <= 0) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        float actualDamage = previousHealth - currentHealth;

        UpdateHealthBar();

        // Запуск тряски при получении урона
        if (shakeOnDamage && actualDamage > 0)
        {
            ShakeHealthBar();
        }

        // Вызываем события
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(HealthPercentage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (!IsAlive || healAmount <= 0) return;

        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);
        UpdateHealthBar();
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        if (newMaxHealth <= 0) return;

        float healthPercentage = HealthPercentage;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercentage;

        UpdateHealthBar();
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        targetFillAmount = HealthPercentage;

        // Останавливаем предыдущую анимацию если она есть
        if (healthTransitionCoroutine != null)
        {
            StopCoroutine(healthTransitionCoroutine);
        }

        // Запускаем плавный переход
        healthTransitionCoroutine = StartCoroutine(SmoothHealthTransition());
    }

    IEnumerator SmoothHealthTransition()
    {
        float startFillAmount = healthBarFill.fillAmount;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * smoothTransitionSpeed;
            float currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, elapsedTime);

            healthBarFill.fillAmount = currentFillAmount;
            UpdateHealthBarColor();
            UpdateHealthText();

            yield return null;
        }

        // Устанавливаем точное значение в конце
        healthBarFill.fillAmount = targetFillAmount;
        UpdateHealthBarColor();
        UpdateHealthText();
    }

    void ShakeHealthBar(float intensity = -1f)
    {
        if (healthBarFill == null) return;

        // Используем переданную интенсивность или стандартную
        float shakeForce = intensity > 0 ? intensity : shakeIntensity;

        // Останавливаем предыдущую тряску если она активна
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(shakeForce));
    }

    IEnumerator ShakeCoroutine(float intensity)
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Вычисляем случайное смещение
            Vector3 randomOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0
            );

            // Применяем смещение к позиции health bar
            healthBarFill.transform.position = originalHealthBarPosition + randomOffset;

            yield return null;
        }

        // Возвращаем health bar в исходную позицию
        healthBarFill.transform.position = originalHealthBarPosition;
        shakeCoroutine = null;
    }

    void UpdateHealthBarColor()
    {
        if (!useColorTransition || healthBarFill == null || healthColorGradient == null) return;

        healthBarFill.color = healthColorGradient.Evaluate(HealthPercentage);
    }

    void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }

    void Die()
    {
        if (isDead) return; // Предотвращаем повторный вызов
        
        isDead = true;
        Debug.Log("Босс побежден!");

        // Останавливаем тряску при смерти
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            healthBarFill.transform.position = originalHealthBarPosition;
        }

        // Отключаем все скрипты на объекте
        DisableAllScripts();

        // Вызываем событие смерти
        OnDeath?.Invoke();

        // Скрываем health bar
        if (healthBarFill != null)
        {
            healthBarFill.transform.parent.gameObject.SetActive(false);
        }

        // Запускаем анимацию и эффекты смерти
        StartCoroutine(DeathSequence());
    }

    void DisableAllScripts()
    {
        if (!disableScriptsOnDeath) return;

        // Получаем все MonoBehaviour компоненты на объекте
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        
        foreach (MonoBehaviour script in scripts)
        {
            // Не отключаем сам BossHealth, чтобы DeathSequence мог завершиться
            if (script != this && script != null)
            {
                script.enabled = false;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Отключен скрипт: {script.GetType().Name}");
                }
            }
        }

        // Отключаем коллайдеры
        if (disableColliderOnDeath)
        {
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Отключен коллайдер: {col.GetType().Name}");
                    }
                }
            }
        }

        // Отключаем Rigidbody (делаем кинематическим)
        if (disableRigidbodyOnDeath)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                if (showDebugInfo)
                {
                    Debug.Log("Rigidbody отключен (isKinematic = true)");
                }
            }
        }
    }

    IEnumerator DeathSequence()
    {
        // Воспроизводим анимацию смерти
        if (animator != null && !string.IsNullOrEmpty(deathAnimationName))
        {
            animator.Play(deathAnimationName);
            
            if (waitForDeathAnimation)
            {
                yield return new WaitForSeconds(deathAnimationDuration);
            }
        }

        // Создаем эффект смерти
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Воспроизводим звук смерти
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
            
            // Ждем завершения звука если он есть
            if (waitForDeathAnimation)
            {
                yield return new WaitForSeconds(deathSound.length);
            }
        }

        // Ждем перед уничтожением
        yield return new WaitForSeconds(destroyDelay);
        
        // Отключаем сам скрипт BossHealth перед уничтожением
        if (showDebugInfo)
        {
            Debug.Log("Отключен скрипт: BossHealth");
        }
        this.enabled = false;
        
        Destroy(gameObject);
    }

    // Методы для настройки триггера в рантайме
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(1f, multiplier);
    }

    public void SetUseDamageMultiplierTrigger(bool use)
    {
        useDamageMultiplierTrigger = use;

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = use;
        }
    }

    public void SetBulletTags(string[] tags)
    {
        bulletTags = tags;
    }

    public void SetBulletLayers(LayerMask layers)
    {
        bulletLayers = layers;
    }

    // Методы для тестинга в инспекторе
    [ContextMenu("Take 10 Damage")]
    void TestDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Take Critical Damage")]
    void TestCriticalDamage()
    {
        TakeCriticalDamage(10f * damageMultiplier, null);
    }

    [ContextMenu("Heal 20")]
    void TestHeal()
    {
        Heal(20f);
    }

    [ContextMenu("Set Invulnerable")]
    void ToggleInvulnerability()
    {
        invulnerable = !invulnerable;
        Debug.Log($"Boss invulnerability: {invulnerable}");
    }

    [ContextMenu("Test Shake")]
    void TestShake()
    {
        ShakeHealthBar();
    }

    [ContextMenu("Test Critical Shake")]
    void TestCriticalShake()
    {
        ShakeHealthBar(criticalShakeIntensity);
    }

    [ContextMenu("Test Death Animation")]
    void TestDeathAnimation()
    {
        Die();
    }

    [ContextMenu("Test Trigger Setup")]
    void TestTriggerSetup()
    {
        Collider triggerCollider = GetComponent<Collider>();
        Debug.Log($"Trigger enabled: {useDamageMultiplierTrigger}");
        Debug.Log($"Has Collider: {triggerCollider != null}");
        Debug.Log($"Is Trigger: {triggerCollider != null && triggerCollider.isTrigger}");
        Debug.Log($"Damage Multiplier: {damageMultiplier}x");
    }

    // Метод для принудительной остановки тряски (может быть полезно)
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
            if (healthBarFill != null)
            {
                healthBarFill.transform.position = originalHealthBarPosition;
            }
        }
    }

    // Визуализация триггера в редакторе
    void OnDrawGizmosSelected()
    {
        if (!useDamageMultiplierTrigger) return;

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null) return;

        // Рисуем границы триггера
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Оранжевый полупрозрачный
        Gizmos.matrix = transform.localToWorldMatrix;

        if (triggerCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
        else if (triggerCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (triggerCollider is CapsuleCollider capsuleCollider)
        {
            Gizmos.DrawWireCube(capsuleCollider.center,
                new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2));
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}