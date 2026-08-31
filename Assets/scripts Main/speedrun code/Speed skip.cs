using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraPushTrigger : MonoBehaviour
{
    [Header("Push Settings")]
    [SerializeField] private float pushForce = 10f; // Сила отталкивания
    [SerializeField] private float upwardForce = 5f; // Дополнительная сила вверх
    [SerializeField] private float pushDuration = 0.5f; // Длительность отталкивания
    [SerializeField] private AnimationCurve pushCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); // Кривая затухания силы
    [SerializeField] private bool useMainCamera = true; // Использовать главную камеру
    [SerializeField] private Camera customCamera; // Кастомная камера

    [Header("Character Controller Settings")]
    [SerializeField] private bool overridePlayerControl = true; // Отключить управление игроком во время отталкивания
    [SerializeField] private bool useGravity = true; // Применять гравитацию во время отталкивания
    [SerializeField] private float gravityMultiplier = 1f; // Множитель гравитации

    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player"; // Тег игрока
    [SerializeField] private bool triggerOnce = false; // Сработать только один раз
    [SerializeField] private float cooldownTime = 1f; // Время перезарядки

    [Header("Effects")]
    [SerializeField] private ParticleSystem pushEffect; // Эффект частиц
    [SerializeField] private AudioSource audioSource; // Звук
    [SerializeField] private AudioClip pushSound; // Звук отталкивания

    private Camera targetCamera;
    private bool hasTriggered = false;
    private float lastTriggerTime = 0f;
    private Coroutine currentPushCoroutine;

    void Start()
    {
        // Определяем камеру для использования
        if (useMainCamera)
        {
            targetCamera = Camera.main;
        }
        else if (customCamera != null)
        {
            targetCamera = customCamera;
        }
        else
        {
            targetCamera = Camera.main;
        }

        // Проверяем наличие Collider и что он является триггером
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"CameraPushTrigger на {gameObject.name} требует Collider компонент!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider на {gameObject.name} должен быть триггером!");
        }

        // Настраиваем AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Настраиваем кривую по умолчанию, если не задана
        if (pushCurve.keys.Length == 0)
        {
            // Создаем кривую затухания: начинаем с полной силы (1) и плавно уменьшаем до 0
            Keyframe[] keys = new Keyframe[3];
            keys[0] = new Keyframe(0f, 1f);     // В начале полная сила
            keys[1] = new Keyframe(0.7f, 0.3f); // В середине уменьшаем
            keys[2] = new Keyframe(1f, 0f);     // В конце сила = 0

            // Настраиваем касательные для плавности
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].inTangent = 0f;
                keys[i].outTangent = 0f;
            }

            pushCurve = new AnimationCurve(keys);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем тег игрока
        if (!other.CompareTag(playerTag))
            return;

        // Проверяем условия срабатывания
        if (triggerOnce && hasTriggered)
            return;

        if (Time.time - lastTriggerTime < cooldownTime)
            return;

        // Выполняем отталкивание
        PushPlayer(other);

        // Обновляем состояние
        hasTriggered = true;
        lastTriggerTime = Time.time;
    }

    void PushPlayer(Collider player)
    {
        // Получаем Character Controller игрока
        CharacterController playerController = player.GetComponent<CharacterController>();
        if (playerController == null)
        {
            Debug.LogWarning($"Игрок {player.name} не имеет CharacterController компонента!");
            return;
        }

        // Останавливаем предыдущее отталкивание, если оно есть
        if (currentPushCoroutine != null)
        {
            StopCoroutine(currentPushCoroutine);
        }

        // Вычисляем направление отталкивания
        Vector3 pushDirection = CalculatePushDirection(player.transform.position);

        // Запускаем корутину отталкивания
        currentPushCoroutine = StartCoroutine(PushPlayerCoroutine(playerController, pushDirection));

        // Воспроизводим эффекты
        PlayEffects();

        Debug.Log($"Игрок {player.name} оттолкнут в направлении {pushDirection}");
    }

    IEnumerator PushPlayerCoroutine(CharacterController controller, Vector3 direction)
    {
        // Отключаем управление игроком, если нужно
        IPlayerController playerScript = null;
        if (overridePlayerControl)
        {
            playerScript = controller.GetComponent<IPlayerController>();
            if (playerScript != null)
            {
                playerScript.SetControlEnabled(false);
            }
        }

        float elapsedTime = 0f;
        Vector3 velocity = Vector3.zero;

        // Начальная скорость отталкивания
        Vector3 initialPushVelocity = direction * pushForce;
        initialPushVelocity.y = upwardForce;

        while (elapsedTime < pushDuration)
        {
            float normalizedTime = elapsedTime / pushDuration;
            float forceMultiplier = pushCurve.Evaluate(normalizedTime);

            // Вычисляем текущую скорость отталкивания
            Vector3 currentPushVelocity = initialPushVelocity * forceMultiplier;

            // Применяем гравитацию
            if (useGravity)
            {
                velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }

            // Комбинируем силу отталкивания и гравитацию
            Vector3 finalVelocity = currentPushVelocity + new Vector3(0, velocity.y, 0);

            // Перемещаем игрока
            controller.Move(finalVelocity * Time.deltaTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Восстанавливаем управление игроком
        if (overridePlayerControl && playerScript != null)
        {
            playerScript.SetControlEnabled(true);
        }

        currentPushCoroutine = null;
    }

    Vector3 CalculatePushDirection(Vector3 playerPosition)
    {
        if (targetCamera == null)
        {
            Debug.LogError("Камера не найдена!");
            return Vector3.forward;
        }

        // Получаем направление взгляда камеры
        Vector3 cameraForward = targetCamera.transform.forward;

        // Убираем вертикальную составляющую для более естественного отталкивания
        cameraForward.y = 0f;
        cameraForward.Normalize();

        // Если направление нулевое, используем направление от триггера к игроку
        if (cameraForward.magnitude < 0.1f)
        {
            Vector3 fromTriggerToPlayer = (playerPosition - transform.position).normalized;
            fromTriggerToPlayer.y = 0f;
            return fromTriggerToPlayer;
        }

        return cameraForward;
    }

    void PlayEffects()
    {
        // Воспроизводим эффект частиц
        if (pushEffect != null)
        {
            pushEffect.Play();
        }

        // Воспроизводим звук
        if (audioSource != null && pushSound != null)
        {
            audioSource.PlayOneShot(pushSound);
        }
    }

    // Публичные методы для внешнего управления
    public void ResetTrigger()
    {
        hasTriggered = false;
        lastTriggerTime = 0f;
    }

    public void SetPushForce(float newForce)
    {
        pushForce = Mathf.Max(0f, newForce);
    }

    public void SetUpwardForce(float newForce)
    {
        upwardForce = Mathf.Max(0f, newForce);
    }

    public void SetPushDuration(float newDuration)
    {
        pushDuration = Mathf.Max(0.1f, newDuration);
    }

    public void SetCamera(Camera newCamera)
    {
        customCamera = newCamera;
        targetCamera = newCamera;
        useMainCamera = false;
    }

    public void StopCurrentPush()
    {
        if (currentPushCoroutine != null)
        {
            StopCoroutine(currentPushCoroutine);
            currentPushCoroutine = null;
        }
    }

    public bool HasTriggered()
    {
        return hasTriggered;
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTime - (Time.time - lastTriggerTime));
    }

    public bool IsPushingPlayer()
    {
        return currentPushCoroutine != null;
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (targetCamera != null)
        {
            // Рисуем направление камеры
            Gizmos.color = Color.blue;
            Vector3 cameraDirection = targetCamera.transform.forward;
            cameraDirection.y = 0f;
            Gizmos.DrawRay(transform.position, cameraDirection.normalized * 3f);

            // Рисуем зону триггера
            Gizmos.color = Color.yellow;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.DrawWireCube(transform.position, col.bounds.size);
            }

            // Рисуем предполагаемую траекторию
            Gizmos.color = Color.red;
            Vector3 pushDir = cameraDirection.normalized * pushForce * 0.1f;
            pushDir.y = upwardForce * 0.1f;
            Gizmos.DrawRay(transform.position, pushDir);
        }
    }
}

// Интерфейс для взаимодействия с контроллером игрока
public interface IPlayerController
{
    void SetControlEnabled(bool enabled);
}