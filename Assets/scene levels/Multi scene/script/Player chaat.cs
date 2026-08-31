using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Playerchaat : NetworkBehaviour
{
    [SerializeField] private GameObject chatUI = null;
    [SerializeField] private TMP_Text chatText = null;
    [SerializeField] private TMP_InputField inputField = null;
    private bool hasAuthority;

    private static event Action<string> onMessage;

    public override void OnStartAuthority()
    {
        chatUI.SetActive(true);

        onMessage += HandleNewMessage;

    }

    [ClientCallback]
    private void OnDestroy()
    {
        if (!hasAuthority) { return; }
        onMessage -= HandleNewMessage;

    }

    private void HandleNewMessage(String messsage)
    {
        chatText.text += messsage;
    }

    [Client]
    public void Send(String message)
    {
        if (!Input.GetKeyDown(KeyCode.Return)) { return; }

        if (string.IsNullOrWhiteSpace(message)) { return; }

        CmdSendMessage(inputField.text);

        inputField.text = String.Empty;
    }

    [Command]
    private void CmdSendMessage(String message)
    {
        RpcHandleMessage($"[{connectionToClient.connectionId}]: {message}");
    }

    [ClientRpc]
    private void RpcHandleMessage(String message)
    {
        onMessage?.Invoke($"\n{message}");
    }
}
