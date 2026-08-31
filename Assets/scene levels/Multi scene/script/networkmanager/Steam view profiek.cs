using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using System.Collections;

namespace Mirror_steam_Profiel
{
    public class NetworkPlayerProfile : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnSteamNameChanged))]
        public string steamName;

        [SyncVar(hook = nameof(OnSteamIdChanged))]
        public ulong steamId;

        private Image avatarImage;
        private Text nameText;
        private Texture2D avatarTexture;

        private void Start()
        {
            // Найти UI элементы
            avatarImage = GetComponentInChildren<Image>();
            nameText = GetComponentInChildren<Text>();

            if (isLocalPlayer)
            {
                // Для локального игрока - получить данные из Steam
                RequestSteamData();
            }
        }

        [Client]
        private void RequestSteamData()
        {
            if (SteamManager.Initialized)
            {
                string name = SteamFriends.GetPersonaName();
                ulong id = SteamUser.GetSteamID().m_SteamID;

                CmdSetSteamData(name, id);
                RequestAvatar();
            }
        }

        [Command]
        private void CmdSetSteamData(string name, ulong id)
        {
            steamName = name;
            steamId = id;
        }

        private void OnSteamNameChanged(string oldName, string newName)
        {
            if (nameText != null)
                nameText.text = newName;
        }

        private void OnSteamIdChanged(ulong oldId, ulong newId)
        {
            if (!isLocalPlayer && newId != 0)
            {
                // Для удаленных игроков - запросить аватар
                StartCoroutine(LoadSteamAvatar(new CSteamID(newId)));
            }
        }

        // Метод для запроса аватара локального игрока
        private void RequestAvatar()
        {
            if (SteamManager.Initialized)
            {
                CSteamID steamId = SteamUser.GetSteamID();
                int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);

                if (avatarHandle != -1)
                {
                    LoadAvatarFromHandle(avatarHandle);
                }
                else
                {
                    // Если аватар не готов, попробовать позже
                    StartCoroutine(WaitForAvatar(steamId));
                }
            }
        }

        // Корутина для ожидания загрузки аватара
        private IEnumerator WaitForAvatar(CSteamID steamId)
        {
            int attempts = 0;
            int maxAttempts = 10;

            while (attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.5f);

                int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);
                if (avatarHandle != -1)
                {
                    LoadAvatarFromHandle(avatarHandle);
                    yield break;
                }

                attempts++;
            }

            Debug.LogWarning("Не удалось загрузить аватар после " + maxAttempts + " попыток");
        }

        // Метод для загрузки аватара удаленного игрока
        private IEnumerator LoadSteamAvatar(CSteamID steamId)
        {
            // Проверить кэш
            Texture2D cachedAvatar = SteamAvatarCache.GetCachedAvatar(steamId.m_SteamID);
            if (cachedAvatar != null)
            {
                ApplyAvatarTexture(cachedAvatar);
                yield break;
            }

            int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);

            if (avatarHandle == -1)
            {
                // Аватар не готов, ждем
                int attempts = 0;
                int maxAttempts = 10;

                while (attempts < maxAttempts && avatarHandle == -1)
                {
                    yield return new WaitForSeconds(0.5f);
                    avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);
                    attempts++;
                }
            }

            if (avatarHandle != -1)
            {
                LoadAvatarFromHandle(avatarHandle);
            }
            else
            {
                Debug.LogWarning($"Не удалось загрузить аватар для игрока {steamId.m_SteamID}");
            }
        }

        // Загрузка аватара из хендла
        private void LoadAvatarFromHandle(int handle)
        {
            if (SteamUtils.GetImageSize(handle, out uint width, out uint height))
            {
                byte[] imageData = new byte[width * height * 4];

                if (SteamUtils.GetImageRGBA(handle, imageData, (int)(width * height * 4)))
                {
                    // Создать текстуру из данных
                    avatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                    avatarTexture.LoadRawTextureData(imageData);
                    avatarTexture.Apply();

                    // Применить к UI
                    ApplyAvatarTexture(avatarTexture);

                    // Кэшировать аватар
                    if (steamId != 0)
                    {
                        SteamAvatarCache.CacheAvatar(steamId, avatarTexture);
                    }

                    // Отправить данные на сервер для других клиентов
                    if (isLocalPlayer)
                    {
                        // Сжать изображение перед отправкой
                        byte[] compressedData = CompressImageData(imageData, (int)width, (int)height);
                        CmdSendAvatarData(compressedData, (int)width, (int)height);
                    }
                }
            }
        }

        // Применить текстуру аватара к UI
        private void ApplyAvatarTexture(Texture2D texture)
        {
            if (avatarImage != null && texture != null)
            {
                avatarImage.sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        // Сжатие данных изображения
        private byte[] CompressImageData(byte[] originalData, int width, int height)
        {
            Texture2D tempTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tempTexture.LoadRawTextureData(originalData);
            tempTexture.Apply();

            // Сжать в JPG с качеством 75%
            byte[] compressedData = tempTexture.EncodeToJPG(75);

            Destroy(tempTexture);
            return compressedData;
        }

        [Command]
        private void CmdSendAvatarData(byte[] imageData, int width, int height)
        {
            // Отправить данные аватара всем клиентам
            RpcReceiveAvatarData(imageData, width, height);
        }

        [ClientRpc]
        private void RpcReceiveAvatarData(byte[] imageData, int width, int height)
        {
            if (!isLocalPlayer)
            {
                // Создать текстуру для удаленного игрока
                avatarTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

                // Если данные сжаты (JPG), декодировать их
                if (IsJPEGData(imageData))
                {
                    avatarTexture.LoadImage(imageData);
                }
                else
                {
                    avatarTexture.LoadRawTextureData(imageData);
                }

                avatarTexture.Apply();

                ApplyAvatarTexture(avatarTexture);

                // Кэшировать аватар
                if (steamId != 0)
                {
                    SteamAvatarCache.CacheAvatar(steamId, avatarTexture);
                }
            }
        }

        // Проверка, являются ли данные JPEG
        private bool IsJPEGData(byte[] data)
        {
            return data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8;
        }

        private void OnDestroy()
        {
            if (avatarTexture != null)
            {
                Destroy(avatarTexture);
            }
        }
    }

    // Класс для кэширования аватаров
    public static class SteamAvatarCache
    {
        private static System.Collections.Generic.Dictionary<ulong, Texture2D> avatarCache =
            new System.Collections.Generic.Dictionary<ulong, Texture2D>();

        public static void CacheAvatar(ulong steamId, Texture2D avatar)
        {
            if (!avatarCache.ContainsKey(steamId))
            {
                avatarCache[steamId] = avatar;
            }
        }

        public static Texture2D GetCachedAvatar(ulong steamId)
        {
            return avatarCache.ContainsKey(steamId) ? avatarCache[steamId] : null;
        }

        public static void ClearCache()
        {
            foreach (var texture in avatarCache.Values)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                }
            }
            avatarCache.Clear();
        }
    }






















}

