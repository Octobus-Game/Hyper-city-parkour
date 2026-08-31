using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

/// <summary>
/// Компонент на Player Prefab.
/// Хранит ник игрока в SyncVar и синхронизирует его на всех клиентах.
/// Отображает ник в TMP InputField (UI) и в 3D TextMesh (над персонажем).
/// </summary>
public class NetworkPlayerName : NetworkBehaviour
{
    // -- Список всех игроков в сети ---------------------------------------------
    public static readonly List<NetworkPlayerName> AllPlayers = new();

    // -- Синхронизируемый ник -----------------------------------------------------
    [SyncVar(hook = nameof(OnNicknameChanged))]
    public string nickname = "";

    // -- Ссылки на отображение ника ------------------------------------------------
    [Header("UI (TMP InputField)")]
    [Tooltip("Поле ввода ника. Как правило активно/редактируется только у локального игрока.")]
    [SerializeField] private TMP_InputField nicknameInputField;

    [Header("3D Text (TextMesh)")]
    [Tooltip("3D-текст над персонажем, видимый всем игрокам.")]
    [SerializeField] private TextMesh nicknameTextMesh;

    // -- Lifecycle ------------------------------------------------------------------
    public override void OnStartClient()
    {
        base.OnStartClient();
        AllPlayers.Add(this);

        // Применяем текущее значение сразу, т.к. хук SyncVar
        // не вызывается для значения, уже установленного до старта клиента.
        ApplyNickname(nickname);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (nicknameInputField != null)
        {
            nicknameInputField.text = nickname;
            nicknameInputField.onEndEdit.AddListener(OnInputFieldSubmit);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        AllPlayers.Remove(this);

        if (nicknameInputField != null)
            nicknameInputField.onEndEdit.RemoveListener(OnInputFieldSubmit);
    }

    // -- Ввод нового ника через InputField ---------------------------------------------
    private void OnInputFieldSubmit(string value)
    {
        CmdSetNickname(value);
    }

    // -- Command: клиент > сервер -------------------------------------------------------
    [Command]
    public void CmdSetNickname(string newName)
    {
        newName = newName.Trim();
        if (newName.Length == 0 || newName.Length > 32) return;
        nickname = newName; // SyncVar -> автоматически разойдётся всем клиентам
    }

    // -- Hook: вызывается у каждого клиента при изменении SyncVar -----------------------
    void OnNicknameChanged(string _, string newValue)
    {
        ApplyNickname(newValue);
    }

    // -- Применение ника к UI (TMP InputField) и к 3D-тексту (TextMesh) -----------------
    private void ApplyNickname(string value)
    {
        if (nicknameTextMesh != null)
            nicknameTextMesh.text = value;

        // У локального игрока не перетираем поле ввода, пока он сам печатает в нём
        if (isLocalPlayer && nicknameInputField != null && !nicknameInputField.isFocused)
            nicknameInputField.text = value;
    }
}