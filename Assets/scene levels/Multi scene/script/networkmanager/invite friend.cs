using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace mirror_steam_name
{
    public class SteamInviteManager : MonoBehaviour
    {
        [Header("Lobby Settings")]
        public int maxLobbyMembers = 4;
        public ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

        [Header("Events")]
        public System.Action OnLobbyCreated;
        public System.Action OnLobbyJoined;
        public System.Action OnLobbyLeft;
        public System.Action<string> OnFriendJoined;
        public System.Action<string> OnFriendLeft;
        public System.Action<string> OnFriendInvited;

        private CSteamID currentLobbyID;
        private bool isInLobby = false;
        private List<CSteamID> lobbyMembers = new List<CSteamID>();

        // Steam Callbacks
        private Callback<LobbyCreated_t> lobbyCreatedCallback;
        private Callback<LobbyEnter_t> lobbyEnterCallback;
        private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
        private Callback<LobbyInvite_t> lobbyInviteCallback;
        private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequestedCallback;

        void Start()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized!");
                return;
            }

            InitializeCallbacks();
        }

        void InitializeCallbacks()
        {
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCallback);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnterCallback);
            lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdateCallback);
            lobbyInviteCallback = Callback<LobbyInvite_t>.Create(OnLobbyInviteCallback);
            gameLobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequestedCallback);
        }

        public void CreateLobby()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized!");
                return;
            }

            if (isInLobby)
            {
                Debug.Log("Already in a lobby!");
                return;
            }

            Debug.Log("Creating lobby...");
            SteamMatchmaking.CreateLobby(lobbyType, maxLobbyMembers);
        }

        public void LeaveLobby()
        {
            if (!isInLobby)
            {
                Debug.Log("Not in a lobby!");
                return;
            }

            Debug.Log("Leaving lobby...");
            SteamMatchmaking.LeaveLobby(currentLobbyID);

            isInLobby = false;
            currentLobbyID = CSteamID.Nil;
            lobbyMembers.Clear();

            OnLobbyLeft?.Invoke();
        }

        public void JoinLobby(CSteamID lobbyID)
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized!");
                return;
            }

            Debug.Log($"Joining lobby: {lobbyID}");
            SteamMatchmaking.JoinLobby(lobbyID);
        }

        public void InviteFriend(CSteamID friendID)
        {
            if (!isInLobby)
            {
                Debug.Log("Not in a lobby to invite to!");
                return;
            }

            Debug.Log($"Inviting friend: {friendID}");
            SteamMatchmaking.InviteUserToLobby(currentLobbyID, friendID);

            string friendName = SteamFriends.GetFriendPersonaName(friendID);
            OnFriendInvited?.Invoke(friendName);
        }

        public bool IsInLobby()
        {
            return isInLobby;
        }

        public bool IsLobbyHost()
        {
            if (!isInLobby) return false;
            return SteamMatchmaking.GetLobbyOwner(currentLobbyID) == SteamUser.GetSteamID();
        }

        public CSteamID GetCurrentLobbyID()
        {
            return currentLobbyID;
        }

        public List<CSteamID> GetLobbyMembers()
        {
            return new List<CSteamID>(lobbyMembers);
        }

        public int GetLobbyMemberCount()
        {
            return lobbyMembers.Count;
        }

        public CSteamID GetLobbyOwner()
        {
            if (!isInLobby) return CSteamID.Nil;
            return SteamMatchmaking.GetLobbyOwner(currentLobbyID);
        }

        // Steam Callback Handlers
        private void OnLobbyCreatedCallback(LobbyCreated_t callback)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log("Lobby created successfully!");
                currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;

                // Set lobby data
                SteamMatchmaking.SetLobbyData(currentLobbyID, "name", "Game Lobby");
                SteamMatchmaking.SetLobbyData(currentLobbyID, "game", Application.productName);

                OnLobbyCreated?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to create lobby: {callback.m_eResult}");
            }
        }

        private void OnLobbyEnterCallback(LobbyEnter_t callback)
        {
            if (callback.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log("Successfully entered lobby!");
                currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;
                isInLobby = true;

                UpdateLobbyMembers();
                OnLobbyJoined?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to enter lobby: {callback.m_EChatRoomEnterResponse}");
            }
        }

        private void OnLobbyChatUpdateCallback(LobbyChatUpdate_t callback)
        {
            CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;
            CSteamID userID = (CSteamID)callback.m_ulSteamIDUserChanged;

            if (lobbyID != currentLobbyID) return;

            string userName = SteamFriends.GetFriendPersonaName(userID);

            if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                Debug.Log($"User {userName} joined the lobby");
                UpdateLobbyMembers();
                OnFriendJoined?.Invoke(userName);
            }
            else if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0)
            {
                Debug.Log($"User {userName} left the lobby");
                UpdateLobbyMembers();
                OnFriendLeft?.Invoke(userName);
            }
            else if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0)
            {
                Debug.Log($"User {userName} disconnected from the lobby");
                UpdateLobbyMembers();
                OnFriendLeft?.Invoke(userName);
            }
            else if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked) != 0)
            {
                Debug.Log($"User {userName} was kicked from the lobby");
                UpdateLobbyMembers();
                OnFriendLeft?.Invoke(userName);
            }
        }

        private void OnLobbyInviteCallback(LobbyInvite_t callback)
        {
            CSteamID friendID = (CSteamID)callback.m_ulSteamIDUser;
            CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;

            string friendName = SteamFriends.GetFriendPersonaName(friendID);
            Debug.Log($"Received lobby invite from {friendName}");

            // You can automatically join or show a UI prompt
            // For now, we'll just log it
        }

        private void OnGameLobbyJoinRequestedCallback(GameLobbyJoinRequested_t callback)
        {
            CSteamID lobbyID = (CSteamID)callback.m_steamIDLobby;
            CSteamID friendID = (CSteamID)callback.m_steamIDFriend;

            Debug.Log($"Join requested for lobby: {lobbyID}");

            // Leave current lobby if in one
            if (isInLobby)
            {
                LeaveLobby();
            }

            // Join the requested lobby
            JoinLobby(lobbyID);
        }

        private void UpdateLobbyMembers()
        {
            if (!isInLobby) return;

            lobbyMembers.Clear();
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);

            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
                lobbyMembers.Add(memberID);
            }
        }

        void OnDestroy()
        {
            if (isInLobby)
            {
                LeaveLobby();
            }
        }
    }
}