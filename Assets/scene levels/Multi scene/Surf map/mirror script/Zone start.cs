using Mirror;
using UnityEngine;

public class ZoneTrigger : NetworkBehaviour
{
    [Header("Зона зупинки секундоміра")]
    [SerializeField] private StopZoneTrigger stopZone;

    // Викликається на сервері, коли об'єкт виходить із тригера
    [ServerCallback]
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player mirror")) return;

        // Сповіщаємо всіх клієнтів — запускаємо секундомір
        RpcStartTimer();
    }

    [ClientRpc]
    private void RpcStartTimer()
    {
        StopwatchUI.Instance?.StartTimer();
    }
}