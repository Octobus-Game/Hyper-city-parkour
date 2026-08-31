using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

namespace mirror_p2p_
{
    public class SteamP2PTransport : Transport
    {
        [Header("Steam P2P Settings")]
        public EP2PSend[] channels = { EP2PSend.k_EP2PSendReliable, EP2PSend.k_EP2PSendUnreliable };

        private CSteamID serverSteamID;
        private Dictionary<int, CSteamID> connectedClients = new Dictionary<int, CSteamID>();
        private int nextConnectionId = 1;

        // Callbacks
        private Callback<P2PSessionRequest_t> p2pSessionRequestCallback;
        private Callback<P2PSessionConnectFail_t> p2pSessionConnectFailCallback;

        void Start()
        {
            if (SteamManager.Initialized)
            {
                p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
                p2pSessionConnectFailCallback = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
            }
        }

        void OnP2PSessionRequest(P2PSessionRequest_t callback)
        {
            Debug.Log($"P2P Session request from: {callback.m_steamIDRemote}");

            // Автоматически принимаем запрос (в реальном проекте можно добавить проверки)
            SteamNetworking.AcceptP2PSessionWithUser(callback.m_steamIDRemote);
        }

        void OnP2PSessionConnectFail(P2PSessionConnectFail_t callback)
        {
            Debug.LogError($"P2P Session connect failed: {callback.m_eP2PSessionError}");

            // Находим соединение и отключаем его
            foreach (var kvp in connectedClients)
            {
                if (kvp.Value == callback.m_steamIDRemote)
                {
                    OnServerDisconnected.Invoke(kvp.Key);
                    connectedClients.Remove(kvp.Key);
                    break;
                }
            }
        }

        public override bool Available()
        {
            return SteamManager.Initialized;
        }

        public override void ClientConnect(string address)
        {
            if (!SteamManager.Initialized)
            {
                OnClientError.Invoke(TransportError.Unexpected, "Steam not initialized");
                return;
            }

            // Адрес должен быть Steam ID
            if (ulong.TryParse(address, out ulong steamID))
            {
                serverSteamID = new CSteamID(steamID);

                // Отправляем запрос на подключение
                byte[] connectMsg = System.Text.Encoding.UTF8.GetBytes("CONNECT");

                if (SteamNetworking.SendP2PPacket(serverSteamID, connectMsg, (uint)connectMsg.Length, EP2PSend.k_EP2PSendReliable, 0))
                {
                    OnClientConnected.Invoke();
                    Debug.Log($"Connected to server: {serverSteamID}");
                }
                else
                {
                    OnClientError.Invoke(TransportError.Unexpected, "Failed to send connect packet");
                }
            }
            else
            {
                OnClientError.Invoke(TransportError.Unexpected, "Invalid Steam ID format");
            }
        }

        public override void ClientConnect(Uri uri)
        {
            ClientConnect(uri.Host);
        }

        public override bool ClientConnected()
        {
            return serverSteamID.IsValid();
        }

        public override void ClientDisconnect()
        {
            if (serverSteamID.IsValid())
            {
                // Отправляем сообщение об отключении
                byte[] disconnectMsg = System.Text.Encoding.UTF8.GetBytes("DISCONNECT");
                SteamNetworking.SendP2PPacket(serverSteamID, disconnectMsg, (uint)disconnectMsg.Length, EP2PSend.k_EP2PSendReliable, 0);

                // Закрываем P2P сессию
                SteamNetworking.CloseP2PSessionWithUser(serverSteamID);

                serverSteamID = CSteamID.Nil;
                OnClientDisconnected.Invoke();
                Debug.Log("Disconnected from server");
            }
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            if (!serverSteamID.IsValid()) return;

            if (channelId >= 0 && channelId < channels.Length)
            {
                byte[] data = new byte[segment.Count];
                Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

                SteamNetworking.SendP2PPacket(serverSteamID, data, (uint)data.Length, channels[channelId], channelId);
            }
        }

        public override bool ServerActive()
        {
            return SteamManager.Initialized && connectedClients.Count >= 0;
        }

