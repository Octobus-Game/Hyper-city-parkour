using UnityEngine;

public class TriggerDisableObjects : MonoBehaviour
{
    [Header("Настройки триггера")]
    [SerializeField] private string playerTag = "Player"; // Тег игрока

    [Header("Объекты для отключения")]
    [SerializeField] private GameObject[] objectsToDisable; // Массив объектов для отключения

    [Header("Опции")]
    [SerializeField] private bool disableOnlyOnce = true; // Сработать только один раз
    [SerializeField] private bool destroyTriggerAfterUse = false; // Уничтожить триггер после использования

    private bool hasTriggered = false; // Флаг для отслеживания срабатывания

    void Start()
    {
        // Проверяем, что у объекта есть Collider с включенным IsTrigger
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogWarning($"У объекта {gameObject.name} нет Collider компонента!");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"У Collider объекта {gameObject.name} не включен параметр IsTrigger!");
        }

        // Проверяем, что массив объектов не пустой
        if (objectsToDisable == null || objectsToDisable.Length == 0)
        {
            Debug.LogWarning($"Массив объектов для отключения в {gameObject.name} пуст!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок и триггер еще не срабатывал (если нужно сработать только один раз)
        if (other.CompareTag(playerTag) && (!disableOnlyOnce || !hasTriggered))
        {
            DisableObjects();
            hasTriggered = true;

            // Уничтожаем триггер после использования, если нужно
            if (destroyTriggerAfterUse)
            {
                Destroy(gameObject);
            }
        }
    }

    void DisableObjects()
    {
        if (objectsToDisable == null) return;

        // Отключаем все объекты в массиве
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Объект {obj.name} отключен");
            }
        }

        Debug.Log($"Отключено {objectsToDisable.Length} объектов триггером {gameObject.name}");
    }

    // Метод для повторного включения объектов (если нужно)
    public void EnableObjects()
    {
        if (objectsToDisable == null) return;

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        hasTriggered = false; // Сбрасываем флаг
    }

    // Метод для сброса триггера
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}