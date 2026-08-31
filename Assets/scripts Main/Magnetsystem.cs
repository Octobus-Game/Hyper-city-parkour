using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Magnetsystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask targetLayers = -1;
    [SerializeField] private string[] magneticTags = { "Magnetic", "Player" };

    [Header("Movement Settings")]
    [SerializeField] private float pullSpeed = 5f;
    [SerializeField] private float stopDistance = 1f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool canPullMultipleTimes = true;
    [SerializeField] private float cooldownTime = 0.5f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode magnetKey = KeyCode.R;
    [SerializeField] private bool requireHold = false;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject targetIndicator;
    [SerializeField] private Color validTargetColor = Color.green;
    [SerializeField] private Color invalidTargetColor = Color.red;
    [SerializeField] private LineRenderer pullLine;

    [Header("Audio")]
    [SerializeField] private AudioClip pullSound;
    [SerializeField] private AudioClip completeSound;

    [Header("Events")]
    public UnityEvent<GameObject> OnTargetFound;
    public UnityEvent OnTargetLost;
    public UnityEvent<GameObject> OnPullStart;
    public UnityEvent<GameObject> OnPullComplete;

    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private AudioSource audioSource;

    private GameObject currentTarget;
    private bool isPulling = false;
    private bool canPull = true;
    private float lastPullTime;

    // Компоненты для управления движением
    private Vector3 pullDirection;
    private float pullProgress;

    void Start()
    {
        InitializeComponents();
    }

    void InitializeComponents()
    {
        // Получаем камеру если не назначена
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Получаем компоненты движения
        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Настраиваем визуальные элементы
        if (pullLine != null)
        {
            pullLine.enabled = false;
            pullLine.positionCount = 2;
        }

        if (targetIndicator != null)
            targetIndicator.SetActive(false);
    }

    void Update()
    {
        if (!isPulling)
        {
            CheckForTarget();
            HandleInput();
        }

        UpdateVisualFeedback();
    }

    void CheckForTarget()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, targetLayers))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsValidTarget(hitObject))
            {
                if (currentTarget != hitObject)
                {
                    SetCurrentTarget(hitObject);
                }
            }
            else
            {
                ClearCurrentTarget();
            }
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    bool IsValidTarget(GameObject obj)
    {
        if (magneticTags.Length == 0) return true;

        foreach (string tag in magneticTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void SetCurrentTarget(GameObject target)
    {
        currentTarget = target;
        OnTargetFound?.Invoke(target);

        Debug.Log($"Цель найдена: {target.name}");
    }

    void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget = null;
            OnTargetLost?.Invoke();
        }
    }

    void HandleInput()
    {
        bool inputPressed = requireHold ? Input.GetKey(magnetKey) : Input.GetKeyDown(magnetKey);

        if (inputPressed && currentTarget != null && canPull)
        {
            StartPull();
        }
    }

    void StartPull()
    {
        if (!canPull) return;

        isPulling = true;
        pullProgress = 0f;
        pullDirection = (currentTarget.transform.position - transform.position).normalized;

        // Воспроизводим звук
        if (pullSound != null && audioSource != null)
            audioSource.PlayOneShot(pullSound);

        // Вызываем событие
        OnPullStart?.Invoke(currentTarget);

        // Запускаем корутину притягивания
        StartCoroutine(PullToTarget());

        Debug.Log($"Начинаем притягивание к {currentTarget.name}");
    }

    IEnumerator PullToTarget()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = currentTarget.transform.position;

        // Корректируем целевую позицию с учетом дистанции остановки
        Vector3 direction = (targetPosition - startPosition).normalized;
        targetPosition = targetPosition - direction * stopDistance;

        float journey = 0f;
        float totalDistance = Vector3.Distance(startPosition, targetPosition);

        while (journey <= 1f && isPulling)
        {
            journey += Time.deltaTime * pullSpeed / totalDistance;
            pullProgress = speedCurve.Evaluate(journey);

            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, pullProgress);

            // Перемещаем игрока в зависимости от типа компонента
            MovePlayer(currentPosition - transform.position);

            // Проверяем достижение цели
            if (Vector3.Distance(transform.position, targetPosition) <= 0.1f)
            {
                break;
            }

            yield return null;
        }

        CompletePull();
    }

    void MovePlayer(Vector3 movement)
    {
        if (characterController != null)
        {
            characterController.Move(movement);
        }
        else if (playerRigidbody != null)
        {
            playerRigidbody.MovePosition(transform.position + movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    void CompletePull()
    {
        isPulling = false;

        // Воспроизводим звук завершения
        if (completeSound != null && audioSource != null)
            audioSource.PlayOneShot(completeSound);

        // Вызываем событие
        OnPullComplete?.Invoke(currentTarget);

        // Устанавливаем кулдаун
        if (!canPullMultipleTimes)
        {
            canPull = false;
            lastPullTime = Time.time;
            StartCoroutine(CooldownCoroutine());
        }

        Debug.Log($"Притягивание к {currentTarget.name} завершено");
    }

    IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        canPull = true;
    }

    void UpdateVisualFeedback()
    {
        // Обновляем индикатор цели
        if (targetIndicator != null)
        {
            bool shouldShow = currentTarget != null && !isPulling;
            targetIndicator.SetActive(shouldShow);

            if (shouldShow)
            {
                targetIndicator.transform.position = currentTarget.transform.position;

                // Меняем цвет в зависимости от возможности притягивания
                Renderer renderer = targetIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = canPull ? validTargetColor : invalidTargetColor;
                }
            }
        }

        // Обновляем линию притягивания
        if (pullLine != null)
        {
            bool shouldShowLine = isPulling && currentTarget != null;
            pullLine.enabled = shouldShowLine;

            if (shouldShowLine)
            {
                pullLine.SetPosition(0, transform.position);
                pullLine.SetPosition(1, currentTarget.transform.position);

                // Анимируем прозрачность линии
                Color lineColor = pullLine.material.color;
                lineColor.a = Mathf.Lerp(0.2f, 0.8f, pullProgress);
                pullLine.material.color = lineColor;
            }
        }
    }

    // Методы для внешнего управления
    public void SetPullSpeed(float newSpeed)
    {
        pullSpeed = Mathf.Max(0.1f, newSpeed);
    }

    public void SetRaycastDistance(float newDistance)
    {
        raycastDistance = Mathf.Max(0.1f, newDistance);
    }

    public void StopPull()
    {
        if (isPulling)
        {
            StopAllCoroutines();
            isPulling = false;
        }
    }

    public bool IsPulling => isPulling;
    public bool CanPull => canPull;
    public GameObject CurrentTarget => currentTarget;

    // Методы для отладки
    [ContextMenu("Test Pull")]
    void TestPull()
    {
        if (currentTarget != null)
            StartPull();
        else
            Debug.Log("Нет доступной цели для притягивания");
    }

    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            // Рисуем луч от камеры
            Gizmos.color = Color.yellow;
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward * raycastDistance;
            Gizmos.DrawRay(rayOrigin, rayDirection);
        }

        // Рисуем дистанцию остановки вокруг текущей цели
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget.transform.position, stopDistance);
        }
    }
}