        public override void ServerDisconnect(int connectionId)
        {
            if (connectedClients.TryGetValue(connectionId, out CSteamID steamID))
            {
                // Отправляем сообщение об отключении
                byte[] disconnectMsg = System.Text.Encoding.UTF8.GetBytes("DISCONNECT");
                SteamNetworking.SendP2PPacket(steamID, disconnectMsg, (uint)disconnectMsg.Length, EP2PSend.k_EP2PSendReliable, 0);

                // Закрываем P2P сессию
                SteamNetworking.CloseP2PSessionWithUser(steamID);

                connectedClients.Remove(connectionId);
                Debug.Log($"Disconnected client {connectionId} ({steamID})");
            }
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            if (connectedClients.TryGetValue(connectionId, out CSteamID steamID))
            {
                return steamID.ToString();
            }
            return "";
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            if (connectedClients.TryGetValue(connectionId, out CSteamID steamID))
            {
                if (channelId >= 0 && channelId < channels.Length)
                {
                    byte[] data = new byte[segment.Count];
                    Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

                    SteamNetworking.SendP2PPacket(steamID, data, (uint)data.Length, channels[channelId], channelId);
                }
            }
        }

        public override void ServerStart()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam not initialized");
                return;
            }

            Debug.Log("Steam P2P Server started");
        }

        public override void ServerStop()
        {
            // Отключаем всех клиентов
            foreach (var kvp in connectedClients)
            {
                ServerDisconnect(kvp.Key);
            }

            connectedClients.Clear();
            nextConnectionId = 1;

            Debug.Log("Steam P2P Server stopped");
        }

        public override Uri ServerUri()
        {
            if (SteamManager.Initialized)
            {
                return new Uri($"steam://{SteamUser.GetSteamID()}");
            }
            return null;
        }

        public override void Shutdown()
        {
            if (ServerActive())
                ServerStop();

            if (ClientConnected())
                ClientDisconnect();
        }

        void UpdateP2P()
        {
            if (!SteamManager.Initialized) return;

            // Обрабатываем входящие P2P пакеты
            for (int channel = 0; channel < channels.Length; channel++)
            {
                while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize, channel))
                {
                    byte[] buffer = new byte[msgSize];

                    if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out uint bytesRead, out CSteamID senderID, channel))
                    {
                        ProcessPacket(buffer, (int)bytesRead, senderID, channel);
                    }
                }
            }
        }

        void ProcessPacket(byte[] data, int length, CSteamID senderID, int channel)
        {
            string message = System.Text.Encoding.UTF8.GetString(data, 0, length);

            // Обработка служебных сообщений
            if (message == "CONNECT")
            {
                // Новое подключение к серверу
                if (ServerActive())
                {
                    int connectionId = nextConnectionId++;
                    connectedClients[connectionId] = senderID;

                    OnServerConnected.Invoke(connectionId);
                    Debug.Log($"Client connected: {senderID} (ID: {connectionId})");
                }
                return;
            }
            else if (message == "DISCONNECT")
            {
                // Отключение клиента
                foreach (var kvp in connectedClients)
                {
                    if (kvp.Value == senderID)
                    {
                        OnServerDisconnected.Invoke(kvp.Key);
                        connectedClients.Remove(kvp.Key);
                        Debug.Log($"Client disconnected: {senderID} (ID: {kvp.Key})");
                        break;
                    }
                }
                return;
            }

            // Обработка игровых данных
            if (ServerActive())
            {
                // Сервер получил данные от клиента
                foreach (var kvp in connectedClients)
                {
                    if (kvp.Value == senderID)
                    {
                        OnServerDataReceived.Invoke(kvp.Key, new ArraySegment<byte>(data, 0, length), channel);
                        break;
                    }
                }
            }
            else if (ClientConnected() && senderID == serverSteamID)
            {
                // Клиент получил данные от сервера
                OnClientDataReceived.Invoke(new ArraySegment<byte>(data, 0, length), channel);
            }
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            return 1200; // Максимальный размер P2P пакета
        }

        // Утилиты для работы с Steam ID
        public static string GetSteamAddress(CSteamID steamID)
        {
            return steamID.ToString();
        }

        public static CSteamID ParseSteamID(string address)
        {
            if (ulong.TryParse(address, out ulong steamID))
            {
                return new CSteamID(steamID);
            }
            return CSteamID.Nil;
        }
    }
}