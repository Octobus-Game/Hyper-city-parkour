using System.Collections.Generic;
using UnityEngine;
using Mirror;
using P90brush;

public class TPTrigger_Mirror : NetworkBehaviour
{
    public Vector3 _tpPos;
    public List<string> _tagList = new List<string>();

    private void OnTriggerEnter(Collider other)
    {
        // Выполняем логику только на сервере
        if (!isServer) return;

        NetworkIdentity identity = other.GetComponentInParent<NetworkIdentity>();

        // Телепортация игрока (объект с NetworkIdentity и тегом "Player")
        if (identity != null && IsPlayer(other))
        {
            Debug.Log("Teleporting player: " + other.name);
            TeleportPlayer(identity, _tpPos);
            return;
        }

        // Телепортация других объектов по тегам
        foreach (string tag in _tagList)
        {
            if (other.transform.root.CompareTag(tag))
            {
                Debug.Log("Teleporting tagged object: " + other.name);

                if (identity != null)
                {
                    // Сетевой объект — телепортируем через RPC
                    TeleportPlayer(identity, _tpPos);
                }
                else
                {
                    // Не сетевой объект — двигаем напрямую на сервере
                    other.transform.position = _tpPos;
                }
                break;
            }
        }
    }

    /// <summary>
    /// Перемещает сетевой объект на сервере и синхронизирует на все клиенты
    /// </summary>
    [Server]
    private void TeleportPlayer(NetworkIdentity target, Vector3 destination)
    {
        PlayerLogic_mirror player = target.GetComponent<PlayerLogic_mirror>();
        if (player == null)
            player = target.GetComponentInChildren<PlayerLogic_mirror>();

        if (player != null)
        {
            player.ServerTeleport(destination);
            return;
        }

        target.transform.position = destination;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.CompareTag("Player mirror")
            || other.transform.root.CompareTag("Player")
            || other.transform.root.CompareTag("Player mirror");
    }
}
