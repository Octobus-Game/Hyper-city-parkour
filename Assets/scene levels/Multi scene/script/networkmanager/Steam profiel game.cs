
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Mirror;
using Steamworks;
using TMPro;
using System.Collections;

/// <summary>
/// UI панель для отображения информации игрока Steam
/// Работает с Mirror networking и Steamworks API
/// </summary>
public class SteamPlayerUI : NetworkBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI steamIdText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image statusIndicator;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button profileButton;
    [SerializeField] private GameObject loadingPanel;

    [Header("Status Colors")]
    [SerializeField] private Color onlineColor = Color.green;
    [SerializeField] private Color awayColor = Color.yellow;
    [SerializeField] private Color busyColor = Color.red;
    [SerializeField] private Color offlineColor = Color.gray;

    [Header("Network Sync")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    private string syncedPlayerName = "";

    [SyncVar(hook = nameof(OnSteamIdChanged))]
    private ulong syncedSteamId = 0;

    [SyncVar(hook = nameof(OnPlayerLevelChanged))]
    private int syncedPlayerLevel = 0;

    [SyncVar(hook = nameof(OnPlayerStatusChanged))]
    private int syncedPlayerStatus = 0;

    // Локальные переменные
    private CSteamID currentSteamId;
    private Texture2D avatarTexture;
    private Coroutine avatarLoadCoroutine;

    #region Unity Events

    private void Start()
    {
        InitializeUI();

        if (isLocalPlayer)
        {
            LoadLocalPlayerData();
        }

        SetupEventHandlers();
    }

    private void OnDestroy()
    {
        if (avatarLoadCoroutine != null)
        {
            StopCoroutine(avatarLoadCoroutine);
        }
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (profileButton != null)
            profileButton.onClick.AddListener(OpenSteamProfile);

        // Установка значений по умолчанию
        if (playerNameText != null)
            playerNameText.text = "Loading...";

        if (steamIdText != null)
            steamIdText.text = "Steam ID: ...";

        if (levelText != null)
            levelText.text = "Level: ...";
    }

    private void SetupEventHandlers()
    {
        if (SteamManager.Initialized)
        {
            // Подписка на события Steam
            Callback<PersonaStateChange_t>.Create(OnPersonaStateChanged);
            Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
        }
    }

    #endregion

    #region Local Player Data Loading

    private void LoadLocalPlayerData()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam не инициализирован!");
            return;
        }

        currentSteamId = SteamUser.GetSteamID();

        // Загрузка данных игрока
        string playerName = SteamFriends.GetPersonaName();
        ulong steamId = currentSteamId.m_SteamID;
        int playerLevel = SteamUser.GetPlayerSteamLevel();
        EPersonaState status = SteamFriends.GetPersonaState();

        // Отправка данных на сервер для синхронизации
        CmdUpdatePlayerData(playerName, steamId, playerLevel, (int)status);

        // Загрузка аватара
        LoadPlayerAvatar();
    }

    [Command]
    private void CmdUpdatePlayerData(string playerName, ulong steamId, int level, int status)
    {
        syncedPlayerName = playerName;
        syncedSteamId = steamId;
        syncedPlayerLevel = level;
        syncedPlayerStatus = status;
    }

    #endregion

    #region Avatar Loading

    private void LoadPlayerAvatar()
    {
        if (!SteamManager.Initialized) return;

        int avatarHandle = SteamFriends.GetMediumFriendAvatar(currentSteamId);

        if (avatarHandle != -1)
        {
            LoadAvatarTexture(avatarHandle);
        }
        else
        {
            // Аватар еще не загружен, ждем callback
            Debug.Log("Ожидание загрузки аватара...");
        }
    }

    private void LoadAvatarTexture(int avatarHandle)
    {
        if (avatarLoadCoroutine != null)
        {
            StopCoroutine(avatarLoadCoroutine);
        }

        avatarLoadCoroutine = StartCoroutine(LoadAvatarCoroutine(avatarHandle));
    }

    private IEnumerator LoadAvatarCoroutine(int avatarHandle)
    {
        yield return new WaitForEndOfFrame();

        // ИСПРАВЛЕНИЕ: Используем правильные методы Steamworks
        if (SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height))
        {
            byte[] imageBuffer = new byte[width * height * 4];

            if (SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, (int)(width * height * 4)))
            {
                avatarTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
                avatarTexture.LoadRawTextureData(imageBuffer);
                avatarTexture.Apply();

                // Применение текстуры к UI
                if (avatarImage != null)
                {
                    Sprite avatarSprite = Sprite.Create(
                        avatarTexture,
                        new Rect(0, 0, width, height),
                        new Vector2(0.5f, 0.5f)
                    );
                    avatarImage.sprite = avatarSprite;
                }
            }
        }
    }

    #endregion

    #region Steam Events

    private void OnPersonaStateChanged(PersonaStateChange_t callback)
    {
        if (callback.m_ulSteamID == currentSteamId.m_SteamID)
        {
            if (isLocalPlayer)
            {
                // Обновление статуса
                EPersonaState newStatus = SteamFriends.GetPersonaState();
                CmdUpdatePlayerStatus((int)newStatus);
            }
        }
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID == currentSteamId)
        {
            LoadAvatarTexture(callback.m_iImage);
        }
    }

    [Command]
    private void CmdUpdatePlayerStatus(int newStatus)
    {
        syncedPlayerStatus = newStatus;
    }

    #endregion

    #region SyncVar Hooks

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newName;
        }
    }

    private void OnSteamIdChanged(ulong oldId, ulong newId)
    {
        if (steamIdText != null)
        {
            steamIdText.text = $"Steam ID: {newId}";
        }

        currentSteamId = new CSteamID(newId);

        // Загрузка аватара для удаленного игрока
        if (!isLocalPlayer && SteamManager.Initialized)
        {
            LoadPlayerAvatar();
        }
    }

    private void OnPlayerLevelChanged(int oldLevel, int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Level: {newLevel}";
        }
    }

    private void OnPlayerStatusChanged(int oldStatus, int newStatus)
    {
        UpdateStatusDisplay((EPersonaState)newStatus);
    }

    #endregion

    #region UI Updates

    private void UpdateStatusDisplay(EPersonaState status)
    {
        Color statusColor = GetStatusColor(status);
        string statusString = GetStatusString(status);

        if (statusIndicator != null)
        {
            statusIndicator.color = statusColor;
        }

        if (statusText != null)
        {
            statusText.text = statusString;
            statusText.color = statusColor;
        }
    }

    private Color GetStatusColor(EPersonaState status)
    {
        return status switch
        {
            EPersonaState.k_EPersonaStateOnline => onlineColor,
            EPersonaState.k_EPersonaStateAway => awayColor,
            EPersonaState.k_EPersonaStateBusy => busyColor,
            EPersonaState.k_EPersonaStateSnooze => awayColor,
            EPersonaState.k_EPersonaStateLookingToTrade => onlineColor,
            EPersonaState.k_EPersonaStateLookingToPlay => onlineColor,
            _ => offlineColor
        };
    }

    private string GetStatusString(EPersonaState status)
    {
        return status switch
        {
            EPersonaState.k_EPersonaStateOnline => "В сети",
            EPersonaState.k_EPersonaStateAway => "Отошел",
            EPersonaState.k_EPersonaStateBusy => "Занят",
            EPersonaState.k_EPersonaStateSnooze => "Нет на месте",
            EPersonaState.k_EPersonaStateLookingToTrade => "Торговля",
            EPersonaState.k_EPersonaStateLookingToPlay => "Игра",
            _ => "Не в сети"
        };
    }

    #endregion

    #region UI Interactions

    private void OpenSteamProfile()
    {
        if (currentSteamId.IsValid())
        {
            SteamFriends.ActivateGameOverlayToUser("steamid", currentSteamId);
        }
    }

    /// <summary>
    /// Публичный метод для обновления UI с внешними данными
    /// </summary>
    /// <param name="playerData">Данные игрока</param>
    public void SetPlayerData(SteamPlayerData playerData)
    {
        if (playerNameText != null)
            playerNameText.text = playerData.playerName;

        if (steamIdText != null)
            steamIdText.text = $"Steam ID: {playerData.steamId}";

        if (levelText != null)
            levelText.text = $"Level: {playerData.level}";

        currentSteamId = new CSteamID(playerData.steamId);
        UpdateStatusDisplay(playerData.status);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Принудительное обновление данных игрока
    /// </summary>
    public void RefreshPlayerData()
    {
        if (isLocalPlayer && SteamManager.Initialized)
        {
            LoadLocalPlayerData();
        }
    }

    /// <summary>
    /// Получение текущих данных игрока
    /// </summary>
    /// <returns>Структура с данными игрока</returns>
    public SteamPlayerData GetCurrentPlayerData()
    {
        return new SteamPlayerData
        {
            playerName = syncedPlayerName,
            steamId = syncedSteamId,
            level = syncedPlayerLevel,
            status = (EPersonaState)syncedPlayerStatus
        };
    }

    #endregion
}

