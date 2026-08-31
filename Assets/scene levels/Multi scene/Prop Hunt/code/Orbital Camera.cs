using UnityEngine;

public class OrbitalCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Цель для орбиты

    [Header("Distance Settings")]
    public float distance = 10f;
    public float minDistance = 2f;
    public float maxDistance = 20f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 2f;
    public float scrollSensitivity = 2f;

    [Header("Angle Limits")]
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    [Header("Smoothing")]
    public float rotationSmoothing = 5f;
    public float zoomSmoothing = 5f;

    [Header("Input Settings")]
    public KeyCode orbitKey = KeyCode.Mouse0; // ЛКМ для орбиты
    public bool invertY = false;

    private float currentX = 0f;
    private float currentY = 0f;
    private float targetDistance;
    private Vector3 targetPosition;

    void Start()
    {
        // Если цель не задана, попробуем найти GameObject с тегом "Player"
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // Инициализация начальных углов
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        targetDistance = distance;

        // Если есть цель, позиционируем камеру
        if (target != null)
        {
            UpdateCameraPosition();
        }
    }

    void Update()
    {
        if (target == null) return;

        HandleInput();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        // Обработка вращения мышью
        if (Input.GetKey(orbitKey))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentX += mouseX;
            currentY -= invertY ? -mouseY : mouseY;

            // Ограничиваем вертикальный угол
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        }

        // Обработка зума колесом мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            targetDistance -= scroll * scrollSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // Альтернативное управление зумом клавишами
        if (Input.GetKey(KeyCode.Q))
            targetDistance = Mathf.Clamp(targetDistance + Time.deltaTime * scrollSensitivity, minDistance, maxDistance);
        if (Input.GetKey(KeyCode.E))
            targetDistance = Mathf.Clamp(targetDistance - Time.deltaTime * scrollSensitivity, minDistance, maxDistance);
    }

    void UpdateCameraPosition()
    {
        // Плавное приближение к целевому расстоянию
        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * zoomSmoothing);

        // Вычисляем желаемое вращение
        Quaternion targetRotation = Quaternion.Euler(currentY, currentX, 0);

        // Плавное вращение
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);

        // Позиционируем камеру на орбите вокруг цели
        Vector3 offset = transform.rotation * Vector3.back * distance;
        targetPosition = target.position + offset;

        // Применяем позицию
        transform.position = targetPosition;

        // Убеждаемся, что камера смотрит на цель
        transform.LookAt(target.position);
    }

    // Метод для установки новой цели
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            UpdateCameraPosition();
        }
    }

    // Метод для установки расстояния
    public void SetDistance(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    // Метод для сброса камеры в позицию по умолчанию
    public void ResetCamera()
    {
        currentX = 0f;
        currentY = 0f;
        targetDistance = 10f;
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // Рисуем сферу вокруг цели для визуализации орбиты
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position, distance);

            // Рисуем линию от камеры к цели
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}