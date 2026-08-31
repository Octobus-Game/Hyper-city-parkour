using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ObjectSpawnSystem : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] spawnObjects = new GameObject[13]; // Element 0-12
    [SerializeField] private Button[] spawnButtons = new Button[13]; // Element 0-12

    [Header("Camera Settings")]
    [SerializeField] private Camera targetCamera; // Камера для спавна
    [SerializeField] private float distanceFromCamera = 5f; // Расстояние от камеры
    [SerializeField] private Vector3 offsetFromCamera = Vector3.zero; // Дополнительное смещение

    [Header("Spawn Parameters")]
    public bool randomSpawnPosition = false;
    public float spawnRadius = 2f; // Радиус случайного спавна вокруг точки

    [Header("Spawned Objects Management")]
    public bool limitSpawnedObjects = false;
    public int maxSpawnedObjects = 10;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        InitializeButtons();
        SetupCamera();
    }

    void InitializeButtons()
    {
        // Привязываем события к кнопкам
        for (int i = 0; i < spawnButtons.Length; i++)
        {
            if (spawnButtons[i] != null)
            {
                int index = i; // Локальная копия для замыкания
                spawnButtons[i].onClick.AddListener(() => SpawnObject(index));

                // Опционально: устанавливаем текст кнопки
                Text buttonText = spawnButtons[i].GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Spawn {index}";
                }
            }
        }
    }

    void SetupCamera()
    {
        // Если камера не назначена, используем главную камеру
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError("No camera found! Please assign a camera in the inspector.");
        }
    }

    public void SpawnObject(int elementIndex)
    {
        // Проверяем корректность индекса
        if (elementIndex < 0 || elementIndex >= spawnObjects.Length)
        {
            Debug.LogWarning($"Invalid element index: {elementIndex}");
            return;
        }

        // Проверяем, что объект существует
        if (spawnObjects[elementIndex] == null)
        {
            Debug.LogWarning($"Spawn object at index {elementIndex} is null!");
            return;
        }

        // Проверяем наличие камеры
        if (targetCamera == null)
        {
            Debug.LogError("Target camera is null! Cannot spawn object.");
            return;
        }

        // Проверяем лимит спавненных объектов
        if (limitSpawnedObjects && spawnedObjects.Count >= maxSpawnedObjects)
        {
            Debug.Log("Maximum spawned objects limit reached!");
            return;
        }

        // Определяем позицию спавна возле камеры
        Vector3 spawnPosition = GetSpawnPositionNearCamera();

        // Спавним объект
        GameObject spawnedObject = Instantiate(spawnObjects[elementIndex], spawnPosition, Quaternion.identity);

        // Добавляем в список спавненных объектов
        spawnedObjects.Add(spawnedObject);

        // Добавляем компонент для отслеживания уничтожения
        SpawnedObjectTracker tracker = spawnedObject.AddComponent<SpawnedObjectTracker>();
        tracker.Initialize(this);

        Debug.Log($"Spawned object {elementIndex}: {spawnObjects[elementIndex].name} near camera at position {spawnPosition}");
    }

    Vector3 GetSpawnPositionNearCamera()
    {
        // Базовая позиция: камера + направление вперед * расстояние + смещение
        Vector3 basePosition = targetCamera.transform.position +
                              targetCamera.transform.forward * distanceFromCamera +
                              offsetFromCamera;

        if (randomSpawnPosition)
        {
            // Случайная позиция в радиусе вокруг базовой точки
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

            // Используем право и верх камеры для создания случайного смещения в пространстве камеры
            Vector3 randomOffset = targetCamera.transform.right * randomCircle.x +
                                  targetCamera.transform.up * randomCircle.y;

            return basePosition + randomOffset;
        }

        return basePosition;
    }

    // Альтернативный метод для спавна в поле зрения камеры
    Vector3 GetSpawnPositionInCameraView()
    {
        // Получаем случайную точку в области видимости камеры
        float viewportX = Random.Range(0.2f, 0.8f); // Избегаем краев экрана
        float viewportY = Random.Range(0.2f, 0.8f);

        // Конвертируем viewport координаты в world координаты
        Vector3 viewportPoint = new Vector3(viewportX, viewportY, distanceFromCamera);
        Vector3 worldPoint = targetCamera.ViewportToWorldPoint(viewportPoint);

        return worldPoint + offsetFromCamera;
    }

    // Метод для изменения режима спавна
    public void SetSpawnMode(bool useViewportSpawn)
    {
        // Можно переключать между двумя режимами спавна
        // В будущем можно добавить enum для разных режимов
    }

    // Метод для удаления объекта из списка при его уничтожении
    public void RemoveSpawnedObject(GameObject obj)
    {
        spawnedObjects.Remove(obj);
    }

    // Утилитарные методы
    public void ClearAllSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
        Debug.Log("All spawned objects cleared!");
    }

    public void SetButtonInteractable(int elementIndex, bool interactable)
    {
        if (elementIndex >= 0 && elementIndex < spawnButtons.Length && spawnButtons[elementIndex] != null)
        {
            spawnButtons[elementIndex].interactable = interactable;
        }
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < spawnButtons.Length; i++)
        {
            SetButtonInteractable(i, interactable);
        }
    }

    // Метод для спавна через код (без кнопок)
    public GameObject SpawnObjectByIndex(int elementIndex)
    {
        if (elementIndex < 0 || elementIndex >= spawnObjects.Length || spawnObjects[elementIndex] == null)
        {
            return null;
        }

        if (targetCamera == null)
        {
            Debug.LogError("Target camera is null! Cannot spawn object.");
            return null;
        }

        Vector3 spawnPosition = GetSpawnPositionNearCamera();
        GameObject spawnedObject = Instantiate(spawnObjects[elementIndex], spawnPosition, Quaternion.identity);
        spawnedObjects.Add(spawnedObject);

        SpawnedObjectTracker tracker = spawnedObject.AddComponent<SpawnedObjectTracker>();
        tracker.Initialize(this);

        return spawnedObject;
    }

    // Методы для настройки камеры
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
    }

    public void SetDistanceFromCamera(float distance)
    {
        distanceFromCamera = distance;
    }

    public void SetCameraOffset(Vector3 offset)
    {
        offsetFromCamera = offset;
    }

    // Геттеры для отладки
    public int GetSpawnedObjectsCount()
    {
        return spawnedObjects.Count;
    }

    public List<GameObject> GetSpawnedObjects()
    {
        return new List<GameObject>(spawnedObjects);
    }

    public Camera GetTargetCamera()
    {
        return targetCamera;
    }
}

