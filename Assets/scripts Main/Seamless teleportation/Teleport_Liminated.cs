using System.Collections;
using UnityEngine;

/// <summary>
/// Бесшовная телепортация с эффектом растворения через материал (shader dissolve).
/// Прикрепи этот скрипт к объекту игрока.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class SeamlessTeleport_01 : MonoBehaviour
{
    [Header("Настройки телепортации")]
    [Tooltip("Точка назначения телепортации")]
    public Transform destination;

    [Tooltip("Длительность эффекта растворения (секунды)")]
    [Range(0.1f, 3f)]
    public float fadeDuration = 0.5f;

    [Tooltip("Имя свойства в шейдере, управляющего растворением (float 0..1)")]
    public string dissolveProperty = "_DissolveAmount";

    [Header("Необязательно: камера (если не назначена — берётся Camera.main)")]
    public Camera playerCamera;

    // ─── Приватные поля ────────────────────────────────────────────────────────
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;   // не мутирует shared material
    private bool _isTeleporting;

    // ─── Инициализация ─────────────────────────────────────────────────────────
    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    // ─── Публичный вход ────────────────────────────────────────────────────────
    /// <summary>Вызови этот метод (или кнопку) для начала телепортации.</summary>
    public void TeleportTo(Transform target)
    {
        if (_isTeleporting) return;
        StartCoroutine(TeleportRoutine(target));
    }

    /// <summary>Телепортация к полю destination (удобно для кнопки UI).</summary>
    public void Teleport() => TeleportTo(destination);

    // ─── Основная корутина ─────────────────────────────────────────────────────
    private IEnumerator TeleportRoutine(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[SeamlessTeleport] destination не задан!");
            yield break;
        }

        _isTeleporting = true;

        // 1. Плавное растворение: 0 → 1
        yield return StartCoroutine(LerpDissolve(0f, 1f, fadeDuration));

        // 2. Мгновенное перемещение (игрок уже невидим)
        MoveToDestination(target);

        // 3. Один кадр паузы (чтобы камера успела обновиться)
        yield return null;

        // 4. Плавное появление: 1 → 0
        yield return StartCoroutine(LerpDissolve(1f, 0f, fadeDuration));

        _isTeleporting = false;
    }

    // ─── Перемещение ───────────────────────────────────────────────────────────
    private void MoveToDestination(Transform target)
    {
        // Отключаем CharacterController на время перемещения, если он есть
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(target.position, target.rotation);

        if (cc != null) cc.enabled = true;
    }

    // ─── Анимация свойства шейдера ─────────────────────────────────────────────
    private IEnumerator LerpDissolve(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t); // плавная S-кривая
            float value   = Mathf.Lerp(from, to, smoothT);

            SetDissolve(value);
            yield return null;
        }

        SetDissolve(to); // финальное значение точно
    }

    private void SetDissolve(float value)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(dissolveProperty, value);
        _renderer.SetPropertyBlock(_mpb);
    }

    // ─── Быстрый тест с клавиатуры ─────────────────────────────────────────────
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            Teleport();
    }
}