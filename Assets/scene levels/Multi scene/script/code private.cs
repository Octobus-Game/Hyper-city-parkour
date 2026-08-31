using UnityEngine;
using System.Collections.Generic;
using Mirror;
using UnityEngine.UI;

namespace Mirro_code_private
{
    public class HostAccessController : NetworkBehaviour
    {
        [Header("UI Elements")]
        public Button lockButton;
        public InputField codeInputField;
        public Button connectButton;
        public Text statusText;
        public GameObject connectionPanel;

        [Header("Settings")]
        public string accessCode = "1234";

        [SyncVar(hook = nameof(OnHostLockedChanged))]
        private bool isHostLocked = false;

        private Dictionary<int, bool> authorizedClients = new Dictionary<int, bool>();

        private void Start()
        {
            // Настройка UI
            if (lockButton != null)
                lockButton.onClick.AddListener(ToggleHostLock);

            if (connectButton != null)
                connectButton.onClick.AddListener(AttemptConnection);

            // Подписка на события Mirror
            if (NetworkManager.singleton != null)
            {
                NetworkServer.OnConnectedEvent += OnServerConnect;
                NetworkClient.OnConnectedEvent += OnClientConnect;
                NetworkClient.OnDisconnectedEvent += OnClientDisconnect;
            }

            UpdateUI();
        }

        private void Update()
        {
            // Обновление UI в зависимости от роли
            if (isServer)
            {
                UpdateServerUI();
            }
            else if (isClient)
            {
                UpdateClientUI();
            }
        }

        [Server]
        private void ToggleHostLock()
        {
            isHostLocked = !isHostLocked;

            if (isHostLocked)
            {
                // Отключить всех неавторизованных клиентов
                DisconnectUnauthorizedClients();
                UpdateStatusText("Хост заблокирован. Требуется код доступа.");
            }
            else
            {
                authorizedClients.Clear();
                UpdateStatusText("Хост разблокирован. Свободный доступ.");
            }

            RpcUpdateHostStatus(isHostLocked);
        }

        [Server]
        private void DisconnectUnauthorizedClients()
        {
            List<NetworkConnectionToClient> connectionsToDisconnect = new List<NetworkConnectionToClient>();

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && !authorizedClients.ContainsKey(conn.connectionId))
                {
                    connectionsToDisconnect.Add(conn);
                }
            }

            foreach (var conn in connectionsToDisconnect)
            {
                conn.Disconnect();
            }
        }

        [Server]
        private void OnServerConnect(NetworkConnectionToClient conn)
        {
            if (isHostLocked)
            {
                // Если хост заблокирован, сразу отключаем клиента
                conn.Disconnect();
                return;
            }
        }

        private void OnClientConnect()
        {
            if (isClient && !isServer)
            {
                // Клиент подключился, проверяем статус хоста
                CmdRequestHostStatus();
            }
        }

        private void OnClientDisconnect()
        {
            if (statusText != null)
                statusText.text = "Отключен от хоста";
        }

        [Command(requiresAuthority = false)]
        private void CmdRequestHostStatus(NetworkConnectionToClient sender = null)
        {
            if (sender != null)
            {
                TargetUpdateHostStatus(sender, isHostLocked);
            }
        }

        [Command(requiresAuthority = false)]
        private void CmdSubmitAccessCode(string code, NetworkConnectionToClient sender = null)
        {
            if (sender == null) return;

            if (code == accessCode)
            {
                // Код верный, авторизуем клиента
                authorizedClients[sender.connectionId] = true;
                TargetCodeAccepted(sender);
            }
            else
            {
                // Код неверный
                TargetCodeRejected(sender);
            }
        }

        [TargetRpc]
        private void TargetUpdateHostStatus(NetworkConnection target, bool locked)
        {
            OnHostLockedChanged(locked, locked);
        }

        [TargetRpc]
        private void TargetCodeAccepted(NetworkConnection target)
        {
            statusText.text = "Код принят! Доступ разрешен.";
            connectionPanel.SetActive(false);
        }

        [TargetRpc]
        private void TargetCodeRejected(NetworkConnection target)
        {
            statusText.text = "Неверный код доступа!";
            codeInputField.text = "";
        }

        [ClientRpc]
        private void RpcUpdateHostStatus(bool locked)
        {
            OnHostLockedChanged(locked, locked);
        }

        private void OnHostLockedChanged(bool oldValue, bool newValue)
        {
            isHostLocked = newValue;
            UpdateUI();
        }

        private void AttemptConnection()
        {
            if (codeInputField != null && !string.IsNullOrEmpty(codeInputField.text))
            {
                CmdSubmitAccessCode(codeInputField.text);
            }
        }

        private void UpdateUI()
        {
            if (isServer)
            {
                UpdateServerUI();
            }
            else
            {
                UpdateClientUI();
            }
        }

        private void UpdateServerUI()
        {
            if (lockButton != null)
            {
                lockButton.gameObject.SetActive(true);
                lockButton.GetComponentInChildren<Text>().text = isHostLocked ? "Разблокировать хост" : "Заблокировать хост";
            }

            if (connectionPanel != null)
                connectionPanel.SetActive(false);

            if (statusText != null)
                statusText.text = isHostLocked ? "Хост заблокирован" : "Хост активен";
        }

        private void UpdateClientUI()
        {
            if (lockButton != null)
                lockButton.gameObject.SetActive(false);

            if (connectionPanel != null)
                connectionPanel.SetActive(isHostLocked);

            if (statusText != null)
            {
                if (isHostLocked)
                    statusText.text = "Требуется код доступа";
                else
                    statusText.text = "Подключен к хосту";
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void OnDestroy()
        {
            // Очистка подписок
            if (NetworkManager.singleton != null)
            {
                NetworkServer.OnConnectedEvent -= OnServerConnect;
                NetworkClient.OnConnectedEvent -= OnClientConnect;
                NetworkClient.OnDisconnectedEvent -= OnClientDisconnect;
            }
        }
    }

    // Дополнительный компонент для управления подключением
    public class ConnectionManager : MonoBehaviour
    {
        [Header("Network Settings")]
        public string serverAddress = "localhost";
        public ushort serverPort = 7777;

        [Header("UI Elements")]
        public Button hostButton;
        public Button joinButton;
        public InputField addressInput;
        public GameObject menuPanel;

        private void Start()
        {
            if (hostButton != null)
                hostButton.onClick.AddListener(StartHost);

            if (joinButton != null)
                joinButton.onClick.AddListener(JoinServer);

            if (addressInput != null)
                addressInput.text = serverAddress;
        }

        private void StartHost()
        {
            NetworkManager.singleton.StartHost();
            menuPanel.SetActive(false);
        }

        private void JoinServer()
        {
            if (addressInput != null && !string.IsNullOrEmpty(addressInput.text))
            {
                NetworkManager.singleton.networkAddress = addressInput.text;
            }

            NetworkManager.singleton.StartClient();
            menuPanel.SetActive(false);
        }
    }
}









