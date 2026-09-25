using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>
/// Компонент на Player Prefab.
/// Хранит имя игрока в SyncVar и синхронизирует его на всех клиентах.
/// Отображает имя в TMP InputField (UI) и в 3D TextMesh (над персонажем).
/// Если Steamworks.NET инициализирован, при первом входе локального игрока
/// автоматически подставляет его Steam-никнейм (только если поле ещё пустое).
/// </summary>
public class NetworkPlayerName : NetworkBehaviour
{
    // -- Список всех игроков в сети ---------------------------------------------
    public static readonly List<NetworkPlayerName> AllPlayers = new();

    // -- Синхронизированное имя -----------------------------------------------------
    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string nickname = "";

    // -- Ссылки на компоненты UI ------------------------------------------------
    [Header("UI (TMP InputField)")]
    [Tooltip("Поле ввода имени. При потере фокуса/подтверждении отправит на сервер имя.")]
    [SerializeField] private TMP_InputField nicknameInputField;

    [Header("3D Text (TextMesh)")]
    [Tooltip("3D-текст над персонажем, куда пишем никнейм.")]
    [SerializeField] private TextMesh nicknameTextMesh;

    [Header("Steam Integration")]
    [Tooltip("Если включено и Steamworks.NET инициализирован, подставлять Steam-никнейм автоматически, когда серверное имя ещё не задано.")]
    [SerializeField] private bool useSteamNameByDefault = true;

    // -- Lifecycle ------------------------------------------------------------------
    public override void OnStartClient()
    {
        base.OnStartClient();
        AllPlayers.Add(this);

        // Применяем текущее значение ника, т.к. хук SyncVar
        // не вызывается при старте, при подключении к уже созданному игроку.
        ApplyNickname(nickname);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (nicknameInputField != null)
        {
            nicknameInputField.text = nickname;
            nicknameInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        }

        TrySubmitSteamNameIfEmpty();
        TrySubmitRandomNameIfEmpty();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        AllPlayers.Remove(this);

        if (nicknameInputField != null)
            nicknameInputField.onEndEdit.RemoveListener(OnInputFieldSubmit);
    }

    // -- Подстановка Steam-никнейма по умолчанию -----------------------------------
    private void TrySubmitSteamNameIfEmpty()
    {
        if (!useSteamNameByDefault) return;
        if (!string.IsNullOrEmpty(nickname)) return; // сервер уже хранит имя — не перетираем

        string steamName = GetSteamPersonaNameSafe();
        if (string.IsNullOrEmpty(steamName)) return;

        if (nicknameInputField != null)
            nicknameInputField.text = steamName;

        CmdSetNickname(steamName);
    }

    // -- Автогенерация ника вида "Player_<случайное число>" -----------------------
    private void TrySubmitRandomNameIfEmpty()
    {
        if (!string.IsNullOrEmpty(nickname)) return; // ник уже задан (например, Steam-именем)
        if (nicknameInputField != null && !string.IsNullOrEmpty(nicknameInputField.text)) return; // уже что-то введено в поле

        string randomName = GenerateRandomName();

        if (nicknameInputField != null)
            nicknameInputField.text = randomName;

        CmdSetNickname(randomName);
    }

    /// <summary>
    /// Генерирует имя вида "Player_1234" со случайным числом.
    /// </summary>
    private string GenerateRandomName()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100000);
        return $"Player_{randomNumber}";
    }

    /// <summary>
    /// Безопасно получает текущее отображаемое имя пользователя в Steam.
    /// Возвращает пустую строку, если Steamworks.NET не инициализирован
    /// (например, игра запущена не через Steam или билд без Steam).
    /// </summary>
    private string GetSteamPersonaNameSafe()
    {
        try
        {
            if (!SteamManager.Initialized) return string.Empty;
            return SteamFriends.GetPersonaName();
        }
        catch
        {
            // SteamManager может отсутствовать в проекте или Steam API не запущен.
            return string.Empty;
        }
    }

    /// <summary>
    /// Публичный метод для кнопки "Использовать Steam-имя" в UI —
    /// принудительно подставляет Steam-ник, даже если имя уже задано.
    /// </summary>
    public void UseSteamNameButton_OnClick()
    {
        string steamName = GetSteamPersonaNameSafe();
        if (string.IsNullOrEmpty(steamName)) return;

        if (nicknameInputField != null)
            nicknameInputField.text = steamName;

        CmdSetNickname(steamName);
    }

    // -- Ввод нового ника через InputField ---------------------------------------------
    private void OnInputFieldSubmit(string value)
    {
        CmdSetNickname(value);
    }

    // -- Command: клиент -> сервер -------------------------------------------------------
    [Command]
    public void CmdSetNickname(string newName)
    {
        newName = newName.Trim();
        if (newName.Length == 0 || newName.Length > 32) return;
        nickname = newName; // SyncVar -> автоматически разошлёт всем клиентам
    }

    // -- Hook: срабатывает у КАЖДОГО клиента при изменении SyncVar -----------------------
    void OnNicknameChanged(string _, string newValue)
    {
        ApplyNickname(newValue);
    }

    // -- Обновление ника в UI (TMP InputField) и в 3D-тексте (TextMesh) -----------------
    private void ApplyNickname(string value)
    {
        if (nicknameTextMesh != null)
            nicknameTextMesh.text = value;

        // У локального игрока не перезаписываем поле ввода, если он его сейчас редактирует
        if (isLocalPlayer && nicknameInputField != null && !nicknameInputField.isFocused)
            nicknameInputField.text = value;
    }
}