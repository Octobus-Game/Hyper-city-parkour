
using UnityEngine;
using Mirror;

// Компонент для объектов, которые можно уничтожать
public class DestructibleObject : NetworkBehaviour
{
    [Header("Destruction Settings")]
    public GameObject destructionEffect; // Эффект уничтожения (опционально)
    public AudioClip destructionSound;   // Звук уничтожения (опционально)

    private void OnMouseDown()
    {
        // Проверяем, что это левая кнопка мыши и объект активен
        if (Input.GetMouseButtonDown(0) && gameObject.activeInHierarchy)
        {
            // Только клиент может инициировать уничтожение
            if (isClient)
            {
                // Отправляем команду на сервер для уничтожения объекта
                CmdDestroyObject();
            }
        }
    }

    // Команда, которая выполняется на сервере
    [Command(requiresAuthority = false)]
    void CmdDestroyObject()
    {
        // Проверяем, что объект еще существует (защита от дублирования)
        if (this != null && gameObject != null)
        {
            // Вызываем RPC для всех клиентов перед уничтожением
            RpcOnObjectDestroyed();

            // Уничтожаем объект на сервере (автоматически синхронизируется с клиентами)
            NetworkServer.Destroy(gameObject);
        }
    }

    // RPC для воспроизведения эффектов на всех клиентах
    [ClientRpc]
    void RpcOnObjectDestroyed()
    {
        // Воспроизводим эффекты уничтожения на всех клиентах
        PlayDestructionEffects();
    }

    private void PlayDestructionEffects()
    {
        // Создаем эффект частиц, если он задан
        if (destructionEffect != null)
        {
            GameObject effect = Instantiate(destructionEffect, transform.position, transform.rotation);
            // Уничтожаем эффект через несколько секунд
            Destroy(effect, 3f);
        }

        // Воспроизводим звук, если он задан
        if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, transform.position);
        }
    }
}
