using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firecamggie : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color _zoneColor = Color.green;
    [SerializeField] private float _zoneAlpha = 0.3f;
    [SerializeField] private bool _showGizmos = true;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _enterEffect;
    [SerializeField] private ParticleSystem _exitEffect;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _enterSound;
    [SerializeField] private AudioClip _exitSound;

    [Header("References")]
    [SerializeField] private TimerTriggerSystem _timerSystem;
    [SerializeField] private string _playerTag = "Player";

    private bool _isPlayerInside = false;

    private void Start()
    {
        // Автоматически найти TimerTriggerSystem если не назначен
        if (_timerSystem == null)
        {
            _timerSystem = FindObjectOfType<TimerTriggerSystem>();
        }

        // Убедиться что у объекта есть Collider с включенным IsTrigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("TimerTriggerZone: Не найден Collider! Добавьте Collider и включите IsTrigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_playerTag) && !_isPlayerInside)
        {
            _isPlayerInside = true;

            Debug.Log("Игрок вошел в зону восстановления времени");

            // Воспроизводим эффект входа
            if (_enterEffect != null)
            {
                _enterEffect.Play();
            }

            // Воспроизводим звук входа
            if (_audioSource != null && _enterSound != null)
            {
                _audioSource.PlayOneShot(_enterSound);
            }

            // Уведомляем систему таймера
            if (_timerSystem != null)
            {
                _timerSystem.SendMessage("OnTriggerEnter", other, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_playerTag) && _isPlayerInside)
        {
            _isPlayerInside = false;

            Debug.Log("Игрок покинул зону восстановления времени");

            // Воспроизводим эффект выхода
            if (_exitEffect != null)
            {
                _exitEffect.Play();
            }

            // Воспроизводим звук выхода
            if (_audioSource != null && _exitSound != null)
            {
                _audioSource.PlayOneShot(_exitSound);
            }

            // Уведомляем систему таймера
            if (_timerSystem != null)
            {
                _timerSystem.SendMessage("OnTriggerExit", other, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_showGizmos)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(_zoneColor.r, _zoneColor.g, _zoneColor.b, _zoneAlpha);

                // Рисуем зону в зависимости от типа коллайдера
                if (col is BoxCollider)
                {
                    BoxCollider boxCol = col as BoxCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(boxCol.center, boxCol.size);
                }
                else if (col is SphereCollider)
                {
                    SphereCollider sphereCol = col as SphereCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawSphere(sphereCol.center, sphereCol.radius);
                }
                else if (col is CapsuleCollider)
                {
                    CapsuleCollider capsuleCol = col as CapsuleCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(capsuleCol.center, new Vector3(capsuleCol.radius * 2, capsuleCol.height, capsuleCol.radius * 2));
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_showGizmos)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = _zoneColor;

                if (col is BoxCollider)
                {
                    BoxCollider boxCol = col as BoxCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCol.center, boxCol.size);
                }
                else if (col is SphereCollider)
                {
                    SphereCollider sphereCol = col as SphereCollider;
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
                }
            }
        }
    }

    // Публичные методы
    public bool IsPlayerInside()
    {
        return _isPlayerInside;
    }

    public void SetTimerSystem(TimerTriggerSystem timerSystem)
    {
        _timerSystem = timerSystem;
    }
}
