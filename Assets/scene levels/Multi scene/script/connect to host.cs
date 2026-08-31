using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Reflection;

public class RandomHostConnector : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverIP = "localhost";
    [SerializeField] private int minPort = 7777;
    [SerializeField] private int maxPort = 8000;
    [SerializeField] private int connectionTimeout = 3; // секунды

    [Header("UI References")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Button manualConnectButton;
    [SerializeField] private InputField portInputField;
    [SerializeField] private InputField ipInputField; // Опционально для ввода IP
    [SerializeField] private Text statusText;
    [SerializeField] private Slider progressSlider;

    private NetworkManager networkManager;
    private bool isSearching = false;

    void Start()
    {
        networkManager = NetworkManager.singleton;

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(ConnectToRandomHost);
        }

        if (manualConnectButton != null)
        {
            manualConnectButton.onClick.AddListener(ConnectToSpecificHost);
        }

        // Устанавливаем placeholder для InputField если есть
        if (portInputField != null && portInputField.placeholder != null)
        {
            var placeholderText = portInputField.placeholder.GetComponent<Text>();
            if (placeholderText != null)
            {
                placeholderText.text = $"Port ({minPort}-{maxPort})";
            }
        }

        if (ipInputField != null && ipInputField.placeholder != null)
        {
            var placeholderText = ipInputField.placeholder.GetComponent<Text>();
            if (placeholderText != null)
            {
                placeholderText.text = "IP Address (optional)";
            }
        }

        UpdateUI("Ready to connect");
    }

    public void ConnectToRandomHost()
    {
        if (isSearching) return;

        StartCoroutine(SearchAndConnectRoutine());
    }

    public void ConnectToSpecificHost()
    {
        if (isSearching) return;

        // Проверяем введенный порт
        if (portInputField == null || string.IsNullOrEmpty(portInputField.text))
        {
            UpdateUI("Please enter a port number");
            return;
        }

        if (!int.TryParse(portInputField.text, out int port))
        {
            UpdateUI("Invalid port number");
            return;
        }

        if (port < 1 || port > 65535)
        {
            UpdateUI("Port must be between 1 and 65535");
            return;
        }

        // Получаем IP адрес (если указан)
        string targetIP = serverIP; // Используем IP по умолчанию
        if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
        {
            targetIP = ipInputField.text.Trim();
        }

        StartCoroutine(ManualConnectRoutine(targetIP, port));
    }

    private IEnumerator SearchAndConnectRoutine()
    {
        isSearching = true;
        SetButtonsInteractable(false);

        UpdateUI("Searching for hosts...");

        List<int> availablePorts = new List<int>();

        // Сканируем порты в диапазоне
        int totalPorts = maxPort - minPort + 1;
        int scannedPorts = 0;

        for (int port = minPort; port <= maxPort; port++)
        {
            if (IsPortOpen(serverIP, port))
            {
                availablePorts.Add(port);
                Debug.Log($"Found open port: {port}");
            }

            scannedPorts++;
            float progress = (float)scannedPorts / totalPorts;

            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }

            UpdateUI($"Scanning... {scannedPorts}/{totalPorts}");

            // Небольшая задержка чтобы не зависла игра
            if (scannedPorts % 10 == 0)
            {
                yield return null;
            }
        }

        if (availablePorts.Count > 0)
        {
            // Выбираем случайный порт из найденных
            int randomIndex = Random.Range(0, availablePorts.Count);
            int selectedPort = availablePorts[randomIndex];

            UpdateUI($"Connecting to {serverIP}:{selectedPort}...");

            // Устанавливаем параметры подключения
            SetClientConnection(serverIP, selectedPort);

            // Подключаемся
            networkManager.StartClient();

            // Ждем результат подключения
            yield return StartCoroutine(WaitForConnectionResult());
        }
        else
        {
            UpdateUI("No hosts found");
            ResetUI();
        }
    }

    private IEnumerator ManualConnectRoutine(string ip, int port)
    {
        isSearching = true;
        SetButtonsInteractable(false);

        UpdateUI($"Checking host {ip}:{port}...");

        // Проверяем доступность хоста
        if (!IsPortOpen(ip, port))
        {
            UpdateUI($"Host {ip}:{port} is not available");
            ResetUI();
            yield break;
        }

        UpdateUI($"Connecting to {ip}:{port}...");

        // Устанавливаем параметры подключения
        SetClientConnection(ip, port);

        // Подключаемся
        networkManager.StartClient();

        // Ждем результат подключения
        yield return StartCoroutine(WaitForConnectionResult());
    }

    private bool IsPortOpen(string host, int port)
    {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(100); // 100ms timeout

                if (success)
                {
                    client.EndConnect(result);
                    return client.Connected;
                }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void SetClientConnection(string ip, int port)
    {
        networkManager.networkAddress = ip;

        // Устанавливаем порт для транспорта
        if (networkManager.transport is kcp2k.KcpTransport kcpTransport)
        {
            kcpTransport.Port = (ushort)port;
        }
        else
        {
            // Универсальный метод для других транспортов
            var transportType = networkManager.transport.GetType();
            var portField = transportType.GetField("Port") ?? transportType.GetField("port");
            var portProperty = transportType.GetProperty("Port") ?? transportType.GetProperty("port");

            if (portProperty != null && portProperty.CanWrite)
            {
                portProperty.SetValue(networkManager.transport, (ushort)port);
            }
            else if (portField != null)
            {
                portField.SetValue(networkManager.transport, (ushort)port);
            }
        }
    }

    private IEnumerator WaitForConnectionResult()
    {
        float timer = 0f;

        while (timer < connectionTimeout)
        {
            if (NetworkClient.isConnected)
            {
                UpdateUI("Connected successfully!");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Таймаут подключения
        UpdateUI("Connection failed");
        networkManager.StopClient();
        ResetUI();
    }

    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }

    private void ResetUI()
    {
        isSearching = false;
        SetButtonsInteractable(true);

        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (connectButton != null)
            connectButton.interactable = interactable;

        if (manualConnectButton != null)
            manualConnectButton.interactable = interactable;
    }

    // События Mirror для обработки подключения
    public void OnClientConnect()
    {
        UpdateUI("Connected to host!");
        ResetUI();
    }

    public void OnClientDisconnect()
    {
        UpdateUI("Disconnected from host");
        ResetUI();
    }

    // Дополнительные методы для работы с UI
    public void OnPortInputChanged()
    {
        // Можно добавить валидацию при вводе
        if (portInputField != null && !string.IsNullOrEmpty(portInputField.text))
        {
            if (int.TryParse(portInputField.text, out int port))
            {
                if (port < 1 || port > 65535)
                {
                    portInputField.text = Mathf.Clamp(port, 1, 65535).ToString();
                }
            }
        }
    }

    public void ClearInputFields()
    {
        if (portInputField != null)
            portInputField.text = "";

        if (ipInputField != null)
            ipInputField.text = "";
    }

    // Метод для быстрого подключения через код (полезно для тестирования)
    public void QuickConnect(string ip, int port)
    {
        if (!isSearching)
        {
            StartCoroutine(ManualConnectRoutine(ip, port));
        }
    }

    // Валидация IP адреса
    private bool IsValidIPAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}

// Альтернативный простой вариант
public class SimpleRandomConnector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string serverIP = "localhost";
    [SerializeField] private int minPort = 7777;
    [SerializeField] private int maxPort = 8000;
    [SerializeField] private int maxAttempts = 10;

    [Header("UI")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Text statusText;

    public void ConnectToRandomHost()
    {
        StartCoroutine(TryRandomConnections());
    }

    private IEnumerator TryRandomConnections()
    {
        connectButton.interactable = false;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int randomPort = Random.Range(minPort, maxPort + 1);

            statusText.text = $"Trying {serverIP}:{randomPort}... ({attempt + 1}/{maxAttempts})";

            // Устанавливаем параметры
            NetworkManager.singleton.networkAddress = serverIP;

            var kcpTransport = NetworkManager.singleton.transport as kcp2k.KcpTransport;
            if (kcpTransport != null)
            {
                kcpTransport.Port = (ushort)randomPort;
            }

            // Пробуем подключиться
            NetworkManager.singleton.StartClient();

            // Ждем 2 секунды
            yield return new WaitForSeconds(2f);

            // Проверяем подключение
            if (NetworkClient.isConnected)
            {
                statusText.text = $"Connected to {serverIP}:{randomPort}!";
                connectButton.interactable = true;
                yield break;
            }

            // Останавливаем клиент перед следующей попыткой
            NetworkManager.singleton.StopClient();
            yield return new WaitForSeconds(0.5f);
        }

        statusText.text = "Could not connect to any host";
        connectButton.interactable = true;
    }
}