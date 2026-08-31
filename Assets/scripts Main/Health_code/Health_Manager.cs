using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // Добавлен namespace для TextMeshPro

public class PlayerHealth_01 : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image healthBackgroundFillImage; // Фоновая полоса здоровья
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float backgroundFillDelay = 0.5f; // Задержка для фона
    [SerializeField] private float backgroundFillSpeed = 2f; // Скорость уменьшения фона
    [SerializeField] private float lowHealthThreshold = 20f; // Порог низкого здоровья (20%)
    private float currentHealth;
    private float targetBackgroundFill;
    private Coroutine backgroundFillCoroutine;

    private CanvasGroup panelDamageDiedCG;

    [Header("Health Text Settings")]
    [SerializeField] private TMP_Text healthText; // Текст для отображения здоровья
    [SerializeField] private bool showMaxHealth = true; // Показывать ли максимальное здоровье
    [SerializeField] private bool showPercentage = false; // Показывать ли проценты
    [SerializeField] private bool roundHealthValue = true; // Округлять ли значения

    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Damage Panel Settings")]
    [SerializeField] private GameObject damagePanel; // Панель урона
    [SerializeField] private float damagePanelDuration = 3f; // Длительность показа панели
    private Coroutine damagePanelCoroutine;

    [Header("Low Health Panel Settings")]
    [SerializeField] private GameObject panelDamageDied; // Панель при низком здоровье (20%)
    private bool isLowHealthPanelActive = false;

    [Header("Camera & Audio Settings")]
    [SerializeField] private MonoBehaviour[] cameraScripts; // Скрипты управления камерой
    private bool isDead = false;
    private bool isGameOver = false; // Флаг активного Game Over — время всегда заморожено

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (healthBackgroundFillImage != null)
            healthBackgroundFillImage.fillAmount = 1f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (damagePanel != null)
            damagePanel.SetActive(false);

        // ✅ Вместо SetActive(false) — инициализируем CanvasGroup
        if (panelDamageDied != null)
        {
            panelDamageDiedCG = panelDamageDied.GetComponent<CanvasGroup>();
            if (panelDamageDiedCG == null)
                panelDamageDiedCG = panelDamageDied.AddComponent<CanvasGroup>();

            panelDamageDied.SetActive(true);   // держим активной
            panelDamageDiedCG.alpha = 0f;      // просто невидима
            panelDamageDiedCG.interactable = false;
            panelDamageDiedCG.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.Return))
        {
            RestartScene();
        }
    }

    private void LateUpdate()
    {
        // Принудительно удерживаем время замороженным, пока активен Game Over.
        // Срабатывает ПОСЛЕ всех Update-методов сцены, поэтому любой внешний код,
        // который попытается выставить Time.timeScale = 1f, будет перезаписан.
        if (isGameOver)
        {
            Time.timeScale = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("damage"))
        {
            DamageObject damageObj = collision.gameObject.GetComponent<DamageObject>();
            if (damageObj != null)
            {
                TakeDamage(damageObj.damageAmount);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Существующий код для урона
        if (collision.gameObject.CompareTag("damage"))
        {
            DamageObject damageObj = collision.gameObject.GetComponent<DamageObject>();
            if (damageObj != null && damageObj.continuousDamage)
            {
                TakeDamage(damageObj.damageAmount * Time.deltaTime);
            }
        }

        // Новый код для лечения
        if (collision.gameObject.CompareTag("heal"))
        {
            HealObject healObj = collision.gameObject.GetComponent<HealObject>();
            if (healObj != null && healObj.continuousHeal)
            {
                Heal(healObj.healAmount * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("damage"))
        {
            DamageObject damageObj = other.GetComponent<DamageObject>();
            if (damageObj != null)
            {
                TakeDamage(damageObj.damageAmount);
            }
        }
    }

    private void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateHealthUI();

        // Показать панель урона
        ShowDamagePanel();

        // Обновить фоновую полосу с задержкой
        UpdateBackgroundFill();

        // Проверить, достигло ли здоровье 20%
        CheckLowHealth();

        if (currentHealth <= 0.5)
        {
            Die();
        }
    }

    private void CheckLowHealth()
    {
        float healthPercentage = (currentHealth / maxHealth) * 100f;

        // Если здоровье упало до 20% или ниже и панель еще не активна
        if (healthPercentage <= lowHealthThreshold && !isLowHealthPanelActive)
        {
            ActivateLowHealthPanel();
        }
        // Если здоровье восстановилось выше 20% и панель активна
        else if (healthPercentage > lowHealthThreshold && isLowHealthPanelActive)
        {
            DeactivateLowHealthPanel();
        }
    }

    private void ActivateLowHealthPanel()
    {
        if (panelDamageDiedCG != null)
        {
            panelDamageDiedCG.alpha = 1f;
            panelDamageDiedCG.interactable = true;
            panelDamageDiedCG.blocksRaycasts = true;
            isLowHealthPanelActive = true;
        }
    }

    private void DeactivateLowHealthPanel()
    {
        if (panelDamageDiedCG != null)
        {
            panelDamageDiedCG.alpha = 0f;
            panelDamageDiedCG.interactable = false;
            panelDamageDiedCG.blocksRaycasts = false;
            isLowHealthPanelActive = false;
        }
    }

    private void ShowDamagePanel()
    {
        if (damagePanel != null)
        {
            // Если уже есть активная корутина, останавливаем её
            if (damagePanelCoroutine != null)
            {
                StopCoroutine(damagePanelCoroutine);
            }

            // Запускаем новую корутину
            damagePanelCoroutine = StartCoroutine(DamagePanelRoutine());
        }
    }

    private IEnumerator DamagePanelRoutine()
    {
        damagePanel.SetActive(true);
        yield return new WaitForSeconds(damagePanelDuration);
        damagePanel.SetActive(false);
        damagePanelCoroutine = null;
    }

    private void UpdateHealthUI()
    {
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = currentHealth / maxHealth;
        }

        // Обновление текста здоровья
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            float displayCurrentHealth = roundHealthValue ? Mathf.Round(currentHealth) : currentHealth;
            float displayMaxHealth = roundHealthValue ? Mathf.Round(maxHealth) : maxHealth;

            if (showPercentage)
            {
                // Показать в процентах
                float percentage = (currentHealth / maxHealth) * 100f;
                float displayPercentage = roundHealthValue ? Mathf.Round(percentage) : percentage;
                healthText.text = $"{displayPercentage}%";
            }
            else if (showMaxHealth)
            {
                // Показать текущее/максимальное (например: 75/100)
                healthText.text = $"{displayCurrentHealth}/{displayMaxHealth}";
            }
            else
            {
                // Показать только текущее здоровье
                healthText.text = $"{displayCurrentHealth}";
            }
        }
    }

    private void UpdateBackgroundFill()
    {
        if (healthBackgroundFillImage != null)
        {
            // Остановить предыдущую корутину, если она существует
            if (backgroundFillCoroutine != null)
            {
                StopCoroutine(backgroundFillCoroutine);
            }

            // Запустить новую корутину с задержкой
            backgroundFillCoroutine = StartCoroutine(BackgroundFillRoutine());
        }
    }

    private IEnumerator BackgroundFillRoutine()
    {
        // Задержка перед началом уменьшения
        yield return new WaitForSeconds(backgroundFillDelay);

        targetBackgroundFill = currentHealth / maxHealth;

        // Плавное уменьшение фоновой полосы
        while (healthBackgroundFillImage.fillAmount > targetBackgroundFill)
        {
            healthBackgroundFillImage.fillAmount = Mathf.MoveTowards(
                healthBackgroundFillImage.fillAmount,
                targetBackgroundFill,
                backgroundFillSpeed * Time.deltaTime
            );
            yield return null;
        }

        healthBackgroundFillImage.fillAmount = targetBackgroundFill;
        backgroundFillCoroutine = null;
    }

    private void Die()
    {
        
        
            isDead = true;
            isGameOver = true; // Запрещаем разморозку времени

            if (damagePanelCoroutine != null) { StopCoroutine(damagePanelCoroutine); damagePanelCoroutine = null; }
            if (backgroundFillCoroutine != null) { StopCoroutine(backgroundFillCoroutine); backgroundFillCoroutine = null; }

            if (damagePanel != null) damagePanel.SetActive(false);

            // ✅ Тоже через CanvasGroup
            if (panelDamageDiedCG != null)
            {
                panelDamageDiedCG.alpha = 0f;
                panelDamageDiedCG.interactable = false;
                panelDamageDiedCG.blocksRaycasts = false;
                isLowHealthPanelActive = false;
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            Time.timeScale = 0f;
            DisableCameraScripts();
            PauseAllAudio();
        
    }

    private void DisableCameraScripts()
    {
        // Отключить указанные скрипты камеры
        if (cameraScripts != null)
        {
            foreach (var script in cameraScripts)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void PauseAllAudio()
    {
        // Остановить все AudioSource в сцене
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in allAudioSources)
        {
            audioSource.Pause();
        }
    }

    private void RestartScene()
    {
        // Сначала снимаем флаг — только после этого разрешаем трогать timeScale
        isGameOver = false;

        // Восстановить время
        Time.timeScale = 1f;

        // Включить скрипты камеры обратно
        EnableCameraScripts();

        // Возобновить все звуки
        ResumeAllAudio();

        // Перезагрузить сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void EnableCameraScripts()
    {
        if (cameraScripts != null)
        {
            foreach (var script in cameraScripts)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }
    }

    private void ResumeAllAudio()
    {
        // Возобновить все AudioSource в сцене
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in allAudioSources)
        {
            audioSource.UnPause();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Не превышать максимум
        UpdateHealthUI();

        // Обновить фоновую полосу
        UpdateBackgroundFill();

        // Проверить состояние здоровья (возможно выключить панель низкого здоровья)
        CheckLowHealth();

        Debug.Log($"Здоровье восстановлено на {amount}. Текущее здоровье: {currentHealth}");
    }

    /// <summary>
    /// Увеличивает максимальное здоровье игрока
    /// </summary>
    public void IncreaseMaxHealth(float amount)
    {
        if (isDead) return;

        maxHealth += amount;
        currentHealth += amount; // Также увеличить текущее здоровье
        UpdateHealthUI();

        // Обновить фоновую полосу
        if (healthBackgroundFillImage != null)
        {
            healthBackgroundFillImage.fillAmount = currentHealth / maxHealth;
        }

        Debug.Log($"Максимальное здоровье увеличено на {amount}. Новый максимум: {maxHealth}");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("heal"))
        {
            HealObject healObj = other.GetComponent<HealObject>();
            if (healObj != null && healObj.continuousHeal)
            {
                Heal(healObj.healAmount * Time.deltaTime);
            }
        }
    }
}