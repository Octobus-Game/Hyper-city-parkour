using UnityEngine;
using System.Collections;

public class BulletHitEffect : MonoBehaviour
{
    [Header("Hit Effect Settings")]
    [SerializeField] private ParticleSystem _hitEffectPrefab; // Префаб эффекта попадания
    [SerializeField] private float _effectLifetime = 2f; // Время жизни эффекта
    [SerializeField] private LayerMask _bulletLayers = -1; // Слои пуль

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] _hitSounds; // Звуки попадания
    [SerializeField] private AudioSource _audioSource;
    [SerializeField, Range(0f, 1f)] private float _hitSoundVolume = 1f;
    [SerializeField, Range(0.5f, 2f)] private float _pitchVariation = 0.3f;

    [Header("Advanced Settings")]
    [SerializeField] private bool _alignToSurface = true; // Выравнивание по поверхности
    [SerializeField] private bool _useObjectPooling = true; // Использовать пулинг объектов
    [SerializeField] private int _poolSize = 10;

    // Пулинг эффектов
    private ParticleSystem[] _effectPool;
    private int _currentPoolIndex = 0;

    private void Start()
    {
        // Создание AudioSource если не назначен
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D звук
        }

        // Инициализация пула эффектов
        if (_useObjectPooling && _hitEffectPrefab != null)
        {
            InitializeEffectPool();
        }

        // Проверка наличия коллайдера
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"No Collider found on {gameObject.name}! Add a Collider for bullet detection.", this);
        }
    }

    private void InitializeEffectPool()
    {
        _effectPool = new ParticleSystem[_poolSize];

        for (int i = 0; i < _poolSize; i++)
        {
            GameObject effectObject = Instantiate(_hitEffectPrefab.gameObject);
            _effectPool[i] = effectObject.GetComponent<ParticleSystem>();
            effectObject.SetActive(false);

            // Делаем дочерним объектом для организации иерархии
            effectObject.transform.SetParent(transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект пулей
        if (IsBullet(other))
        {
            HandleBulletHit(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем, является ли объект пулей
        if (IsBullet(collision.collider))
        {
            // Используем точку контакта для более точного позиционирования
            Vector3 hitPoint = collision.contacts.Length > 0 ?
                collision.contacts[0].point :
                collision.transform.position;

            Vector3 hitNormal = collision.contacts.Length > 0 ?
                collision.contacts[0].normal :
                -collision.transform.forward;

            HandleBulletHit(collision.collider, hitPoint, hitNormal);
        }
    }

    private bool IsBullet(Collider collider)
    {
        // Проверяем по слою
        if (((1 << collider.gameObject.layer) & _bulletLayers) != 0)
            return true;

        // Проверяем по тегу
        if (collider.CompareTag("Bullet"))
            return true;

        // Проверяем по компоненту
        if (collider.GetComponent<Bullet>() != null)
            return true;

        return false;
    }

    private void HandleBulletHit(Collider bullet, Vector3? hitPoint = null, Vector3? hitNormal = null)
    {
        // Определяем точку попадания
        Vector3 effectPosition = hitPoint ?? bullet.transform.position;
        Vector3 effectNormal = hitNormal ?? -bullet.transform.forward;

        // Создаем эффект попадания
        CreateHitEffect(effectPosition, effectNormal);

        // Воспроизводим звук
        PlayHitSound();

      
    }

    private void CreateHitEffect(Vector3 position, Vector3 normal)
    {
        if (_hitEffectPrefab == null) return;

        ParticleSystem effectInstance;

        if (_useObjectPooling)
        {
            effectInstance = GetPooledEffect();
            if (effectInstance == null) return;

            effectInstance.transform.position = position;
        }
        else
        {
            // Создаем новый экземпляр
            GameObject effectObject = Instantiate(_hitEffectPrefab.gameObject, position, Quaternion.identity);
            effectInstance = effectObject.GetComponent<ParticleSystem>();

            // Автоматически уничтожаем через время
            Destroy(effectObject, _effectLifetime);
        }

        // Выравниваем по поверхности
        if (_alignToSurface)
        {
            effectInstance.transform.rotation = Quaternion.LookRotation(normal);
        }

        // Активируем эффект
        effectInstance.gameObject.SetActive(true);
        effectInstance.Play();

        // Если используем пулинг, деактивируем через время
        if (_useObjectPooling)
        {
            StartCoroutine(DeactivateEffectAfterTime(effectInstance, _effectLifetime));
        }
    }

    private ParticleSystem GetPooledEffect()
    {
        if (_effectPool == null) return null;

        // Ищем неактивный эффект
        for (int i = 0; i < _effectPool.Length; i++)
        {
            int index = (_currentPoolIndex + i) % _effectPool.Length;
            if (!_effectPool[index].gameObject.activeInHierarchy)
            {
                _currentPoolIndex = (index + 1) % _effectPool.Length;
                return _effectPool[index];
            }
        }

        // Если все заняты, используем следующий по очереди
        ParticleSystem effect = _effectPool[_currentPoolIndex];
        _currentPoolIndex = (_currentPoolIndex + 1) % _effectPool.Length;
        return effect;
    }

    private void PlayHitSound()
    {
        if (_hitSounds == null || _hitSounds.Length == 0 || _audioSource == null)
            return;

        // Выбираем случайный звук
        AudioClip randomSound = _hitSounds[Random.Range(0, _hitSounds.Length)];

        // Применяем вариацию высоты тона и громкости
        _audioSource.pitch = Random.Range(1f - _pitchVariation, 1f + _pitchVariation);
        _audioSource.volume = _hitSoundVolume;

        _audioSource.PlayOneShot(randomSound);
    }

   

    private IEnumerator DeactivateEffectAfterTime(ParticleSystem effect, float time)
    {
        yield return new WaitForSeconds(time);

        if (effect != null)
        {
            effect.Stop();
            effect.gameObject.SetActive(false);
        }
    }

    // Публичные методы для внешнего управления
    public void SetHitEffectPrefab(ParticleSystem newPrefab)
    {
        _hitEffectPrefab = newPrefab;
        if (_useObjectPooling)
        {
            // Пересоздаем пул с новым префабом
            DestroyCurrentPool();
            InitializeEffectPool();
        }
    }

    public void SetHitSoundVolume(float volume)
    {
        _hitSoundVolume = Mathf.Clamp01(volume);
    }

    public void TriggerHitEffectManually(Vector3 position, Vector3 normal)
    {
        CreateHitEffect(position, normal);
        PlayHitSound();
    }

    private void DestroyCurrentPool()
    {
        if (_effectPool != null)
        {
            for (int i = 0; i < _effectPool.Length; i++)
            {
                if (_effectPool[i] != null)
                {
                    DestroyImmediate(_effectPool[i].gameObject);
                }
            }
            _effectPool = null;
        }
    }

    private void OnDestroy()
    {
        DestroyCurrentPool();
    }
}



