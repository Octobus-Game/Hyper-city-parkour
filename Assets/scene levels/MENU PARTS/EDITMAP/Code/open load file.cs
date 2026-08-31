using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using SFB;

public class openloadfile : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button openFolderButton;
    public Button loadSceneButton;

    private string folderPath => Path.Combine(Application.dataPath, "Edit Maps");

    [System.Serializable]
    public class SavedObjectData
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    public class SceneData
    {
        public List<SavedObjectData> objects = new List<SavedObjectData>();
    }

    void Start()
    {
        openFolderButton.onClick.AddListener(OpenEditMapsFolder);
        loadSceneButton.onClick.AddListener(OpenFileBrowser);

        // Создаём папку, если не существует
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            UnityEngine.Debug.Log("Edit Maps folder created.");
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
        {
            SaveSceneToJson();
        }
    }

    void OpenEditMapsFolder()
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }

    void OpenFileBrowser()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        var extensions = new[] {
            new ExtensionFilter("JSON Files", "json"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open Scene File", folderPath, extensions, false);
        if (paths.Length > 0 && File.Exists(paths[0]))
        {
            LoadSavedScene(paths[0]);
        }
#endif
    }

    void SaveSceneToJson()
    {
        SceneData sceneData = new SceneData();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.activeInHierarchy && obj.hideFlags == HideFlags.None)
            {
                SavedObjectData data = new SavedObjectData
                {
                    name = obj.name,
                    position = obj.transform.position,
                    rotation = obj.transform.eulerAngles,
                    scale = obj.transform.localScale
                };

                sceneData.objects.Add(data);
            }
        }

        string json = JsonUtility.ToJson(sceneData, true);

        string filePath = Path.Combine(folderPath, "scene_save.json");
        File.WriteAllText(filePath, json);

        UnityEngine.Debug.Log("Сцена сохранена в: " + filePath);
    }

    void LoadSavedScene(string filePath)
    {
        string json = File.ReadAllText(filePath);
        SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

        foreach (var objData in sceneData.objects)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); // Можно заменить на префаб
            obj.name = objData.name;
            obj.transform.position = objData.position;
            obj.transform.eulerAngles = objData.rotation;
            obj.transform.localScale = objData.scale;
        }

        UnityEngine.Debug.Log($"[LoadScene] Загружено из файла: {filePath}");
    }
}
