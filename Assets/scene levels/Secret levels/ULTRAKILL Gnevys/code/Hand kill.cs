using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Handkill : MonoBehaviour
{
    [Header("Настройки отбивания")]
    public float pushForce = 10f;
    public float reflectionRadius = 3f;
    public LayerMask objectLayerMask = -1;
    public bool showRadius = true;
    public Camera playerCamera;

    [Header("Звуковые эффекты")]
    public AudioClip hitSound;
    public float soundVolume = 1f;

    [Header("Анимация")]
    public float scaleMultiplier = 1.2f;
    public float animationDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

    [Header("Эффекты")]
    public GameObject hitParticleEffect;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private Renderer objectRenderer;
    private Color originalColor;
    private bool isAnimating = false;

    void Start()
    {
        // Получаем компонент Rigidbody объекта
        rb = GetComponent<Rigidbody>();

        // Настраиваем AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Сохраняем оригинальный размер
        originalScale = transform.localScale;

        // Получаем рендерер для эффекта вспышки
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }

        // Если камера не назначена, ищем главную камеру
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
            }
        }

        // Проверяем наличие необходимых компонентов
        if (rb == null)
        {
            Debug.LogError("У объекта " + gameObject.name + " отсутствует компонент Rigidbody!");
        }
        if (playerCamera == null)
        {
            Debug.LogError("Камера не найдена! Назначьте камеру в инспекторе.");
        }
    }

    void Update()
    {
        // Проверяем нажатие клавиши F
        if (Input.GetKeyDown(KeyCode.F))
        {
            PushObjectsInRadius();
        }
    }

    // Отбивание объектов в радиусе
    void PushObjectsInRadius()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 playerPosition = playerCamera.transform.position;

        // Находим все коллайдеры в радиусе
        Collider[] objectsInRadius = Physics.OverlapSphere(playerPosition, reflectionRadius, objectLayerMask);

        foreach (Collider col in objectsInRadius)
        {
            // Пропускаем объект игрока
            if (col.transform == playerCamera.transform || col.transform.parent == playerCamera.transform)
                continue;

            Rigidbody objectRb = col.GetComponent<Rigidbody>();
            if (objectRb != null)
            {
                // Вычисляем направление от игрока к объекту
                Vector3 direction = (col.transform.position - playerPosition).normalized;

                // Вычисляем силу в зависимости от расстояния (ближе = сильнее)
                float distance = Vector3.Distance(playerPosition, col.transform.position);
                float forceMagnitude = pushForce * (1f - (distance / reflectionRadius));

                // Применяем силу
                objectRb.AddForce(direction * forceMagnitude, ForceMode.Impulse);

                // Запускаем эффекты для объекта
                Handkill handkillComponent = col.GetComponent<Handkill>();
                if (handkillComponent != null)
                {
                    handkillComponent.PlayHitEffects();
                }

                Debug.Log($"Объект {col.name} отбит с силой {forceMagnitude:F1} на расстоянии {distance:F1}");
            }
        }

        // Также запускаем эффекты на текущем объекте (если это один из объектов в сцене)
        PlayHitEffects();
    }

    void PushObject()
    {
        // Проверяем наличие необходимых компонентов
        if (rb == null || playerCamera == null)
        {
            return;
        }

        // Получаем направление взгляда камеры
        Vector3 pushDirection = playerCamera.transform.forward;

        // Применяем силу к объекту в направлении камеры
        rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

        // Запускаем эффекты
        PlayHitEffects();

        Debug.Log("Объект " + gameObject.name + " отбит в направлении камеры с силой " + pushForce);
    }

    // Альтернативный метод для отбивания от определенной точки
    public void PushFromPosition(Vector3 fromPosition)
    {
        if (rb == null) return;

        // Вычисляем направление от указанной позиции к объекту
        Vector3 pushDirection = (transform.position - fromPosition).normalized;

        // Применяем силу
        rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

        // Запускаем эффекты
        PlayHitEffects();
    }

    // Воспроизведение всех эффектов удара
    public void PlayHitEffects()
    {
        PlayHitSound();
        StartCoroutine(PlayHitAnimation());
        PlayParticleEffect();
        StartCoroutine(FlashEffect());
    }

    // Воспроизведение звука удара
    void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.clip = hitSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
    }

    // Анимация масштабирования при ударе
    IEnumerator PlayHitAnimation()
    {
        if (isAnimating) yield break;

        isAnimating = true;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float progress = elapsedTime / animationDuration;
            float scaleValue = scaleCurve.Evaluate(progress);

            // Создаем эффект "удара" - увеличение и возврат к нормальному размеру
            float currentScale = Mathf.Lerp(1f, scaleMultiplier, scaleValue);
            if (progress > 0.5f)
            {
                currentScale = Mathf.Lerp(scaleMultiplier, 1f, (progress - 0.5f) * 2f);
            }

            transform.localScale = originalScale * currentScale;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Возвращаем оригинальный размер
        transform.localScale = originalScale;
        isAnimating = false;
    }

    // Эффект частиц при ударе
    void PlayParticleEffect()
    {
        if (hitParticleEffect != null)
        {
            GameObject effect = Instantiate(hitParticleEffect, transform.position, Quaternion.identity);

            // Автоматически удаляем эффект через некоторое время
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 2f);
            }
        }
    }

    // Эффект вспышки цвета
    IEnumerator FlashEffect()
    {
        if (objectRenderer == null) yield break;

        // Плавно меняем цвет на цвет вспышки
        float elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2f)
        {
            float progress = elapsedTime / (flashDuration / 2f);
            objectRenderer.material.color = Color.Lerp(originalColor, flashColor, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Возвращаем оригинальный цвет
        elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2f)
        {
            float progress = elapsedTime / (flashDuration / 2f);
            objectRenderer.material.color = Color.Lerp(flashColor, originalColor, progress);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        objectRenderer.material.color = originalColor;
    }

    // Публичный метод для настройки звука
    public void SetHitSound(AudioClip newSound, float volume = 1f)
    {
        hitSound = newSound;
        soundVolume = volume;
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // Публичный метод для настройки анимации
    public void SetAnimationSettings(float multiplier, float duration)
    {
        scaleMultiplier = multiplier;
        animationDuration = duration;
    }

    // Визуализация радиуса в редакторе
    void OnDrawGizmosSelected()
    {
        if (showRadius && playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCamera.transform.position, reflectionRadius);
        }
    }

    // Альтернативный метод для отбивания в радиусе от определенной позиции
    public void PushObjectsInRadiusFromPosition(Vector3 centerPosition, float radius = 0f)
    {
        if (radius <= 0f) radius = reflectionRadius;

        Collider[] objectsInRadius = Physics.OverlapSphere(centerPosition, radius, objectLayerMask);

        foreach (Collider col in objectsInRadius)
        {
            Rigidbody objectRb = col.GetComponent<Rigidbody>();
            if (objectRb != null)
            {
                Vector3 direction = (col.transform.position - centerPosition).normalized;
                float distance = Vector3.Distance(centerPosition, col.transform.position);
                float forceMagnitude = pushForce * (1f - (distance / radius));

                objectRb.AddForce(direction * forceMagnitude, ForceMode.Impulse);

                Handkill handkillComponent = col.GetComponent<Handkill>();
                if (handkillComponent != null)
                {
                    handkillComponent.PlayHitEffects();
                }
            }
        }
    }
}