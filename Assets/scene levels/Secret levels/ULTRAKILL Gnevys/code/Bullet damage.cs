using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

public class Bulletdamage : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Damage Radius Settings")]
    [SerializeField] private bool useAreaDamage = false;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private AnimationCurve damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
    [SerializeField] private bool damageMultipleTargets = true;
    [SerializeField] private int maxTargetsInRadius = 10;
    [SerializeField] private bool showDebugRadius = true;

    [Header("Target Settings")]
    [SerializeField] private LayerMask targetLayers = -1;
    [SerializeField] private string[] targetTags = { "Enemy", "Boss" };

    [Header("Ground Settings")]
    [SerializeField] private bool destroyOnGround = true;
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private GameObject groundHitEffect;
    [SerializeField] private AudioClip groundHitSound;

    [Header("Push Trigger Integration")]
    [SerializeField] private bool canBePushed = true;
    [SerializeField] private float pushForceMultiplier = 1f;
    [SerializeField] private bool destroyOnPush = false;
    [SerializeField] private GameObject pushEffect;
    [SerializeField] private AudioClip pushSound;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private GameObject areaHitEffect; // Эффект для радиуса урона

    [Header("Events")]
    public UnityEvent<GameObject> OnHit;
    public UnityEvent<GameObject> OnGroundHit;
    public UnityEvent<List<GameObject>> OnAreaHit; // Событие для попадания по области
    public UnityEvent<Vector3> OnPushed; // Событие для отталкивания
    public UnityEvent OnDestroy;

    private Rigidbody rb;
    private AudioSource audioSource;
    private bool hasHit = false;
    private bool wasPushed = false;
    private Vector3 originalDirection;

    // Свойства для внешнего доступа
    public float Damage => damage;
    public float Speed => speed;
    public bool HasHit => hasHit;
    public float DamageRadius => damageRadius;
    public bool UseAreaDamage => useAreaDamage;
    public bool CanBePushed => canBePushed;
    public bool WasPushed => wasPushed;

    void Start()
    {
        InitializeBullet();
    }

    void InitializeBullet()
    {
        // Получаем компоненты
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Сохраняем изначальное направление
        originalDirection = transform.forward;

        // Настраиваем движение
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }

        // Добавляем тег для совместимости с CameraPushTrigger
        if (canBePushed && !gameObject.CompareTag("Bullet"))
        {
            gameObject.tag = "Bullet";
        }

        // Уничтожаем пулю через заданное время
        Destroy(gameObject, lifetime);
    }

    // Метод для отталкивания пули (вызывается CameraPushTrigger)
    public void OnCameraPush(Vector3 pushDirection, float pushForce)
    {
        if (!canBePushed || hasHit) return;

        wasPushed = true;

        if (rb != null)
        {
            // Сохраняем текущую скорость
            float currentSpeed = rb.velocity.magnitude;

            // Применяем новое направление с учетом множителя силы
            Vector3 newVelocity = pushDirection.normalized * currentSpeed * pushForceMultiplier;
            rb.velocity = newVelocity;

            // Поворачиваем пулю в направлении движения
            if (newVelocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(newVelocity);
            }
        }

        // Создаем эффект отталкивания
        CreatePushEffect();

        // Вызываем событие
        OnPushed?.Invoke(pushDirection);

        // Уничтожаем пулю, если включено
        if (destroyOnPush)
        {
            DestroyBullet();
        }

        Debug.Log($"Пуля {gameObject.name} была оттолкнута в направлении {pushDirection}");
    }

    void CreatePushEffect()
    {
        // Создаем визуальный эффект отталкивания
        if (pushEffect != null)
        {
            GameObject effect = Instantiate(pushEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        // Воспроизводим звук отталкивания
        if (pushSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pushSound);
        }
    }

    // Метод для проверки совместимости с CameraPushTrigger
    public bool IsCompatibleWithPushTrigger()
    {
        return canBePushed && rb != null;
    }

    // Метод для получения информации о пуле для CameraPushTrigger
    public BulletInfo GetBulletInfo()
    {
        return new BulletInfo
        {
            damage = this.damage,
            speed = this.speed,
            canBePushed = this.canBePushed,
            wasPushed = this.wasPushed,
            hasHit = this.hasHit
        };
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    void HandleCollision(GameObject hitObject)
    {
        if (hasHit) return; // Предотвращаем множественные попадания

        // Проверяем столкновение с землей
        if (destroyOnGround && hitObject.CompareTag(groundTag))
        {
            HandleGroundHit(hitObject);
            return;
        }

        // Проверяем слой
        if (!IsInLayerMask(hitObject.layer, targetLayers)) return;

        // Проверяем тег
        if (!HasValidTag(hitObject)) return;

        hasHit = true;

        // Если используется радиус урона, наносим урон по области
        if (useAreaDamage)
        {
            DealAreaDamage(transform.position);
        }
        else
        {
            // Наносим урон только прямому попаданию
            DealDamage(hitObject, 1f); // Полный урон
        }

        // Создаем эффекты
        CreateHitEffects();

        // Вызываем событие
        OnHit?.Invoke(hitObject);

        // Уничтожаем пулю если нужно
        if (destroyOnHit)
        {
            DestroyBullet();
        }
    }

    void HandleGroundHit(GameObject ground)
    {
        hasHit = true;

        // Если используется радиус урона, наносим урон по области даже при попадании в землю
        if (useAreaDamage)
        {
            DealAreaDamage(transform.position);
        }

        // Создаем эффекты попадания в землю
        CreateGroundHitEffects();

        // Вызываем событие попадания в землю
        OnGroundHit?.Invoke(ground);

        // Уничтожаем пулю
        DestroyBullet();

        Debug.Log("Пуля попала в землю и была уничтожена");
    }

    void DealAreaDamage(Vector3 explosionCenter)
    {
        // Находим все коллайдеры в радиусе
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, damageRadius, targetLayers);
        List<GameObject> hitTargets = new List<GameObject>();
        int targetsHit = 0;

        foreach (Collider hitCollider in hitColliders)
        {
            // Проверяем лимит целей
            if (!damageMultipleTargets && targetsHit >= 1) break;
            if (targetsHit >= maxTargetsInRadius) break;

            GameObject target = hitCollider.gameObject;

            // Проверяем тег
            if (!HasValidTag(target)) continue;

            // Избегаем попадания по самой пуле
            if (target == gameObject) continue;

            // Вычисляем расстояние и коэффициент урона
            float distance = Vector3.Distance(explosionCenter, target.transform.position);
            float damageMultiplier = CalculateDamageMultiplier(distance);

            // Наносим урон с учетом расстояния
            DealDamage(target, damageMultiplier);
            hitTargets.Add(target);
            targetsHit++;

            Debug.Log($"Урон по {target.name}: {damage * damageMultiplier} (расстояние: {distance:F2}м)");
        }

        // Создаем эффект области поражения
        CreateAreaHitEffect(explosionCenter);

        // Вызываем событие попадания по области
        if (hitTargets.Count > 0)
        {
            OnAreaHit?.Invoke(hitTargets);
        }

        Debug.Log($"Радиус урона: поражено {hitTargets.Count} целей");
    }

    float CalculateDamageMultiplier(float distance)
    {
        if (distance >= damageRadius) return 0f;

        // Нормализуем расстояние (0 = центр взрыва, 1 = край радиуса)
        float normalizedDistance = distance / damageRadius;

        // Используем кривую для определения коэффициента урона
        return damageFalloff.Evaluate(normalizedDistance);
    }

    void DealDamage(GameObject target, float multiplier = 1f)
    {
        float finalDamage = damage * multiplier;

        // Пробуем найти компонент здоровья разными способами
        BossHealth bossHealth = target.GetComponent<BossHealth>();
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(finalDamage);
            Debug.Log($"Нанесен урон боссу: {finalDamage}");
            return;
        }

        // Можно добавить поддержку других типов здоровья
        IHealth healthComponent = target.GetComponent<IHealth>();
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(finalDamage);
            Debug.Log($"Нанесен урон: {finalDamage}");
            return;
        }

        // Если не найден компонент здоровья, пробуем SendMessage
        target.SendMessage("TakeDamage", finalDamage, SendMessageOptions.DontRequireReceiver);
    }

    void CreateHitEffects()
    {
        // Создаем визуальный эффект
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, transform.rotation);
            Destroy(effect, 2f); // Удаляем эффект через 2 секунды
        }

        // Воспроизводим звук
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    void CreateAreaHitEffect(Vector3 position)
    {
        // Создаем эффект области поражения
        if (areaHitEffect != null)
        {
            GameObject effect = Instantiate(areaHitEffect, position, Quaternion.identity);

            // Масштабируем эффект под радиус урона
            effect.transform.localScale = Vector3.one * (damageRadius * 2f);

            Destroy(effect, 3f); // Удаляем эффект через 3 секунды
        }
    }

    void CreateGroundHitEffects()
    {
        // Создаем визуальный эффект попадания в землю
        if (groundHitEffect != null)
        {
            GameObject effect = Instantiate(groundHitEffect, transform.position, transform.rotation);
            Destroy(effect, 2f); // Удаляем эффект через 2 секунды
        }
        else if (hitEffect != null) // Используем обычный эффект, если нет специального
        {
            GameObject effect = Instantiate(hitEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        // Воспроизводим звук попадания в землю
        if (groundHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(groundHitSound);
        }
        else if (hitSound != null && audioSource != null) // Используем обычный звук, если нет специального
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    bool HasValidTag(GameObject obj)
    {
        if (targetTags.Length == 0) return true;

        foreach (string tag in targetTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void DestroyBullet()
    {
        OnDestroy?.Invoke();

        // Определяем какой звук воспроизводить
        AudioClip soundToPlay = null;
        if (groundHitSound != null && hasHit)
        {
            soundToPlay = groundHitSound;
        }
        else if (hitSound != null)
        {
            soundToPlay = hitSound;
        }

        // Если есть звук попадания, ждем его завершения
        if (soundToPlay != null && audioSource != null)
        {
            // Отключаем визуал, но оставляем аудио
            Renderer renderer = GetComponent<Renderer>();
            Collider collider = GetComponent<Collider>();

            if (renderer != null) renderer.enabled = false;
            if (collider != null) collider.enabled = false;

            Destroy(gameObject, soundToPlay.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Методы для изменения параметров в рантайме
    public void SetDamage(float newDamage)
    {
        damage = Mathf.Max(0, newDamage);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0, newSpeed);
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    public void SetLifetime(float newLifetime)
    {
        lifetime = Mathf.Max(0.1f, newLifetime);
    }

    public void SetGroundTag(string newGroundTag)
    {
        groundTag = newGroundTag;
    }

    public void SetDestroyOnGround(bool destroy)
    {
        destroyOnGround = destroy;
    }

    // Новые методы для радиуса урона
    public void SetDamageRadius(float newRadius)
    {
        damageRadius = Mathf.Max(0f, newRadius);
    }

    public void SetUseAreaDamage(bool useArea)
    {
        useAreaDamage = useArea;
    }

    public void SetMaxTargetsInRadius(int maxTargets)
    {
        maxTargetsInRadius = Mathf.Max(1, maxTargets);
    }

    public void SetDamageMultipleTargets(bool multiple)
    {
        damageMultipleTargets = multiple;
    }

    // Методы для настройки отталкивания
    public void SetCanBePushed(bool canPush)
    {
        canBePushed = canPush;
    }

    public void SetPushForceMultiplier(float multiplier)
    {
        pushForceMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetDestroyOnPush(bool destroy)
    {
        destroyOnPush = destroy;
    }

    // Методы для отладки
    [ContextMenu("Test Hit")]
    void TestHit()
    {
        Debug.Log($"Bullet damage: {damage}, speed: {speed}");
    }

    [ContextMenu("Test Ground Hit")]
    void TestGroundHit()
    {
        Debug.Log($"Ground tag: {groundTag}, Destroy on ground: {destroyOnGround}");
    }

    [ContextMenu("Test Area Damage")]
    void TestAreaDamage()
    {
        Debug.Log($"Area damage: {useAreaDamage}, radius: {damageRadius}, max targets: {maxTargetsInRadius}");
        if (useAreaDamage)
        {
            DealAreaDamage(transform.position);
        }
    }

    [ContextMenu("Test Push Compatibility")]
    void TestPushCompatibility()
    {
        Debug.Log($"Can be pushed: {canBePushed}, Has Rigidbody: {rb != null}, Compatible: {IsCompatibleWithPushTrigger()}");
    }

    // Визуализация радиуса в редакторе
    void OnDrawGizmosSelected()
    {
        if (useAreaDamage && showDebugRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, damageRadius);
        }

        // Показываем направление пули
        Gizmos.color = wasPushed ? Color.yellow : Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}

// Интерфейс для универсального нанесения урона
public interface IHealth
{
    void TakeDamage(float damage);
    float CurrentHealth { get; }
    bool IsAlive { get; }
}

// Структура для передачи информации о пуле
[System.Serializable]
public struct BulletInfo
{
    public float damage;
    public float speed;
    public bool canBePushed;
    public bool wasPushed;
    public bool hasHit;
}