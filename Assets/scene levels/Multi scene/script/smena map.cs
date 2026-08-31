using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

// Менеджер сцен для переключения между главным меню и игровой сценой
public class SceneManagerController : MonoBehaviour
{
    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string networkMenuSceneName = "NetworkMenu";
    public string gameSceneName = "GameScene";

    [Header("UI References")]
    public Button playButton;
    public Button exitButton;

    private void Start()
    {
        // Настройка кнопок
        if (playButton != null)
            playButton.onClick.AddListener(GoToNetworkMenu);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    public void GoToNetworkMenu()
    {
        Debug.Log("Переход к сетевому меню");
        SceneManager.LoadScene(networkMenuSceneName);
    }

    public void GoToMainMenu()
    {
        Debug.Log("Возврат в главное меню");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}

// Контроллер сетевого меню
public class NetworkMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;
    public Button backButton;
    public InputField serverAddressInput;
    public Text statusText;

    [Header("Network Settings")]
    public string defaultServerAddress = "localhost";

    private NetworkManager networkManager;

    private void Start()
    {
        // Получаем ссылку на NetworkManager
        networkManager = NetworkManager.singleton;

        if (networkManager == null)
        {
            Debug.LogError("NetworkManager не найден в сцене!");
            return;
        }

        // Настройка кнопок
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);

        if (backButton != null)
            backButton.onClick.AddListener(GoBackToMainMenu);

        // Настройка поля ввода адреса
        if (serverAddressInput != null)
        {
            serverAddressInput.text = defaultServerAddress;
            serverAddressInput.onEndEdit.AddListener(OnServerAddressChanged);
        }

        // Обновляем статус
        UpdateStatus("Готов к подключению");
    }

    private void OnServerAddressChanged(string newAddress)
    {
        if (networkManager != null)
        {
            networkManager.networkAddress = newAddress;
        }
    }

    public void StartHost()
    {
        if (networkManager != null)
        {
            UpdateStatus("Запуск хоста...");
            networkManager.StartHost();
        }
    }

    public void StartClient()
    {
        if (networkManager != null)
        {
            UpdateStatus("Подключение к серверу...");

            // Устанавливаем адрес сервера
            if (serverAddressInput != null && !string.IsNullOrEmpty(serverAddressInput.text))
            {
                networkManager.networkAddress = serverAddressInput.text;
            }

            networkManager.StartClient();
        }
    }

    public void StopConnection()
    {
        if (networkManager != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                networkManager.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
            }
            else if (NetworkServer.active)
            {
                networkManager.StopServer();
            }

            UpdateStatus("Соединение остановлено");
        }
    }

    public void GoBackToMainMenu()
    {
        // Останавливаем все сетевые соединения перед возвратом в меню
        StopConnection();

        // Возвращаемся в главное меню
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Network Status: {message}");
    }

    private void Update()
    {
        // Простая проверка состояния сети для обновления UI
        if (networkManager != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                UpdateStatus("Хост активен");
            }
            else if (NetworkClient.isConnected)
            {
                UpdateStatus("Подключен к серверу");
            }
            else if (NetworkServer.active)
            {
                UpdateStatus("Сервер запущен, ожидание клиентов...");
            }
        }
    }
}

// Дополнительный скрипт для управления сетевыми событиями
public class NetworkEventHandler : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        // Используем простую проверку состояния вместо событий
        InvokeRepeating("CheckNetworkStatus", 1f, 1f);
    }

    private void OnDestroy()
    {
        CancelInvoke("CheckNetworkStatus");
    }

    private void CheckNetworkStatus()
    {
        // Проверяем, подключился ли клиент к серверу
        if (NetworkClient.isConnected && !NetworkServer.active)
        {
            Debug.Log("Клиент подключен к серверу");
            LoadGameScene();
            CancelInvoke("CheckNetworkStatus"); // Останавливаем проверку
        }
        // Проверяем, есть ли подключенные клиенты к хосту
        else if (NetworkServer.active && NetworkServer.connections.Count > 0)
        {
            Debug.Log("Клиенты подключены к хосту");
            LoadGameScene();
            CancelInvoke("CheckNetworkStatus"); // Останавливаем проверку
        }
    }

    public void LoadGameScene()
    {
        if (NetworkManager.singleton != null)
        {
            // Если мы хост или сервер, меняем сцену для всех
            if (NetworkServer.active)
            {
                NetworkManager.singleton.ServerChangeScene(gameSceneName);
            }
            // Если мы клиент, ждем смены сцены от сервера
            else if (NetworkClient.isConnected)
            {
                Debug.Log("Ожидание смены сцены от сервера...");
            }
        }
    }
}

// Простой игровой контроллер для игровой сцены
public class GameController : NetworkBehaviour
{
    [Header("UI References")]
    public Button disconnectButton;
    public Text connectionStatusText;

    private void Start()
    {
        // Настройка кнопки отключения
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(DisconnectAndReturnToMenu);

        // Обновляем статус подключения
        UpdateConnectionStatus();
    }

    public void DisconnectAndReturnToMenu()
    {
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
        }

        // Возвращаемся в главное меню
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateConnectionStatus()
    {
        if (connectionStatusText != null)
        {
            string status = "";
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                status = "Хост";
            }
            else if (NetworkClient.isConnected)
            {
                status = "Клиент";
            }
            else
            {
                status = "Не подключен";
            }

            connectionStatusText.text = $"Статус: {status}";
        }
    }

    private void Update()
    {
        // Обновляем статус каждый кадр
        UpdateConnectionStatus();
    }
}