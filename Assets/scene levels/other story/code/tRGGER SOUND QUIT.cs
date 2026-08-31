
using UnityEngine;
using System.Collections;

public class AudioVolumeTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _reducedVolume = 0.2f;
    [SerializeField] private float _fadeSpeed = 2f;

    [Header("Player Detection")]
    [SerializeField] private string _playerTag = "Player";

    private float _originalVolume;
    private bool _playerInside = false;
    private Coroutine _volumeFadeCoroutine;

    void Start()
    {
        // Получаем AudioSource если не назначен
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        // Проверяем наличие AudioSource
        if (_audioSource == null)
        {
            Debug.LogError("AudioSource не найден на объекте " + gameObject.name);
            return;
        }

        // Сохраняем изначальную громкость
        _originalVolume = _audioSource.volume;

        // Проверяем наличие Collider с включенным IsTrigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("Collider не найден на объекте " + gameObject.name);
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning("Collider на объекте " + gameObject.name + " должен быть триггером (IsTrigger = true)");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag(_playerTag))
        {
            _playerInside = true;
            StartVolumeFade(_reducedVolume);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag(_playerTag))
        {
            _playerInside = false;
            StartVolumeFade(_originalVolume);
        }
    }

    void StartVolumeFade(float targetVolume)
    {
        // Останавливаем предыдущую корутину, если она выполняется
        if (_volumeFadeCoroutine != null)
        {
            StopCoroutine(_volumeFadeCoroutine);
        }

        // Запускаем новую корутину для плавного изменения громкости
        _volumeFadeCoroutine = StartCoroutine(FadeVolume(targetVolume));
    }

    IEnumerator FadeVolume(float targetVolume)
    {
        if (_audioSource == null) yield break;

        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * _fadeSpeed;
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed);
            yield return null;
        }

        // Убеждаемся, что достигнута целевая громкость
        _audioSource.volume = targetVolume;
        _volumeFadeCoroutine = null;
    }

    // Методы для настройки из других скриптов
    public void SetReducedVolume(float volume)
    {
        _reducedVolume = Mathf.Clamp01(volume);
    }

    public void SetFadeSpeed(float speed)
    {
        _fadeSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetPlayerTag(string tag)
    {
        _playerTag = tag;
    }

    // Принудительно восстановить изначальную громкость
    public void ResetVolume()
    {
        StartVolumeFade(_originalVolume);
    }

    // Получить текущее состояние
    public bool IsPlayerInside()
    {
        return _playerInside;
    }

    public float GetOriginalVolume()
    {
        return _originalVolume;
    }

    public float GetCurrentVolume()
    {
        return _audioSource != null ? _audioSource.volume : 0f;
    }

    // Отладочная информация
    void OnDrawGizmosSelected()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            Gizmos.color = _playerInside ? Color.red : Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (triggerCollider is BoxCollider)
            {
                BoxCollider box = triggerCollider as BoxCollider;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (triggerCollider is SphereCollider)
            {
                SphereCollider sphere = triggerCollider as SphereCollider;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (triggerCollider is CapsuleCollider)
            {
                CapsuleCollider capsule = triggerCollider as CapsuleCollider;
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
        }
    }
}