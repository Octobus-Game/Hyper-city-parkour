
using UnityEngine;

public class ObjectArrayController : MonoBehaviour
{
    [Header("Объекты для отслеживания")]
    [Tooltip("Массив объектов, состояние которых отслеживается")]
    public GameObject[] objectsToWatch;

    [Header("Объект для управления")]
    [Tooltip("Объект, который будет отключаться когда все объекты в массиве отключены")]
    public GameObject targetObject;

    [Header("Настройки")]
    [Tooltip("Проверять состояние каждый кадр (может снизить производительность)")]
    public bool checkEveryFrame = false;

    [Tooltip("Интервал проверки в секундах (если checkEveryFrame = false)")]
    public float checkInterval = 0.5f;

    private float nextCheckTime;

    void Start()
    {
        // Проверяем при старте
        CheckObjectStates();
        nextCheckTime = Time.time + checkInterval;
    }

    void Update()
    {
        if (checkEveryFrame)
        {
            CheckObjectStates();
        }
        else if (Time.time >= nextCheckTime)
        {
            CheckObjectStates();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    /// <summary>
    /// Проверяет состояние всех объектов в массиве
    /// </summary>
    public void CheckObjectStates()
    {
        // Проверяем что массив не пустой и целевой объект указан
        if (objectsToWatch == null || objectsToWatch.Length == 0 || targetObject == null)
            return;

        bool allDisabled = true;

        // Проверяем каждый объект в массиве
        foreach (GameObject obj in objectsToWatch)
        {
            // Пропускаем null объекты
            if (obj == null)
                continue;

            // Если хотя бы один объект активен, то не все отключены
            if (obj.activeInHierarchy)
            {
                allDisabled = false;
                break;
            }
        }

        // Управляем состоянием целевого объекта
        // Если все объекты отключены - отключаем целевой объект
        // Если хотя бы один включен - включаем целевой объект
        targetObject.SetActive(!allDisabled);
    }

    /// <summary>
    /// Принудительно проверить состояние (можно вызвать из других скриптов)
    /// </summary>
    public void ForceCheck()
    {
        CheckObjectStates();
    }

    /// <summary>
    /// Добавить объект в массив отслеживания
    /// </summary>
    public void AddObjectToWatch(GameObject obj)
    {
        if (obj == null) return;

        // Создаем новый массив с дополнительным элементом
        GameObject[] newArray = new GameObject[objectsToWatch.Length + 1];

        // Копируем существующие элементы
        for (int i = 0; i < objectsToWatch.Length; i++)
        {
            newArray[i] = objectsToWatch[i];
        }

        // Добавляем новый объект
        newArray[newArray.Length - 1] = obj;
        objectsToWatch = newArray;

        // Сразу проверяем состояние
        CheckObjectStates();
    }

    /// <summary>
    /// Удалить объект из массива отслеживания
    /// </summary>
    public void RemoveObjectFromWatch(GameObject obj)
    {
        if (obj == null || objectsToWatch == null) return;

        // Ищем объект в массиве
        int indexToRemove = -1;
        for (int i = 0; i < objectsToWatch.Length; i++)
        {
            if (objectsToWatch[i] == obj)
            {
                indexToRemove = i;
                break;
            }
        }

        // Если объект найден, удаляем его
        if (indexToRemove >= 0)
        {
            GameObject[] newArray = new GameObject[objectsToWatch.Length - 1];
            int newIndex = 0;

            for (int i = 0; i < objectsToWatch.Length; i++)
            {
                if (i != indexToRemove)
                {
                    newArray[newIndex] = objectsToWatch[i];
                    newIndex++;
                }
            }

            objectsToWatch = newArray;

            // Сразу проверяем состояние
            CheckObjectStates();
        }
    }
}