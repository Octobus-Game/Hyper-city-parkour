using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;

public class NavMeshGenerator : MonoBehaviour
{
    [Header("UI References")]
    public Button generateButton;           // Кнопка для генерации NavMesh
    public Button clearButton;              // Кнопка для очистки NavMesh (опционально)

    [Header("NavMesh Settings")]
    public GameObject navMeshObject;        // Объект, на котором будет NavMeshSurface
    public bool createNewObject = true;     // Создать новый объект для NavMeshSurface
    public string navMeshObjectName = "NavMesh Surface"; // Имя для нового объекта

    [Header("NavMesh Surface Parameters")]
    public int agentTypeID = 0;            // ID типа агента
    public LayerMask layerMask = -1;       // Маска слоев для NavMesh
    public bool overrideVoxelSize = false; // Переопределить размер вокселя
    public float voxelSize = 0.16666667f;   // Размер вокселя
    public bool overrideTileSize = false;   // Переопределить размер тайла
    public int tileSize = 256;              // Размер тайла
    public bool buildHeightMesh = false;    // Строить высотную сетку

    [Header("Collection Settings")]
    public CollectObjects collectObjects = CollectObjects.All; // Что собирать для NavMesh
    public bool useGeometry = true;         // Использовать геометрию

    [Header("Generation Settings")]
    public bool generateOnStart = false;    // Генерировать при старте
    public bool showDebugLog = true;        // Показывать отладочные сообщения

    [Header("UI Feedback")]
    public Text statusText;                 // Текстовое поле для отображения статуса
    public Slider progressSlider;           // Слайдер прогресса (опционально)

    private NavMeshSurface navMeshSurface;  // Ссылка на NavMeshSurface компонент
    private bool isGenerating = false;      // Флаг процесса генерации

    private void Start()
    {
        // Настраиваем кнопки
        SetupButtons();

        // Инициализация UI элементов
        InitializeUI();

        // Генерация при старте если включена
        if (generateOnStart)
        {
            GenerateNavMesh();
        }
    }

    private void SetupButtons()
    {
        // Подписываемся на событие нажатия кнопки генерации
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(GenerateNavMesh);
        }
        else
        {
            Debug.LogWarning("Generate Button не назначена в NavMeshGenerator!");
        }

