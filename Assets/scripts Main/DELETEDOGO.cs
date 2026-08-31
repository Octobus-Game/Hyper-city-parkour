using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DELETEDOGO : MonoBehaviour
{
    // Этот метод вызывается, когда другой Collider входит в триггерный Collider
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли другой объект игроком
        if (other.CompareTag("Player"))
        {
            // Выводим сообщение в консоль
            Debug.Log("Игрок коснулся триггера!");
            // Здесь можно добавить другие действия, например, уменьшить здоровье игрока или запустить анимацию
        }
    }
}
