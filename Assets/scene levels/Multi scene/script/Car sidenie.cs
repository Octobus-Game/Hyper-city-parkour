using UnityEngine;
using Mirror;

// Скрипт для объекта, к которому можно прилипнуть
public class AttachableObject : NetworkBehaviour
{
    [Header("Attachment Settings")]
    public Transform attachPoint; // Точка прикрепления (центр объекта)
    public float attachDistance = 2f; // Дистанция активации триггера
    public KeyCode attachKey = KeyCode.F;

    [Header("Camera Settings")]
    public Transform cameraPoint; // Точка для позиции камеры
    public Vector3 cameraOffset = new Vector3(0, 2, -3); // Смещение камеры от объекта
    public bool useObjectCamera = true; // Использовать камеру объекта

    [Header("UI")]
    public GameObject interactionUI; // UI подсказка "Нажмите F"

    private PlayerController attachedPlayer;
    private bool playerInRange = false;

    void Start()
    {
        // Если точка прикрепления не задана, используем центр объекта
        if (attachPoint == null)
            attachPoint = transform;

        // Если точка камеры не задана, создаем её
        if (cameraPoint == null)
        {
            GameObject cameraObj = new GameObject("CameraPoint");
            cameraObj.transform.SetParent(transform);
            cameraObj.transform.localPosition = cameraOffset;
            cameraPoint = cameraObj.transform;
        }

        // Создаем триггер коллайдер если его нет
        if (GetComponent<Collider>() == null)
        {
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = attachDistance;
        }

        if (interactionUI != null)
            interactionUI.SetActive(false);
    }

    void Update()
    {
        // Проверяем нажатие F только для локального игрока
        if (playerInRange && Input.GetKeyDown(attachKey))
        {
            PlayerController localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();
            if (localPlayer != null)
            {
                if (attachedPlayer == null)
                {
                    // Прилепляем игрока
                    localPlayer.CmdAttachToObject(netId);
                }
                else if (attachedPlayer == localPlayer)
                {
                    // Отлепляем игрока
                    localPlayer.CmdDetachFromObject();
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.isLocalPlayer)
        {
            playerInRange = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.isLocalPlayer)
        {
            playerInRange = false;
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }
    }

    [Server]
    public void AttachPlayer(PlayerController player)
    {
        if (attachedPlayer == null)
        {
            attachedPlayer = player;
            player.AttachToObject(this);
            RpcOnPlayerAttached(player.netId);
        }
    }

    [Server]
    public void DetachPlayer()
    {
        if (attachedPlayer != null)
        {
            PlayerController player = attachedPlayer;
            attachedPlayer = null;
            player.DetachFromObject();
            RpcOnPlayerDetached();
        }
    }

    [ClientRpc]
    void RpcOnPlayerAttached(uint playerNetId)
    {
        // Визуальные эффекты при прикреплении
        Debug.Log($"Player {playerNetId} attached to {gameObject.name}");
    }

    [ClientRpc]
    void RpcOnPlayerDetached()
    {
        // Визуальные эффекты при отсоединении
        Debug.Log($"Player detached from {gameObject.name}");
    }

    public bool IsPlayerAttached()
    {
        return attachedPlayer != null;
    }

    public Transform GetAttachPoint()
    {
        return attachPoint;
    }

    public Transform GetCameraPoint()
    {
        return cameraPoint;
    }

    public bool UseObjectCamera()
    {
        return useObjectCamera;
    }
}

// Скрипт для игрока
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Camera")]
    public Camera playerCamera; // Камера игрока
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("Attachment")]
    public bool canMove = true;

    [SyncVar]
    private bool isAttached = false;

    [SyncVar]
    private uint attachedObjectId;