/// <summary>
/// Структура данных игрока Steam
/// </summary>
[System.Serializable]
public struct SteamPlayerData
{
    public string playerName;
    public ulong steamId;
    public int level;
    public EPersonaState status;
}

/// <summary>
/// Менеджер для управления несколькими панелями игроков
/// </summary>
public class SteamPlayersManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private int maxPlayersToShow = 10;

    private System.Collections.Generic.Dictionary<NetworkIdentity, SteamPlayerUI> playerPanels =
        new System.Collections.Generic.Dictionary<NetworkIdentity, SteamPlayerUI>();

    // ИСПРАВЛЕНИЕ: Использование UnityEvent вместо Action
    [Header("Events")]
    public UnityEvent OnClientConnectedEvent;
    public UnityEvent OnClientDisconnectedEvent;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // ИСПРАВЛЕНИЕ: Правильная подписка на события Mirror
        NetworkClient.OnConnectedEvent += OnClientConnected;
        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
    }

    private void OnDisable()
    {
        // Отписка от событий при отключении
        NetworkClient.OnConnectedEvent -= OnClientConnected;
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
    }

    private void OnClientConnected()
    {
        Debug.Log("Клиент подключен к серверу");
        OnClientConnectedEvent?.Invoke();
    }

    private void OnClientDisconnected()
    {
        Debug.Log("Клиент отключен от сервера");
        OnClientDisconnectedEvent?.Invoke();
        ClearAllPlayerPanels();
    }

    /// <summary>
    /// Добавление панели игрока
    /// </summary>
    /// <param name="networkIdentity">Network Identity игрока</param>
    /// <param name="playerData">Данные игрока Steam</param>
    public void AddPlayerPanel(NetworkIdentity networkIdentity, SteamPlayerData playerData)
    {
        if (playerPanels.ContainsKey(networkIdentity)) return;
        if (playerPanels.Count >= maxPlayersToShow) return;

        GameObject panelObj = Instantiate(playerPanelPrefab, playersContainer);
        SteamPlayerUI playerUI = panelObj.GetComponent<SteamPlayerUI>();

        if (playerUI != null)
        {
            playerUI.SetPlayerData(playerData);
            playerPanels.Add(networkIdentity, playerUI);
        }
    }

    /// <summary>
    /// Удаление панели игрока
    /// </summary>
    /// <param name="networkIdentity">Network Identity игрока</param>
    public void RemovePlayerPanel(NetworkIdentity networkIdentity)
    {
        if (playerPanels.TryGetValue(networkIdentity, out SteamPlayerUI playerUI))
        {
            if (playerUI != null && playerUI.gameObject != null)
            {
                Destroy(playerUI.gameObject);
            }
            playerPanels.Remove(networkIdentity);
        }
    }

    /// <summary>
    /// Очистка всех панелей игроков
    /// </summary>
    public void ClearAllPlayerPanels()
    {
        foreach (var panel in playerPanels.Values)
        {
            if (panel != null && panel.gameObject != null)
            {
                Destroy(panel.gameObject);
            }
        }
        playerPanels.Clear();
    }

    /// <summary>
    /// Обновление данных конкретного игрока
    /// </summary>
    /// <param name="networkIdentity">Network Identity игрока</param>
    /// <param name="playerData">Новые данные игрока</param>
    public void UpdatePlayerPanel(NetworkIdentity networkIdentity, SteamPlayerData playerData)
    {
        if (playerPanels.TryGetValue(networkIdentity, out SteamPlayerUI playerUI))
        {
            playerUI.SetPlayerData(playerData);
        }
    }
}

