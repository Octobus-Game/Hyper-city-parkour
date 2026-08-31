using System.Collections;
using UnityEngine;

/// <summary>
/// Принудительно показывает и разблокирует курсор, перекрывая все другие скрипты.
/// Добавьте компонент на любой GameObject в сцене.
/// </summary>
[DefaultExecutionOrder(int.MaxValue)] // Этот скрипт выполняется САМЫМ ПОСЛЕДНИМ среди всех
public class ForceCursorEnabler : MonoBehaviour
{
    // Кешируем WaitForEndOfFrame — создаётся один раз
    private static readonly WaitForEndOfFrame WaitEndOfFrame = new WaitForEndOfFrame();

    private static ForceCursorEnabler _instance;

    [Header("Настройки")]
    [Tooltip("Сохранять объект при загрузке новых сцен")]
    [SerializeField] private bool persistAcrossScenes = true;

    // ─────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Гарантируем существование только одного экземпляра
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        ForceCursor(); // Применяем сразу при старте
    }

    private void OnEnable()
    {
        StartCoroutine(EndOfFrameLoop());
        ForceCursor();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Уровень 1: LateUpdate с int.MaxValue приоритетом.
    /// Выполняется после LateUpdate ВСЕХ других скриптов.
    /// Перекрывает любой Update() и LateUpdate() сцены.
    /// </summary>
    private void LateUpdate()
    {
        ForceCursor();
    }

    /// <summary>
    /// Уровень 2: корутина WaitForEndOfFrame.
    /// Выполняется ПОСЛЕ рендеринга — это самая поздняя точка в кадре.
    /// Ловит даже скрипты, которые меняют курсор в своём LateUpdate.
    /// </summary>
    private IEnumerator EndOfFrameLoop()
    {
        while (true)
        {
            yield return WaitEndOfFrame;
            ForceCursor();
        }
    }

    /// <summary>
    /// Уровень 3: восстанавливаем курсор при возвращении фокуса в окно.
    /// Нужно, если другие скрипты сбрасывают курсор в OnApplicationFocus.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ForceCursor();
    }

    // ─────────────────────────────────────────────────────────────────
    // Core
    // ─────────────────────────────────────────────────────────────────

    private static void ForceCursor()
    {
        // Проверяем перед записью, чтобы не генерировать лишних событий
        if (!Cursor.visible)         Cursor.visible   = true;
        if (Cursor.lockState != CursorLockMode.None)
                                     Cursor.lockState = CursorLockMode.None;
    }

    // ─────────────────────────────────────────────────────────────────
    // Публичный API — для управления из других скриптов
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Включить / отключить принудительный режим курсора на лету.
    /// </summary>
    public static void SetActive(bool active)
    {
        if (_instance != null)
            _instance.enabled = active;
    }

    /// <summary>
    /// Проверить, активен ли принудительный режим.
    /// </summary>
    public static bool IsActive => _instance != null && _instance.enabled;
}