using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using System;

namespace mirror_friend_steam
{
    public class SteamInviteManager : MonoBehaviour
    {
        [Header("Network Settings")]
        public NetworkManager networkManager;

        [Header("UI References")]
        public GameObject friendsListPanel;
        public Transform friendsListContent;
        public GameObject friendItemPrefab;

        [Header("Invite Settings")]
        public string inviteMessage = "Join my game!";
        public int maxPlayersInLobby = 4;

        private List<CSteamID> friendsList = new List<CSteamID>();
        private Dictionary<CSteamID, GameObject> friendsUIItems = new Dictionary<CSteamID, GameObject>();
        private CSteamID currentLobbyID;
        private bool isHost = false;

        // Steam Callbacks
        private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestedCallback;
        private Callback<LobbyCreated_t> lobbyCreatedCallback;
        private Callback<LobbyEnter_t> lobbyEnterCallback;
        private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;

        void Start()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam not initialized!");
                return;
            }

            InitializeSteamCallbacks();
            LoadFriendsList();
        }

        void InitializeSteamCallbacks()
        {
            lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        }

        void LoadFriendsList()
        {
            friendsList.Clear();

            int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

            for (int i = 0; i < friendCount; i++)
            {
                CSteamID friendID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

                // Проверяем что друг онлайн и играет в ту же игру
                if (SteamFriends.GetFriendPersonaState(friendID) != EPersonaState.k_EPersonaStateOffline)
                {
                    friendsList.Add(friendID);
                }
            }

            UpdateFriendsUI();
        }

        void UpdateFriendsUI()
        {
            // Очищаем существующие элементы UI
            foreach (var item in friendsUIItems.Values)
            {
                if (item != null)
                    Destroy(item);
            }
            friendsUIItems.Clear();

            // Создаем новые элементы для друзей
            foreach (CSteamID friendID in friendsList)
            {
                CreateFriendUIItem(friendID);
            }
        }

        void CreateFriendUIItem(CSteamID friendID)
        {
            if (friendItemPrefab == null || friendsListContent == null) return;

            GameObject friendItem = Instantiate(friendItemPrefab, friendsListContent);
            SteamFriendItem friendScript = friendItem.GetComponent<SteamFriendItem>();

            if (friendScript != null)
            {
                string friendName = SteamFriends.GetFriendPersonaName(friendID);
                EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendID);

                friendScript.SetupFriend(friendID, friendName, friendState, this);
                friendsUIItems[friendID] = friendItem;
            }
        }

        // Создание лобби для хоста
        public void CreateLobby()
        {
            if (!SteamManager.Initialized) return;

            Debug.Log("Creating Steam lobby...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayersInLobby);
        }

        // Callback при создании лобби
        void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
                isHost = true;

                // Устанавливаем метаданные лобби
                SteamMatchmaking.SetLobbyData(currentLobbyID, "name", $"{SteamFriends.GetPersonaName()}'s Game");
                SteamMatchmaking.SetLobbyData(currentLobbyID, "version", Application.version);

                // Запускаем хост
                StartHost();

                Debug.Log($"Lobby created successfully! ID: {currentLobbyID}");
            }
            else
            {
                Debug.LogError($"Failed to create lobby: {callback.m_eResult}");
            }
        }

        // Приглашение друга
        public void InviteFriend(CSteamID friendID)
        {
            if (!SteamManager.Initialized || !currentLobbyID.IsValid())
            {
                // Если лобби не создано, создаем его сначала
                CreateLobby();
                StartCoroutine(WaitAndInvite(friendID));
                return;
            }

            bool success = SteamMatchmaking.InviteUserToLobby(currentLobbyID, friendID);

            if (success)
            {
                string friendName = SteamFriends.GetFriendPersonaName(friendID);
                Debug.Log($"Invited {friendName} to lobby");

                // Показываем уведомление
                ShowNotification($"Invited {friendName} to game");
            }
            else
            {
                Debug.LogError("Failed to invite friend");
            }
        }

        IEnumerator WaitAndInvite(CSteamID friendID)
        {
            // Ждем пока лобби создастся
            float timeout = 10f;
            while (!currentLobbyID.IsValid() && timeout > 0)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            if (currentLobbyID.IsValid())
            {
                InviteFriend(friendID);
            }
        }

        // Callback при запросе присоединения к лобби
        void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            Debug.Log($"Lobby join requested: {callback.m_steamIDLobby}");
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        // Callback при входе в лобби
        void OnLobbyEnter(LobbyEnter_t callback)
        {
            currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

            if (callback.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log("Successfully entered lobby");

                // Получаем информацию о лобби
                string lobbyName = SteamMatchmaking.GetLobbyData(currentLobbyID, "name");
                Debug.Log($"Joined lobby: {lobbyName}");

                // Если мы не хост, подключаемся как клиент
                if (!isHost)
                {
                    // Получаем IP хоста из лобби или используем Steam P2P
                    ConnectAsClient();
                }
            }
            else
            {
                Debug.LogError($"Failed to enter lobby: {callback.m_EChatRoomEnterResponse}");
            }
        }

        // Callback при изменении состава лобби
        void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            CSteamID userID = new CSteamID(callback.m_ulSteamIDUserChanged);
            string userName = SteamFriends.GetFriendPersonaName(userID);

            if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                Debug.Log($"{userName} joined the lobby");
                ShowNotification($"{userName} joined the game");
            }
            else if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
            {
                Debug.Log($"{userName} left the lobby");
                ShowNotification($"{userName} left the game");
            }
        }

        void StartHost()
        {
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<NetworkManager>();
            }

            if (networkManager != null)
            {
                networkManager.StartHost();
                Debug.Log("Started hosting game");
            }
        }

        void ConnectAsClient()
        {
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<NetworkManager>();
            }

            if (networkManager != null)
            {
                // Здесь можно использовать Steam P2P или получить IP из лобби
                // Для простоты используем localhost (в реальном проекте нужно Steam P2P)
                networkManager.networkAddress = "localhost";
                networkManager.StartClient();
                Debug.Log("Started client connection");
            }
        }

        // Покинуть лобби
        public void LeaveLobby()
        {
            if (currentLobbyID.IsValid())
            {
                SteamMatchmaking.LeaveLobby(currentLobbyID);
                currentLobbyID = CSteamID.Nil;
                isHost = false;

                if (networkManager != null)
                {
                    if (NetworkServer.active)
                        networkManager.StopHost();
                    else
                        networkManager.StopClient();
                }

                Debug.Log("Left lobby");
            }
        }

        // Показать/скрыть список друзей
        public void ToggleFriendsList()
        {
            if (friendsListPanel != null)
            {
                friendsListPanel.SetActive(!friendsListPanel.activeSelf);

                if (friendsListPanel.activeSelf)
                {
                    LoadFriendsList(); // Обновляем список при открытии
                }
            }
        }

        void ShowNotification(string message)
        {
            Debug.Log($"Notification: {message}");
            // Здесь можно добавить UI уведомление
        }

        // Получить список участников лобби
        public List<CSteamID> GetLobbyMembers()
        {
            List<CSteamID> members = new List<CSteamID>();

            if (currentLobbyID.IsValid())
            {
                int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);

                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
                    members.Add(memberID);
                }
            }

            return members;
        }

        void OnDestroy()
        {
            LeaveLobby();
        }

        // Публичные методы для UI
        public bool IsInLobby() => currentLobbyID.IsValid();
        public bool IsLobbyHost() => isHost;
        public CSteamID GetCurrentLobbyID() => currentLobbyID;
    }

    // Компонент для элемента друга в UI
    public class SteamFriendItem : MonoBehaviour
    {
        [Header("UI Components")]
        public UnityEngine.UI.Text nameText;
        public UnityEngine.UI.Text statusText;
        public UnityEngine.UI.Button inviteButton;
        public UnityEngine.UI.Image statusIcon;

        [Header("Status Colors")]
        public Color onlineColor = Color.green;
        public Color awayColor = Color.yellow;
        public Color busyColor = Color.red;
        public Color offlineColor = Color.gray;

        private CSteamID friendID;
        private SteamInviteManager inviteManager;

        public void SetupFriend(CSteamID steamID, string friendName, EPersonaState status, SteamInviteManager manager)
        {
            friendID = steamID;
            inviteManager = manager;

            if (nameText != null)
                nameText.text = friendName;

            if (statusText != null)
                statusText.text = GetStatusString(status);

            if (statusIcon != null)
                statusIcon.color = GetStatusColor(status);

            if (inviteButton != null)
            {
                inviteButton.onClick.RemoveAllListeners();
                inviteButton.onClick.AddListener(() => inviteManager.InviteFriend(friendID));

                // Отключаем кнопку если друг оффлайн
                inviteButton.interactable = status != EPersonaState.k_EPersonaStateOffline;
            }
        }

        string GetStatusString(EPersonaState status)
        {
            switch (status)
            {
                case EPersonaState.k_EPersonaStateOnline: return "Online";
                case EPersonaState.k_EPersonaStateAway: return "Away";
                case EPersonaState.k_EPersonaStateBusy: return "Busy";
                case EPersonaState.k_EPersonaStateSnooze: return "Snooze";
                case EPersonaState.k_EPersonaStateLookingToTrade: return "Looking to Trade";
                case EPersonaState.k_EPersonaStateLookingToPlay: return "Looking to Play";
                default: return "Offline";
            }
        }

        Color GetStatusColor(EPersonaState status)
        {
            switch (status)
            {
                case EPersonaState.k_EPersonaStateOnline:
                case EPersonaState.k_EPersonaStateLookingToPlay:
                case EPersonaState.k_EPersonaStateLookingToTrade:
                    return onlineColor;
                case EPersonaState.k_EPersonaStateAway:
                case EPersonaState.k_EPersonaStateSnooze:
                    return awayColor;
                case EPersonaState.k_EPersonaStateBusy:
                    return busyColor;
                default:
                    return offlineColor;
            }
        }
    }
}