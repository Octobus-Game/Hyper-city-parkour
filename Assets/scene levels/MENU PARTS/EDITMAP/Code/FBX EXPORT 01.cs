using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class FBXModelSpawnSystem : MonoBehaviour
{
    [Header("Folder Settings")]
    [SerializeField] private string fbxFolderPath = "FBX Model"; // Папка в Resources

    [Header("UI Settings")]
    [SerializeField] private Transform buttonParent; // Родительский объект для кнопок
    [SerializeField] private GameObject buttonPrefab; // Префаб кнопки
    [SerializeField] private int buttonsPerRow = 5;
    [SerializeField] private float buttonSpacing = 110f;
    [SerializeField] private Vector2 buttonSize = new Vector2(100f, 30f);

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [SerializeField] private bool randomSpawnPosition = false;
    [SerializeField] private float spawnRadius = 5f;

    [Header("Model Settings")]
    [SerializeField] private float modelScale = 1f;
    [SerializeField] private bool addCollider = true;
    [SerializeField] private bool addRigidbody = false;

    private List<GameObject> fbxModels = new List<GameObject>();
    private List<Button> spawnButtons = new List<Button>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        LoadFBXModels();
        CreateSpawnButtons();
        SetupSpawnPoint();
    }

    void LoadFBXModels()
    {
        // Загружаем все GameObject из папки Resources/FBX Model
        GameObject[] loadedModels = Resources.LoadAll<GameObject>(fbxFolderPath);

        if (loadedModels.Length == 0)
        {
            Debug.LogWarning($"No FBX models found in Resources/{fbxFolderPath}");
            return;
        }

        fbxModels.Clear();

        foreach (GameObject model in loadedModels)
        {
            // Проверяем, что это действительно модель (имеет MeshRenderer или SkinnedMeshRenderer)
            if (IsValidModel(model))
            {
                fbxModels.Add(model);
                Debug.Log($"Loaded FBX model: {model.name}");
            }
        }

        Debug.Log($"Total FBX models loaded: {fbxModels.Count}");
    }

    bool IsValidModel(GameObject model)
    {
        // Проверяем наличие MeshRenderer или SkinnedMeshRenderer в модели или её дочерних объектах
        MeshRenderer meshRenderer = model.GetComponentInChildren<MeshRenderer>();
        SkinnedMeshRenderer skinnedMeshRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();

        return meshRenderer != null || skinnedMeshRenderer != null;
    }

    void CreateSpawnButtons()
    {
        if (buttonParent == null)
        {
            Debug.LogError("Button Parent is not assigned!");
            return;
        }

        if (buttonPrefab == null)
        {
            Debug.LogError("Button Prefab is not assigned!");
            return;
        }

        spawnButtons.Clear();

        for (int i = 0; i < fbxModels.Count; i++)
        {
            CreateButton(i);
        }
    }

    void CreateButton(int modelIndex)
    {
        // Создаем кнопку
        GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("Button Prefab doesn't have Button component!");
            Destroy(buttonObj);
            return;
        }

        // Настраиваем позицию кнопки
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            SetupButtonPosition(rectTransform, modelIndex);
        }

        // Настраиваем текст кнопки
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = GetModelDisplayName(fbxModels[modelIndex].name);
        }

        // Привязываем событие клика
        int index = modelIndex; // Локальная копия для замыкания
        button.onClick.AddListener(() => SpawnModel(index));

        spawnButtons.Add(button);

        Debug.Log($"Created button for model: {fbxModels[modelIndex].name}");
    }

    void SetupButtonPosition(RectTransform rectTransform, int index)
    {
        rectTransform.sizeDelta = buttonSize;

        int row = index / buttonsPerRow;
        int col = index % buttonsPerRow;

        float x = col * buttonSpacing;
        float y = -row * buttonSpacing;

        rectTransform.anchoredPosition = new Vector2(x, y);
    }

    string GetModelDisplayName(string modelName)
    {
        // Убираем расширение и форматируем имя для отображения
        string displayName = modelName.Replace(".fbx", "").Replace("_", " ");

        // Ограничиваем длину имени
        if (displayName.Length > 12)
        {
            displayName = displayName.Substring(0, 12) + "...";
        }

        return displayName;
    }

    void SetupSpawnPoint()
    {
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
        }
    }

    public void SpawnModel(int modelIndex)
    {
        if (modelIndex < 0 || modelIndex >= fbxModels.Count)
        {
            Debug.LogWarning($"Invalid model index: {modelIndex}");
            return;
        }

        GameObject modelPrefab = fbxModels[modelIndex];
        Vector3 spawnPosition = GetSpawnPosition();

        // Спавним модель
        GameObject spawnedModel = Instantiate(modelPrefab, spawnPosition, Quaternion.identity);

        // Применяем настройки к модели
        SetupSpawnedModel(spawnedModel);

        // Добавляем в список спавненных объектов
        spawnedObjects.Add(spawnedModel);

        // Добавляем трекер для отслеживания уничтожения
        ModelTracker tracker = spawnedModel.AddComponent<ModelTracker>();
        tracker.Initialize(this);

        Debug.Log($"Spawned model: {modelPrefab.name} at position: {spawnPosition}");
    }

    void SetupSpawnedModel(GameObject model)
    {
        // Применяем масштаб
        model.transform.localScale = Vector3.one * modelScale;

        // Добавляем коллайдер если нужно
        if (addCollider && model.GetComponent<Collider>() == null)
        {
            // Пытаемся добавить подходящий коллайдер
            MeshRenderer meshRenderer = model.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                MeshCollider meshCollider = model.AddComponent<MeshCollider>();
                meshCollider.convex = addRigidbody; // Convex нужен для Rigidbody
            }
            else
            {
                // Если MeshRenderer нет, добавляем BoxCollider
                model.AddComponent<BoxCollider>();
            }
        }

        // Добавляем Rigidbody если нужно
        if (addRigidbody && model.GetComponent<Rigidbody>() == null)
        {
            model.AddComponent<Rigidbody>();
        }

        // Добавляем тег для идентификации
        model.tag = "SpawnedModel";
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 basePosition = spawnPoint.position + spawnOffset;

        if (randomSpawnPosition)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return basePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        return basePosition;
    }

    // Утилитарные методы
    public void RefreshModels()
    {
        // Удаляем старые кнопки
        foreach (Button button in spawnButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }

        // Перезагружаем модели и создаем новые кнопки
        LoadFBXModels();
        CreateSpawnButtons();
    }

    public void ClearAllSpawnedModels()
    {
        foreach (GameObject model in spawnedObjects)
        {
            if (model != null)
            {
                DestroyImmediate(model);
            }
        }
        spawnedObjects.Clear();
        Debug.Log("All spawned models cleared!");
    }

    public void RemoveSpawnedModel(GameObject model)
    {
        spawnedObjects.Remove(model);
    }

    // Геттеры для отладки
    public int GetLoadedModelsCount()
    {
        return fbxModels.Count;
    }

    public int GetSpawnedModelsCount()
    {
        return spawnedObjects.Count;
    }

    public List<string> GetModelNames()
    {
        List<string> names = new List<string>();
        foreach (GameObject model in fbxModels)
        {
            names.Add(model.name);
        }
        return names;
    }
}

// Трекер для отслеживания уничтожения моделей
public class ModelTracker : MonoBehaviour
{
    private FBXModelSpawnSystem spawnSystem;

    public void Initialize(FBXModelSpawnSystem system)
    {
        spawnSystem = system;
    }

    void OnDestroy()
    {
        if (spawnSystem != null)
        {
            spawnSystem.RemoveSpawnedModel(gameObject);
        }
    }
}

