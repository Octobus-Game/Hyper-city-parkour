using Mirror;
using TMPro;
using UnityEngine;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Player Settings")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName = "";

    [SerializeField] private string defaultPlayerName = "Player";

    void Start()
    {
        // Если это локальный игрок, устанавливаем имя
        if (isLocalPlayer)
        {
            CmdSetPlayerName(GetPlayerName());
        }
    }

    // Команда для установки имени игрока (выполняется на сервере)
    [Command]
    void CmdSetPlayerName(string newName)
    {
        playerName = newName;
    }

    // Вызывается при изменении SyncVar playerName
    void OnPlayerNameChanged(string oldName, string newName)
    {
        UpdateNameDisplay();
    }

    // Обновляет отображение имени в UI
    void UpdateNameDisplay()
    {
        if (playerNameText != null)
        {
            playerNameText.text = string.IsNullOrEmpty(playerName) ? defaultPlayerName : playerName;
        }
    }

    // Получает имя игрока (можно настроить логику получения имени)
    string GetPlayerName()
    {
        // Здесь можно добавить логику получения имени из:
        // - PlayerPrefs
        // - системы авторизации
        // - UI input field
        // - случайного генератора и т.д.

        string savedName = PlayerPrefs.GetString("PlayerName", "");

        if (string.IsNullOrEmpty(savedName))
        {
            savedName = defaultPlayerName + Random.Range(1000, 9999);
            PlayerPrefs.SetString("PlayerName", savedName);
        }

        return savedName;
    }

    // Публичный метод для изменения имени (можно вызвать из UI)
    public void ChangePlayerName(string newName)
    {
        if (isLocalPlayer && !string.IsNullOrEmpty(newName))
        {
            PlayerPrefs.SetString("PlayerName", newName);
            CmdSetPlayerName(newName);
        }
    }

    // Альтернативная версия для отображения имен всех игроков в одном TextMeshPro
    public void UpdateAllPlayersDisplay()
    {
        if (playerNameText != null && isServer)
        {
            string allNames = "Игроки:\n";

            foreach (var player in FindObjectsOfType<PlayerNameDisplay>())
            {
                if (!string.IsNullOrEmpty(player.playerName))
                {
                    allNames += "• " + player.playerName + "\n";
                }
            }

            playerNameText.text = allNames;
        }
    }

    // Для отладки
    void OnValidate()
    {
        if (playerNameText == null)
        {
            playerNameText = GetComponent<TextMeshProUGUI>();
        }
    }
}