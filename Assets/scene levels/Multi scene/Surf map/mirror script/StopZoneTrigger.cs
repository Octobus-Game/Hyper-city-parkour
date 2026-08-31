using Mirror;
using UnityEngine;

public class StopZoneTrigger : NetworkBehaviour
{
    // Викликається на сервері, коли гравець входить у зону зупинки
    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player mirror")) return;

        // Сповіщаємо всіх клієнтів — зупиняємо секундомір
        RpcStopTimer();
    }

    [ClientRpc]
    private void RpcStopTimer()
    {
        StopwatchUI.Instance?.StopTimer();
    }
}