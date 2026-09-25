using System.Collections.Generic;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySteamProfile : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Button joinButton;

    [Header("Аватары игроков")]
    [Tooltip("Grid Layout Group, в который будут спавниться аватарки")]
    [SerializeField] private GridLayoutGroup avatarsGrid;
    [Tooltip("Префаб с Image + скриптом PlayerAvatarImage")]
    [SerializeField] private PlayerAvatarImage avatarPrefab;
    [Tooltip("Максимум аватарок в одном лобби")]
    [SerializeField, Min(1)] private int maxAvatars = 5;

    public string LobbyName { get; private set; } = "";

    private CSteamID lobbyId;
    private CSteamID hostId;
    private float pingTimer;

    private readonly List<PlayerAvatarImage> spawnedAvatars = new List<PlayerAvatarImage>();

    private void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClicked);
    }

    /// <summary>
    /// Заполняет префаб данными лобби. Вызывается из LobbySteamList.
    /// </summary>
    public void Setup(CSteamID lobby)
    {
        lobbyId = lobby;
        hostId = SteamMatchmaking.GetLobbyOwner(lobbyId);

        // Название: "Ник пользователя Steam - Lobby"
        string hostName = SteamMatchmaking.GetLobbyData(lobbyId, SteamLobbyHost.HostNameKey);
        if (string.IsNullOrEmpty(hostName))
            hostName = SteamFriends.GetFriendPersonaName(hostId);

        LobbyName = $"{hostName} - Lobby";
        lobbyNameText.text = LobbyName;

        // Количество игроков
        int current = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        int max = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);
        playerCountText.text = $"{current}/{max}";

        // Аватары игроков
        RefreshAvatars();

        // Пинг
        pingText.text = "-- ms";
        UpdatePing();
    }

    // ---------------- АВАТАРЫ ----------------

    private void RefreshAvatars()
    {
        if (avatarsGrid == null || avatarPrefab == null) return;

        // Очищаем прошлые аватарки (Setup вызывается повторно при обновлении данных лобби)
        foreach (var avatar in spawnedAvatars)
        {
            if (avatar != null)
                Destroy(avatar.gameObject);
        }
        spawnedAvatars.Clear();

        int members = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        int count = Mathf.Min(members, maxAvatars);

        for (int i = 0; i < count; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);

            PlayerAvatarImage avatar = Instantiate(avatarPrefab, avatarsGrid.transform);
            avatar.Setup(memberId);
            spawnedAvatars.Add(avatar);
        }
    }

    // ---------------- ПИНГ ----------------

    private void Update()
    {
        pingTimer += Time.unscaledDeltaTime;
        if (pingTimer >= 2f)
        {
            pingTimer = 0f;
            UpdatePing();
        }
    }

    private void UpdatePing()
    {
        if (!lobbyId.IsValid()) return;

        if (SteamNetworkingUtils.GetLocalPingLocation(out SteamNetworkPingLocation_t localLoc) > 0f)
        {
            string hostLocStr = SteamMatchmaking.GetLobbyData(lobbyId, SteamLobbyHost.PingLocationKey);
            if (!string.IsNullOrEmpty(hostLocStr) &&
                SteamNetworkingUtils.ParsePingLocationString(hostLocStr, out SteamNetworkPingLocation_t hostLoc))
            {
                int ping = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref hostLoc);
                if (ping >= 0)
                {
                    pingText.text = $"{ping} ms";
                    return;
                }
            }
        }

        pingText.text = "-- ms";
    }

    // ---------------- JOIN ----------------

    private void OnJoinClicked()
    {
        if (!lobbyId.IsValid()) return;

        joinButton.interactable = false;
        SteamLobbyHost.Instance.JoinLobby(lobbyId);
    }
}