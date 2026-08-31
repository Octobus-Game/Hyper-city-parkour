using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class HostDisconnectManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject playerListPrefab; // Префаб для отображения игрока в списке
    public Transform playerListParent; // Родительский объект для списка игроков
    public Button disconnectAllButton; // Кнопка для отключения всех игроков
    public Button refreshListButton; // Кнопка для обновления списка
    public Text statusText; // Текст для отображения статуса

    [Header("Settings")]
    public bool showOnlyAsHost = true; // Показывать только для хоста
    public float refreshInterval = 2f; // Интервал автообновления списка

    // Словарь для хранения UI элементов игроков
    private Dictionary<NetworkConnectionToClient, GameObject> playerListItems = new Dictionary<NetworkConnectionToClient, GameObject>();

    void Start()
    {
        // Настраиваем кнопки
        if (disconnectAllButton != null)
        {
            disconnectAllButton.onClick.AddListener(DisconnectAllPlayers);
        }

        if (refreshListButton != null)
        {
            refreshListButton.onClick.AddListener(RefreshPlayerList);
        }

        // Автообновление списка игроков
        InvokeRepeating(nameof(RefreshPlayerList), 1f, refreshInterval);
    }

    void Update()
    {
        // Показываем UI только для хоста
        bool isHost = NetworkServer.active && NetworkClient.isConnected;

        if (showOnlyAsHost)
        {
            gameObject.SetActive(isHost);
        }

        // Обновляем статус
        if (statusText != null)
        {
            if (isHost)
            {
                int playerCount = NetworkServer.connections.Count;
                statusText.text = $"Host Mode - Players: {playerCount}";
            }
            else
            {
                statusText.text = "Not Host";
            }
        }
    }

    void RefreshPlayerList()
    {
        // Работает только на сервере
        if (!NetworkServer.active) return;

        // Очищаем старый список
        ClearPlayerList();

        // Создаем элементы для каждого подключенного игрока
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                CreatePlayerListItem(conn);
            }
        }
    }

    void ClearPlayerList()
    {
        foreach (var item in playerListItems.Values)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        playerListItems.Clear();
    }

    void CreatePlayerListItem(NetworkConnectionToClient conn)
    {
        if (playerListPrefab == null || playerListParent == null) return;

        GameObject playerItem = Instantiate(playerListPrefab, playerListParent);

        // Настраиваем компонент элемента списка игроков
        PlayerListItem listItem = playerItem.GetComponent<PlayerListItem>();
        if (listItem != null)
        {
            listItem.Initialize(conn, this);
        }

        playerListItems[conn] = playerItem;
    }

    // Метод для отключения конкретного игрока
    [Server]
    public void DisconnectPlayer(NetworkConnectionToClient conn)
    {
        if (conn != null)
        {
            Debug.Log($"Host disconnecting player: {conn.connectionId}");

            // Отправляем сообщение игроку о том, что его отключает хост
            conn.Send(new DisconnectMessage { reason = "Disconnected by host" });

            // Отключаем игрока
            conn.Disconnect();

            // Обновляем список
            RefreshPlayerList();
        }
    }

    // Метод для отключения всех игроков
    [Server]
    public void DisconnectAllPlayers()
    {
        if (!NetworkServer.active) return;

        Debug.Log("Host disconnecting all players");

        // Получаем копию списка соединений
        var connections = NetworkServer.connections.Values.ToList();

        foreach (var conn in connections)
        {
            if (conn != null && conn.connectionId != 0) // Не отключаем хоста (connectionId = 0)
            {
                conn.Send(new DisconnectMessage { reason = "All players disconnected by host" });
                conn.Disconnect();
            }
        }

        // Обновляем список
        RefreshPlayerList();
    }

    // Метод для кика игрока с причиной
    [Server]
    public void KickPlayer(NetworkConnectionToClient conn, string reason = "Kicked by host")
    {
        if (conn != null)
        {
            Debug.Log($"Host kicking player {conn.connectionId}: {reason}");

            conn.Send(new DisconnectMessage { reason = reason });
            conn.Disconnect();

            RefreshPlayerList();
        }
    }

    void OnDestroy()
    {
        // Очищаем список при уничтожении
        ClearPlayerList();
    }
}

// Дополнительный класс для элементов списка игроков
public class PlayerListItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Text playerNameText;
    public Text playerIdText;
    public Button kickButton;
    public Button banButton; // Для будущего функционала бана

    private NetworkConnectionToClient playerConnection;
    private HostDisconnectManager hostManager;

    public void Initialize(NetworkConnectionToClient conn, HostDisconnectManager manager)
    {
        playerConnection = conn;
        hostManager = manager;

        // Настраиваем отображение информации об игроке
        if (playerNameText != null)
        {
            string playerName = conn.identity != null ? conn.identity.name : "Unknown";
            playerNameText.text = $"Player: {playerName}";
        }

        if (playerIdText != null)
        {
            playerIdText.text = $"ID: {conn.connectionId}";
        }

        // Настраиваем кнопку кика
        if (kickButton != null)
        {
            kickButton.onClick.AddListener(KickThisPlayer);
        }

        if (banButton != null)
        {
            banButton.onClick.AddListener(BanThisPlayer);
        }
    }

    void KickThisPlayer()
    {
        if (hostManager != null && playerConnection != null)
        {
            hostManager.KickPlayer(playerConnection, "Kicked by host");
        }
    }

    void BanThisPlayer()
    {
        if (hostManager != null && playerConnection != null)
        {
            // Здесь можно добавить логику бана (сохранение в PlayerPrefs, файл и т.д.)
            hostManager.KickPlayer(playerConnection, "Banned by host");
        }
    }
}

// Сообщение для уведомления игрока о причине отключения
public struct DisconnectMessage : NetworkMessage
{
    public string reason;
}