using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gridSize = 1f;

    [Header("Объект для сбора")]
    [SerializeField] private GameObject targetObject;

    [Header("Настройки змейки")]
    [SerializeField] private GameObject bodySegmentPrefab;

    private Vector3 direction = Vector3.right;
    private Vector3 nextDirection = Vector3.right;
    private float moveTimer;
    private float moveInterval;

    private List<Transform> bodySegments = new List<Transform>();
    private List<Vector3> previousPositions = new List<Vector3>();

    void Start()
    {
        moveInterval = 1f / moveSpeed;
        bodySegments.Add(transform);
        previousPositions.Add(transform.position);
    }

    void Update()
    {
        HandleInput();

        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;
            Move();
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (direction != Vector3.back)
                nextDirection = Vector3.forward;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (direction != Vector3.forward)
                nextDirection = Vector3.back;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (direction != Vector3.right)
                nextDirection = Vector3.left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (direction != Vector3.left)
                nextDirection = Vector3.right;
        }
    }

    void Move()
    {
        direction = nextDirection;

        // Сохраняем предыдущие позиции
        previousPositions.Insert(0, transform.position);
        if (previousPositions.Count > bodySegments.Count)
        {
            previousPositions.RemoveAt(previousPositions.Count - 1);
        }

        // Движение головы
        transform.position += direction * gridSize;

        // Движение тела
        for (int i = 1; i < bodySegments.Count; i++)
        {
            bodySegments[i].position = previousPositions[i];
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект целевым
        if (targetObject != null && other.gameObject == targetObject)
        {
            // Деактивируем объект
            targetObject.SetActive(false);

            // Увеличиваем змейку
            GrowSnake();
        }
    }

    void GrowSnake()
    {
        if (bodySegmentPrefab != null)
        {
            Vector3 spawnPosition = bodySegments[bodySegments.Count - 1].position;
            GameObject newSegment = Instantiate(bodySegmentPrefab, spawnPosition, Quaternion.identity);
            newSegment.transform.parent = transform.parent;
            bodySegments.Add(newSegment.transform);
        }
    }
}