using UnityEngine;

public class ObjectMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public bool smoothMovement = true;
    public float smoothTime = 0.1f;

    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        HandleInput();

        if (smoothMovement)
        {
            // Плавное перемещение
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else
        {
            // Мгновенное перемещение
            transform.position = targetPosition;
        }
    }

    void HandleInput()
    {
        Vector3 movement = Vector3.zero;

        // Получаем ввод от клавиш-стрелок
        if (Input.GetKey(KeyCode.UpArrow))
        {
            movement += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            movement += Vector3.back;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            movement += Vector3.left;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            movement += Vector3.right;
        }

        // Нормализуем вектор для диагонального движения
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }

        // Обновляем целевую позицию
        if (smoothMovement)
        {
            targetPosition += movement * moveSpeed * Time.deltaTime;
        }
        else
        {
            targetPosition += movement * moveSpeed * Time.deltaTime;
        }
    }
}

// Альтернативный вариант с дискретным движением (по клеткам)
public class GridMovement : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1f;
    public float moveSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        if (!isMoving)
        {
            HandleGridInput();
        }

        // Плавное движение к целевой позиции
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            isMoving = true;
        }
        else
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    void HandleGridInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            targetPosition += Vector3.forward * gridSize;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            targetPosition += Vector3.back * gridSize;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            targetPosition += Vector3.left * gridSize;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            targetPosition += Vector3.right * gridSize;
        }
    }
}