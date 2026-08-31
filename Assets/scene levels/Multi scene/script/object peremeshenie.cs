using UnityEngine;
using Mirror;

public class AdvancedObjectMover : NetworkBehaviour
{
    [Header("Movable Objects")]
    [SerializeField] private GameObject[] movableObjects;

    [Header("Movement Settings")]
    [SerializeField] private LayerMask movableLayer = -1;
    [SerializeField] private float rayDistance = 100f;
    [SerializeField] private bool constrainToXY = true;
    [SerializeField] private bool useGroundProjection = false;
    [SerializeField] private LayerMask groundLayer = 1;

    [Header("Visual Feedback")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private GameObject dragIndicator;

    private Camera playerCamera;
    private GameObject selectedObject;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Plane dragPlane;
    private Material originalMaterial;
    private Renderer selectedRenderer;

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
        UpdateVisualFeedback();
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

    private void UpdateVisualFeedback()
    {
        if (!isDragging)
        {
            // Подсвечиваем объекты при наведении
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, movableLayer))
            {
                if (IsMovableObject(hit.collider.gameObject))
                {
                    HighlightObject(hit.collider.gameObject);
                }
                else
                {
                    RemoveHighlight();
                }
            }
            else
            {
                RemoveHighlight();
            }
        }
    }

    private void HighlightObject(GameObject obj)
    {
        if (selectedRenderer != null && selectedRenderer.gameObject != obj)
        {
            RemoveHighlight();
        }

        if (selectedRenderer == null || selectedRenderer.gameObject != obj)
        {
            selectedRenderer = obj.GetComponent<Renderer>();
            if (selectedRenderer != null && highlightMaterial != null)
            {
                originalMaterial = selectedRenderer.material;
                selectedRenderer.material = highlightMaterial;
            }
        }
    }

    private void RemoveHighlight()
    {
        if (selectedRenderer != null && originalMaterial != null)
        {
            selectedRenderer.material = originalMaterial;
            selectedRenderer = null;
            originalMaterial = null;
        }
    }

    private void StartDrag()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, movableLayer))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsMovableObject(hitObject))
            {
                selectedObject = hitObject;
                isDragging = true;

                // Создаем плоскость для перемещения
                if (constrainToXY)
                {
                    // Плоскость XY (Z остается неизменным)
                    dragPlane = new Plane(Vector3.forward, selectedObject.transform.position);
                }
                else
                {
                    // Плоскость параллельная камере
                    dragPlane = new Plane(playerCamera.transform.forward, hit.point);
                }

                dragOffset = selectedObject.transform.position - hit.point;

                // Показываем индикатор перемещения
                if (dragIndicator != null)
                {
                    dragIndicator.SetActive(true);
                }
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

            // Проецируем на землю если включено
            if (useGroundProjection)
            {
                RaycastHit groundHit;
                if (Physics.Raycast(worldPosition + Vector3.up * 100, Vector3.down, out groundHit, 200f, groundLayer))
                {
                    worldPosition.y = groundHit.point.y;
                }
            }

            // Обновляем позицию индикатора
            if (dragIndicator != null)
            {
                dragIndicator.transform.position = worldPosition;
            }

            // Отправляем команду на сервер
            CmdMoveObject(selectedObject, worldPosition);
        }
    }

    private void StopDrag()
    {
        if (isDragging)
        {
            isDragging = false;
            selectedObject = null;

            // Скрываем индикатор
            if (dragIndicator != null)
            {
                dragIndicator.SetActive(false);
            }
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

        if (IsMovableObject(targetObject))
        {
            NetworkMovableObject networkObj = targetObject.GetComponent<NetworkMovableObject>();
            if (networkObj != null)
            {
                networkObj.SetNetworkPosition(newPosition);
            }
            else
            {
                // Fallback для обычных объектов
                targetObject.transform.position = newPosition;
                RpcMoveObject(targetObject, newPosition);
            }
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