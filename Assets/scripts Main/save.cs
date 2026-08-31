using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Save : MonoBehaviour
{
    [Header("Save Settings")]
    public bool saveOnlyWhitelisted = false;
    public bool logFilteredObjects = true;

    [Space(10)]
    [Header("Quick Actions")]
    [SerializeField] private bool _saveButton = false;
    [SerializeField] private bool _loadButton = false;
    [SerializeField] private bool _loadWithDialogButton = false;
    [SerializeField] private bool _testButton = false;

    [Header("Prefab References")]
    public PrefabMapping[] prefabMappings; // Массив сопоставлений префабов с ID

    [Header("Security Settings")]
    public string[] objectBlacklist = {
        "Main Camera", "Directional Light", "Canvas", "EventSystem",
        "AudioListener", "NetworkManager", "GameManager", "UIManager",
        "ScriptableObject", "DontDestroyOnLoad", "Singleton"
    };

    [Header("Auto-loadable Objects")]
    public string[] autoLoadObjects = {
        "Player", "SpawnPoint", "Checkpoint", "Collectible",
        "Enemy", "NPC", "Interactable", "Door", "Key"
    };

    // Словарь для быстрого поиска префабов по ID
    private Dictionary<byte, GameObject> prefabDictionary;
    private Dictionary<string, byte> nameToIdDictionary;

    void Start()
    {
        InitializePrefabDictionary();
    }

    void InitializePrefabDictionary()
    {
        prefabDictionary = new Dictionary<byte, GameObject>();
        nameToIdDictionary = new Dictionary<string, byte>();

        if (prefabMappings != null)
        {
            foreach (PrefabMapping mapping in prefabMappings)
            {
                if (mapping.prefab != null)
                {
                    prefabDictionary[mapping.id] = mapping.prefab;

                    // Добавляем все возможные варианты имен
                    string[] possibleNames = {
                        mapping.objectName.ToLower(),
                        GetCleanObjectName(mapping.objectName).ToLower(),
                        mapping.prefab.name.ToLower(),
                        GetCleanObjectName(mapping.prefab.name).ToLower()
                    };

                    foreach (string name in possibleNames)
                    {
                        if (!string.IsNullOrEmpty(name) && !nameToIdDictionary.ContainsKey(name))
                        {
                            nameToIdDictionary[name] = mapping.id;
                        }
                    }

                    Debug.Log($"Mapped: {mapping.objectName} -> ID {mapping.id:X2} -> Prefab {mapping.prefab.name}");
                }
            }
        }

        Debug.Log($"Initialized {prefabDictionary.Count} prefab mappings");
    }

    void Update()
    {
        // Проверяем нажатие Ctrl+S в Play Mode (сохранение)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
        {
            SaveSceneData();
        }

        // Проверяем нажатие Ctrl+L в Play Mode (загрузка)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.L))
        {
            LoadLatestScene();
        }

        // Проверяем нажатие Ctrl+O в Play Mode (загрузка с диалогом)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.O))
        {
            LoadSceneWithDialog();
        }
    }

    void OnValidate()
    {
        // Кнопки в инспекторе
        if (_saveButton)
        {
            _saveButton = false;
            SaveSceneData();
        }

        if (_loadButton)
        {
            _loadButton = false;
            LoadLatestScene();
        }

        if (_loadWithDialogButton)
        {
            _loadWithDialogButton = false;
            LoadSceneWithDialog();
        }

        if (_testButton)
        {
            _testButton = false;
            Debug.Log("Test: Save Only Whitelisted = " + saveOnlyWhitelisted);
            Debug.Log("Test: Log Filtered Objects = " + logFilteredObjects);
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Save Scene Data %s")] // % = Ctrl, s = клавиша S
    static void SaveSceneDataMenuItem()
    {
        Save saver = FindObjectOfType<Save>();
        if (saver != null)
        {
            saver.SaveSceneData();
        }
        else
        {
            Debug.LogWarning("Save component not found in scene!");
        }
    }
    
    [MenuItem("Tools/Load Scene Data %l")] // % = Ctrl, l = клавиша L
    static void LoadSceneDataMenuItem()
    {
        Save saver = FindObjectOfType<Save>();
        if (saver != null)
        {
            saver.LoadLatestScene();
        }
        else
        {
            Debug.LogWarning("Save component not found in scene!");
        }
    }
    
    [MenuItem("Tools/Load Scene Data with Dialog %o")] // % = Ctrl, o = клавиша O
    static void LoadSceneDataWithDialogMenuItem()
    {
        Save saver = FindObjectOfType<Save>();
        if (saver != null)
        {
            saver.LoadSceneWithDialog();
        }
        else
        {
            Debug.LogWarning("Save component not found in scene!");
        }
    }
#endif

    public void SaveSceneData()
    {
        Debug.Log("Starting scene save...");

        byte headerv1 = 222;
        byte headerv2 = 173;
        byte version = 10;
        int i = 1;
        string path = Application.persistentDataPath + "/scene" + i + ".dat";

        // Находим свободное имя файла
        while (File.Exists(path))
        {
            Debug.Log("File exists: " + path);
            i++;
            if (i >= 128)
            {
                Debug.LogError("Too many scene files!");
                return;
            }
            path = Application.persistentDataPath + "/scene" + i + ".dat";
        }

        Debug.Log("Will save to: " + path);

        // Собираем все объекты сцены
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<SceneObjectData> sceneData = new List<SceneObjectData>();

        Debug.Log("Found " + allObjects.Length + " objects in scene");

        foreach (GameObject obj in allObjects)
        {
            // Проверяем черный список
            if (IsObjectBlacklisted(obj.name))
            {
                if (logFilteredObjects)
                    Debug.Log("Blacklisted object skipped: " + obj.name);
                continue;
            }

            // Если включен режим только разрешенных объектов
            if (saveOnlyWhitelisted && !IsObjectAutoLoadable(obj.name))
            {
                if (logFilteredObjects)
                    Debug.Log("Non-whitelisted object skipped: " + obj.name);
                continue;
            }

            // Пропускаем системные объекты Unity
            if (obj.hideFlags != HideFlags.None || obj.name.StartsWith("__"))
            {
                if (logFilteredObjects)
                    Debug.Log("System object skipped: " + obj.name);
                continue;
            }

            byte objectId = GetObjectIdByName(obj.name);
            if (objectId != 0xFF) // 0xFF означает неизвестный объект
            {
                SceneObjectData data = new SceneObjectData();
                data.id = objectId;
                data.x = (short)(obj.transform.position.x * 100); // умножаем на 100 для точности
                data.y = (short)(obj.transform.position.y * 100);
                data.z = (short)(obj.transform.position.z * 100);
                data.isAutoLoadable = IsObjectAutoLoadable(obj.name);

                sceneData.Add(data);
                Debug.Log("Added object: " + obj.name + " at " + obj.transform.position);
            }
            else
            {
                Debug.LogWarning("Unknown object ID for: " + obj.name);
            }
        }

        Debug.Log("Total objects to save: " + sceneData.Count);

        // Записываем в файл
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Записываем заголовок
                writer.Write(headerv1);
                writer.Write(headerv2);
                writer.Write(version);

                // Записываем количество объектов
                writer.Write((ushort)sceneData.Count);

                // Записываем данные объектов
                foreach (SceneObjectData data in sceneData)
                {
                    writer.Write(data.id);              // 1 байт - ID объекта
                    writer.Write(data.x);               // 2 байта - X координата
                    writer.Write(data.y);               // 2 байта - Y координата
                    writer.Write(data.z);               // 2 байта - Z координата
                    writer.Write(data.isAutoLoadable);  // 1 байт - флаг автозагрузки
                }
            }

            Debug.Log("Scene saved successfully: " + path);
            Debug.Log("Objects saved: " + sceneData.Count);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error saving scene: " + e.Message);
        }
    }

    bool IsObjectBlacklisted(string objectName)
    {
        string cleanName = GetCleanObjectName(objectName);

        foreach (string blacklistedName in objectBlacklist)
        {
            if (cleanName.ToLower().Contains(blacklistedName.ToLower()) ||
                cleanName.Equals(blacklistedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Дополнительные проверки безопасности
        if (cleanName.ToLower().Contains("manager") ||
            cleanName.ToLower().Contains("system") ||
            cleanName.ToLower().Contains("controller") ||
            cleanName.ToLower().Contains("singleton") ||
            cleanName.StartsWith("_") ||
            cleanName.StartsWith("GameObject"))
        {
            return true;
        }

        return false;
    }

    bool IsObjectAutoLoadable(string objectName)
    {
        string cleanName = GetCleanObjectName(objectName);

        foreach (string autoLoadName in autoLoadObjects)
        {
            if (cleanName.ToLower().Contains(autoLoadName.ToLower()) ||
                cleanName.Equals(autoLoadName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    string GetCleanObjectName(string objectName)
    {
        string cleanName = objectName;
        if (cleanName.Contains("(Clone)"))
            cleanName = cleanName.Replace("(Clone)", "").Trim();
        if (cleanName.Contains("("))
            cleanName = cleanName.Substring(0, cleanName.IndexOf("(")).Trim();
        return cleanName;
    }

    byte GetObjectIdByName(string objectName)
    {
        // Убираем префиксы и суффиксы, которые Unity может добавить
        string cleanName = GetCleanObjectName(objectName).ToLower();

        // Сначала ищем в нашем словаре сопоставлений
        if (nameToIdDictionary != null && nameToIdDictionary.ContainsKey(cleanName))
        {
            Debug.Log($"Found ID for {objectName} -> {nameToIdDictionary[cleanName]:X2}");
            return nameToIdDictionary[cleanName];
        }

        // Если не найдено в сопоставлениях, используем старую систему как fallback
        switch (cleanName)
        {
            case "cube": return 0x01;
            case "dom 3": return 0x02;
            case "hromakey": return 0x03;
            case "building": return 0x04;
            case "exit": return 0x05;
            case "wide building": return 0x06;
            case "gruzovik": return 0x07;
            case "road": return 0x08;
            case "4 buildings": return 0x09;
            case "sixth radio": return 0x0A;
            case "feik zdanie": return 0x0B;
            case "blue skybox": return 0x0C;
            case "purple skybox": return 0x0D;
            case "x3": return 0x0E;
            case "scene": return 0x0F;
            case "cafe": return 0x10;
            case "cafe building": return 0x11;
            case "fancy platform": return 0x12;
            case "platforma tonshe": return 0x13;
            case "huinya kakaya to": return 0x14;
            case "build": return 0x15;
            case "zhytomir": return 0x16;
            case "hayp zdanie": return 0x17;
            case "strange cube": return 0x18;
            case "tree": return 0x19;
            case "tv": return 0x1A;
            case "vyshka": return 0x1B;
            case "wider cube": return 0x1C;
            default:
                Debug.LogWarning($"No ID mapping found for object: {objectName} (clean: {cleanName})");
                return 0xFF; // Неизвестный объект
        }
    }

    // Структура для хранения данных объекта
    [System.Serializable]
    public struct SceneObjectData
    {
        public byte id;             // 1 байт - ID объекта
        public short x;             // 2 байта - координата X
        public short y;             // 2 байта - координата Y  
        public short z;             // 2 байта - координата Z
        public bool isAutoLoadable; // 1 байт - флаг автозагрузки
    }

    // Метод для загрузки последней сохраненной сцены
    public void LoadLatestScene()
    {
        string latestFile = FindLatestSceneFile();
        if (latestFile != null)
        {
            LoadSceneData(latestFile);
        }
        else
        {
            Debug.LogWarning("No scene files found!");
        }
    }

    // Метод для загрузки сцены с выбором файла через диалог
    public void LoadSceneWithDialog()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Load Scene Data", Application.persistentDataPath, "dat");
        if (!string.IsNullOrEmpty(path))
        {
            LoadSceneData(path);
        }
        else
        {
            Debug.Log("File selection cancelled");
        }
#else
        // В билде используем простой UI для выбора файлов
        StartCoroutine(ShowFileSelectionUI());
#endif
    }

    // Находит последний сохраненный файл сцены
    string FindLatestSceneFile()
    {
        string directory = Application.persistentDataPath;
        int i = 1;
        string latestFile = null;

        while (i < 128)
        {
            string path = directory + "/scene" + i + ".dat";
            if (File.Exists(path))
            {
                latestFile = path;
                i++;
            }
            else
            {
                break;
            }
        }

        return latestFile;
    }

    // Корутина для показа UI выбора файлов в билде
    IEnumerator ShowFileSelectionUI()
    {
        string[] sceneFiles = GetAllSceneFiles();

        if (sceneFiles.Length == 0)
        {
            Debug.LogWarning("No scene files found!");
            yield break;
        }

        Debug.Log("Available scene files:");
        for (int i = 0; i < sceneFiles.Length; i++)
        {
            Debug.Log($"{i + 1}. {Path.GetFileName(sceneFiles[i])} - {File.GetLastWriteTime(sceneFiles[i])}");
        }

        Debug.Log("Use LoadSceneByIndex(index) method to load specific file");
        yield return null;
    }

    // Получить все файлы сцен
    string[] GetAllSceneFiles()
    {
        string directory = Application.persistentDataPath;
        List<string> sceneFiles = new List<string>();

        for (int i = 1; i < 128; i++)
        {
            string path = directory + "/scene" + i + ".dat";
            if (File.Exists(path))
            {
                sceneFiles.Add(path);
            }
        }

        return sceneFiles.ToArray();
    }

    // Загрузить сцену по индексу (для использования в билде)
    public void LoadSceneByIndex(int index)
    {
        string[] sceneFiles = GetAllSceneFiles();

        if (index >= 0 && index < sceneFiles.Length)
        {
            LoadSceneData(sceneFiles[index]);
        }
        else
        {
            Debug.LogError($"Invalid scene index: {index}. Available: 0-{sceneFiles.Length - 1}");
        }
    }

    // Загрузить сцену по номеру файла
    public void LoadSceneByNumber(int sceneNumber)
    {
        string path = Application.persistentDataPath + "/scene" + sceneNumber + ".dat";

        if (File.Exists(path))
        {
            LoadSceneData(path);
        }
        else
        {
            Debug.LogError($"Scene file not found: scene{sceneNumber}.dat");
        }
    }

    [ContextMenu("Save Scene Data")]
    public void SaveScene()
    {
        SaveSceneData();
    }

    // ИСПРАВЛЕННЫЙ метод для загрузки данных сцены
    public void LoadSceneData(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        // Инициализируем словарь префабов если он пустой
        if (prefabDictionary == null)
        {
            InitializePrefabDictionary();
        }

        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                // Читаем заголовок
                byte h1 = reader.ReadByte();
                byte h2 = reader.ReadByte();
                byte ver = reader.ReadByte();

                if (h1 != 222 || h2 != 173)
                {
                    Debug.LogError("Invalid file header");
                    return;
                }

                Debug.Log("Loading scene version: " + ver);

                // Читаем количество объектов
                ushort objectCount = reader.ReadUInt16();
                Debug.Log("Objects to load: " + objectCount);

                int loadedObjects = 0;

                // Читаем объекты
                for (int i = 0; i < objectCount; i++)
                {
                    byte id = reader.ReadByte();
                    short x = reader.ReadInt16();
                    short y = reader.ReadInt16();
                    short z = reader.ReadInt16();
                    bool isAutoLoadable = reader.ReadBoolean();

                    string prefabName = GetPrefabNameById(id);
                    if (prefabName != null)
                    {
                        Vector3 position = new Vector3(x / 100f, y / 100f, z / 100f);
                        string autoLoadText = isAutoLoadable ? " [AUTO-LOAD]" : "";
                        Debug.Log("Loading object " + i + ": " + prefabName + " at " + position + autoLoadText);

                        // СОЗДАЕМ ОБЪЕКТ В СЦЕНЕ
                        GameObject createdObject = CreateObjectInScene(prefabName, position);

                        if (createdObject != null)
                        {
                            loadedObjects++;

                            // Добавляем тег для автозагружаемых объектов
                            if (isAutoLoadable && !createdObject.CompareTag("Untagged"))
                            {
                                createdObject.tag = "AutoLoad";
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Unknown object ID: " + id.ToString("X2"));
                    }
                }

                Debug.Log("Scene loaded successfully! Objects created: " + loadedObjects + "/" + objectCount);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading scene: " + e.Message);
        }
    }

    // Новый метод для создания объектов в сцене
    GameObject CreateObjectInScene(string prefabName, Vector3 position)
    {
        GameObject createdObject = null;

        // Получаем ID объекта по имени
        byte objectId = GetObjectIdByName(prefabName);

        // Пытаемся найти префаб по ID
        if (prefabDictionary != null && prefabDictionary.ContainsKey(objectId))
        {
            GameObject prefab = prefabDictionary[objectId];
            createdObject = Instantiate(prefab, position, Quaternion.identity);
            createdObject.name = prefabName; // Устанавливаем правильное имя
            Debug.Log($"Created from prefab: {prefabName} using ID {objectId:X2}");
        }
        else
        {
            // Если префаба нет, создаем примитив или пустой объект
            string cleanName = prefabName.ToLower();
            switch (cleanName)
            {
                case "cube":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "sphere":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case "capsule":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                case "cylinder":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case "plane":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                case "quad":
                    createdObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    break;
                default:
                    // Создаем пустой GameObject для неизвестных объектов
                    createdObject = new GameObject(prefabName);
                    Debug.LogWarning($"Created empty GameObject for: {prefabName} (ID: {objectId:X2}) - no prefab mapping found");
                    break;
            }

            if (createdObject != null)
            {
                createdObject.transform.position = position;
                createdObject.name = prefabName;
            }
        }

        return createdObject;
    }

    string GetPrefabNameById(byte id)
    {
        // Сначала ищем в наших сопоставлениях
        if (prefabMappings != null)
        {
            foreach (PrefabMapping mapping in prefabMappings)
            {
                if (mapping.id == id)
                {
                    return mapping.objectName;
                }
            }
        }

        // Fallback к старой системе
        switch (id)
        {
            case 0x01: return "Cube";
            case 0x02: return "DOM 3";
            case 0x03: return "hromakey";
            case 0x04: return "building";
            case 0x05: return "EXIT";
            case 0x06: return "wide building";
            case 0x07: return "gruzovik";
            case 0x08: return "road";
            case 0x09: return "4 buildings";
            case 0x0A: return "sixth radio";
            case 0x0B: return "feik zdanie";
            case 0x0C: return "blue skybox";
            case 0x0D: return "purple skybox";
            case 0x0E: return "x3";
            case 0x0F: return "scene";
            case 0x10: return "cafe";
            case 0x11: return "cafe building";
            case 0x12: return "fancy platform";
            case 0x13: return "platforma tonshe";
            case 0x14: return "huinya kakaya to";
            case 0x15: return "build";
            case 0x16: return "zhytomir";
            case 0x17: return "hayp zdanie";
            case 0x18: return "strange cube";
            case 0x19: return "tree";
            case 0x1A: return "tv";
            case 0x1B: return "vyshka";
            case 0x1C: return "wider cube";
            default: return null;
        }
    }

    // Структура для сопоставления префабов с ID
    [System.Serializable]
    public struct PrefabMapping
    {
        [Tooltip("Имя объекта в сцене (как он сохраняется)")]
        public string objectName;

        [Tooltip("Уникальный ID объекта (в hex формате)")]
        public byte id;

        [Tooltip("Префаб для создания объекта")]
        public GameObject prefab;

        [Tooltip("Описание объекта")]
        public string description;
    }
}