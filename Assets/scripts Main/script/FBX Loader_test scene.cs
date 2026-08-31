using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FBXLoader : MonoBehaviour
{
    [Header("Настройки загрузки")]
    [SerializeField] private string fbxFolderPath = "Assets/nps"; // Папка с FBX файлами
    [SerializeField] private bool loadOnStart = false; // Загружать при старте
    [SerializeField] private Vector3 spawnOffset = new Vector3(5f, 0f, 0f); // Смещение между объектами
    [SerializeField] private bool createParentContainer = true; // Создать родительский контейнер

    private List<GameObject> loadedObjects = new List<GameObject>();

    void Start()
    {
        if (loadOnStart)
        {
            LoadAllFBXFiles();
        }
    }

    [ContextMenu("Загрузить все FBX файлы")]
    public void LoadAllFBXFiles()
    {
        ClearLoadedObjects();

        GameObject parentContainer = null;
        if (createParentContainer)
        {
            parentContainer = new GameObject("FBX_Container");
            parentContainer.transform.position = transform.position;
        }

        // Поиск всех FBX файлов в указанной папке
        string[] fbxPaths = GetAllFBXPaths();

        if (fbxPaths.Length == 0)
        {
            Debug.LogWarning($"FBXLoader: Не найдено FBX файлов в папке {fbxFolderPath}");
            return;
        }

        Vector3 currentPosition = transform.position;
        int objectCount = 0;

        foreach (string path in fbxPaths)
        {
            GameObject fbxObject = LoadFBXFromPath(path);
            if (fbxObject != null)
            {
                // Позиционирование объекта
                fbxObject.transform.position = currentPosition + (spawnOffset * objectCount);

                // Установка родителя
                if (parentContainer != null)
                {
                    fbxObject.transform.SetParent(parentContainer.transform);
                }

                loadedObjects.Add(fbxObject);
                objectCount++;

                Debug.Log($"FBXLoader: Загружен объект {fbxObject.name} из {path}");
            }
        }

        Debug.Log($"FBXLoader: Загружено {objectCount} FBX объектов");
    }

    private string[] GetAllFBXPaths()
    {
        List<string> fbxPaths = new List<string>();

        if (Directory.Exists(fbxFolderPath))
        {
            string[] files = Directory.GetFiles(fbxFolderPath, "*.fbx", SearchOption.AllDirectories);
            fbxPaths.AddRange(files);
        }
        else
        {
            // Поиск по всему проекту если папка не найдена
            Debug.LogWarning($"FBXLoader: Папка {fbxFolderPath} не найдена. Поиск по всему проекту...");
            string[] allFiles = Directory.GetFiles(Application.dataPath, "*.fbx", SearchOption.AllDirectories);

            foreach (string file in allFiles)
            {
                // Конвертация абсолютного пути в относительный путь Assets
                string relativePath = "Assets" + file.Substring(Application.dataPath.Length);
                fbxPaths.Add(relativePath.Replace('\\', '/'));
            }
        }

        return fbxPaths.ToArray();
    }

    private GameObject LoadFBXFromPath(string assetPath)
    {
        try
        {
            // Загрузка ассета из Resources или напрямую
            Object loadedAsset = null;

#if UNITY_EDITOR
            // В редакторе используем AssetDatabase
            loadedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            // В билде используем Resources (файл должен быть в папке Resources)
            string resourcePath = GetResourcesPath(assetPath);
            if (!string.IsNullOrEmpty(resourcePath))
            {
                loadedAsset = Resources.Load<GameObject>(resourcePath);
            }
#endif

            if (loadedAsset != null)
            {
                GameObject instantiatedObject = Instantiate(loadedAsset as GameObject);
                instantiatedObject.name = Path.GetFileNameWithoutExtension(assetPath);
                return instantiatedObject;
            }
            else
            {
                Debug.LogError($"FBXLoader: Не удалось загрузить FBX файл: {assetPath}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FBXLoader: Ошибка при загрузке {assetPath}: {e.Message}");
            return null;
        }
    }

    private string GetResourcesPath(string assetPath)
    {
        // Конвертация пути Assets в путь Resources
        int resourcesIndex = assetPath.IndexOf("Resources/");
        if (resourcesIndex >= 0)
        {
            string resourcePath = assetPath.Substring(resourcesIndex + "Resources/".Length);
            return Path.ChangeExtension(resourcePath, null); // Убираем расширение
        }
        return null;
    }

    [ContextMenu("Очистить загруженные объекты")]
    public void ClearLoadedObjects()
    {
        foreach (GameObject obj in loadedObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(obj);
#endif
                }
            }
        }
        loadedObjects.Clear();

        // Удаление контейнера если он существует
        GameObject container = GameObject.Find("FBX_Container");
        if (container != null)
        {
            if (Application.isPlaying)
            {
                Destroy(container);
            }
            else
            {
#if UNITY_EDITOR
                DestroyImmediate(container);
#endif
            }
        }
    }

    // Метод для загрузки конкретного FBX файла
    public GameObject LoadSpecificFBX(string fileName)
    {
        string[] allPaths = GetAllFBXPaths();
        foreach (string path in allPaths)
        {
            if (Path.GetFileName(path).ToLower() == fileName.ToLower())
            {
                return LoadFBXFromPath(path);
            }
        }

        Debug.LogWarning($"FBXLoader: FBX файл {fileName} не найден");
        return null;
    }

    // Метод для получения списка всех найденных FBX файлов
    public List<string> GetAvailableFBXFiles()
    {
        List<string> fileNames = new List<string>();
        string[] paths = GetAllFBXPaths();

        foreach (string path in paths)
        {
            fileNames.Add(Path.GetFileName(path));
        }

        return fileNames;
    }

#if UNITY_EDITOR
    // Кнопки в инспекторе для удобства
    [CustomEditor(typeof(FBXLoader))]
    public class FBXLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FBXLoader loader = (FBXLoader)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Загрузить все FBX файлы"))
            {
                loader.LoadAllFBXFiles();
            }

            if (GUILayout.Button("Очистить загруженные объекты"))
            {
                loader.ClearLoadedObjects();
            }

            if (GUILayout.Button("Показать доступные FBX файлы"))
            {
                List<string> fbxFiles = loader.GetAvailableFBXFiles();
                Debug.Log($"Найдено {fbxFiles.Count} FBX файлов:");
                foreach (string file in fbxFiles)
                {
                    Debug.Log($"- {file}");
                }
            }
        }
    }
#endif
}