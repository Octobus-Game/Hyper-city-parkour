using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class SteamLobbyHost : MonoBehaviour
{
    public static SteamLobbyHost Instance { get; private set; }

    // Ключи данных лобби
    public const string HostAddressKey = "HostAddress";
    public const string HostNameKey = "HostName";
    public const string PingKey = "Ping";
    public const string PingLocationKey = "PingLocation";
    public const string GameKey = "game";
    public const string GameValue = "Steam480Mirror"; // уникальная метка вашей игры

    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("Настройки лобби")]
    [SerializeField, Min(2)] private int maxPlayers = 4;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;

    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<LobbyMatchList_t> lobbyMatchList;

    private NetworkManager networkManager;

    // true, когда Client Button ищет лобби (чтобы не реагировать на запросы списка из LobbySteamList)
    private bool isQuickJoinSearching;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        networkManager = NetworkManager.singleton;

        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam не инициализирован! Запустите Steam и проверьте steam_appid.txt (480).");
            SetButtonsInteractable(false);
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);

        if (hostButton != null)
            hostButton.onClick.AddListener(HostLobby);

        if (clientButton != null)
            clientButton.onClick.AddListener(JoinAnyLobby);

        // Заранее запрашиваем данные о relay-сети для расчёта пинга
        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveListener(HostLobby);

        if (clientButton != null)
            clientButton.onClick.RemoveListener(JoinAnyLobby);

        lobbyCreated?.Dispose();
        gameLobbyJoinRequested?.Dispose();
        lobbyEntered?.Dispose();
        lobbyMatchList?.Dispose();
    }

    private void SetButtonsInteractable(bool value)
    {
        if (hostButton != null) hostButton.interactable = value;
        if (clientButton != null) clientButton.interactable = value;
    }

    // ---------------- HOST ----------------

    public void HostLobby()
    {
        if (!SteamManager.Initialized) return;

        SetButtonsInteractable(false);

        SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Не удалось создать лобби: {callback.m_eResult}");
            SetButtonsInteractable(true);
            return;
        }

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        // Запускаем Mirror хост
        networkManager.StartHost();

        // Данные лобби (видны в списке)
        SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(lobbyId, HostNameKey, SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(lobbyId, GameKey, GameValue);

        // Публикуем ping-location хоста, чтобы клиенты могли оценить пинг до него
        if (SteamNetworkingUtils.GetLocalPingLocation(out SteamNetworkPingLocation_t loc) >= 0f)
        {
            SteamNetworkingUtils.ConvertPingLocationToString(ref loc, out string locStr, 512);
            SteamMatchmaking.SetLobbyData(lobbyId, PingLocationKey, locStr);
        }

        Debug.Log($"Лобби создано: {lobbyId}");
    }

    // ---------------- CLIENT (Quick Join) ----------------

    /// <summary>
    /// Кнопка Client: ищет лобби нашей игры и подключается к первому подходящему.
    /// </summary>
    public void JoinAnyLobby()
    {
        if (!SteamManager.Initialized) return;
        if (NetworkClient.active || NetworkServer.active) return;

        SetButtonsInteractable(false);
        isQuickJoinSearching = true;

        SteamMatchmaking.AddRequestLobbyListStringFilter(
            GameKey, GameValue, ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        // Берём только свободные лобби (есть хотя бы одно место)
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);

        SteamMatchmaking.RequestLobbyList();
    }

    private void OnLobbyMatchList(LobbyMatchList_t result)
    {
        // Реагируем только на поиск, запущенный Client Button
        if (!isQuickJoinSearching) return;
        isQuickJoinSearching = false;

        if (result.m_nLobbiesMatching == 0)
        {
            Debug.LogWarning("Свободных лобби не найдено.");
            SetButtonsInteractable(true);
            return;
        }

        CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
        Debug.Log($"Подключаемся к лобби: {lobbyId}");
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    // ---------------- JOIN ----------------

    /// <summary>
    /// Вход в конкретное лобби из списка (кнопка Join на префабе).
    /// </summary>
    public void JoinLobby(CSteamID lobbyId)
    {
        SetButtonsInteractable(false);
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    // Приглашение через Steam overlay (друг пригласил / Join Game)
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SetButtonsInteractable(false);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // Хост сам зашёл в своё лобби — ничего делать не нужно
        if (NetworkServer.active) return;

        // Если вход в лобби не удался — возвращаем кнопки
        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"Не удалось войти в лобби: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}");
            SetButtonsInteractable(true);
            return;
        }

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressKey);

        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("В лобби нет адреса хоста.");
            SteamMatchmaking.LeaveLobby(lobbyId);
            SetButtonsInteractable(true);
            return;
        }

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
    }

    //  LEAVE 

    public void LeaveLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.LeaveLobby(lobbyId);

        if (NetworkServer.active && NetworkClient.isConnected)
            networkManager.StopHost();
        else if (NetworkClient.isConnected)
            networkManager.StopClient();

        SetButtonsInteractable(true);
    }
}