using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class PingManager : NetworkBehaviour
{
    [Header("UI Settings")]
    public GameObject pingDisplayPrefab; // Префаб для отображения ping игрока
    public Transform pingDisplayParent; // Родительский объект для ping элементов
    public Text serverPingText; // Текст для отображения ping сервера

    [Header("Display Settings")]
    public bool showAllPlayers = true; // Показывать ping всех игроков
    public bool showServerPing = true; // Показывать ping сервера
    public Vector2 displayOffset = new Vector2(0, -30); // Смещение между элементами

    [Header("Server Settings")]
    public float serverPingUpdateInterval = 2f; // Интервал обновления серверного ping
    public float playerCheckInterval = 1f; // Интервал проверки игроков

    // Словарь для хранения ping дисплеев игроков
    private Dictionary<uint, GameObject> playerPingDisplays = new Dictionary<uint, GameObject>();
    private Dictionary<uint, int> playerPings = new Dictionary<uint, int>();
    private HashSet<uint> lastKnownPlayers = new HashSet<uint>();

    void Start()
    {
        if (isServer)
        {
            // Запускаем обновление серверного ping
            InvokeRepeating(nameof(UpdateServerPing), 1f, serverPingUpdateInterval);
        }

        // Запускаем проверку игроков для всех клиентов
        InvokeRepeating(nameof(CheckForPlayerChanges), 1f, playerCheckInterval);
    }

    void CheckForPlayerChanges()
    {
        HashSet<uint> currentPlayers = new HashSet<uint>();

        // Получаем всех текущих игроков
        PingDisplay[] pingDisplays = FindObjectsOfType<PingDisplay>();
        foreach (PingDisplay display in pingDisplays)
        {
            if (display != null)
            {
                NetworkIdentity identity = display.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    currentPlayers.Add(identity.netId);
                }
            }
        }

        // Проверяем новых игроков
        foreach (uint netId in currentPlayers)
        {
            if (!lastKnownPlayers.Contains(netId))
            {
                OnPlayerJoined(netId);
            }
        }

        // Проверяем отключившихся игроков
        foreach (uint netId in lastKnownPlayers)
        {
            if (!currentPlayers.Contains(netId))
            {
                OnPlayerLeft(netId);
            }
        }

        lastKnownPlayers = currentPlayers;
    }

    void OnPlayerJoined(uint netId)
    {
        if (showAllPlayers)
        {
            CreatePingDisplayForPlayer(netId);
        }
    }

    void OnPlayerLeft(uint netId)
    {
        RemovePingDisplayForPlayer(netId);
    }

    void CreatePingDisplayForPlayer(uint netId)
    {
        if (playerPingDisplays.ContainsKey(netId) || pingDisplayPrefab == null || pingDisplayParent == null)
            return;

        GameObject pingDisplay = Instantiate(pingDisplayPrefab, pingDisplayParent);

        // Позиционируем элемент
        RectTransform rectTransform = pingDisplay.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 position = displayOffset * playerPingDisplays.Count;
            rectTransform.anchoredPosition = position;
        }

        // Настраиваем компонент отображения ping
        PingDisplay pingDisplayComponent = pingDisplay.GetComponent<PingDisplay>();
        if (pingDisplayComponent != null)
        {
            pingDisplayComponent.showOnlyLocalPlayer = false;
        }

        playerPingDisplays[netId] = pingDisplay;
    }

    void RemovePingDisplayForPlayer(uint netId)
    {
        if (playerPingDisplays.TryGetValue(netId, out GameObject display))
        {
            if (display != null)
            {
                Destroy(display);
            }
            playerPingDisplays.Remove(netId);
            playerPings.Remove(netId);

            // Переупорядочиваем остальные элементы
            ReorganizePingDisplays();
        }
    }

    void ReorganizePingDisplays()
    {
        int index = 0;
        var validDisplays = new Dictionary<uint, GameObject>();

        foreach (var kvp in playerPingDisplays)
        {
            if (kvp.Value != null)
            {
                RectTransform rectTransform = kvp.Value.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector2 position = displayOffset * index;
                    rectTransform.anchoredPosition = position;
                }
                validDisplays[kvp.Key] = kvp.Value;
                index++;
            }
        }

        playerPingDisplays = validDisplays;
    }

    [Server]
    void UpdateServerPing()
    {
        if (showServerPing)
        {
            // Для сервера ping обычно 0 или очень маленький
            RpcUpdateServerPing(0);
        }
    }

    [ClientRpc]
    void RpcUpdateServerPing(int ping)
    {
        if (serverPingText != null)
        {
            serverPingText.text = $"Server: {ping}ms";
            serverPingText.color = Color.green;
        }
    }

    // Метод для получения среднего ping всех игроков
    public float GetAveragePing()
    {
        if (playerPings.Count == 0)
            return 0f;

        return (float)playerPings.Values.Average();
    }

    // Метод для получения максимального ping
    public int GetMaxPing()
    {
        if (playerPings.Count == 0)
            return 0;

        return playerPings.Values.Max();
    }

    // Метод для получения минимального ping
    public int GetMinPing()
    {
        if (playerPings.Count == 0)
            return 0;

        return playerPings.Values.Min();
    }

    // Публичный метод для обновления ping игрока
    public void UpdatePlayerPing(uint netId, int ping)
    {
        playerPings[netId] = ping;
    }

    // Метод для получения статистики ping
    public string GetPingStatistics()
    {
        if (playerPings.Count == 0)
            return "No players connected";

        return $"Players: {playerPings.Count}, Avg: {GetAveragePing():F1}ms, Max: {GetMaxPing()}ms, Min: {GetMinPing()}ms";
    }

    // Метод для принудительного обновления всех дисплеев
    public void RefreshAllDisplays()
    {
        // Очищаем все существующие дисплеи
        foreach (var display in playerPingDisplays.Values)
        {
            if (display != null)
                Destroy(display);
        }
        playerPingDisplays.Clear();
        lastKnownPlayers.Clear();

        // Перезапускаем проверку игроков
        CheckForPlayerChanges();
    }

    void OnDestroy()
    {
        // Очищаем все дисплеи при уничтожении
        foreach (var display in playerPingDisplays.Values)
        {
            if (display != null)
                Destroy(display);
        }
        playerPingDisplays.Clear();
    }
}