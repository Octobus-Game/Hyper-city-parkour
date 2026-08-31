using System.Collections;
using System.Reflection;
using UnityEngine;

public class GravityZone : MonoBehaviour
{
    [Header("Gravity Settings")]
    [SerializeField] private float _newGravityValue = 20f;
    [SerializeField] private bool _invertGravity = true;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private Vector3 _targetRotation = new Vector3(-90f, 0f, 0f);
    [SerializeField] private bool _rotateGravityObjects = true; // Поворачивать ли объекты с тегом Gravity

    [Header("Physics Objects Settings")]
    [SerializeField] private float _physicsGravityScale = -1f; // Множитель гравитации для Rigidbody

    [Header("Audio")]
    [SerializeField] private AudioClip _gravityChangeSound;
    [SerializeField] private float _soundVolume = 0.7f;

    // Для CharacterMovement0 (игрок)
    private CharacterMovement0 _playerMovement;
    private float _originalGravity;
    private Quaternion _originalRotation;
    private bool _isPlayerInZone = false;
    private FieldInfo _gravityField;

    // Для объектов с физикой
    private System.Collections.Generic.Dictionary<Rigidbody, GravityObjectData> _gravityObjects =
        new System.Collections.Generic.Dictionary<Rigidbody, GravityObjectData>();

    private AudioSource _audioSource;

    private class GravityObjectData
    {
        public bool useGravity;
        public Quaternion originalRotation;
        public Vector3 originalGravityDirection;
    }

    private void Start()
    {
        if (_gravityChangeSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Обработка игрока
        if (other.CompareTag("Player"))
        {
            HandlePlayerEnter(other);
        }
        // Обработка объектов с гравитацией
        else if (other.CompareTag("Gravity"))
        {
            HandleGravityObjectEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Обработка выхода игрока
        if (other.CompareTag("Player") && _isPlayerInZone)
        {
            HandlePlayerExit(other);
        }
        // Обработка выхода объектов с гравитацией
        else if (other.CompareTag("Gravity"))
        {
            HandleGravityObjectExit(other);
        }
    }

    private void HandlePlayerEnter(Collider other)
    {
        _playerMovement = other.GetComponent<CharacterMovement0>();

        if (_playerMovement != null)
        {
            _isPlayerInZone = true;

            // Получаем доступ к приватному полю через Reflection
            _gravityField = typeof(CharacterMovement0).GetField("_gravity",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_gravityField != null)
            {
                _originalGravity = (float)_gravityField.GetValue(_playerMovement);
                float newGravity = _invertGravity ? -_newGravityValue : _newGravityValue;
                _gravityField.SetValue(_playerMovement, newGravity);
            }

            _originalRotation = other.transform.rotation;
            StartCoroutine(RotatePlayer(other.transform, _targetRotation));
            PlayGravityChangeSound();

            Debug.Log($"Player entered gravity zone. New gravity: {(_invertGravity ? -_newGravityValue : _newGravityValue)}");
        }
    }

    private void HandlePlayerExit(Collider other)
    {
        if (_playerMovement != null && _gravityField != null)
        {
            _gravityField.SetValue(_playerMovement, _originalGravity);
            StartCoroutine(RotatePlayer(other.transform, _originalRotation.eulerAngles));
            PlayGravityChangeSound();

            Debug.Log($"Player exited gravity zone. Gravity restored: {_originalGravity}");

            _isPlayerInZone = false;
            _playerMovement = null;
        }
    }

    private void HandleGravityObjectEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null && !_gravityObjects.ContainsKey(rb))
        {
            // Сохраняем оригинальные данные
            GravityObjectData data = new GravityObjectData
            {
                useGravity = rb.useGravity,
                originalRotation = other.transform.rotation,
                originalGravityDirection = Physics.gravity
            };

            _gravityObjects.Add(rb, data);

            // Отключаем стандартную гравитацию Unity
            rb.useGravity = false;

            // Поворачиваем объект, если включено
            if (_rotateGravityObjects)
            {
                StartCoroutine(RotateObject(other.transform, _targetRotation));
            }

            PlayGravityChangeSound();

            Debug.Log($"Gravity object '{other.name}' entered gravity zone");
        }
    }

    private void HandleGravityObjectExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null && _gravityObjects.ContainsKey(rb))
        {
            GravityObjectData data = _gravityObjects[rb];

            // Восстанавливаем оригинальную гравитацию
            rb.useGravity = data.useGravity;

            // Возвращаем оригинальный поворот
            if (_rotateGravityObjects)
            {
                StartCoroutine(RotateObject(other.transform, data.originalRotation.eulerAngles));
            }

            // Удаляем из словаря
            _gravityObjects.Remove(rb);

            PlayGravityChangeSound();

            Debug.Log($"Gravity object '{other.name}' exited gravity zone");
        }
    }

    private void FixedUpdate()
    {
        // Применяем кастомную гравитацию к объектам в зоне
        foreach (var kvp in _gravityObjects)
        {
            Rigidbody rb = kvp.Key;

            if (rb != null)
            {
                // Направление гравитации (вверх или вниз в зависимости от инверсии)
                Vector3 gravityDirection = _invertGravity ? Vector3.up : Vector3.down;
                Vector3 gravityForce = gravityDirection * _newGravityValue * _physicsGravityScale;

                // Применяем силу гравитации
                rb.AddForce(gravityForce, ForceMode.Acceleration);
            }
        }
    }

    private IEnumerator RotatePlayer(Transform playerTransform, Vector3 targetEulerAngles)
    {
        Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
        Quaternion startRotation = playerTransform.rotation;

        float elapsedTime = 0f;
        float duration = 1f / _rotationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            playerTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        playerTransform.rotation = targetRotation;
    }

    private IEnumerator RotateObject(Transform objectTransform, Vector3 targetEulerAngles)
    {
        Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
        Quaternion startRotation = objectTransform.rotation;

        float elapsedTime = 0f;
        float duration = 1f / _rotationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            objectTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        objectTransform.rotation = targetRotation;
    }

    private void PlayGravityChangeSound()
    {
        if (_gravityChangeSound != null && _audioSource != null)
        {
            _audioSource.volume = _soundVolume;
            _audioSource.pitch = Random.Range(0.95f, 1.05f);
            _audioSource.PlayOneShot(_gravityChangeSound);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider boxCollider)
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (col is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
            }
        }

        // Визуализация направления гравитации
        Gizmos.color = _invertGravity ? Color.green : Color.red;
        Vector3 arrowStart = transform.position;
        Vector3 arrowDirection = _invertGravity ? Vector3.up : Vector3.down;
        Gizmos.DrawRay(arrowStart, arrowDirection * 2f);
    }

    // Очистка при уничтожении
    private void OnDestroy()
    {
        _gravityObjects.Clear();
    }
}