using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MultiplayerPrefabSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Button spawnButton;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Options")]
    [SerializeField] private bool randomPosition = false;
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private bool onlyOwnerCanSpawn = true;
    [SerializeField] private int maxSpawnedObjects = 10; // Ограничение количества объектов

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private int currentSpawnedCount = 0;

    private void Start()
    {
        ValidateComponents();

        // Вешаем обработчик только если это клиент
        if (isClient)
        {
            spawnButton.onClick.AddListener(RequestSpawn);
        }

        UpdateButtonState();
    }

    private void ValidateComponents()
    {
        if (spawnButton == null)
        {
            Debug.LogError("Spawn Button не назначена!");
            return;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("Префаб для спавна не назначен!");
            return;
        }

        // Проверяем, есть ли у префаба NetworkIdentity
        NetworkIdentity networkIdentity = prefabToSpawn.GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
        {
            Debug.LogError($"У префаба {prefabToSpawn.name} отсутствует компонент NetworkIdentity!");
            return;
        }

        // Проверяем, зарегистрирован ли префаб в NetworkManager
        if (NetworkManager.singleton != null && !NetworkManager.singleton.spawnPrefabs.Contains(prefabToSpawn))
        {
            Debug.LogWarning($"Префаб {prefabToSpawn.name} не зарегистрирован в NetworkManager.spawnPrefabs!");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateButtonState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (enableDebugLogs)
            Debug.Log("MultiplayerPrefabSpawner запущен на сервере");
    }

    private void UpdateButtonState()
    {
        if (spawnButton != null)
        {
            bool canSpawn = (!onlyOwnerCanSpawn || isOwned) && (currentSpawnedCount < maxSpawnedObjects);
            spawnButton.interactable = canSpawn;
        }
    }

    private void RequestSpawn()
    {
        if (onlyOwnerCanSpawn && !isOwned)
        {
            Debug.LogWarning("Только владелец может спавнить объект!");
            return;
        }

        if (currentSpawnedCount >= maxSpawnedObjects)
        {
            Debug.LogWarning($"Достигнуто максимальное количество объектов ({maxSpawnedObjects})!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"Клиент {netId} запрашивает спавн объекта");

        // Команда выполняется на сервере
        CmdRequestSpawn(GetClientSpawnPosition());
    }

    [Command(requiresAuthority = true)]
    private void CmdRequestSpawn(Vector3 requestedPosition)
    {
        if (enableDebugLogs)
            Debug.Log($"Сервер получил команду спавна от клиента {connectionToClient.connectionId}");

        if (!ValidateSpawnRequest())
            return;

        Vector3 spawnPosition = ValidateSpawnPosition(requestedPosition);
        Quaternion spawnRotation = GetSpawnRotation();

        // Создаем объект на сервере
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        // ВАЖНО: NetworkServer.Spawn делает объект видимым для всех клиентов
        NetworkServer.Spawn(spawnedObject, connectionToClient);

        // Увеличиваем счетчик заспавненных объектов
        currentSpawnedCount++;

        if (enableDebugLogs)
            Debug.Log($"Сервер заспавнил объект {spawnedObject.name} на позиции {spawnPosition} для всех клиентов. Владелец: {connectionToClient.connectionId}");

        // Уведомляем всех клиентов о спавне (опционально для эффектов)
        RpcOnObjectSpawned(spawnPosition, connectionToClient.connectionId);

        // Обновляем состояние кнопки для владельца
        TargetUpdateSpawnCount(connectionToClient, currentSpawnedCount);
    }

    private bool ValidateSpawnRequest()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("Префаб не назначен!");
            return false;
        }

        if (currentSpawnedCount >= maxSpawnedObjects)
        {
            Debug.LogWarning($"Достигнуто максимальное количество объектов ({maxSpawnedObjects})!");
            return false;
        }

        return true;
    }

    [ClientRpc]
    private void RpcOnObjectSpawned(Vector3 position, int ownerConnectionId)
    {
        if (enableDebugLogs)
            Debug.Log($"Все клиенты получили уведомление о спавне объекта на позиции: {position}, владелец: {ownerConnectionId}");

        // Здесь можно добавить визуальные эффекты для всех клиентов
        // Например, эффект появления, звук и т.д.
        PlaySpawnEffect(position);
    }

    [TargetRpc]
    private void TargetUpdateSpawnCount(NetworkConnection target, int newCount)
    {
        currentSpawnedCount = newCount;
        UpdateButtonState();

        if (enableDebugLogs)
            Debug.Log($"Обновлен счетчик объектов: {currentSpawnedCount}/{maxSpawnedObjects}");
    }

    private void PlaySpawnEffect(Vector3 position)
    {
        // Добавьте здесь код для воспроизведения эффектов
        // Например: партиклы, звуки, анимации
    }

    private Vector3 GetClientSpawnPosition()
    {
        Vector3 basePosition = spawnPoint != null ? spawnPoint.position : transform.position;

        if (randomPosition)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return basePosition + new Vector3(offset.x, 0, offset.y);
        }

        return basePosition;
    }

    private Vector3 ValidateSpawnPosition(Vector3 clientSuggestedPosition)
    {
        // Сервер может валидировать и корректировать позицию
        // Например, проверить на коллизии, границы карты и т.д.

        // Проверка на валидность позиции
        if (Physics.CheckSphere(clientSuggestedPosition, 0.5f))
        {
            // Если место занято, найти свободное поблизости
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                Vector3 newPos = clientSuggestedPosition + new Vector3(offset.x, 0, offset.y);
                if (!Physics.CheckSphere(newPos, 0.5f))
                {
                    return newPos;
                }
            }
        }

        return clientSuggestedPosition;
    }

    private Quaternion GetSpawnRotation()
    {
        // Можно добавить случайный поворот или использовать поворот spawn point'а
        if (spawnPoint != null)
            return spawnPoint.rotation;

        return Quaternion.identity;
    }

    // Метод для очистки при уничтожении объектов (вызывать извне)
    public void OnSpawnedObjectDestroyed()
    {
        if (isServer)
        {
            currentSpawnedCount = Mathf.Max(0, currentSpawnedCount - 1);
            if (enableDebugLogs)
                Debug.Log($"Объект уничтожен, осталось: {currentSpawnedCount}/{maxSpawnedObjects}");
        }
    }

    private void OnDestroy()
    {
        if (spawnButton != null && isClient)
        {
            spawnButton.onClick.RemoveListener(RequestSpawn);
        }
    }

    // Метод для отладки (можно вызывать из инспектора)
    [ContextMenu("Force Spawn (Server Only)")]
    private void ForceSpawn()
    {
        if (!isServer)
        {
            Debug.LogWarning("Этот метод работает только на сервере!");
            return;
        }

        CmdRequestSpawn(GetClientSpawnPosition());
    }
}