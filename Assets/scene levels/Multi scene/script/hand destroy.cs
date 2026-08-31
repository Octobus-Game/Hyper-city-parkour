using UnityEngine;
using Mirror;

public class ClickToDestroy : NetworkBehaviour
{
    [Header("Click Detection")]
    public LayerMask destructibleLayers = 1; // Слои объектов, которые можно уничтожать
    public float maxClickDistance = 100f;    // Максимальное расстояние клика

    private Camera playerCamera;

    void Start()
    {
        // Получаем камеру игрока
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }

    void Update()
    {
        // Проверяем клик только для локального игрока
        if (!isLocalPlayer) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxClickDistance, destructibleLayers))
        {
            // Проверяем, есть ли у объекта компонент DestructibleObject
            DestructibleObject destructible = hit.collider.GetComponent<DestructibleObject>();

            if (destructible != null && destructible.gameObject.activeInHierarchy)
            {
                // Отправляем команду на сервер для уничтожения
                CmdDestroyClickedObject(destructible.netId);
            }
        }
    }

    [Command]
    void CmdDestroyClickedObject(uint netId)
    {
        // Находим объект по NetworkIdentity ID
        if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity networkIdentity))
        {
            if (networkIdentity != null)
            {
                // Получаем компонент DestructibleObject и уничтожаем
                DestructibleObject destructible = networkIdentity.GetComponent<DestructibleObject>();
                if (destructible != null)
                {
                    

                    // Уничтожаем на сервере
                    NetworkServer.Destroy(networkIdentity.gameObject);
                }
            }
        }
    }
}