// Вспомогательный класс для отслеживания уничтожения объектов
public class SpawnedObjectTracker : MonoBehaviour
{
    private ObjectSpawnSystem spawnSystem;

    public void Initialize(ObjectSpawnSystem system)
    {
        spawnSystem = system;
    }

    void OnDestroy()
    {
        if (spawnSystem != null)
        {
            spawnSystem.RemoveSpawnedObject(gameObject);
        }
    }
}

// Продвинутая версия с дополнительными настройками камеры
[System.Serializable]
public class CameraSpawnSettings
{
    [Header("Camera Reference")]
    public Camera targetCamera;

    [Header("Position Settings")]
    public float distanceFromCamera = 5f;
    public Vector3 offsetFromCamera = Vector3.zero;

    [Header("Spawn Area")]
    public bool useRandomPosition = false;
    public float spawnRadius = 2f;

    [Header("Advanced Options")]
    public bool spawnInViewport = false; // Спавн в области видимости
    public bool alignWithCameraRotation = false; // Поворачивать объекты по направлению камеры
}

[System.Serializable]
public class SpawnElement
{
    public string elementName;
    public GameObject prefab;
    public Button button;
    public bool isActive = true;
    public bool useCustomSpawnSettings = false;
    public CameraSpawnSettings customSettings;
}

public class AdvancedCameraSpawnSystem : MonoBehaviour
{
    [Header("Spawn Elements")]
    [SerializeField] private SpawnElement[] spawnElements = new SpawnElement[13];

    [Header("Default Camera Settings")]
    [SerializeField] private CameraSpawnSettings defaultSpawnSettings;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        InitializeSpawnElements();
        SetupDefaultCamera();
    }

    void SetupDefaultCamera()
    {
        if (defaultSpawnSettings.targetCamera == null)
        {
            defaultSpawnSettings.targetCamera = Camera.main ?? FindObjectOfType<Camera>();
        }
    }

    void InitializeSpawnElements()
    {
        for (int i = 0; i < spawnElements.Length; i++)
        {
            if (spawnElements[i].button != null && spawnElements[i].isActive)
            {
                int index = i;
                spawnElements[i].button.onClick.AddListener(() => SpawnElementByIndex(index));

                // Устанавливаем текст кнопки
                Text buttonText = spawnElements[i].button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = string.IsNullOrEmpty(spawnElements[i].elementName)
                        ? $"Element {index}"
                        : spawnElements[i].elementName;
                }
            }
        }
    }

    public void SpawnElementByIndex(int index)
    {
        if (index < 0 || index >= spawnElements.Length)
            return;

        SpawnElement element = spawnElements[index];

        if (element.prefab == null || !element.isActive)
            return;

        // Выбираем настройки спавна
        CameraSpawnSettings settings = element.useCustomSpawnSettings && element.customSettings.targetCamera != null
            ? element.customSettings
            : defaultSpawnSettings;

        if (settings.targetCamera == null)
        {
            Debug.LogError("No camera assigned for spawning!");
            return;
        }

        Vector3 spawnPosition = GetSpawnPositionForSettings(settings);
        Quaternion spawnRotation = settings.alignWithCameraRotation
            ? settings.targetCamera.transform.rotation
            : Quaternion.identity;

        GameObject spawnedObj = Instantiate(element.prefab, spawnPosition, spawnRotation);
        spawnedObjects.Add(spawnedObj);

        Debug.Log($"Spawned: {element.elementName} (Index: {index}) near camera at {spawnPosition}");
    }

    Vector3 GetSpawnPositionForSettings(CameraSpawnSettings settings)
    {
        Vector3 basePosition;

        if (settings.spawnInViewport)
        {
            // Спавн в области видимости камеры
            float viewportX = Random.Range(0.2f, 0.8f);
            float viewportY = Random.Range(0.2f, 0.8f);
            Vector3 viewportPoint = new Vector3(viewportX, viewportY, settings.distanceFromCamera);
            basePosition = settings.targetCamera.ViewportToWorldPoint(viewportPoint);
        }
        else
        {
            // Спавн перед камерой
            basePosition = settings.targetCamera.transform.position +
                          settings.targetCamera.transform.forward * settings.distanceFromCamera;
        }

        basePosition += settings.offsetFromCamera;

        if (settings.useRandomPosition)
        {
            Vector2 randomCircle = Random.insideUnitCircle * settings.spawnRadius;
            Vector3 randomOffset = settings.targetCamera.transform.right * randomCircle.x +
                                  settings.targetCamera.transform.up * randomCircle.y;
            return basePosition + randomOffset;
        }

        return basePosition;
    }
}