using UnityEngine;
using Mirror;

public class ObjectMover : NetworkBehaviour
{
    [Header("Movable Objects")]
    [SerializeField] private GameObject[] movableObjects;

    [Header("Settings")]
    [SerializeField] private LayerMask movableLayer = -1;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rayDistance = 100f;

    private Camera playerCamera;
    private GameObject selectedObject;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Plane dragPlane;

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopDrag();
        }
    }

    private void StartDrag()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, movableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Проверяем, находится ли объект в списке перемещаемых
            if (IsMovableObject(hitObject))
            {
                selectedObject = hitObject;
                isDragging = true;

                // Создаем плоскость для перемещения (параллельную камере)
                dragPlane = new Plane(playerCamera.transform.forward, hit.point);

                // Вычисляем смещение между точкой клика и центром объекта
                dragOffset = selectedObject.transform.position - hit.point;

                Debug.Log($"Started dragging: {selectedObject.name}");
            }
        }
    }

    private void UpdateDrag()
    {
        if (selectedObject == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        float distance;

        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance) + dragOffset;

            // Отправляем команду на сервер для перемещения объекта
            CmdMoveObject(selectedObject, worldPosition);
        }
    }

    private void StopDrag()
    {
        if (isDragging)
        {
            Debug.Log($"Stopped dragging: {selectedObject?.name}");
            isDragging = false;
            selectedObject = null;
        }
    }

    private bool IsMovableObject(GameObject obj)
    {
        foreach (GameObject movableObj in movableObjects)
        {
            if (movableObj == obj)
                return true;
        }
        return false;
    }

    [Command]
    private void CmdMoveObject(GameObject targetObject, Vector3 newPosition)
    {
        if (targetObject == null) return;

        // Проверяем, что объект действительно можно перемещать
        if (IsMovableObject(targetObject))
        {
            // Перемещаем объект на сервере
            targetObject.transform.position = newPosition;

            // Синхронизируем с клиентами
            RpcMoveObject(targetObject, newPosition);
        }
    }

    [ClientRpc]
    private void RpcMoveObject(GameObject targetObject, Vector3 newPosition)
    {
        if (targetObject != null)
        {
            targetObject.transform.position = newPosition;
        }
    }
}

