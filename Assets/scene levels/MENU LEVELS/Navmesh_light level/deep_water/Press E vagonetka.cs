using UnityEngine;
using System.Collections.Generic;

// Компонент для создания сплайн-пути
[System.Serializable]
public class SplinePoint : MonoBehaviour 
{
    public Vector3 position;
    public Vector3 tangent;

    public SplinePoint(Vector3 pos)
    {
        position = pos;
        tangent = Vector3.forward;
    }
}

public class SplinePath : MonoBehaviour
{
    [Header("Spline Settings")]
    public List<SplinePoint> points = new List<SplinePoint>();
    public bool isLoop = false;
    public Color gizmoColor = Color.yellow;

    [Header("Debug")]
    public bool showGizmos = true;
    public int gizmoResolution = 50;

    private void Start()
    {
        // Создаем базовые точки если их нет
        if (points.Count == 0)
        {
            points.Add(new SplinePoint(transform.position));
            points.Add(new SplinePoint(transform.position + Vector3.forward * 10));
        }
    }

    // Получить позицию на сплайне (t от 0 до 1)
    public Vector3 GetPosition(float t)
    {
        if (points.Count < 2) return transform.position;

        t = Mathf.Clamp01(t);

        // Определяем между какими точками мы находимся
        float scaledT = t * (points.Count - 1);
        int index = Mathf.FloorToInt(scaledT);
        float localT = scaledT - index;

        if (index >= points.Count - 1)
        {
            if (isLoop && points.Count > 2)
            {
                return CubicBezier(points[points.Count - 1].position,
                                  points[points.Count - 1].position + points[points.Count - 1].tangent,
                                  points[0].position - points[0].tangent,
                                  points[0].position, localT);
            }
            return points[points.Count - 1].position;
        }

        Vector3 p0 = points[index].position;
        Vector3 p1 = points[index].position + points[index].tangent;
        Vector3 p2 = points[index + 1].position - points[index + 1].tangent;
        Vector3 p3 = points[index + 1].position;

        return CubicBezier(p0, p1, p2, p3, localT);
    }

    // Получить направление на сплайне
    public Vector3 GetDirection(float t)
    {
        float delta = 0.01f;
        Vector3 pos1 = GetPosition(t - delta);
        Vector3 pos2 = GetPosition(t + delta);
        return (pos2 - pos1).normalized;
    }

    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || points.Count < 2) return;

        Gizmos.color = gizmoColor;

        // Рисуем сплайн
        Vector3 prevPos = GetPosition(0);
        for (int i = 1; i <= gizmoResolution; i++)
        {
            float t = (float)i / gizmoResolution;
            Vector3 currentPos = GetPosition(t);
            Gizmos.DrawLine(prevPos, currentPos);
            prevPos = currentPos;
        }

        // Рисуем точки
        Gizmos.color = Color.red;
        foreach (var point in points)
        {
            Gizmos.DrawSphere(point.position, 0.3f);
        }
    }
}

// Система вагонетки
public class MinecartSystem : MonoBehaviour
{
    [Header("Movement")]
    public SplinePath splinePath;
    public float speed = 5f;
    public float acceleration = 2f;
    public float deceleration = 3f;
    public bool autoMove = false;

    [Header("Player Attachment")]
    public Transform playerSeat;
    public float attachDistance = 3f;
    public LayerMask playerLayer = 1;

    [Header("Physics")]
    public float gravity = 9.81f;
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer = 1;

    private float currentSpeed = 0f;
    private float splineProgress = 0f;
    private bool hasPlayer = false;
    private CharacterController attachedPlayer;
    private Vector3 lastPosition;
    private bool isGrounded = true;

    // События
    public System.Action<CharacterController> OnPlayerAttached;
    public System.Action<CharacterController> OnPlayerDetached;

    private void Start()
    {
        if (splinePath == null)
            splinePath = GetComponent<SplinePath>();

        if (playerSeat == null)
            playerSeat = transform;

        lastPosition = transform.position;
    }

    private void Update()
    {
        CheckForPlayer();
        HandleMovement();
        UpdatePosition();
        HandlePlayerAttachment();
    }

    private void CheckForPlayer()
    {
        if (hasPlayer) return;

        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, attachDistance, playerLayer);