/// <summary>
/// Компонент игрока для интеграции Steam с Mirror
/// Размещается на игровом объекте игрока
/// </summary>
public class SteamNetworkPlayer : NetworkBehaviour
{
    [Header("Steam Integration")]
    [SerializeField] private bool autoLoadSteamData = true;

    [SyncVar(hook = nameof(OnSteamDataUpdated))]
    private SteamPlayerData steamData;

    // ИСПРАВЛЕНИЕ: Использование UnityEvent вместо Action
    [Header("Events")]
    public UnityEvent<SteamPlayerData> OnPlayerDataLoadedEvent;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (autoLoadSteamData && SteamManager.Initialized)
        {
            LoadSteamPlayerData();
        }
    }

    /// <summary>
    /// Загрузка данных Steam игрока
    /// </summary>
    public void LoadSteamPlayerData()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam не инициализирован!");
            return;
        }

        CSteamID steamId = SteamUser.GetSteamID();

        SteamPlayerData data = new SteamPlayerData
        {
            playerName = SteamFriends.GetPersonaName(),
            steamId = steamId.m_SteamID,
            level = SteamUser.GetPlayerSteamLevel(),
            status = SteamFriends.GetPersonaState()
        };

        CmdUpdateSteamData(data);
    }

    [Command]
    private void CmdUpdateSteamData(SteamPlayerData data)
    {
        steamData = data;
    }

    private void OnSteamDataUpdated(SteamPlayerData oldData, SteamPlayerData newData)
    {
        OnPlayerDataLoadedEvent?.Invoke(newData);

        // Уведомление менеджера игроков
        SteamPlayersManager playersManager = FindObjectOfType<SteamPlayersManager>();
        if (playersManager != null)
        {
            playersManager.UpdatePlayerPanel(netIdentity, newData);
        }
    }

    /// <summary>
    /// Получение текущих данных Steam
    /// </summary>
    /// <returns>Данные игрока Steam</returns>
    public SteamPlayerData GetSteamData()
    {
        return steamData;
    }
}

