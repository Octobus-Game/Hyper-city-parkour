using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySteamList : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GridLayoutGroup gridLayoutGroup;   // родитель для префабов
    [SerializeField] private LobbySteamProfile lobbyProfilePrefab;
    [SerializeField] private Button refreshLobbyButton;
    [SerializeField] private TMP_InputField searchInputField;   // поиск по названию

    [Header("Статистика")]
    [SerializeField] private TMP_Text playersOnlineText;   // "N игроков играет"
    [SerializeField] private TMP_Text serversCountText;    // "N серверов"

    [Tooltip("Брать общее число игроков из Steam (SteamUserStats), а не считать по лобби. " +
             "Для App ID 480 это будут ВСЕ игроки Spacewar по миру.")]
    [SerializeField] private bool useGlobalSteamPlayerCount = false;

    [Header("Настройки")]
    [Tooltip("Сколько префабов лобби максимум отображать (после фильтрации)")]
    [SerializeField, Min(1)] private int maxDisplayedLobbies = 10;

    [Tooltip("Сколько лобби запрашивать у Steam (с запасом, чтобы поиск было из чего выбирать)")]
    [SerializeField, Min(1)] private int maxRequestedLobbies = 50;

    [Tooltip("Если включено — показывает ВСЕ лобби App ID 480 (для отладки). Выключи в релизе.")]
    [SerializeField] private bool ignoreGameFilter = false;

    private readonly List<LobbySteamProfile> spawned = new List<LobbySteamProfile>();
    private readonly Dictionary<ulong, LobbySteamProfile> pending = new Dictionary<ulong, LobbySteamProfile>();

    private Callback<LobbyMatchList_t> lobbyMatchListCallback;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdateCallback;
    private CallResult<NumberOfCurrentPlayers_t> numberOfCurrentPlayersCallResult;

    private string searchQuery = "";

    private void Awake()
    {
        if (refreshLobbyButton != null)
            refreshLobbyButton.onClick.AddListener(RefreshLobbies);

        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchChanged);
    }

    private IEnumerator Start()
    {
        while (!SteamManager.Initialized)
            yield return null;

        lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
        numberOfCurrentPlayersCallResult =
            CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);

        Debug.Log("[LobbySteamList] Steam готов, запрашиваю список лобби.");
        RefreshLobbies();
    }

    private void OnDestroy()
    {
        if (refreshLobbyButton != null)
            refreshLobbyButton.onClick.RemoveListener(RefreshLobbies);

        if (searchInputField != null)
            searchInputField.onValueChanged.RemoveListener(OnSearchChanged);

        lobbyMatchListCallback?.Dispose();
        lobbyDataUpdateCallback?.Dispose();
        numberOfCurrentPlayersCallResult?.Dispose();
    }

    // ---------------- ПОИСК ----------------

    private void OnSearchChanged(string text)
    {
        searchQuery = text.Trim();
        ApplyFilter();
    }

    /// <summary>
    /// Показывает только те лобби, чьё название содержит строку поиска.
    /// Ограничивает количество видимых префабов значением maxDisplayedLobbies.
    /// </summary>
    private void ApplyFilter()
    {
        int shown = 0;

        foreach (var item in spawned)
        {
            if (item == null) continue;

            bool matches = string.IsNullOrEmpty(searchQuery) ||
                           item.LobbyName.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0;

            bool visible = matches && shown < maxDisplayedLobbies;
            item.gameObject.SetActive(visible);

            if (visible) shown++;
        }
    }

    // ---------------- ЗАПРОС СПИСКА ----------------

    public void RefreshLobbies()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[LobbySteamList] Steam не инициализирован!");
            return;
        }

        ClearList();

        if (!ignoreGameFilter)
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter(
                SteamLobbyHost.GameKey, SteamLobbyHost.GameValue,
                ELobbyComparison.k_ELobbyComparisonEqual);
        }

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(
            ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        // Запрашиваем с запасом — фильтрация по названию идёт локально
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(maxRequestedLobbies);

        Debug.Log("[LobbySteamList] RequestLobbyList отправлен.");
        SteamMatchmaking.RequestLobbyList();

        // Пока идёт запрос — показываем прочерки
        UpdateStatsTexts(0, 0, loading: true);

        if (useGlobalSteamPlayerCount && numberOfCurrentPlayersCallResult != null)
        {
            SteamAPICall_t call = SteamUserStats.GetNumberOfCurrentPlayers();
            numberOfCurrentPlayersCallResult.Set(call);
        }
    }

    private void OnLobbyMatchList(LobbyMatchList_t result)
    {
        Debug.Log($"[LobbySteamList] Найдено лобби: {result.m_nLobbiesMatching}");

        ClearList();

        int total = (int)result.m_nLobbiesMatching;
        int count = Mathf.Min(total, maxRequestedLobbies);
        int playersInLobbies = 0;

        for (int i = 0; i < count; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

            LobbySteamProfile item = Instantiate(lobbyProfilePrefab, gridLayoutGroup.transform);
            spawned.Add(item);

            bool requested = SteamMatchmaking.RequestLobbyData(lobbyId);

            item.Setup(lobbyId);
            pending[lobbyId.m_SteamID] = item;

            if (!requested)
                pending.Remove(lobbyId.m_SteamID);

            playersInLobbies += SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        }

        ApplyFilter();

        // Серверов = все найденные лобби (не зависит от строки поиска)
        UpdateStatsTexts(playersInLobbies, count, loading: false);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t data)
    {
        if (pending.TryGetValue(data.m_ulSteamIDLobby, out LobbySteamProfile item) && item != null)
        {
            item.Setup(new CSteamID(data.m_ulSteamIDLobby));

            // Название могло измениться — пересчитываем фильтр
            ApplyFilter();
        }
    }

    private void ClearList()
    {
        foreach (var item in spawned)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawned.Clear();
        pending.Clear();
    }

    // ---------------- СТАТИСТИКА ----------------

    private void UpdateStatsTexts(int players, int servers, bool loading)
    {
        // Если включён глобальный счётчик Steam, текст игроков не трогаем —
        // его обновит OnNumberOfCurrentPlayers
        if (playersOnlineText != null && !(useGlobalSteamPlayerCount && !loading))
        {
            playersOnlineText.text = loading
                ? "- игроков играет"
                : $"{players} {Plural(players, "игрок играет", "игрока играют", "игроков играют")}";
        }

        if (serversCountText != null)
        {
            serversCountText.text = loading
                ? "- серверов"
                : $"{servers} {Plural(servers, "сервер", "сервера", "серверов")}";
        }
    }

    // Ответ Steam на GetNumberOfCurrentPlayers (общий онлайн игры)
    private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t data, bool ioFailure)
    {
        if (ioFailure || data.m_bSuccess != 1) return;
        if (playersOnlineText == null) return;

        int players = data.m_cPlayers;
        playersOnlineText.text =
            $"{players} {Plural(players, "игрок играет", "игрока играют", "игроков играют")}";
    }

    /// <summary>
    /// Русское склонение: 1 игрок, 2-4 игрока, 5+ игроков (с учётом 11-14).
    /// </summary>
    private static string Plural(int n, string one, string few, string many)
    {
        n = Mathf.Abs(n);
        int mod100 = n % 100;
        int mod10 = n % 10;

        if (mod100 >= 11 && mod100 <= 14) return many;
        if (mod10 == 1) return one;
        if (mod10 >= 2 && mod10 <= 4) return few;
        return many;
    }
}