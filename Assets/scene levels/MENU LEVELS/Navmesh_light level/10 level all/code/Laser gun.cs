using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Префаб для создания")]
    public GameObject prefabToSpawn;

    [Header("Настройки")]
    public LayerMask groundLayerMask = 1; // Слой земли для raycast
    public bool spawnOnMousePosition = true; // Создавать в позиции мыши
    public Vector3 spawnOffset = Vector3.zero; // Смещение от точки клика
    public bool attachToPointObject = false; // Прикреплять к точечному объекту

    [Header("Точечный объект")]
    public Transform pointObject; // Объект, к которому прикрепляем префаб
    public bool makeChild = true; // Делать созданный объект дочерним

    [Header("Камера (оставить пустым для Main Camera)")]
    public Camera targetCamera;

    private void Start()
    {
        // Если камера не назначена, используем главную камеру
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        // Проверяем зажатие левой кнопки мыши
        if (Input.GetMouseButtonDown(0))
        {
            SpawnPrefabAtMousePosition();
        }
    }

    private void SpawnPrefabAtMousePosition()
    {
        // Проверяем, что префаб назначен
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Префаб не назначен!");
            return;
        }

        // Проверяем, что камера найдена
        if (targetCamera == null)
        {
            Debug.LogWarning("Камера не найдена!");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;
        Transform parentTransform = null;

        // Определяем позицию для создания
        if (attachToPointObject && pointObject != null)
        {
            // Создаем в позиции точечного объекта
            spawnPosition = pointObject.position + spawnOffset;
            spawnRotation = pointObject.rotation;

            if (makeChild)
            {
                parentTransform = pointObject;
            }
        }
        else if (spawnOnMousePosition)
        {
            // Получаем позицию мыши и создаем луч от камеры
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = targetCamera.ScreenPointToRay(mousePosition);

            // Выполняем raycast для определения точки пересечения
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
            {
                spawnPosition = hit.point + spawnOffset;
            }
            else
            {
                // Если raycast не попал, создаем объект на определенном расстоянии от камеры
                spawnPosition = ray.origin + ray.direction * 10f + spawnOffset;
            }
        }
        else
        {
            // Используем позицию этого объекта как точку создания
            spawnPosition = transform.position + spawnOffset;
        }

        // Создаем префаб
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        // Прикрепляем к родительскому объекту, если нужно
        if (parentTransform != null)
        {
            spawnedObject.transform.SetParent(parentTransform);
        }

        // Опционально: выводим информацию в консоль
        string parentInfo = parentTransform != null ? $" (прикреплен к {parentTransform.name})" : "";
        Debug.Log($"Префаб создан в позиции: {spawnPosition}{parentInfo}");
    }

    // Альтернативный метод для создания с заданной позицией
    public void SpawnPrefabAtPosition(Vector3 position)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Префаб не назначен!");
            return;
        }

        GameObject spawnedObject = Instantiate(prefabToSpawn, position + spawnOffset, Quaternion.identity);

        // Прикрепляем к point object, если нужно
        if (attachToPointObject && pointObject != null && makeChild)
        {
            spawnedObject.transform.SetParent(pointObject);
        }

        Debug.Log($"Префаб создан в заданной позиции: {position + spawnOffset}");
    }

    // Метод для создания с заданной позицией и поворотом
    public void SpawnPrefabAtPositionWithRotation(Vector3 position, Quaternion rotation)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Префаб не назначен!");
            return;
        }

        GameObject spawnedObject = Instantiate(prefabToSpawn, position + spawnOffset, rotation);

        // Прикрепляем к point object, если нужно
        if (attachToPointObject && pointObject != null && makeChild)
        {
            spawnedObject.transform.SetParent(pointObject);
        }

        Debug.Log($"Префаб создан в позиции: {position + spawnOffset} с поворотом: {rotation}");
    }

    // Новый метод для прикрепления к конкретному объекту
    public void SpawnPrefabAttachedToObject(Transform targetObject, bool asChild = true)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Префаб не назначен!");
            return;
        }

        if (targetObject == null)
        {
            Debug.LogWarning("Целевой объект не назначен!");
            return;
        }

        Vector3 spawnPosition = targetObject.position + spawnOffset;
        Quaternion spawnRotation = targetObject.rotation;

        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        if (asChild)
        {
            spawnedObject.transform.SetParent(targetObject);
        }

        Debug.Log($"Префаб создан и прикреплен к объекту: {targetObject.name}");
    }
}