/// <summary>
/// Утилитарный класс для работы с Steam
/// </summary>
public static class SteamUtilsHelper
{
    /// <summary>
    /// Проверка, является ли Steam ID валидным
    /// </summary>
    /// <param name="steamId">Steam ID для проверки</param>
    /// <returns>True если валидный</returns>
    public static bool IsValidSteamId(ulong steamId)
    {
        CSteamID cSteamId = new CSteamID(steamId);
        return cSteamId.IsValid();
    }

    /// <summary>
    /// Получение URL профиля Steam
    /// </summary>
    /// <param name="steamId">Steam ID игрока</param>
    /// <returns>URL профиля</returns>
    public static string GetSteamProfileUrl(ulong steamId)
    {
        return $"https://steamcommunity.com/profiles/{steamId}";
    }

    /// <summary>
    /// Форматирование Steam ID для отображения
    /// </summary>
    /// <param name="steamId">Steam ID</param>
    /// <returns>Отформатированная строка</returns>
    public static string FormatSteamId(ulong steamId)
    {
        return steamId.ToString("X");
    }

    /// <summary>
    /// ИСПРАВЛЕНИЕ: Альтернативный метод загрузки аватара без использования SteamUtils.GetImageSize
    /// </summary>
    /// <param name="steamId">Steam ID игрока</param>
    /// <param name="callback">Callback для получения текстуры</param>
    public static void LoadAvatarAsync(CSteamID steamId, System.Action<Texture2D> callback)
    {
        if (!SteamManager.Initialized)
        {
            callback?.Invoke(null);
            return;
        }

        int avatarHandle = SteamFriends.GetMediumFriendAvatar(steamId);

        if (avatarHandle == -1)
        {
            callback?.Invoke(null);
            return;
        }

        // Используем стандартный размер Steam аватара (64x64)
        int width = 64;
        int height = 64;

        byte[] imageBuffer = new byte[width * height * 4];

        if (SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, width * height * 4))
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(imageBuffer);
            texture.Apply();

            callback?.Invoke(texture);
        }
        else
        {
            callback?.Invoke(null);
        }
    }
}