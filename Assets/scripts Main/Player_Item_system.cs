using UnityEngine;

public class Player_Item_system : MonoBehaviour
{
    [Header("Объекты для активации")]
    [Tooltip("Массив объектов, которые будут активированы при касании игрока")]
    public GameObject[] activateObjects;

    [Header("Объекты для проверки и деактивации")]
    [Tooltip("Массив объектов, которые будут проверены и деактивированы, если активны")]
    public GameObject[] checkAndDeactivateObjects;

    [Header("Настройки")]
    [Tooltip("Тег игрока для определения столкновения")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что столкнулись с игроком
        if (other.CompareTag(playerTag))
        {
            // Проверяем и деактивируем объекты из массива проверки
            foreach (GameObject obj in checkAndDeactivateObjects)
            {
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                }
            }

            // Активируем все объекты из массива активации
            foreach (GameObject obj in activateObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }

            // Деактивируем сам триггер
            gameObject.SetActive(false);
        }
    }
}