using Mirror;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatBehavior : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI chatText = null;
    [SerializeField] private InputField inputField = null;
    [SerializeField] private GameObject canvas = null;

    [SerializeField] private int maxMessages = 5; // Максимум видимых сообщений

    [Header("Links")]
    [Tooltip("Нужна только если Canvas в режиме Screen Space - Camera / World Space. Для Overlay оставь пустым.")]
    [SerializeField] private Camera uiCamera = null;

    private static event Action<string> OnMessage;
    private static event Action<int> OnClearChat; // count <= 0 → очистить всё

    // Очередь хранит не более maxMessages строк
    private readonly Queue<string> messageQueue = new Queue<string>();

    // Ловит "http://..." и "https://..." до первого пробела
    private static readonly Regex UrlRegex = new Regex(@"(https?://[^\s]+)", RegexOptions.Compiled);

    // "/clear-chat" или "/clear-chat 3" (число — опционально)
    private static readonly Regex ClearCommandRegex =
        new Regex(@"^/clear-chat(?:\s+(\d+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    public override void OnStartAuthority()
    {
        canvas.SetActive(true);
        OnMessage += HandleNewMessage;
        OnClearChat += HandleClearChat;
    }

    public override void OnStopAuthority()
    {
        OnMessage -= HandleNewMessage;
        OnClearChat -= HandleClearChat;
    }

    [ClientCallback]
    private void OnDestroy()
    {
        if (!isOwned) { return; }
        OnMessage -= HandleNewMessage;
        OnClearChat -= HandleClearChat;
    }

    [Client]
    private void Update()
    {
        if (!isOwned) { return; }
        if (canvas == null || !canvas.activeInHierarchy) { return; }
        if (!Input.GetMouseButtonDown(0)) { return; }

        TryOpenClickedLink();
    }

    // ──────────────────────────────────────────────
    // Message handling
    // ──────────────────────────────────────────────

    private void HandleNewMessage(string message)
    {
        messageQueue.Enqueue(message);

        while (messageQueue.Count > maxMessages)
        {
            messageQueue.Dequeue();
        }

        chatText.text = string.Join("\n", messageQueue);
    }

    private void HandleClearChat(int count)
    {
        if (count <= 0 || count >= messageQueue.Count)
        {
            messageQueue.Clear();
        }
        else
        {
            // Удаляем count самых старых сообщений
            for (int i = 0; i < count; i++)
            {
                messageQueue.Dequeue();
            }
        }

        chatText.text = string.Join("\n", messageQueue);
    }

    // ──────────────────────────────────────────────
    // Input → Network
    // ──────────────────────────────────────────────

    [Client]
    public void Send()
    {
        if (!Input.GetKeyDown(KeyCode.Return)) { return; }
        if (string.IsNullOrWhiteSpace(inputField.text)) { return; }

        string text = inputField.text.Trim();

        Match clearMatch = ClearCommandRegex.Match(text);
        if (clearMatch.Success)
        {
            int count = clearMatch.Groups[1].Success
                ? int.Parse(clearMatch.Groups[1].Value)
                : -1; // -1 = очистить всё

            CmdClearChat(count);
            inputField.text = string.Empty;
            return;
        }

        CmdSendMessage(text);
        inputField.text = string.Empty;
    }

    [Command]
    private void CmdSendMessage(string message)
    {
        // Имя берётся на сервере из SyncVar — клиент ничего не подделает
        string playerName = GetSenderName();

        // Экранируем спецсимволы и оборачиваем найденные URL в кликабельную ссылку
        string formattedMessage = FormatMessageWithLinks(message);

        RpcHandleMessage($"[{playerName}]: {formattedMessage}");
    }

    [Command]
    private void CmdClearChat(int count)
    {
        if (!IsRequestFromHost())
        {
            // Не хост — отказываем и тихо сообщаем только ему
            TargetClearChatDenied();
            return;
        }

        RpcClearChat(count);
    }

    // true, только если команда пришла от подключения локального хоста (listen server)
    private bool IsRequestFromHost()
    {
        return NetworkServer.localConnection != null
            && connectionToClient == NetworkServer.localConnection;
    }

    // Выполняется на сервере: имя отправителя по connectionId
    private string GetSenderName()
    {
        return $"Player_{connectionToClient.connectionId}";
    }

    // Экранирует rich-text символы (защита от внедрения чужих TMP-тегов через чат),
    // затем оборачивает все найденные URL в <link> с подсветкой и подчёркиванием
    private string FormatMessageWithLinks(string rawMessage)
    {
        string escaped = rawMessage
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return UrlRegex.Replace(escaped, match =>
        {
            string url = match.Value;
            return $"<link=\"{url}\"><u><color=#4EA1F3>{url}</color></u></link>";
        });
    }

    [ClientRpc]
    private void RpcHandleMessage(string message)
    {
        OnMessage?.Invoke(message);
    }

    [ClientRpc]
    private void RpcClearChat(int count)
    {
        OnClearChat?.Invoke(count);
    }

    [TargetRpc]
    private void TargetClearChatDenied()
    {
        // Системное сообщение видно только тому, кто пытался выполнить команду
        HandleNewMessage("[Система]: команда /clear-chat доступна только хосту.");
    }

    // ──────────────────────────────────────────────
    // Link clicking
    // ──────────────────────────────────────────────

    private void TryOpenClickedLink()
    {
        if (chatText == null) { return; }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(chatText, Input.mousePosition, uiCamera);
        if (linkIndex == -1) { return; }

        TMP_LinkInfo linkInfo = chatText.textInfo.linkInfo[linkIndex];
        string url = linkInfo.GetLinkID();

        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
    }
}