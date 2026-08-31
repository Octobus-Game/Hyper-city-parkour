using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class JumpTrigger : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDuration = 2f;
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool playerInTrigger = false;
    private GameObject player;
    private CharacterController playerController;
    private bool isJumping = false;

    void Start()
    {
        // Убеждаемся, что у объекта есть Collider с включенным IsTrigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("JumpTrigger: Не найден Collider компонент!");
        }
    }

    void Update()
    {
        // Проверяем нажатие пробела только если игрок в триггере и не прыгает
        if (playerInTrigger && !isJumping && Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(JumpPlayer());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок (по тегу или имени)
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            player = other.gameObject;
            playerController = player.GetComponent<CharacterController>();

            if (playerController == null)
            {
                Debug.LogError("JumpTrigger: У игрока нет CharacterController компонента!");
                return;
            }

            playerInTrigger = true;
            Debug.Log("Игрок вошел в триггер. Нажмите пробел для прыжка!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            playerInTrigger = false;
            player = null;
            playerController = null;
            Debug.Log("Игрок покинул триггер.");
        }
    }

    IEnumerator JumpPlayer()
    {
        if (playerController == null) yield break;

        isJumping = true;

        // Запоминаем начальную позицию
        Vector3 startPosition = player.transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * jumpHeight;

        float elapsedTime = 0f;

        Debug.Log("Игрок взлетает!");

        // Плавное движение вверх и вниз по кривой
        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / jumpDuration;

            // Используем кривую для плавного движения
            float curveValue = jumpCurve.Evaluate(normalizedTime);

            // Вычисляем текущую высоту
            float currentHeight = Mathf.Lerp(0f, jumpHeight, curveValue);
            Vector3 currentPosition = startPosition + Vector3.up * currentHeight;

            // Перемещаем персонажа
            Vector3 movement = currentPosition - player.transform.position;
            playerController.Move(movement);

            yield return null;
        }

        // Убеждаемся, что персонаж вернулся в исходную позицию
        Vector3 finalMovement = startPosition - player.transform.position;
        playerController.Move(finalMovement);

        isJumping = false;

        Debug.Log("Прыжок завершен.");
    }

    // Альтернативный метод с физикой гравитации
    IEnumerator JumpPlayerWithGravity()
    {
        if (playerController == null) yield break;

        isJumping = true;

        // Начальная скорость для достижения нужной высоты
        float initialVelocity = Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(Physics.gravity.y));
        float currentVelocity = initialVelocity;

        float elapsedTime = 0f;

        Debug.Log("Игрок взлетает с гравитацией!");

        // Симулируем физику прыжка
        while (elapsedTime < jumpDuration)
        {
            elapsedTime += Time.deltaTime;

            // Применяем гравитацию
            currentVelocity += Physics.gravity.y * Time.deltaTime;

            // Перемещаем персонажа
            Vector3 movement = Vector3.up * currentVelocity * Time.deltaTime;
            playerController.Move(movement);

            // Если персонаж начал падать и достиг исходной высоты, прерываем
            if (currentVelocity < 0 && elapsedTime > jumpDuration * 0.5f)
            {
                break;
            }

            yield return null;
        }

        isJumping = false;

        Debug.Log("Прыжок с гравитацией завершен.");
    }

    // Визуализация триггера в редакторе
    void OnDrawGizmosSelected()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            Gizmos.color = playerInTrigger ? Color.green : Color.yellow;
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
        }

        // Показываем высоту прыжка
        if (playerInTrigger && player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(player.transform.position, player.transform.position + Vector3.up * jumpHeight);
        }
    }
}