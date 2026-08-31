using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;   // Post Processing Stack v2


[RequireComponent(typeof(Collider))]
public class TeleportTriggerCrystal : MonoBehaviour
{
    [Header("Телепорт")]
    [Tooltip("Точка, куда переместится игрок")]
    public Transform destinationPoint;

    [Tooltip("Смещение от точки назначения (например, чуть выше пола)")]
    public Vector3 destinationOffset = Vector3.zero;

    [Header("Звук")]
    [Tooltip("Звук телепортации")]
    public AudioClip teleportSound;

    [Tooltip("Громкость звука (0–1)")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Кулдаун")]
    [Tooltip("Задержка повторного использования (сек)")]
    public float cooldown = 1.5f;

    [Header("Эффект линзы (Lens Distortion)")]
    [Tooltip("Длительность перехода 1→0 (сек)")]
    public float lensDistortionFadeOut = 2f;

    [Tooltip("Длительность перехода 0→1 (сек)")]
    public float lensDistortionFadeIn = 2f;

    [Header("Очки")]
    [Tooltip("Компонент ScoreMovement на сцене. Если не задан — ищется автоматически.")]
    public ScoreMovement scoreMovement;

    [Tooltip("Название действия, которое отобразится в списке очков")]
    public string teleportScoreLabel = "Teleport";

    [Tooltip("Количество очков за телепортацию (0 = не начислять)")]
    public int teleportScorePoints = 150;

    // ── Приватные поля ──────────────────────────────────────────────────────

    private float _lastTeleportTime = -999f; // давно в прошлом → первое срабатывание всегда разрешено
    private AudioSource _audioSource;

    private LensDistortion _lensDistortion;  // ссылка на эффект
    private Coroutine _lensCoroutine;        // текущая анимация (чтобы не запускать дважды)

    // ── Unity callbacks ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Гарантируем Is Trigger на коллайдере
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[TeleportTrigger] Collider на '{name}' не был триггером — исправлено автоматически.");
        }

        // AudioSource для пространственного звука
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend  = 1f; // 3D-звук
        _audioSource.volume        = soundVolume;
        _audioSource.clip          = teleportSound;

        // ── Автопоиск ScoreMovement, если не задан в инспекторе ──────────
        if (scoreMovement == null)
        {
            scoreMovement = FindObjectOfType<ScoreMovement>();
            if (scoreMovement == null)
                Debug.LogWarning("[TeleportTrigger] ScoreMovement не найден на сцене — очки начисляться не будут.");
        }

        // ── Получаем LensDistortion из PostProcessVolume на Main Camera ──
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[TeleportTrigger] Main Camera не найдена — эффект линзы отключён.");
            return;
        }

        var ppVolume = cam.GetComponent<PostProcessVolume>();
        if (ppVolume == null || ppVolume.profile == null)
        {
            Debug.LogWarning("[TeleportTrigger] PostProcessVolume не найден на Main Camera — эффект линзы отключён.");
            return;
        }

        if (!ppVolume.profile.TryGetSettings(out _lensDistortion))
            Debug.LogWarning("[TeleportTrigger] LensDistortion не найден в PostProcessProfile — эффект линзы отключён.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверка тега
        if (!other.CompareTag("Player")) return;

        // Проверка кулдауна
        if (Time.time - _lastTeleportTime < cooldown) return;

        // Проверка точки назначения
        if (destinationPoint == null)
        {
            Debug.LogError($"[TeleportTrigger] На '{name}' не задана destinationPoint!");
            return;
        }

        Teleport(other.gameObject);
    }

    // ── Основная логика ─────────────────────────────────────────────────────

    private void Teleport(GameObject player)
    {
        // --- CharacterController требует отключения перед Warp ---
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Перемещение
        Vector3 target = destinationPoint.position + destinationOffset;
        player.transform.position = target;

        if (cc != null) cc.enabled = true;

        // Звук (воспроизводим в точке назначения)
        PlayTeleportSound(target);

        // ── Начисление очков ─────────────────────────────────────────────
        if (scoreMovement != null && teleportScorePoints > 0)
            scoreMovement.AddScore(teleportScorePoints, teleportScoreLabel);

        _lastTeleportTime = Time.time;

        // ── Анимация Lens Distortion ──────────────────────────────────────
        // Если предыдущая анимация ещё не завершилась — прерываем её,
        // сбрасываем в исходное значение и запускаем заново.
        if (_lensCoroutine != null)
        {
            StopCoroutine(_lensCoroutine);
            if (_lensDistortion != null)
                _lensDistortion.intensity.value = 1f;
        }
        _lensCoroutine = StartCoroutine(AnimateLensDistortion());

        Debug.Log($"[TeleportTrigger] '{player.name}' телепортирован в {target}");
    }

    private void PlayTeleportSound(Vector3 position)
    {
        if (teleportSound == null) return;

        // AudioSource.PlayClipAtPoint — одноразовый 3D-звук без привязки к объекту
        AudioSource.PlayClipAtPoint(teleportSound, position, soundVolume);
    }

    // ── Анимация линзового искажения ─────────────────────────────────────────

    /// <summary>
    /// Плавно меняет Lens Distortion Intensity:
    ///   1  →  0  за <lensDistortionFadeOut> секунд
    ///   0  →  1  за <lensDistortionFadeIn>  секунд
    /// </summary>
    private IEnumerator AnimateLensDistortion()
    {
        if (_lensDistortion == null) yield break;

        // ── Фаза 1: intensity 1 → 0 ──────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < lensDistortionFadeOut)
        {
            elapsed += Time.deltaTime;
            _lensDistortion.intensity.value =
                Mathf.Lerp(1f, 0f, elapsed / lensDistortionFadeOut);
            yield return null;
        }
        _lensDistortion.intensity.value = 0f;   // гарантируем точное конечное значение

        // ── Фаза 2: intensity 0 → 1 ──────────────────────────────────────
        elapsed = 0f;
        while (elapsed < lensDistortionFadeIn)
        {
            elapsed += Time.deltaTime;
            _lensDistortion.intensity.value =
                Mathf.Lerp(0f, 1f, elapsed / lensDistortionFadeIn);
            yield return null;
        }
        _lensDistortion.intensity.value = 1f;   // гарантируем точное конечное значение

        _lensCoroutine = null;
    }

    // ── Визуализация в редакторе ─────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (destinationPoint == null) return;

        // Стрелка от триггера к точке назначения
        Gizmos.color = Color.cyan;
        Vector3 from = transform.position;
        Vector3 to   = destinationPoint.position + destinationOffset;

        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.25f);

        // Мини-иконка на самом триггере
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawSphere(from, 0.2f);
    }
}