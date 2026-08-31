using UnityEngine;

public class RandomObjectActivator : MonoBehaviour
{
    [Header("Objects to Activate")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Settings")]
    [SerializeField] private bool deactivateOthers = true; 
    [SerializeField] private bool allowNoActivation = false; 
    [SerializeField] private float activationDelay = 0f; 

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    void Start()
    {
        if (activationDelay > 0f)
        {
            Invoke(nameof(ActivateRandomObject), activationDelay);
        }
        else
        {
            ActivateRandomObject();
        }
    }

    void ActivateRandomObject()
    {
        // Проверяем, есть ли объекты в массиве
        if (objectsToActivate == null || objectsToActivate.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("Массив объектов для активации пуст!", this);
            return;
        }

        // Фильтруем null объекты
        var validObjects = System.Array.FindAll(objectsToActivate, obj => obj != null);

        if (validObjects.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("Все объекты в массиве равны null!", this);
            return;
        }

        // Сначала деактивируем все объекты, если нужно
        if (deactivateOthers)
        {
            DeactivateAllObjects();
        }

        // Определяем диапазон для рандома
        int maxRange = allowNoActivation ? validObjects.Length + 1 : validObjects.Length;
        int randomIndex = Random.Range(0, maxRange);

        // Если индекс равен длине массива и разрешено не активировать объекты
        if (randomIndex == validObjects.Length && allowNoActivation)
        {
            if (showDebugLogs)
                Debug.Log("Случайно выбрано: не активировать ни одного объекта", this);
            return;
        }

        // Активируем выбранный объект
        GameObject selectedObject = validObjects[randomIndex];
        selectedObject.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"Активирован объект: {selectedObject.name}", this);
    }

    void DeactivateAllObjects()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    // Публичные методы для управления извне
    public void ForceActivateRandom()
    {
        ActivateRandomObject();
    }

    public void ActivateSpecificObject(int index)
    {
        if (objectsToActivate == null || index < 0 || index >= objectsToActivate.Length)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Неверный индекс: {index}", this);
            return;
        }

        if (objectsToActivate[index] == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"Объект с индексом {index} равен null!", this);
            return;
        }

        if (deactivateOthers)
        {
            DeactivateAllObjects();
        }

        objectsToActivate[index].SetActive(true);

        if (showDebugLogs)
            Debug.Log($"Принудительно активирован объект: {objectsToActivate[index].name}", this);
    }

    public void DeactivateAll()
    {
        DeactivateAllObjects();

        if (showDebugLogs)
            Debug.Log("Все объекты деактивированы", this);
    }

    public GameObject GetActiveObject()
    {
        if (objectsToActivate == null) return null;

        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
    }

    public int GetActiveObjectIndex()
    {
        if (objectsToActivate == null) return -1;

        for (int i = 0; i < objectsToActivate.Length; i++)
        {
            if (objectsToActivate[i] != null && objectsToActivate[i].activeInHierarchy)
            {
                return i;
            }
        }
        return -1;
    }

    // Метод для проверки в редакторе
    void OnValidate()
    {
        if (objectsToActivate != null)
        {
            // Подсчитываем null объекты
            int nullCount = 0;
            foreach (GameObject obj in objectsToActivate)
            {
                if (obj == null) nullCount++;
            }

            if (nullCount > 0 && showDebugLogs)
            {
                Debug.LogWarning($"В массиве найдено {nullCount} null объектов!", this);
            }
        }
    }
}