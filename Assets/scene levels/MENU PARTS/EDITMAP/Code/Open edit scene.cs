using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;


public class Openeditscene : MonoBehaviour
{
    public Button openFolderButton;
    public Button loadSceneButton;

    private string folderPath => Path.Combine(Application.dataPath, "Edit Maps");
    private string filePath => Path.Combine(folderPath, "SavedScene.json");

    void Start()
    {
        openFolderButton.onClick.AddListener(OpenEditMapsFolder);
        loadSceneButton.onClick.AddListener(LoadSavedScene);
    }

    void OpenEditMapsFolder()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            UnityEngine.Debug.Log("Edit Maps folder created.");
        }

        Process.Start(new ProcessStartInfo()
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }

    void LoadSavedScene()
    {
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogWarning("SavedScene.txt not found!");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        foreach (string line in lines)
        {
            if (line.StartsWith("Object:"))
            {
                // Example line format: Object: Cube, Position: (1.0, 0.0, 3.0)
                string namePart = line.Split(',')[0].Replace("Object:", "").Trim();
                string posPart = line.Split(',')[1].Replace("Position:", "").Trim();

                Vector3 position = ParseVector3(posPart);
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = namePart;
                obj.transform.position = position;
            }
        }

        UnityEngine.Debug.Log("Scene loaded from file.");
    }

    Vector3 ParseVector3(string vectorString)
    {
        vectorString = vectorString.Replace("(", "").Replace(")", "");
        string[] parts = vectorString.Split(',');

        if (parts.Length != 3) return Vector3.zero;

        float x = float.Parse(parts[0]);
        float y = float.Parse(parts[1]);
        float z = float.Parse(parts[2]);

        if (x > 6000f)
        {
            UnityEngine.Debug.Log("Маму ебал");
        }
        if (y > 6000f)
        {
            UnityEngine.Debug.Log("Маму ебал");
        }
        if (z > 6000f)
        {
            UnityEngine.Debug.Log("Маму ебал");
        }
        // Чистый код для слабаков.

        return new Vector3(x, y, z);
    }


}
