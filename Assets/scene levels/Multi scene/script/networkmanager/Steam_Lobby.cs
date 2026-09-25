using System.Collections;
using Mirror;
using Mirror.FizzySteam;
using Steamworks;
using UnityEngine;


public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    [Header("Refs")]
    [SerializeField] private NetworkManager networkManager; 

    [Header("Lobby settings")]
    [SerializeField] private int maxMembers = 8;

    public CSteamID CurrentLobbyID { get; private set; }

    // --- Callbacks Steam ---
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyMatchList_t> lobbyMatchList; 

    private const string HostAddressKey = "HostAddress";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    // ---------------------------------------------------------------
    // СОЗДАНИЕ ЛОББИ (хост)
    // ---------------------------------------------------------------
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
        // Для публичного поиска используйте k_ELobbyTypePublic
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Не удалось создать лобби: {callback.m_eResult}");
            return;
        }

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Запускаем Mirror-хост
        networkManager.StartHost();

        // Сохраняем адрес хоста в метаданные лобби —
        // это нужно, чтобы клиенты, зашедшие через инвайт, знали, куда подключаться
        SteamMatchmaking.SetLobbyData(
            CurrentLobbyID,
            HostAddressKey,
            SteamUser.GetSteamID().ToString());

        SteamMatchmaking.SetLobbyData(
            CurrentLobbyID,
            "name",
            $"{SteamFriends.GetPersonaName()}'s Lobby");

        Debug.Log($"Лобби создано: {CurrentLobbyID}");
    }

    // ---------------------------------------------------------------
    // ПРИГЛАШЕНИЕ ДРУЗЕЙ
    // ---------------------------------------------------------------

    /// <summary>Открывает стандартное окно Steam "Пригласить друзей" для текущего лобби.</summary>
    public void OpenInviteOverlay()
    {
        if (CurrentLobbyID.m_SteamID == 0)
        {
            Debug.LogWarning("Лобби ещё не создано.");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
    }

    /// <summary>Прямое приглашение конкретного друга без открытия оверлея-диалога.</summary>
    public void InviteFriend(CSteamID friendSteamID)
    {
        if (CurrentLobbyID.m_SteamID == 0) return;
        SteamMatchmaking.InviteUserToLobby(CurrentLobbyID, friendSteamID);
    }

    // ---------------------------------------------------------------
    // ОБРАБОТКА ПРИГЛАШЕНИЯ (когда друг кликает "Присоединиться к игре"
    // в Steam-клиенте или принимает инвайт)
    // ---------------------------------------------------------------
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"Получен запрос на вход в лобби: {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    // ---------------------------------------------------------------
    // ВХОД В ЛОББИ (и хост, и клиент проходят этот callback)
    // ---------------------------------------------------------------
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Если мы сами хост — Mirror-сервер уже запущен в OnLobbyCreated, выходим
        if (NetworkServer.active) return;

        // Получаем SteamID хоста из метаданных лобби
        string hostAddress = SteamMatchmaking.GetLobbyData(
            CurrentLobbyID,
            HostAddressKey);

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("Не удалось получить адрес хоста из лобби.");
            return;
        }

        // FizzySteamworks в качестве networkAddress ожидает SteamID хоста в виде строки
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        Debug.Log($"Подключаемся к хосту: {hostAddress}");
    }

    // ---------------------------------------------------------------
    // ВЫХОД ИЗ ЛОББИ
    // ---------------------------------------------------------------
    public void LeaveLobby()
    {
        if (CurrentLobbyID.m_SteamID == 0) return;

        SteamMatchmaking.LeaveLobby(CurrentLobbyID);
        CurrentLobbyID = CSteamID.Nil;

        if (NetworkServer.active && NetworkClient.isConnected)
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();
    }
}