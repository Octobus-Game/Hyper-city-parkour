using UnityEngine;

/// <summary>
/// Простой FPS-контроллер для тестирования порталов.
/// Прикрепляется на игрока вместе с CharacterController и PortalTeleporter.
///
/// Управление:
///   WASD / Стрелки — движение
///   Мышь          — поворот камеры
///   Пробел         — прыжок
///   Escape         — освободить курсор
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PortalTeleporter))]
public class PortalPlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed   = 5f;
    public float jumpHeight  = 1.5f;
    public float gravity     = -20f;

    [Header("Камера")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxPitchAngle    = 85f;

    // ── Приватные поля ──────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3             _velocity;
    private float               _pitchAngle;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        // Автоматически ищем камеру среди дочерних объектов
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }

        // Прячем и фиксируем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCursorLock();
    }

    // ── Приватные методы ─────────────────────────────────────────────

    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Поворот тела по горизонтали
        transform.Rotate(Vector3.up * mouseX);

        // Наклон камеры по вертикали (с ограничением)
        _pitchAngle -= mouseY;
        _pitchAngle  = Mathf.Clamp(_pitchAngle, -maxPitchAngle, maxPitchAngle);
        cameraTransform.localRotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool grounded = _cc.isGrounded;

        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f; // Небольшое давление к земле

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        _cc.Move(move * moveSpeed * Time.deltaTime);

        // Прыжок
        if (Input.GetButtonDown("Jump") && grounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Гравитация
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
