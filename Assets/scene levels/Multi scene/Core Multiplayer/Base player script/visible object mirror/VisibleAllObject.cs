using Mirror;
using UnityEngine;

public class VisibleToAllPlayers : NetworkBehaviour
{
    [SerializeField] private GameObject visualObject;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (visualObject != null)
            RpcSetVisible(true);
    }

    [ClientRpc]
    private void RpcSetVisible(bool visible)
    {
        if (visualObject != null)
            visualObject.SetActive(visible);
    }
}