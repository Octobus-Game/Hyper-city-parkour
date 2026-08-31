using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Mirror.Examples.Chat
{
    public class ChatUI : NetworkBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] Text chatHistory;
        [SerializeField] Scrollbar scrollbar;
        [SerializeField] InputField chatMessage;
        [SerializeField] Button sendButton;

        [Header("Player Name")]
        [Tooltip("TextMeshPro компонент с именем локального игрока")]
        [SerializeField] TMP_Text playerNameTMP;

        // Имя локального игрока (только на клиенте)
        internal static string localPlayerName;

        // Словарь соединений -> имена (только на сервере)
        internal static readonly Dictionary<NetworkConnectionToClient, string> connNames =
            new Dictionary<NetworkConnectionToClient, string>();

        public override void OnStartServer()
        {
            connNames.Clear();
        }

        public override void OnStartClient()
        {
            chatHistory.text = "";
        }

        // Вызывается только у локального игрока
        public override void OnStartLocalPlayer()
        {
            if (playerNameTMP != null && !string.IsNullOrWhiteSpace(playerNameTMP.text))
            {
                localPlayerName = playerNameTMP.text.Trim();
            }
            else
            {
                // Фолбэк: берём имя с Player-компонента если TMP не задан
                Player player = GetComponent<Player>();
                if (player != null)
                    localPlayerName = player.playerName;
            }

            Debug.Log($"[ChatUI] localPlayerName установлен: '{localPlayerName}'");
        }

        [Command(requiresAuthority = false)]
        void CmdSend(string message, NetworkConnectionToClient sender = null)
        {
            if (sender == null) return;

            // Регистрируем имя игрока при первом сообщении
            if (!connNames.ContainsKey(sender))
            {
                Player player = sender.identity != null
                    ? sender.identity.GetComponent<Player>()
                    : null;

                string name = (player != null && !string.IsNullOrWhiteSpace(player.playerName))
                    ? player.playerName
                    : $"Player_{sender.connectionId}";

                connNames.Add(sender, name);
            }

            if (!string.IsNullOrWhiteSpace(message))
                RpcReceive(connNames[sender], message.Trim());
        }

        [ClientRpc]
        void RpcReceive(string playerName, string message)
        {
            // Защита от пустых данных
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "Unknown";
            if (string.IsNullOrWhiteSpace(message)) return;

            bool isLocal = !string.IsNullOrEmpty(localPlayerName) &&
                           playerName == localPlayerName;

            string prettyMessage = isLocal
                ? $"<color=red>{playerName}:</color> {message}"
                : $"<color=blue>{playerName}:</color> {message}";

            AppendMessage(prettyMessage);
        }

        void AppendMessage(string message)
        {
            StartCoroutine(AppendAndScroll(message));
        }

        IEnumerator AppendAndScroll(string message)
        {
            chatHistory.text += message + "\n";
            yield return null;
            yield return null;
            scrollbar.value = 0;
        }

        public void ExitButtonOnClick()
        {
            NetworkManager.singleton.StopHost();
        }

        public void ToggleButton(string input)
        {
            sendButton.interactable = !string.IsNullOrWhiteSpace(input);
        }

        public void OnEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetButtonDown("Submit"))
                SendMessage();
        }

        public void SendMessage()
        {
            if (!string.IsNullOrWhiteSpace(chatMessage.text))
            {
                CmdSend(chatMessage.text.Trim());
                chatMessage.text = string.Empty;
                chatMessage.ActivateInputField();
            }
        }
    }
}