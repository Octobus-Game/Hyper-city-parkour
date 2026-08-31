// Вставь в Editor-скрипт для поиска битых ссылок
using UnityEditor;
using UnityEngine;

public class FindMissingAssets : Editor
{
    [MenuItem("Tools/Find Missing Assets")]
    static void Find()
    {
        string[] allAssets = AssetDatabase.GetAllAssetPaths();
        foreach (string path in allAssets)
        {
            var deps = AssetDatabase.GetDependencies(path, false);
            foreach (var dep in deps)
            {
                if (AssetDatabase.AssetPathToGUID(dep) == "")
                    Debug.LogError($"Missing dep in: {path} → {dep}");
            }
        }
    }
}