using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
public class NPSFolderBuildProcessor : UnityEditor.Build.IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        string sourcePath = Path.Combine(Application.dataPath, "nps");
        string resourcesPath = Path.Combine(Application.dataPath, "Resources/nps");

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning("NPS folder not found at: " + sourcePath);
            return;
        }

        // Create Resources/nps directory if it doesn't exist
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        // Copy all FBX files to Resources folder
        string[] fbxFiles = Directory.GetFiles(sourcePath, "*.fbx", SearchOption.AllDirectories);

        foreach (string fbxFile in fbxFiles)
        {
            string relativePath = fbxFile.Substring(sourcePath.Length + 1);
            string destPath = Path.Combine(resourcesPath, relativePath);

            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(fbxFile, destPath, true);
            Debug.Log($"Copied {relativePath} to Resources");
        }

        AssetDatabase.Refresh();
        Debug.Log($"Prepared {fbxFiles.Length} FBX files for build from nps folder");
    }
}
#endif

// Runtime loader for NPS models
public class NPSModelLoader : MonoBehaviour
{
    // Load a specific model by name
    public GameObject LoadModel(string modelName)
    {
        // Remove .fbx extension if provided
        modelName = modelName.Replace(".fbx", "");

        GameObject model = Resources.Load<GameObject>("nps/" + modelName);

        if (model != null)
        {
            return Instantiate(model);
        }
        else
        {
            Debug.LogError($"Model not found: {modelName}");
            return null;
        }
    }

    // Load all models from the nps folder
    public List<GameObject> LoadAllModels()
    {
        List<GameObject> models = new List<GameObject>();
        GameObject[] loadedModels = Resources.LoadAll<GameObject>("nps");

        foreach (GameObject model in loadedModels)
        {
            models.Add(Instantiate(model));
        }

        Debug.Log($"Loaded {models.Count} models from nps folder");
        return models;
    }

    // Example usage
    void Start()
    {
        // Example: Load a specific model
        // GameObject myModel = LoadModel("character");

        // Example: Load all models
        // List<GameObject> allModels = LoadAllModels();
    }
}

// Editor menu item to manually copy files
#if UNITY_EDITOR
public class NPSFolderMenu
{
    [MenuItem("Tools/NPS/Copy FBX to Resources")]
    static void CopyNPSToResources()
    {
        string sourcePath = Path.Combine(Application.dataPath, "nps");
        string resourcesPath = Path.Combine(Application.dataPath, "Resources/nps");

        if (!Directory.Exists(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "NPS folder not found at: " + sourcePath, "OK");
            return;
        }

        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
        }

        string[] fbxFiles = Directory.GetFiles(sourcePath, "*.fbx", SearchOption.AllDirectories);

        foreach (string fbxFile in fbxFiles)
        {
            string relativePath = fbxFile.Substring(sourcePath.Length + 1);
            string destPath = Path.Combine(resourcesPath, relativePath);

            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(fbxFile, destPath, true);
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"Copied {fbxFiles.Length} FBX files to Resources/nps", "OK");
    }
}
#endif