    private AttachableObject currentAttachedObject;
    private Rigidbody rb;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Камера
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;
    private bool usingObjectCamera = false;
    private float verticalRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Находим камеру если не назначена
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        // Сохраняем оригинальные параметры камеры
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
            originalCameraParent = playerCamera.transform.parent;
        }

        // Курсор для управления камерой
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Управление камерой
        if (playerCamera != null)
        {
            HandleCameraInput();
        }

        if (canMove && !isAttached)
        {
            HandleMovement();
        }

        // Синхронизация позиции с прикрепленным объектом
        if (isAttached && currentAttachedObject != null)
        {
            SyncWithAttachedObject();
        }
    }

    void HandleCameraInput()
    {
        // Управление мышью
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (usingObjectCamera && currentAttachedObject != null)
        {
            // Свободная камера при просмотре с объекта
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            playerCamera.transform.parent.Rotate(Vector3.up * mouseX);
        }
        else if (!isAttached)
        {
            // Обычное управление камерой игрока
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed;

        if (rb != null)
        {
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);

            // Прыжок
            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }

    void SyncWithAttachedObject()
    {
        if (currentAttachedObject != null)
        {
            Transform attachPoint = currentAttachedObject.GetAttachPoint();
            transform.position = attachPoint.position;
            transform.rotation = attachPoint.rotation;

            // Останавливаем физику пока прикреплены
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }

    [Command]
    public void CmdAttachToObject(uint objectNetId)
    {
        GameObject obj = NetworkServer.spawned[objectNetId].gameObject;
        AttachableObject attachable = obj.GetComponent<AttachableObject>();

        if (attachable != null && !attachable.IsPlayerAttached())
        {
            attachable.AttachPlayer(this);
        }
    }

    [Command]
    public void CmdDetachFromObject()
    {
        if (currentAttachedObject != null)
        {
            currentAttachedObject.DetachPlayer();
        }
    }

    [Server]
    public void AttachToObject(AttachableObject obj)
    {
        isAttached = true;
        attachedObjectId = obj.netId;
        currentAttachedObject = obj;

        // Сохраняем оригинальную позицию
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // Перемещаем к точке прикрепления
        Transform attachPoint = obj.GetAttachPoint();
        transform.position = attachPoint.position;
        transform.rotation = attachPoint.rotation;

        // Отключаем физику
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        RpcOnAttached(obj.netId);
    }

    [Server]
    public void DetachFromObject()
    {
        isAttached = false;
        attachedObjectId = 0;
        currentAttachedObject = null;

        // Включаем физику обратно
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        RpcOnDetached();
    }

    [ClientRpc]
    void RpcOnAttached(uint objectNetId)
    {
        if (!isServer)
        {
            GameObject obj = NetworkClient.spawned[objectNetId].gameObject;
            currentAttachedObject = obj.GetComponent<AttachableObject>();
        }

        // Переключаем камеру для локального игрока
        if (isLocalPlayer)
        {
            SwitchToObjectCamera();
        }

        Debug.Log($"Player attached to object {objectNetId}");
    }

    [ClientRpc]
    void RpcOnDetached()
    {
        if (!isServer)
        {
            currentAttachedObject = null;
        }

        // Возвращаем камеру для локального игрока
        if (isLocalPlayer)
        {
            SwitchToPlayerCamera();
        }

        Debug.Log("Player detached from object");
    }

    void SwitchToObjectCamera()
    {
        if (playerCamera != null && currentAttachedObject != null &&
            currentAttachedObject.UseObjectCamera())
        {
            usingObjectCamera = true;

            // Перемещаем камеру к точке камеры объекта
            Transform cameraPoint = currentAttachedObject.GetCameraPoint();
            if (cameraPoint != null)
            {
                playerCamera.transform.SetParent(cameraPoint);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
                verticalRotation = 0f;
            }
        }
    }

    void SwitchToPlayerCamera()
    {
        if (playerCamera != null && usingObjectCamera)
        {
            usingObjectCamera = false;

            // Возвращаем камеру к игроку
            playerCamera.transform.SetParent(originalCameraParent);
            playerCamera.transform.localPosition = originalCameraPosition;
            playerCamera.transform.localRotation = originalCameraRotation;
            verticalRotation = 0f;
        }
    }

    public bool IsAttached()
    {
        return isAttached;
    }

    public bool IsUsingObjectCamera()
    {
        return usingObjectCamera;
    }

    // Метод для принудительного переключения камеры (если нужно)
    public void ToggleCameraView()
    {
        if (isAttached && currentAttachedObject != null)
        {
            if (usingObjectCamera)
            {
                SwitchToPlayerCamera();
            }
            else
            {
                SwitchToObjectCamera();
            }
        }
    }
}

// Дополнительный скрипт для управления камерой объекта
public class ObjectCameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public bool allowCameraMovement = true;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    [Header("Camera Limits")]
    public float minVerticalAngle = -60f;
    public float maxVerticalAngle = 60f;

    private Camera attachedCamera;
    private Vector3 originalPosition;
    private float currentZoom;
    private float verticalRotation;
    private float horizontalRotation;

    void Start()
    {
        originalPosition = transform.localPosition;
        currentZoom = originalPosition.magnitude;

        // Ограничиваем zoom в пределах
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    void Update()
    {
        if (attachedCamera != null && allowCameraMovement)
        {
            HandleCameraControls();
        }
    }

    void HandleCameraControls()
    {
        // Зум колесиком мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            // Обновляем позицию камеры
            Vector3 direction = transform.localPosition.normalized;
            transform.localPosition = direction * currentZoom;
        }

        // Дополнительное управление камерой (опционально)
        if (Input.GetKey(KeyCode.LeftControl))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            horizontalRotation += mouseX;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);

            transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
        }
    }

    public void SetAttachedCamera(Camera camera)
    {
        attachedCamera = camera;
    }

    public void ResetCamera()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = Quaternion.identity;
        currentZoom = originalPosition.magnitude;
        verticalRotation = 0f;
        horizontalRotation = 0f;
    }
}

// Улучшенный скрипт для плавного перемещения игрока к точке прикрепления
public class SmoothAttachment : MonoBehaviour
{
    public static void MoveToPosition(Transform target, Vector3 targetPosition,
        Quaternion targetRotation, float duration = 0.5f)
    {
        target.GetComponent<MonoBehaviour>().StartCoroutine(
            MoveCoroutine(target, targetPosition, targetRotation, duration));
    }

    private static System.Collections.IEnumerator MoveCoroutine(Transform target,
        Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPosition = target.position;
        Quaternion startRotation = target.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Используем плавную кривую
            t = Mathf.SmoothStep(0f, 1f, t);

            target.position = Vector3.Lerp(startPosition, targetPosition, t);
            target.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            yield return null;
        }

        target.position = targetPosition;
        target.rotation = targetRotation;
    }
}