        // Подписываемся на событие нажатия кнопки очистки
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearNavMesh);
        }
    }

    private void InitializeUI()
    {
        // Инициализация слайдера прогресса
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.gameObject.SetActive(false);
        }

        // Инициализация текста статуса
        UpdateStatusText("Ready to generate NavMesh");
    }

    // Основной метод генерации NavMesh
    public void GenerateNavMesh()
    {
        if (isGenerating)
        {
            if (showDebugLog)
                Debug.Log("NavMesh generation already in progress!");
            return;
        }

        StartCoroutine(GenerateNavMeshCoroutine());
    }

    private IEnumerator GenerateNavMeshCoroutine()
    {
        isGenerating = true;
        bool success = false;

        // Обновляем UI
        UpdateStatusText("Preparing NavMesh generation...");
        SetButtonsInteractable(false);

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0.1f;
        }

        yield return new WaitForEndOfFrame();

        // Выполняем генерацию без try-catch чтобы избежать ошибки с yield
        success = PerformNavMeshGeneration();

        if (progressSlider != null)
            progressSlider.value = 0.9f;

        yield return new WaitForSeconds(0.5f); // Небольшая задержка для визуального эффекта

        // Завершение
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
            yield return new WaitForSeconds(0.5f);
            progressSlider.gameObject.SetActive(false);
        }

        if (success)
        {
            UpdateStatusText("NavMesh generated successfully!");
            if (showDebugLog)
                Debug.Log("NavMesh generated successfully on object: " + navMeshSurface.gameObject.name);
        }
        else
        {
            UpdateStatusText("Error: NavMesh generation failed!");
            Debug.LogError("NavMesh generation failed!");
        }

        isGenerating = false;
        SetButtonsInteractable(true);
    }

    private bool PerformNavMeshGeneration()
    {
        try
        {
            // Создаем или находим NavMeshSurface
            SetupNavMeshSurface();

            if (progressSlider != null)
                progressSlider.value = 0.3f;

            UpdateStatusText("Configuring NavMesh parameters...");

            // Настраиваем параметры NavMeshSurface
            ConfigureNavMeshSurface();

            if (progressSlider != null)
                progressSlider.value = 0.5f;

            UpdateStatusText("Building NavMesh...");

            // Генерируем NavMesh
            navMeshSurface.BuildNavMesh();

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("NavMesh generation error: " + e.Message);
            return false;
        }
    }

    private void SetupNavMeshSurface()
    {
        if (createNewObject || navMeshObject == null)
        {
            // Создаем новый объект для NavMeshSurface
            navMeshObject = new GameObject(navMeshObjectName);

            if (showDebugLog)
                Debug.Log("Created new NavMesh object: " + navMeshObjectName);
        }

        // Получаем или добавляем NavMeshSurface компонент
        navMeshSurface = navMeshObject.GetComponent<NavMeshSurface>();
        if (navMeshSurface == null)
        {
            navMeshSurface = navMeshObject.AddComponent<NavMeshSurface>();

            if (showDebugLog)
                Debug.Log("Added NavMeshSurface component to object: " + navMeshObject.name);
        }
    }

    private void ConfigureNavMeshSurface()
    {
        if (navMeshSurface == null) return;

        // Настраиваем параметры NavMeshSurface
        navMeshSurface.agentTypeID = agentTypeID;
        navMeshSurface.collectObjects = collectObjects;
        navMeshSurface.layerMask = layerMask;
        navMeshSurface.useGeometry = useGeometry ? UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes : UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

        // Дополнительные параметры
        navMeshSurface.overrideVoxelSize = overrideVoxelSize;
        if (overrideVoxelSize)
            navMeshSurface.voxelSize = voxelSize;

        navMeshSurface.overrideTileSize = overrideTileSize;
        if (overrideTileSize)
            navMeshSurface.tileSize = tileSize;

        navMeshSurface.buildHeightMesh = buildHeightMesh;

        if (showDebugLog)
            Debug.Log("NavMeshSurface configured with custom parameters");
    }

    // Метод для очистки NavMesh
    public void ClearNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.RemoveData();
            UpdateStatusText("NavMesh cleared");

            if (showDebugLog)
                Debug.Log("NavMesh data cleared");
        }
        else
        {
            UpdateStatusText("No NavMesh to clear");
        }
    }

    // Метод для полного удаления NavMesh объекта
    public void DestroyNavMeshObject()
    {
        if (navMeshObject != null && createNewObject)
        {
            if (Application.isPlaying)
                Destroy(navMeshObject);
            else
                DestroyImmediate(navMeshObject);

            navMeshObject = null;
            navMeshSurface = null;
            UpdateStatusText("NavMesh object destroyed");

            if (showDebugLog)
                Debug.Log("NavMesh object destroyed");
        }
    }

    // Обновление текста статуса
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    // Управление интерактивностью кнопок
    private void SetButtonsInteractable(bool interactable)
    {
        if (generateButton != null)
            generateButton.interactable = interactable;

        if (clearButton != null)
            clearButton.interactable = interactable;
    }

    // Публичные методы для внешнего управления
    public bool IsGenerating()
    {
        return isGenerating;
    }

    public NavMeshSurface GetNavMeshSurface()
    {
        return navMeshSurface;
    }

    public void SetAgentType(int newAgentTypeID)
    {
        agentTypeID = newAgentTypeID;
        if (navMeshSurface != null)
        {
            navMeshSurface.agentTypeID = agentTypeID;
        }
    }

    public void SetLayerMask(LayerMask layers)
    {
        layerMask = layers;
        if (navMeshSurface != null)
        {
            navMeshSurface.layerMask = layerMask;
        }
    }

    // Пересборка NavMesh (для случаев когда сцена изменилась)
    public void RebuildNavMesh()
    {
        if (navMeshSurface != null)
        {
            StartCoroutine(RebuildNavMeshCoroutine());
        }
        else
        {
            GenerateNavMesh();
        }
    }

    private IEnumerator RebuildNavMeshCoroutine()
    {
        UpdateStatusText("Rebuilding NavMesh...");

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0.3f;
        }

        yield return new WaitForEndOfFrame();

        bool success = false;
        string errorMessage = "";

        try
        {
            navMeshSurface.BuildNavMesh();
            success = true;
        }
        catch (System.Exception e)
        {
            success = false;
            errorMessage = e.Message;
        }

        // После try-catch — можно использовать yield
        if (progressSlider != null)
        {
            progressSlider.value = 1f;
            yield return new WaitForSeconds(0.5f);
            progressSlider.gameObject.SetActive(false);
        }

        if (success)
        {
            UpdateStatusText("NavMesh rebuilt successfully!");
            if (showDebugLog)
                Debug.Log("NavMesh rebuilt successfully");
        }
        else
        {
            UpdateStatusText("Error: NavMesh rebuild failed!");
            Debug.LogError("NavMesh rebuild failed: " + errorMessage);
        }
    }

    // Метод для установки области сбора объектов
    public void SetCollectObjects(CollectObjects collectType)
    {
        collectObjects = collectType;
        if (navMeshSurface != null)
        {
            navMeshSurface.collectObjects = collectObjects;
        }
    }

    // Метод для переключения типа геометрии
    public void SetUseGeometry(bool useRenderMesh)
    {
        useGeometry = useRenderMesh;
        if (navMeshSurface != null)
        {
            navMeshSurface.useGeometry = useGeometry ?
                UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes :
                UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий кнопок
        if (generateButton != null)
            generateButton.onClick.RemoveListener(GenerateNavMesh);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearNavMesh);
    }

    // Валидация в редакторе
    private void OnValidate()
    {
        // Проверяем корректность значений
        if (voxelSize <= 0)
            voxelSize = 0.16666667f;

        if (tileSize <= 0)
            tileSize = 256;
    }
}