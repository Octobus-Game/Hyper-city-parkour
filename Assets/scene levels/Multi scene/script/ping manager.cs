using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class PingDisplay : NetworkBehaviour
{
    [Header("UI Elements")]
    public Text pingText; // UI Text для отображения ping
    public Text playerNameText; // UI Text для отображения имени игрока (опционально)

    [Header("Ping Settings")]
    public float pingUpdateInterval = 1f; // Интервал обновления ping (в секундах)
    public string pingPrefix = "Ping: "; // Префикс для отображения
    public string pingSuffix = "ms"; // Суффикс для отображения
    public bool showOnlyLocalPlayer = true; // Показывать ping только для локального игрока

    [Header("Color Settings")]
    public bool useColorCoding = true; // Использовать цветовое кодирование
    public Color goodPingColor = Color.green; // Цвет для хорошего ping (< 50ms)
    public Color averagePingColor = Color.yellow; // Цвет для среднего ping (50-100ms)
    public Color badPingColor = Color.red; // Цвет для плохого ping (> 100ms)
    public int goodPingThreshold = 50;
    public int badPingThreshold = 100;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Синхронизированные переменные
    [SyncVar(hook = nameof(OnPingChanged))]
    private int currentPing = 0;

    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    private string playerName = "";

    // Локальные переменные
    private float lastPingUpdate = 0f;

    void Start()
    {
        // Инициализация имени игрока
        if (isLocalPlayer)
        {
            playerName = $"Player {netId}";
            if (isServer)
            {
                // Если это сервер, ping всегда 0
                currentPing = 0;
            }
        }

        // Обновляем UI при старте
        UpdatePingDisplay();

        // Запускаем корутину для обновления ping
        if (isLocalPlayer)
        {
            StartCoroutine(UpdatePingRoutine());
        }
    }

    void Update()
    {
        // Показываем ping только для локального игрока или для всех, если настроено
        if (showOnlyLocalPlayer && !isLocalPlayer)
        {
            if (pingText != null)
                pingText.gameObject.SetActive(false);
            return;
        }

        if (pingText != null && !pingText.gameObject.activeInHierarchy)
        {
            pingText.gameObject.SetActive(true);
        }
    }

    IEnumerator UpdatePingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(pingUpdateInterval);

            if (isLocalPlayer && !isServer)
            {
                // Вычисляем ping для клиента
                double rtt = NetworkTime.rtt;
                int ping = Mathf.RoundToInt((float)(rtt * 1000));

                // Отправляем ping на сервер для синхронизации
                CmdUpdatePing(ping);

                if (showDebugInfo)
                {
                    Debug.Log($"Local ping: {ping}ms, RTT: {rtt}");
                }
            }
        }
    }

    [Command]
    void CmdUpdatePing(int ping)
    {
        currentPing = ping;

        if (showDebugInfo)
        {
            Debug.Log($"Server received ping update: {ping}ms from player {netId}");
        }
    }

    void OnPingChanged(int oldPing, int newPing)
    {
        currentPing = newPing;
        UpdatePingDisplay();

        if (showDebugInfo)
        {
            Debug.Log($"Ping changed from {oldPing} to {newPing}");
        }
    }

    void OnPlayerNameChanged(string oldName, string newName)
    {
        playerName = newName;
        UpdatePlayerNameDisplay();
    }

    void UpdatePingDisplay()
    {
        if (pingText == null) return;

        // Обновляем текст
        pingText.text = $"{pingPrefix}{currentPing}{pingSuffix}";

        // Применяем цветовое кодирование
        if (useColorCoding)
        {
            Color pingColor = GetPingColor(currentPing);
            pingText.color = pingColor;
        }
    }

    void UpdatePlayerNameDisplay()
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
    }

    Color GetPingColor(int ping)
    {
        if (ping < goodPingThreshold)
        {
            return goodPingColor;
        }
        else if (ping < badPingThreshold)
        {
            return averagePingColor;
        }
        else
        {
            return badPingColor;
        }
    }

    // Публичные методы для настройки
    public void SetPlayerName(string name)
    {
        if (isLocalPlayer)
        {
            CmdSetPlayerName(name);
        }
    }

    [Command]
    void CmdSetPlayerName(string name)
    {
        playerName = name;
    }

    public int GetCurrentPing()
    {
        return currentPing;
    }

    public string GetPingString()
    {
        return $"{pingPrefix}{currentPing}{pingSuffix}";
    }

    // Метод для получения качества соединения
    public string GetConnectionQuality()
    {
        if (currentPing < goodPingThreshold)
        {
            return "Excellent";
        }
        else if (currentPing < badPingThreshold)
        {
            return "Good";
        }
        else if (currentPing < 200)
        {
            return "Average";
        }
        else
        {
            return "Poor";
        }
    }
}