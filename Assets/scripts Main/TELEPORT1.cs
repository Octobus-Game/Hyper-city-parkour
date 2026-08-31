using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TELEPORT1 : MonoBehaviour
{
    public Transform targetLocation; // Место, куда будет телепортирован игрок
    public Transform player; // Игрок, который будет телепортирован

    void OnMouseDown()
    {
        if (targetLocation != null && player != null)
        {
            TeleportPlayer();
        }
        else
        {
            Debug.LogWarning("Целевое местоположение или игрок не установлены!");
        }
    }

    void TeleportPlayer()
    {
        player.position = targetLocation.position;
        player.rotation = targetLocation.rotation; // Также можно телепортировать игрока с поворотом
    }
}

