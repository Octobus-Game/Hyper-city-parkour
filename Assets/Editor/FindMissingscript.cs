using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void FindAll()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        int missingCount = 0;
        List<string> report = new List<string>();

        foreach (GameObject go in allObjects)
        {
            // Пропускаем объекты не из сцены (префабы в памяти и т.д.)
            if (!go.scene.isLoaded) continue;

            Component[] components = go.GetComponents<Component>();
            foreach (Component c in components)
            {
                if (c == null)
                {
                    string path = GetFullPath(go);
                    report.Add(path);
                    missingCount++;

                    // Выделяем объект в иерархии
                    Selection.activeGameObject = go;
                    break;
                }
            }
        }

        if (missingCount == 0)
        {
            Debug.Log("[FindMissingScripts] Missing scripts not found.");
        }
        else
        {
            Debug.LogWarning($"[FindMissingScripts] Found {missingCount} object(s) with missing scripts:");
            foreach (string path in report)
                Debug.LogWarning($"  → {path}");
        }
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return $"[{go.scene.name}] {path}";
    }
}