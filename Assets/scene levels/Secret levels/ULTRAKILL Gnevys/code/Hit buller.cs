using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BulletAchievementSystem : MonoBehaviour
{
    [Header("Achievement Settings")]
    [SerializeField] private int multiHitThreshold = 3; // Минимальное количество врагов для мультихита
    [SerializeField] private float achievementDisplayTime = 2f;
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI multiHitText;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private TextMeshProUGUI criticalHitText;
    [SerializeField] private TextMeshProUGUI headShotText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI perfectAimText;

    [Header("Achievement Messages")]
    [SerializeField] private string[] multiHitMessages = { "MULTI HIT!", "TRIPLE KILL!", "DEVASTATION!", "ANNIHILATION!" };
    [SerializeField] private string[] killMessages = { "ELIMINATED!", "TAKEDOWN!", "NEUTRALIZED!", "DESTROYED!" };
    [SerializeField] private string[] criticalMessages = { "CRITICAL HIT!", "WEAK SPOT!", "DEVASTATING BLOW!" };
    [SerializeField] private string[] headShotMessages = { "HEADSHOT!", "PERFECT AIM!", "BULLSEYE!" };
    [SerializeField] private string[] comboMessages = { "COMBO x2!", "COMBO x3!", "COMBO x4!", "COMBO x5!", "UNSTOPPABLE!" };
    [SerializeField] private string[] perfectAimMessages = { "PERFECT AIM!", "SHARPSHOOTER!", "MARKSMAN!" };

    [Header("Colors")]
    [SerializeField] private Color multiHitColor = Color.red;
    [SerializeField] private Color killColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.magenta;
    [SerializeField] private Color headShotColor = Color.cyan;
    [SerializeField] private Color comboColor = Color.green;
    [SerializeField] private Color perfectAimColor = Color.white;

    [Header("Animation Settings")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Sound Effects")]
    [SerializeField] private AudioClip multiHitSound;
    [SerializeField] private AudioClip killSound;
    [SerializeField] private AudioClip criticalSound;
    [SerializeField] private AudioClip headShotSound;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private AudioClip perfectAimSound;

    [Header("Statistics")]
    [SerializeField] private bool trackStatistics = true;
    [SerializeField] private TextMeshProUGUI statsText; // Для отображения статистики

    // Статистика
    private int totalMultiHits = 0;
    private int totalKills = 0;
    private int totalCriticalHits = 0;
    private int totalHeadShots = 0;
    private int currentCombo = 0;
    private int maxCombo = 0;
    private float lastHitTime = 0f;
    private float comboTimeout = 3f; // Время между попаданиями для сохранения комбо

    private AudioSource audioSource;
    private Dictionary<TextMeshProUGUI, Coroutine> activeAnimations = new Dictionary<TextMeshProUGUI, Coroutine>();

    // События для подписки извне
    public System.Action<int> OnMultiHitAchieved;
    public System.Action<string> OnKillAchieved;
    public System.Action<float> OnCriticalHitAchieved;
    public System.Action OnHeadShotAchieved;
    public System.Action<int> OnComboAchieved;
    public System.Action OnPerfectAimAchieved;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Подписываемся на события пуль
        SubscribeToBulletEvents();

        // Инициализируем UI
        InitializeUI();
    }

    void Update()
    {
        // Проверяем таймаут комбо
        if (Time.time - lastHitTime > comboTimeout && currentCombo > 0)
        {
            ResetCombo();
        }

        // Обновляем статистику
        UpdateStatisticsDisplay();
    }

    void SubscribeToBulletEvents()
    {
        // Находим все объекты с компонентом Bulletdamage и подписываемся на их события
        Bulletdamage[] bullets = FindObjectsOfType<Bulletdamage>();
        foreach (var bullet in bullets)
        {
            bullet.OnAreaHit.AddListener(OnBulletAreaHit);
            bullet.OnHit.AddListener(OnBulletHit);
        }
    }

    void InitializeUI()
    {
        // Скрываем все текстовые элементы
        SetTextVisibility(multiHitText, false);
        SetTextVisibility(killText, false);
        SetTextVisibility(criticalHitText, false);
        SetTextVisibility(headShotText, false);
        SetTextVisibility(comboText, false);
        SetTextVisibility(perfectAimText, false);
    }

    void OnBulletAreaHit(List<GameObject> hitTargets)
    {
        int enemyCount = 0;
        int killCount = 0;
        bool hasHeadShot = false;
        bool hasCritical = false;

        foreach (var target in hitTargets)
        {
            if (IsEnemy(target))
            {
                enemyCount++;

                // Проверяем, был ли враг убит
                if (IsEnemyKilled(target))
                {
                    killCount++;
                }

                // Проверяем критическое попадание (можно расширить логику)
                if (IsCriticalHit(target))
                {
                    hasCritical = true;
                }

                // Проверяем попадание в голову
                if (IsHeadShot(target))
                {
                    hasHeadShot = true;
                }
            }
        }

        // Обновляем комбо
        UpdateCombo();

        // Проверяем достижения
        if (enemyCount >= multiHitThreshold)
        {
            TriggerMultiHit(enemyCount);
        }

        if (killCount > 0)
        {
            TriggerKill(killCount);
        }

        if (hasCritical)
        {
            TriggerCriticalHit();
        }

        if (hasHeadShot)
        {
            TriggerHeadShot();
        }

        // Проверяем перфект эйм (все цели поражены критически)
        if (enemyCount > 1 && hasCritical && hasHeadShot)
        {
            TriggerPerfectAim();
        }
    }

    void OnBulletHit(GameObject target)
    {
        if (!IsEnemy(target)) return;

        // Обновляем комбо
        UpdateCombo();

        // Проверяем убийство
        if (IsEnemyKilled(target))
        {
            TriggerKill(1);
        }

        // Проверяем критическое попадание
        if (IsCriticalHit(target))
        {
            TriggerCriticalHit();
        }

        // Проверяем попадание в голову
        if (IsHeadShot(target))
        {
            TriggerHeadShot();
        }
    }

    void TriggerMultiHit(int targetCount)
    {
        totalMultiHits++;

        // Выбираем сообщение в зависимости от количества целей
        int messageIndex = Mathf.Min(targetCount - multiHitThreshold, multiHitMessages.Length - 1);
        string message = multiHitMessages[messageIndex];

        ShowAchievement(multiHitText, message, multiHitColor, multiHitSound);

        OnMultiHitAchieved?.Invoke(targetCount);

        Debug.Log($"Multi-hit achievement: {targetCount} targets hit!");
    }

    void TriggerKill(int killCount)
    {
        totalKills += killCount;

        string message = killMessages[Random.Range(0, killMessages.Length)];
        if (killCount > 1)
        {
            message += $" x{killCount}";
        }

        ShowAchievement(killText, message, killColor, killSound);

        OnKillAchieved?.Invoke(message);

        Debug.Log($"Kill achievement: {killCount} enemies eliminated!");
    }

    void TriggerCriticalHit()
    {
        totalCriticalHits++;

        string message = criticalMessages[Random.Range(0, criticalMessages.Length)];
        ShowAchievement(criticalHitText, message, criticalColor, criticalSound);

        OnCriticalHitAchieved?.Invoke(Random.Range(1.5f, 3f)); // Случайный множитель урона

        Debug.Log("Critical hit achievement!");
    }

    void TriggerHeadShot()
    {
        totalHeadShots++;

        string message = headShotMessages[Random.Range(0, headShotMessages.Length)];
        ShowAchievement(headShotText, message, headShotColor, headShotSound);

        OnHeadShotAchieved?.Invoke();

        Debug.Log("Headshot achievement!");
    }

    void TriggerCombo()
    {
        if (currentCombo < 2) return;

        int messageIndex = Mathf.Min(currentCombo - 2, comboMessages.Length - 1);
        string message = comboMessages[messageIndex];

        ShowAchievement(comboText, message, comboColor, comboSound);

        OnComboAchieved?.Invoke(currentCombo);

        Debug.Log($"Combo achievement: {currentCombo}x combo!");
    }

    void TriggerPerfectAim()
    {
        string message = perfectAimMessages[Random.Range(0, perfectAimMessages.Length)];
        ShowAchievement(perfectAimText, message, perfectAimColor, perfectAimSound);

        OnPerfectAimAchieved?.Invoke();

        Debug.Log("Perfect aim achievement!");
    }

    void UpdateCombo()
    {
        lastHitTime = Time.time;
        currentCombo++;

        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }

        if (currentCombo >= 2)
        {
            TriggerCombo();
        }
    }

    void ResetCombo()
    {
        currentCombo = 0;
    }

    void ShowAchievement(TextMeshProUGUI textComponent, string message, Color color, AudioClip sound)
    {
        if (textComponent == null) return;

        // Останавливаем предыдущую анимацию если она есть
        if (activeAnimations.ContainsKey(textComponent))
        {
            StopCoroutine(activeAnimations[textComponent]);
        }

        // Запускаем новую анимацию
        activeAnimations[textComponent] = StartCoroutine(AnimateAchievement(textComponent, message, color, sound));
    }

    IEnumerator AnimateAchievement(TextMeshProUGUI textComponent, string message, Color color, AudioClip sound)
    {
        // Настраиваем текст
        textComponent.text = message;
        textComponent.color = color;

        // Воспроизводим звук
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }

        // Анимация появления
        yield return StartCoroutine(FadeIn(textComponent));

        // Ждем время отображения
        yield return new WaitForSeconds(achievementDisplayTime);

        // Анимация исчезновения
        yield return StartCoroutine(FadeOut(textComponent));

        // Убираем из активных анимаций
        if (activeAnimations.ContainsKey(textComponent))
        {
            activeAnimations.Remove(textComponent);
        }
    }

    IEnumerator FadeIn(TextMeshProUGUI textComponent)
    {
        SetTextVisibility(textComponent, true);

        float elapsedTime = 0f;
        Color originalColor = textComponent.color;
        Vector3 originalScale = textComponent.transform.localScale;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInTime;

            // Анимация прозрачности
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(0f, 1f, progress);
            textComponent.color = newColor;

            // Анимация масштаба
            if (useScaleAnimation)
            {
                float scaleValue = scaleCurve.Evaluate(progress) * scaleMultiplier;
                textComponent.transform.localScale = originalScale * scaleValue;
            }

            yield return null;
        }

        // Устанавливаем финальные значения
        textComponent.color = originalColor;
        if (useScaleAnimation)
        {
            textComponent.transform.localScale = originalScale;
        }
    }

    IEnumerator FadeOut(TextMeshProUGUI textComponent)
    {
        float elapsedTime = 0f;
        Color originalColor = textComponent.color;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutTime;

            // Анимация прозрачности
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            textComponent.color = newColor;

            yield return null;
        }

        SetTextVisibility(textComponent, false);
    }

    void SetTextVisibility(TextMeshProUGUI textComponent, bool visible)
    {
        if (textComponent != null)
        {
            textComponent.gameObject.SetActive(visible);
        }
    }

    void UpdateStatisticsDisplay()
    {
        if (statsText != null && trackStatistics)
        {
            statsText.text = $"Multi-Hits: {totalMultiHits}\n" +
                            $"Kills: {totalKills}\n" +
                            $"Critical Hits: {totalCriticalHits}\n" +
                            $"Headshots: {totalHeadShots}\n" +
                            $"Max Combo: {maxCombo}\n" +
                            $"Current Combo: {currentCombo}";
        }
    }

    // Методы для определения типов попаданий (можно расширить)
    bool IsEnemy(GameObject target)
    {
        return target.CompareTag("Enemy") || target.CompareTag("Boss");
    }

    bool IsEnemyKilled(GameObject target)
    {
        BossHealth bossHealth = target.GetComponent<BossHealth>();
        if (bossHealth != null)
        {
            return !bossHealth.IsAlive;
        }

        IHealth health = target.GetComponent<IHealth>();
        if (health != null)
        {
            return !health.IsAlive;
        }

        return false;
    }

    bool IsCriticalHit(GameObject target)
    {
        // Простая логика - можно расширить
        // Например, проверять попадание в определенную часть тела
        return Random.Range(0f, 1f) < 0.3f; // 30% шанс критического попадания
    }

    bool IsHeadShot(GameObject target)
    {
        // Простая логика - можно расширить
        // Например, проверять попадание в коллайдер головы
        return Random.Range(0f, 1f) < 0.15f; // 15% шанс попадания в голову
    }

    // Методы для настройки в рантайме
    public void SetMultiHitThreshold(int threshold)
    {
        multiHitThreshold = Mathf.Max(2, threshold);
    }

    public void SetAchievementDisplayTime(float time)
    {
        achievementDisplayTime = Mathf.Max(0.5f, time);
    }

    public void SetComboTimeout(float timeout)
    {
        comboTimeout = Mathf.Max(1f, timeout);
    }

    // Методы для получения статистики
    public int GetTotalMultiHits() => totalMultiHits;
    public int GetTotalKills() => totalKills;
    public int GetTotalCriticalHits() => totalCriticalHits;
    public int GetTotalHeadShots() => totalHeadShots;
    public int GetMaxCombo() => maxCombo;
    public int GetCurrentCombo() => currentCombo;

    // Методы для сброса статистики
    public void ResetStatistics()
    {
        totalMultiHits = 0;
        totalKills = 0;
        totalCriticalHits = 0;
        totalHeadShots = 0;
        currentCombo = 0;
        maxCombo = 0;
    }

    void OnDestroy()
    {
        // Останавливаем все анимации
        foreach (var animation in activeAnimations.Values)
        {
            if (animation != null)
            {
                StopCoroutine(animation);
            }
        }
    }
}