using UnityEngine;
using Mirror;


// Менеджер для управления массивом объектов
public class ObjectManager : NetworkBehaviour
{
    [Header("Object Management")]
    public DestructibleObject[] destructibleObjects; // Массив объектов для уничтожения
    public GameObject objectPrefab; // Префаб для создания новых объектов
    public Transform[] spawnPoints; // Точки спавна для новых объектов

    void Start()
    {
        // Находим все объекты DestructibleObject в сцене, если массив пуст
        if (destructibleObjects == null || destructibleObjects.Length == 0)
        {
            destructibleObjects = FindObjectsOfType<DestructibleObject>();
        }
    }

    // Команда для создания нового объекта (опционально)
    [Command(requiresAuthority = false)]
    public void CmdSpawnNewObject(int spawnPointIndex)
    {
        if (isServer && objectPrefab != null && spawnPointIndex < spawnPoints.Length)
        {
            Vector3 spawnPosition = spawnPoints[spawnPointIndex].position;
            Quaternion spawnRotation = spawnPoints[spawnPointIndex].rotation;

            GameObject newObject = Instantiate(objectPrefab, spawnPosition, spawnRotation);
            NetworkServer.Spawn(newObject);
        }
    }

    // Получить количество активных объектов
    public int GetActiveObjectsCount()
    {
        int count = 0;
        foreach (var obj in destructibleObjects)
        {
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }
}