
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool invulnerable = false;
    [SerializeField] private float respawnTime = 3f;

    [Header("UI Components")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Text healthText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Visual Effects")]
    [SerializeField] private float smoothTransitionSpeed = 3f;
    [SerializeField] private bool useColorTransition = true;
    [SerializeField] private Gradient healthColorGradient;

    [Header("Screen Effects")]
    [SerializeField] private float screenShakeIntensity = 5f;
    [SerializeField] private float screenShakeDuration = 0.2f;
    [SerializeField] private bool shakeOnDamage = true;
    [SerializeField] private Image damageOverlay; // Красный экран при уроне
    [SerializeField] private float overlayFlashDuration = 0.3f;

    [Header("Regeneration")]
    [SerializeField] private bool enableRegeneration = true;
    [SerializeField] private float regenDelay = 3f; // Задержка перед началом регенерации
    [SerializeField] private float regenRate = 5f; // HP в секунду
    [SerializeField] private float regenInterval = 0.2f; // Интервал между тиками регенерации

    [Header("Events")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerRespawn;
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnCriticalHealth; // Когда здоровье меньше 25%

    private float currentHealth;
    private float targetFillAmount;
    private bool isDead = false;
    private bool isRegenerating = false;

    private Coroutine healthTransitionCoroutine;
    private Coroutine shakeCoroutine;
    private Coroutine damageOverlayCoroutine;
    private Coroutine regenerationCoroutine;

    private Vector3 originalCameraPosition;
    private Camera playerCamera;
    private float lastDamageTime;

    // Свойства для внешнего доступа
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsAlive => currentHealth > 0 && !isDead;
    public bool IsCriticalHealth => HealthPercentage <= 0.25f;

    void Start()
    {
        InitializeHealth();
        SetupCamera();
        InitializeDamageOverlay();
    }

    void InitializeHealth()
    {
        currentHealth = maxHealth;
        targetFillAmount = 1f;
        isDead = false;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = targetFillAmount;
            UpdateHealthBarColor();
        }

        UpdateHealthText();
        OnHealthChanged?.Invoke(HealthPercentage);

        // Скрываем панель Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void SetupCamera()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.position;
        }
    }

    void InitializeDamageOverlay()
    {
        if (damageOverlay != null)
        {
            Color overlayColor = damageOverlay.color;
            overlayColor.a = 0f;
            damageOverlay.color = overlayColor;
        }
    }

    public void TakeDamage(float damage)
    {
        if (invulnerable || !IsAlive || damage <= 0) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        float actualDamage = previousHealth - currentHealth;

        lastDamageTime = Time.time;

        UpdateHealthBar();

        // Визуальные эффекты при получении урона
        if (actualDamage > 0)
        {
            if (shakeOnDamage)
                ShakeScreen();

            ShowDamageOverlay();

            // Останавливаем регенерацию
            StopRegeneration();
        }

        // Вызываем события
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(HealthPercentage);

        if (IsCriticalHealth && IsAlive)
        {
            OnCriticalHealth?.Invoke();
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        else if (enableRegeneration)
        {
            // Запускаем регенерацию после задержки
            StartRegenerationAfterDelay();
        }
    }

    public void Heal(float healAmount)
    {
        if (!IsAlive || healAmount <= 0) return;

        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);
        UpdateHealthBar();
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    public void SetMaxHealth(float newMaxHealth, bool healToMax = false)
    {
        if (newMaxHealth <= 0) return;

        if (healToMax)
        {
            maxHealth = newMaxHealth;
            currentHealth = maxHealth;
        }
        else
        {
            float healthPercentage = HealthPercentage;
            maxHealth = newMaxHealth;
            currentHealth = maxHealth * healthPercentage;
        }

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

    void ShakeScreen()
    {
        if (playerCamera == null) return;

        // Останавливаем предыдущую тряску если она активна
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ScreenShakeCoroutine());
    }

    IEnumerator ScreenShakeCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < screenShakeDuration)
        {
            elapsedTime += Time.deltaTime;

            // Вычисляем случайное смещение
            Vector3 randomOffset = new Vector3(
                Random.Range(-screenShakeIntensity, screenShakeIntensity) * 0.01f,
                Random.Range(-screenShakeIntensity, screenShakeIntensity) * 0.01f,
                0
            );

            // Применяем смещение к позиции камеры
            playerCamera.transform.position = originalCameraPosition + randomOffset;

            yield return null;
        }

        // Возвращаем камеру в исходную позицию
        playerCamera.transform.position = originalCameraPosition;
        shakeCoroutine = null;
    }

    void ShowDamageOverlay()
    {
        if (damageOverlay == null) return;

        if (damageOverlayCoroutine != null)
        {
            StopCoroutine(damageOverlayCoroutine);
        }

        damageOverlayCoroutine = StartCoroutine(DamageOverlayCoroutine());
    }

    IEnumerator DamageOverlayCoroutine()
    {
        // Показываем красный оверлей
        Color startColor = damageOverlay.color;
        startColor.a = 0.3f;
        damageOverlay.color = startColor;

        float elapsedTime = 0f;
        while (elapsedTime < overlayFlashDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0.3f, 0f, elapsedTime / overlayFlashDuration);

            Color overlayColor = damageOverlay.color;
            overlayColor.a = alpha;
            damageOverlay.color = overlayColor;

            yield return null;
        }

        // Полностью скрываем оверлей
        Color finalColor = damageOverlay.color;
        finalColor.a = 0f;
        damageOverlay.color = finalColor;

        damageOverlayCoroutine = null;
    }

    void StartRegenerationAfterDelay()
    {
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
        }

        regenerationCoroutine = StartCoroutine(RegenerationCoroutine());
    }

    IEnumerator RegenerationCoroutine()
    {
        // Ждем задержку перед началом регенерации
        yield return new WaitForSeconds(regenDelay);

        isRegenerating = true;

        while (currentHealth < maxHealth && IsAlive)
        {
            // Проверяем, не получил ли игрок урон недавно
            if (Time.time - lastDamageTime < regenDelay)
            {
                yield return new WaitForSeconds(regenDelay);
                continue;
            }

            float healAmount = regenRate * regenInterval;
            currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);

            UpdateHealthBar();
            OnHealthChanged?.Invoke(HealthPercentage);

            yield return new WaitForSeconds(regenInterval);
        }

        isRegenerating = false;
        regenerationCoroutine = null;
    }

    void StopRegeneration()
    {
        if (regenerationCoroutine != null)
        {
            StopCoroutine(regenerationCoroutine);
            regenerationCoroutine = null;
            isRegenerating = false;
        }
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
        if (isDead) return;

        isDead = true;
        Debug.Log("Игрок погиб!");

        // Останавливаем все эффекты
        StopAllEffects();
        StopRegeneration();

        OnPlayerDeath?.Invoke();

        // Показываем панель Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Автоматическое возрождение через время
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);
        Respawn();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;

        UpdateHealthBar();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        OnPlayerRespawn?.Invoke();
        OnHealthChanged?.Invoke(HealthPercentage);

        Debug.Log("Игрок возрожден!");
    }

    void StopAllEffects()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            if (playerCamera != null)
                playerCamera.transform.position = originalCameraPosition;
        }

        if (damageOverlayCoroutine != null)
        {
            StopCoroutine(damageOverlayCoroutine);
            if (damageOverlay != null)
            {
                Color overlayColor = damageOverlay.color;
                overlayColor.a = 0f;
                damageOverlay.color = overlayColor;
            }
        }
    }

    // Публичные методы для управления
    public void SetInvulnerability(bool isInvulnerable)
    {
        invulnerable = isInvulnerable;
    }

    public void ForceRespawn()
    {
        if (isDead)
            Respawn();
    }

    // Методы для тестирования в инспекторе
    [ContextMenu("Take 10 Damage")]
    void TestDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Take Critical Damage")]
    void TestCriticalDamage()
    {
        TakeDamage(currentHealth * 0.8f);
    }

    [ContextMenu("Heal 20")]
    void TestHeal()
    {
        Heal(20f);
    }

    [ContextMenu("Toggle Invulnerability")]
    void ToggleInvulnerability()
    {
        invulnerable = !invulnerable;
        Debug.Log($"Player invulnerability: {invulnerable}");
    }

    [ContextMenu("Kill Player")]
    void TestDeath()
    {
        TakeDamage(currentHealth);
    }

    [ContextMenu("Force Respawn")]
    void TestRespawn()
    {
        ForceRespawn();
    }
}