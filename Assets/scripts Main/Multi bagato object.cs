using UnityEngine;

public class TriggerActivator : MonoBehaviour
{
    [Header("Настройки триггера")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool activateOnce = true; // Активировать только один раз

    [Header("Объекты для активации")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Дополнительные настройки")]
    [SerializeField] private bool deactivateOnExit = false; // Деактивировать при выходе из триггера
    [SerializeField] private float activationDelay = 0f; // Задержка перед активацией

    private bool hasBeenTriggered = false;

    private void Start()
    {
        // Проверяем, что у объекта есть Collider с включенным IsTrigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("TriggerActivator: Не найден Collider на объекте " + gameObject.name);
            return;
        }

        if (!col.isTrigger)
        {
            Debug.LogWarning("TriggerActivator: Collider должен быть Trigger на объекте " + gameObject.name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что коллайдер принадлежит игроку
        if (other.CompareTag(playerTag))
        {
            // Если триггер уже был активирован и настроен на одноразовое использование
            if (hasBeenTriggered && activateOnce)
                return;

            // Активируем объекты с задержкой или сразу
            if (activationDelay > 0)
            {
                Invoke(nameof(ActivateObjects), activationDelay);
            }
            else
            {
                ActivateObjects();
            }

            hasBeenTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Деактивируем объекты при выходе из триггера (если настроено)
        if (other.CompareTag(playerTag) && deactivateOnExit)
        {
            DeactivateObjects();
        }
    }

    private void ActivateObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log("Активирован объект: " + obj.name);
            }
        }

        Debug.Log("Триггер активирован игроком!");
    }

    private void DeactivateObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log("Деактивирован объект: " + obj.name);
            }
        }

        Debug.Log("Объекты деактивированы при выходе из триггера!");
    }

    // Метод для ручного сброса триггера
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
    }

    // Метод для принудительной активации объектов
    public void ForceActivate()
    {
        ActivateObjects();
        hasBeenTriggered = true;
    }
}