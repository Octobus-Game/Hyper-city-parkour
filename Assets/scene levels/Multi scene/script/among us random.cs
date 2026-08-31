using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class MultiplayerRandomPrefab : NetworkBehaviour
{
    [Header("UI Settings")]
    public Button actionButton;
    public GameObject buttonPressedIndicator; // Индикатор того, что кнопка нажата

    [Header("Prefab Settings")]
    public GameObject[] possiblePrefabs; // Массив префабов для случайного выбора
    public Transform spawnPoint; // Точка спавна префаба
    public float prefabLifetime = 5f; // Время жизни префаба (0 = бесконечно)

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Синхронизированные переменные
    [SyncVar] private int totalPlayersCount = 0;
    [SyncVar] private int pressedPlayersCount = 0;
    [SyncVar] private bool allPlayersPressed = false;
    [SyncVar] private uint selectedPlayerNetId = 0;
    [SyncVar] private int selectedPrefabIndex = -1;

    // Локальные переменные
    private bool hasPressed = false;
    private Dictionary<uint, bool> playerPressedStatus = new Dictionary<uint, bool>();
    private GameObject currentSpawnedPrefab;

    void Start()
    {
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnButtonPressed);
        }

        // Инициализация для сервера
        if (isServer)
        {
            UpdateTotalPlayersCount();
            InvokeRepeating(nameof(UpdateTotalPlayersCount), 1f, 1f);
        }

        UpdateButtonState();
    }

    void Update()
    {
        if (showDebugInfo)
        {
            Debug.Log($"Players: {pressedPlayersCount}/{totalPlayersCount}, All pressed: {allPlayersPressed}");
        }
    }

    [Server]
    void UpdateTotalPlayersCount()
    {
        totalPlayersCount = NetworkServer.connections.Count;
    }

    public void OnButtonPressed()
    {
        if (hasPressed || allPlayersPressed)
        {
            Debug.Log("Button already pressed or all players have pressed!");
            return;
        }

        hasPressed = true;
        CmdPlayerPressedButton();
        UpdateButtonState();
    }

    [Command]
    void CmdPlayerPressedButton()
    {
        if (playerPressedStatus.ContainsKey(netId))
        {
            Debug.Log($"Player {netId} already pressed the button!");
            return;
        }

        playerPressedStatus[netId] = true;
        pressedPlayersCount = playerPressedStatus.Count;

        Debug.Log($"Player {netId} pressed button. Total pressed: {pressedPlayersCount}/{totalPlayersCount}");

        // Проверяем, нажали ли все игроки
        if (pressedPlayersCount >= totalPlayersCount && totalPlayersCount > 0)
        {
            allPlayersPressed = true;
            SelectRandomPlayerAndPrefab();
        }

        RpcUpdateButtonStates();
    }

    [Server]
    void SelectRandomPlayerAndPrefab()
    {
        if (possiblePrefabs.Length == 0)
        {
            Debug.LogError("No prefabs available for spawning!");
            return;
        }

        // Выбираем случайного игрока из тех, кто нажал кнопку
        var playerIds = playerPressedStatus.Keys.ToList();
        selectedPlayerNetId = playerIds[Random.Range(0, playerIds.Count)];

        // Выбираем случайный префаб
        selectedPrefabIndex = Random.Range(0, possiblePrefabs.Length);

        Debug.Log($"Selected player: {selectedPlayerNetId}, prefab index: {selectedPrefabIndex}");

        // Уведомляем всех клиентов о результате
        RpcAnnounceResult(selectedPlayerNetId, selectedPrefabIndex);

        // Спавним префаб для выбранного игрока
        SpawnPrefabForPlayer(selectedPlayerNetId);
    }

    [Server]
    void SpawnPrefabForPlayer(uint targetPlayerNetId)
    {
        if (selectedPrefabIndex < 0 || selectedPrefabIndex >= possiblePrefabs.Length)
        {
            Debug.LogError("Invalid prefab index!");
            return;
        }

        // Находим игрока по NetId
        NetworkConnectionToClient targetConnection = null;
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity.netId == targetPlayerNetId)
            {
                targetConnection = conn;
                break;
            }
        }

        if (targetConnection == null)
        {
            Debug.LogError($"Could not find player with NetId: {targetPlayerNetId}");
            return;
        }

        // Определяем позицию спавна
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : targetConnection.identity.transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Спавним префаб
        GameObject spawnedPrefab = Instantiate(possiblePrefabs[selectedPrefabIndex], spawnPosition, spawnRotation);
        NetworkServer.Spawn(spawnedPrefab);

        // Уничтожаем префаб через определенное время, если указано
        if (prefabLifetime > 0)
        {
            Destroy(spawnedPrefab, prefabLifetime);
        }

        Debug.Log($"Spawned prefab for player {targetPlayerNetId}");
    }

    [ClientRpc]
    void RpcUpdateButtonStates()
    {
        UpdateButtonState();
    }

    [ClientRpc]
    void RpcAnnounceResult(uint winnerNetId, int prefabIndex)
    {
        string prefabName = prefabIndex >= 0 && prefabIndex < possiblePrefabs.Length ?
            possiblePrefabs[prefabIndex].name : "Unknown";

        if (netId == winnerNetId)
        {
            Debug.Log($"You won! Prefab '{prefabName}' will be spawned for you!");
        }
        else
        {
            Debug.Log($"Player {winnerNetId} won and got prefab '{prefabName}'!");
        }
    }

    void UpdateButtonState()
    {
        if (actionButton != null)
        {
            actionButton.interactable = !hasPressed && !allPlayersPressed;
        }

        if (buttonPressedIndicator != null)
        {
            buttonPressedIndicator.SetActive(hasPressed);
        }
    }

    [Server]
    public void ResetSystem()
    {
        playerPressedStatus.Clear();
        pressedPlayersCount = 0;
        allPlayersPressed = false;
        selectedPlayerNetId = 0;
        selectedPrefabIndex = -1;

        RpcResetClients();
    }

    [ClientRpc]
    void RpcResetClients()
    {
        hasPressed = false;
        UpdateButtonState();
    }

    // Метод для вызова сброса системы (можно привязать к другой кнопке)
    public void OnResetButtonPressed()
    {
        if (isServer)
        {
            ResetSystem();
        }
        else
        {
            CmdRequestReset();
        }
    }

    [Command]
    void CmdRequestReset()
    {
        ResetSystem();
    }

    void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnButtonPressed);
        }
    }
}