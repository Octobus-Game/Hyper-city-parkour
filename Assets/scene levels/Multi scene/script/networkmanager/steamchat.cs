using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Mirror.Examples.Chat
{
    public class ChatUI : NetworkBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI chatHistory;
        [SerializeField] private Scrollbar scrollbar;
        [SerializeField] private TMP_InputField chatMessage;
        [SerializeField] private Button sendButton;

        [Header("Chat Settings")]
        [SerializeField] private int maxMessageHistory = 100;
        [SerializeField] private int maxMessageLength = 200;
        [SerializeField] private float messageCooldown = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color localPlayerColor = Color.red;
        [SerializeField] private Color remotePlayerColor = Color.blue;
        [SerializeField] private Color systemMessageColor = Color.gray;

        // Cached references
        private RectTransform chatHistoryRect;
        private ScrollRect scrollRect;

        // Message history management (только на сервере)
        [SyncVar(hook = nameof(OnChatHistoryChanged))]
        private string serverChatHistory = "";
        private Queue<string> messageHistory = new Queue<string>();
        private float lastMessageTime;

        // Events
        public static event Action<string, string> OnMessageReceived;

        // This is only set on client to the name of the local player
        internal static string localPlayerName;

        // Server-only cross-reference of connections to player names
        internal static readonly Dictionary<NetworkConnectionToClient, string> connNames =
            new Dictionary<NetworkConnectionToClient, string>();

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            SetupEventListeners();
        }

        private void OnDestroy()
        {
            RemoveEventListeners();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            Debug.Log("InitializeComponents called");

            // Проверяем наличие обязательных компонентов
            if (chatHistory == null)
            {
                Debug.LogError("ChatHistory TextMeshProUGUI component is not assigned!");
                return;
            }

            if (sendButton == null)
            {
                Debug.LogError("SendButton is not assigned!");
                return;
            }

            if (chatMessage == null)
            {
                Debug.LogError("ChatMessage TMP_InputField is not assigned!");
                return;
            }

            Debug.Log($"TextMeshProUGUI component found: {chatHistory.name}");
            Debug.Log($"TextMeshProUGUI component active: {chatHistory.gameObject.activeInHierarchy}");
            Debug.Log($"TextMeshProUGUI component enabled: {chatHistory.enabled}");

            // Cache components for better performance
            chatHistoryRect = chatHistory.GetComponent<RectTransform>();
            scrollRect = chatHistory.GetComponentInParent<ScrollRect>();

            // Initialize UI state
            sendButton.interactable = false;
            chatMessage.characterLimit = maxMessageLength;

            // Тестовое сообщение
            chatHistory.text = "Chat initialized...";
            Debug.Log($"Test text set. Current text: '{chatHistory.text}'");
        }

        private void SetupEventListeners()
        {
            sendButton.onClick.AddListener(SendMessage);
            chatMessage.onValueChanged.AddListener(OnInputValueChanged);
            chatMessage.onEndEdit.AddListener(OnInputEndEdit);
        }

        private void RemoveEventListeners()
        {
            sendButton.onClick.RemoveListener(SendMessage);
            chatMessage.onValueChanged.RemoveListener(OnInputValueChanged);
            chatMessage.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        #endregion

        #region Network Callbacks

        public override void OnStartServer()
        {
            connNames.Clear();
            messageHistory.Clear();
            serverChatHistory = "";
            AddSystemMessageServer("Chat server started");
        }

        public override void OnStartClient()
        {
            // Клиент получает историю от сервера автоматически через SyncVar
            if (!isServer)
            {
                chatHistory.text = serverChatHistory;
                StartCoroutine(ScrollToBottom());
            }
        }

        public override void OnStopClient()
        {
            if (isServer)
            {
                AddSystemMessageServer("Chat server stopped");
            }
        }

        #endregion

        #region Message Handling

        [Command(requiresAuthority = false)]
        private void CmdSend(string message, NetworkConnectionToClient sender = null)
        {
            Debug.Log($"CmdSend called with message: '{message}'");

            // Validate message
            if (string.IsNullOrWhiteSpace(message) || message.Length > maxMessageLength)
            {
                Debug.LogWarning($"Message validation failed. Empty: {string.IsNullOrWhiteSpace(message)}, Length: {message?.Length}");
                return;
            }

            // Get or add player name
            if (!connNames.ContainsKey(sender))
            {
                var player = sender.identity.GetComponent<Player>();
                if (player == null)
                {
                    Debug.LogError($"Player component not found on sender: {sender}");
                    return;
                }

                Debug.Log($"Adding new player: {player.playerName}");
                connNames.Add(sender, player.playerName);
            }

            string playerName = connNames[sender];
            Debug.Log($"Processing message from {playerName}: '{message}'");

            // Sanitize message
            string sanitizedMessage = SanitizeMessage(message.Trim());
            Debug.Log($"Sanitized message: '{sanitizedMessage}'");

            // Process message on server
            ProcessMessageOnServer(playerName, sanitizedMessage);
        }

        [Server]
        private void ProcessMessageOnServer(string playerName, string message)
        {
            Debug.Log($"ProcessMessageOnServer: {playerName} - '{message}'");

            if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(message))
            {
                Debug.LogWarning($"Invalid message data. PlayerName: '{playerName}', Message: '{message}'");
                return;
            }

            // Определяем цвет игрока (на сервере можно использовать разные логики)
            Color playerColor = remotePlayerColor; // По умолчанию все удаленные
            string colorHex = ColorUtility.ToHtmlStringRGB(playerColor);
            string formattedMessage = $"<color=#{colorHex}>{playerName}:</color> {message}";

            Debug.Log($"Formatted message: '{formattedMessage}'");

            // Добавляем сообщение в серверную историю
            AppendMessageToServer(formattedMessage);

            // Вызываем событие
            OnMessageReceived?.Invoke(playerName, message);
        }

        [Server]
        private void AppendMessageToServer(string message)
        {
            Debug.Log($"AppendMessageToServer: '{message}'");

            // Управляем историей сообщений на сервере
            messageHistory.Enqueue(message);

            if (messageHistory.Count > maxMessageHistory)
            {
                messageHistory.Dequeue();
                Debug.Log("Removed old message from history");
            }

            Debug.Log($"Message history count: {messageHistory.Count}");

            // Обновляем серверную историю чата
            RebuildServerChatHistory();
        }

        [Server]
        private void AddSystemMessageServer(string message)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(systemMessageColor);
            string formattedMessage = $"<color=#{colorHex}><i>{message}</i></color>";
            AppendMessageToServer(formattedMessage);
        }

        [Server]
        private void RebuildServerChatHistory()
        {
            string newHistory = string.Join("\n", messageHistory);
            Debug.Log($"RebuildServerChatHistory. New history length: {newHistory.Length}");
            Debug.Log($"History content preview: {(newHistory.Length > 100 ? newHistory.Substring(0, 100) + "..." : newHistory)}");

            serverChatHistory = newHistory;

            // Дополнительно отправляем RPC для надежности
            RpcUpdateChatUI(newHistory);
        }

        [ClientRpc]
        private void RpcUpdateChatUI(string newHistory)
        {
            Debug.Log($"RpcUpdateChatUI called with history length: {newHistory?.Length}");

            if (chatHistory != null)
            {
                chatHistory.text = newHistory;
                Canvas.ForceUpdateCanvases();
                StartCoroutine(ScrollToBottom());
                Debug.Log($"TextMeshProUGUI component updated. Current text length: {chatHistory.text.Length}");
            }
            else
            {
                Debug.LogError("ChatHistory TextMeshProUGUI component is null in RPC!");
            }
        }

        [Server]
        private void ClearChatServer()
        {
            messageHistory.Clear();
            serverChatHistory = "";
        }

        // Hook для SyncVar - вызывается на клиентах при изменении серверной истории
        private void OnChatHistoryChanged(string oldValue, string newValue)
        {
            Debug.Log($"OnChatHistoryChanged called. IsServer: {isServer}");
            Debug.Log($"Old value length: {oldValue?.Length}, New value length: {newValue?.Length}");
            Debug.Log($"ChatHistory TextMeshProUGUI component null: {chatHistory == null}");

            if (chatHistory == null)
            {
                Debug.LogError("ChatHistory TextMeshProUGUI component is null!");
                return;
            }

            Debug.Log($"Setting text to: {newValue}");
            chatHistory.text = newValue;

            // Принудительное обновление
            Canvas.ForceUpdateCanvases();

            StartCoroutine(ScrollToBottom());
        }

        private string SanitizeMessage(string message)
        {
            // Remove potential rich text exploits
            message = message.Replace("<", "&lt;").Replace(">", "&gt;");

            // Remove excessive whitespace
            message = System.Text.RegularExpressions.Regex.Replace(message, @"\s+", " ");

            return message.Trim();
        }

        #endregion

        #region UI Management (в основном для сервера)

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
            else if (scrollbar != null)
                scrollbar.value = 0f;
        }

        #endregion

        #region Input Handling (только на клиентах)

        private void OnInputValueChanged(string input)
        {
            sendButton.interactable = !string.IsNullOrWhiteSpace(input) && NetworkClient.isConnected;
        }

        private void OnInputEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetButtonDown("Submit"))
            {
                SendMessage();
            }
        }

        public void SendMessage()
        {
            // Check cooldown
            if (Time.time - lastMessageTime < messageCooldown)
                return;

            string message = chatMessage.text;

            if (!string.IsNullOrWhiteSpace(message) && NetworkClient.isConnected)
            {
                CmdSend(message.Trim());
                chatMessage.text = string.Empty;
                chatMessage.ActivateInputField();
                lastMessageTime = Time.time;
            }
        }

        #endregion

        #region Public API

        public void ExitButtonOnClick()
        {
            if (isServer)
                AddSystemMessageServer("Server shutting down...");

            NetworkManager.singleton.StopHost();
        }

        public void SetLocalPlayerName(string name)
        {
            localPlayerName = name;
        }

        // Серверный метод для добавления сообщений программно
        [Server]
        public void AddMessageServer(string playerName, string message)
        {
            ProcessMessageOnServer(playerName, message);
        }

        #endregion

        #region Utility Methods

        // Для отладки (только на сервере)
        [ContextMenu("Clear Chat")]
        private void ClearChatDebug()
        {
            if (isServer)
                ClearChatServer();
        }

        [ContextMenu("Add Test Message")]
        private void AddTestMessage()
        {
            if (isServer)
                AddMessageServer("TestPlayer", "This is a test message");
        }

        [ContextMenu("Test UI Direct")]
        private void TestUIDirect()
        {
            if (chatHistory != null)
            {
                chatHistory.text = "DIRECT TEST MESSAGE\nLine 2\nLine 3";
                Debug.Log("Direct UI test completed");
            }
            else
            {
                Debug.LogError("ChatHistory TextMeshProUGUI is null!");
            }
        }

        [ContextMenu("Debug UI Info")]
        private void DebugUIInfo()
        {
            if (chatHistory == null)
            {
                Debug.LogError("ChatHistory TextMeshProUGUI is NULL");
                return;
            }

            Debug.Log($"GameObject: {chatHistory.gameObject.name}");
            Debug.Log($"Active in Hierarchy: {chatHistory.gameObject.activeInHierarchy}");
            Debug.Log($"Component Enabled: {chatHistory.enabled}");
            Debug.Log($"Current Text: '{chatHistory.text}'");
            Debug.Log($"Font Asset: {chatHistory.font}");
            Debug.Log($"Font Size: {chatHistory.fontSize}");
            Debug.Log($"Color: {chatHistory.color}");
            Debug.Log($"Rich Text Enabled: {chatHistory.richText}");
            Debug.Log($"Auto Size Options: {chatHistory.enableAutoSizing}");

            var rectTransform = chatHistory.GetComponent<RectTransform>();
            Debug.Log($"RectTransform Size: {rectTransform.rect.size}");
            Debug.Log($"RectTransform Position: {rectTransform.position}");
        }

        #endregion

        #region Server Administration (дополнительные серверные методы)

        [Server]
        public void BroadcastSystemMessage(string message)
        {
            AddSystemMessageServer(message);
        }

        [Server]
        public void ClearAllMessages()
        {
            ClearChatServer();
        }

        [Server]
        public List<string> GetChatHistory()
        {
            return new List<string>(messageHistory);
        }

        #endregion
    }
}