        foreach (var col in nearbyObjects)
        {
            CharacterController player = col.GetComponent<CharacterController>();
            if (player != null)
            {
                PlayerInput playerInput = player.GetComponent<PlayerInput>();
                if (playerInput != null && playerInput.GetInteractInput())
                {
                    AttachPlayer(player);
                    break;
                }
            }
        }
    }

    private void HandleMovement()
    {
        if (!hasPlayer && !autoMove)
        {
            // Замедляемся если нет игрока
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            return;
        }

        float targetSpeed = speed;

        if (hasPlayer)
        {
            PlayerInput playerInput = attachedPlayer.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                // Проверяем ввод для отцепления
                if (playerInput.GetInteractInput())
                {
                    DetachPlayer();
                    return;
                }

                // Управление скоростью
                Vector2 moveInput = playerInput.GetMovementInput();
                if (moveInput.y > 0.1f)
                    targetSpeed = speed * 1.5f; // Ускорение
                else if (moveInput.y < -0.1f)
                    targetSpeed = speed * 0.3f; // Замедление
            }
        }

        // Плавное изменение скорости
        if (currentSpeed < targetSpeed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
    }

    private void UpdatePosition()
    {
        if (splinePath == null || currentSpeed <= 0.01f) return;

        // Обновляем прогресс по сплайну
        float pathLength = EstimatePathLength();
        float deltaProgress = (currentSpeed * Time.deltaTime) / pathLength;
        splineProgress += deltaProgress;

        if (splinePath.isLoop)
        {
            splineProgress = splineProgress % 1f;
        }
        else
        {
            splineProgress = Mathf.Clamp01(splineProgress);
            if (splineProgress >= 1f)
                currentSpeed = 0f;
        }

        // Получаем новую позицию и поворот
        Vector3 newPosition = splinePath.GetPosition(splineProgress);
        Vector3 direction = splinePath.GetDirection(splineProgress);

        // Проверяем землю под вагонеткой
        CheckGround(newPosition);

        // Применяем гравитацию если не на земле
        if (!isGrounded)
        {
            newPosition.y -= gravity * Time.deltaTime;
        }

        // Обновляем позицию и поворот
        transform.position = newPosition;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        lastPosition = newPosition;
    }

    private void CheckGround(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        isGrounded = Physics.Raycast(ray, groundCheckDistance, groundLayer);
    }

    private float EstimatePathLength()
    {
        float length = 0f;
        Vector3 prevPos = splinePath.GetPosition(0);

        for (int i = 1; i <= 100; i++)
        {
            float t = (float)i / 100f;
            Vector3 currentPos = splinePath.GetPosition(t);
            length += Vector3.Distance(prevPos, currentPos);
            prevPos = currentPos;
        }

        return length;
    }

    private void HandlePlayerAttachment()
    {
        if (!hasPlayer || attachedPlayer == null) return;

        // Перемещаем игрока вместе с вагонеткой
        Vector3 seatWorldPos = playerSeat.position;
        Vector3 deltaMovement = seatWorldPos - attachedPlayer.transform.position;

        // Используем Character Controller для перемещения
        attachedPlayer.Move(deltaMovement);

        // Синхронизируем поворот игрока с вагонеткой
        attachedPlayer.transform.rotation = playerSeat.rotation;
    }

    public void AttachPlayer(CharacterController player)
    {
        if (hasPlayer) return;

        attachedPlayer = player;
        hasPlayer = true;

        // Отключаем обычное управление игрока
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.SetMinecartMode(true);

        // Позиционируем игрока на сиденье
        Vector3 seatWorldPos = playerSeat.position;
        Vector3 deltaMovement = seatWorldPos - player.transform.position;
        player.Move(deltaMovement);

        OnPlayerAttached?.Invoke(player);
        Debug.Log("Player attached to minecart");
    }

    public void DetachPlayer()
    {
        if (!hasPlayer || attachedPlayer == null) return;

        // Включаем обратно обычное управление игрока
        PlayerMovement playerMovement = attachedPlayer.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.SetMinecartMode(false);

        OnPlayerDetached?.Invoke(attachedPlayer);
        Debug.Log("Player detached from minecart");

        attachedPlayer = null;
        hasPlayer = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Показываем радиус прикрепления
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attachDistance);

        // Показываем сиденье
        if (playerSeat != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(playerSeat.position, Vector3.one * 0.5f);
        }
    }
}

// Компонент ввода игрока
public class PlayerInput : MonoBehaviour
{
    private bool interactPressed = false;

    private void Update()
    {
        // Сбрасываем флаг взаимодействия каждый кадр
        interactPressed = Input.GetKeyDown(KeyCode.E);
    }

    public bool GetInteractInput()
    {
        return interactPressed;
    }

    public Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }
}

// Расширенный компонент движения игрока
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = 9.81f;

    [Header("Ground Check")]
    public float groundDistance = 0.4f;
    public LayerMask groundMask = 1;

    private CharacterController controller;
    private PlayerInput playerInput;
    private Transform groundCheck;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isInMinecart = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Создаем объект для проверки земли
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.SetParent(transform);
        groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
        groundCheck = groundCheckObj.transform;
    }

    private void Update()
    {
        if (isInMinecart) return; // Не обрабатываем движение если в вагонетке

        GroundCheck();
        HandleMovement();
        HandleGravity();
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovement()
    {
        Vector2 input = playerInput.GetMovementInput();
        Vector3 move = transform.right * input.x + transform.forward * input.y;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravity);
        }
    }

    private void HandleGravity()
    {
        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void SetMinecartMode(bool inMinecart)
    {
        isInMinecart = inMinecart;

        if (inMinecart)
        {
            velocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}