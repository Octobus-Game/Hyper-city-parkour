#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;


public class OctopusGameLevelCompiler : EditorWindow
{
    private string sceneName       = "NewLevel";
    private string assetBundleName = "level_bundle";
    private string sceneToCompile  = "";
    private SceneAsset sceneAsset;
    private Texture2D levelIcon;
    private Vector2 scrollPosition;

    // Logo texture cached
    private Texture2D logoTexture;

    // ──────────────────────────────────────────
    // Level Pack System
    // ──────────────────────────────────────────
    private bool   useLevelPack       = false;
    private string levelPackBundleName = "level_pack";

    [System.Serializable]
    private class LevelEntry
    {
        public SceneAsset sceneAsset;
        public string     displayName = "";
        public Texture2D  icon;
        public bool       foldout = true;
    }

    private List<LevelEntry> packLevels = new List<LevelEntry>();

    [MenuItem("Tools/Octopus Game/Level Manager")]
    public static void ShowWindow()
    {
        GetWindow<OctopusGameLevelCompiler>("Octopus Game");
    }

    private void OnEnable()  => LoadLogo();

    private void LoadLogo()
    {
        string[] guids = AssetDatabase.FindAssets("Icon logo 0.9 t:Texture2D");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if (logoTexture == null)
            logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icon logo 0.9.png");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Octopus Game - Level Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // ──────────────────────────────────────────
        // New Level Section
        // ──────────────────────────────────────────
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Create New Level", EditorStyles.boldLabel);
        GUILayout.Space(5);

        sceneName       = EditorGUILayout.TextField("Scene Name:", sceneName);
        assetBundleName = EditorGUILayout.TextField("Asset Bundle Name:", assetBundleName);

        GUILayout.Space(10);
        if (GUILayout.Button("New Level", GUILayout.Height(30)))
            CreateNewLevel();

        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // ──────────────────────────────────────────
        // Compile Level Section
        // ──────────────────────────────────────────
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Compile Level", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        useLevelPack = EditorGUILayout.Toggle(
            new GUIContent("  Пакет уровней",
                EditorGUIUtility.IconContent("d_Package Manager@2x").image,
                "Скомпилировать несколько уровней в один пакет"),
            useLevelPack);
        EditorGUI.EndChangeCheck();

        GUILayout.Space(8);

        if (!useLevelPack)
            DrawSingleLevelSection();
        else
            DrawLevelPackSection();

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // ──────────────────────────────────────────
        // Quick Actions
        // ──────────────────────────────────────────
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Open Asset Bundle Output Folder"))
        {
            string path = Application.dataPath + "/../AssetBundles";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // ──────────────────────────────────────────
        // Logo Section
        // ──────────────────────────────────────────
        if (logoTexture == null) LoadLogo();

        if (logoTexture != null)
        {
            float windowWidth   = EditorGUIUtility.currentViewWidth;
            float maxLogoWidth  = Mathf.Min(windowWidth - 20f, 200f);
            float aspect        = (float)logoTexture.height / logoTexture.width;
            float logoHeight    = maxLogoWidth * aspect;
            float offsetX       = (windowWidth - maxLogoWidth) * 0.5f;

            GUILayout.Space(5);
            Rect logoRect = GUILayoutUtility.GetRect(windowWidth, logoHeight);
            logoRect.x     = offsetX;
            logoRect.width = maxLogoWidth;
            GUI.DrawTexture(logoRect, logoTexture, ScaleMode.ScaleToFit, true);
            GUILayout.Space(5);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Logo 'Icon logo 0.9.png' not found.\nMake sure it is imported into the project.",
                MessageType.Warning);
        }

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════════════════
    //  SINGLE LEVEL UI
    // ══════════════════════════════════════════════════════════════

    private void DrawSingleLevelSection()
    {
        sceneAsset = (SceneAsset)EditorGUILayout.ObjectField(
            "Scene to Compile:", sceneAsset, typeof(SceneAsset), false);

        if (sceneAsset != null)
            sceneToCompile = sceneAsset.name;

        GUILayout.Space(5);
        levelIcon = (Texture2D)EditorGUILayout.ObjectField(
            "Level Icon:", levelIcon, typeof(Texture2D), false);

        if (levelIcon != null)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Icon Preview:", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(iconRect, levelIcon);
        }

        EditorGUILayout.HelpBox(
            "Select a scene and an icon texture to compile into the asset bundle.",
            MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Compile This Level", GUILayout.Height(30)))
            CompileLevel();
    }

    // ══════════════════════════════════════════════════════════════
    //  LEVEL PACK UI
    // ══════════════════════════════════════════════════════════════

    private void DrawLevelPackSection()
    {
        EditorGUILayout.HelpBox(
            "Пакет уровней: добавьте несколько уровней — каждый со своей сценой, иконкой и названием.\n" +
            "Все уровни будут скомпилированы в один Asset Bundle.",
            MessageType.Info);

        GUILayout.Space(5);
        levelPackBundleName = EditorGUILayout.TextField("Имя пакета (Bundle):", levelPackBundleName);
        GUILayout.Space(8);

        for (int i = 0; i < packLevels.Count; i++)
        {
            DrawPackLevelEntry(i);
            GUILayout.Space(4);
        }

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Добавить уровень", GUILayout.Height(26)))
            packLevels.Add(new LevelEntry { displayName = $"Level {packLevels.Count + 1}" });

        GUI.enabled = packLevels.Count > 0;
        if (GUILayout.Button("− Удалить последний", GUILayout.Height(26)))
            packLevels.RemoveAt(packLevels.Count - 1);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        bool canCompile = packLevels.Count > 0 && !string.IsNullOrEmpty(levelPackBundleName);
        GUI.enabled = canCompile;

        if (GUILayout.Button($"Скомпилировать пакет ({packLevels.Count} уровн.)", GUILayout.Height(35)))
            CompileLevelPack();

        GUI.enabled = true;

        if (!canCompile && packLevels.Count == 0)
            EditorGUILayout.HelpBox("Добавьте хотя бы один уровень в пакет.", MessageType.Warning);
    }

    private void DrawPackLevelEntry(int index)
    {
        LevelEntry entry = packLevels[index];
        EditorGUILayout.BeginVertical("box");

        string header = string.IsNullOrEmpty(entry.displayName)
            ? $"Уровень {index + 1}"
            : $"[{index + 1}] {entry.displayName}";

        EditorGUILayout.BeginHorizontal();
        entry.foldout = EditorGUILayout.Foldout(entry.foldout, header, true, EditorStyles.foldoutHeader);

        GUIStyle deleteStyle = new GUIStyle(GUI.skin.button);
        deleteStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("✕", deleteStyle, GUILayout.Width(24), GUILayout.Height(18)))
        {
            packLevels.RemoveAt(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (entry.foldout)
        {
            GUILayout.Space(4);
            EditorGUI.indentLevel++;

            entry.displayName = EditorGUILayout.TextField("Название уровня:", entry.displayName);
            entry.sceneAsset  = (SceneAsset)EditorGUILayout.ObjectField(
                "Сцена:", entry.sceneAsset, typeof(SceneAsset), false);
            entry.icon        = (Texture2D)EditorGUILayout.ObjectField(
                "Иконка:", entry.icon, typeof(Texture2D), false);

            if (entry.icon != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15);
                Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(previewRect, entry.icon);
                EditorGUILayout.EndHorizontal();
            }

            if (entry.sceneAsset == null)
                EditorGUILayout.HelpBox("Выберите сцену для этого уровня.", MessageType.Warning);
            if (entry.icon == null)
                EditorGUILayout.HelpBox("Иконка не назначена.", MessageType.None);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // ══════════════════════════════════════════════════════════════
    //  COMPILE LEVEL PACK
    // ══════════════════════════════════════════════════════════════

    private void CompileLevelPack()
    {
        // Validate
        var errors = new List<string>();
        for (int i = 0; i < packLevels.Count; i++)
        {
            LevelEntry entry = packLevels[i];
            string label = string.IsNullOrEmpty(entry.displayName) ? $"Уровень {i + 1}" : entry.displayName;
            if (entry.sceneAsset == null)
                errors.Add($"• {label}: сцена не выбрана.");
        }

        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("Ошибка валидации",
                "Исправьте следующие ошибки перед компиляцией:\n\n" + string.Join("\n", errors), "OK");
            return;
        }

        // Collect scenes
        var sceneAssetPaths = new List<string>();
        var missingScenes   = new List<string>();

        foreach (LevelEntry entry in packLevels)
        {
            string scenePath = FindScenePath(entry.sceneAsset.name);
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
                missingScenes.Add(entry.sceneAsset.name);
            else
                sceneAssetPaths.Add(scenePath);
        }

        if (missingScenes.Count > 0)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Следующие сцены не найдены на диске:\n\n• " + string.Join("\n• ", missingScenes), "OK");
            return;
        }

        var builds = new List<AssetBundleBuild>();

        // Scene bundle
        builds.Add(new AssetBundleBuild
        {
            assetBundleName = levelPackBundleName,
            assetNames      = sceneAssetPaths.ToArray()
        });

        // Icon bundles (one per level entry that has an icon)
        for (int i = 0; i < packLevels.Count; i++)
        {
            LevelEntry entry = packLevels[i];
            if (entry.icon == null) continue;
            string iconPath = AssetDatabase.GetAssetPath(entry.icon);
            if (string.IsNullOrEmpty(iconPath)) continue;

            string safeName = string.IsNullOrEmpty(entry.displayName)
                ? $"level_{i + 1}"
                : entry.displayName.ToLower().Replace(" ", "_");

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = $"{levelPackBundleName}_icon_{safeName}",
                assetNames      = new[] { iconPath }
            });
        }

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AssetBundles"));
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            builds.ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("Build Failed",
                "Сборка пакета уровней не удалась! Проверьте Console на ошибки.", "OK");
            return;
        }

        string[] createdBundles = manifest.GetAllAssetBundles();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Пакет '{levelPackBundleName}' успешно скомпилирован!\n");
        sb.AppendLine($"Уровней: {packLevels.Count}   Bundle-файлов: {createdBundles.Length}");
        sb.AppendLine($"\nВыходная папка:\n{outputPath}\n\nФайлы:");
        foreach (string bundle in createdBundles)
        {
            string bundlePath = Path.Combine(outputPath, bundle);
            if (File.Exists(bundlePath))
            {
                FileInfo fi = new FileInfo(bundlePath);
                sb.AppendLine($"  • {bundle}  ({fi.Length / 1024} KB)");
            }
        }

        EditorUtility.DisplayDialog("Пакет уровней — Успех!", sb.ToString(), "OK");
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);
    }

    // ══════════════════════════════════════════════════════════════
    //  CREATE NEW LEVEL
    // ══════════════════════════════════════════════════════════════

    private void CreateNewLevel()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a scene name.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(assetBundleName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter an asset bundle name.", "OK");
            return;
        }

        string scenesPath = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesPath))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        string levelFolderPath = $"{scenesPath}/{sceneName}";
        if (!AssetDatabase.IsValidFolder(levelFolderPath))
        {
            string guid = AssetDatabase.CreateFolder(scenesPath, sceneName);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create folder: {levelFolderPath}", "OK");
                return;
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Warning",
                $"Folder '{sceneName}' already exists. Scene will be created in existing folder.", "OK");
        }

        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        string scenePath = $"{levelFolderPath}/{sceneName}.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);

        AssetImporter importer = AssetImporter.GetAtPath(scenePath);
        if (importer != null)
        {
            importer.assetBundleName = assetBundleName;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Success",
            $"Level '{sceneName}' created!\n\nFolder: {levelFolderPath}\nBundle: {assetBundleName}\nScene: {scenePath}",
            "OK");

        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(levelFolderPath);
        if (folderObj != null)
        {
            EditorGUIUtility.PingObject(folderObj);
            Selection.activeObject = folderObj;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  COMPILE SINGLE LEVEL
    // ══════════════════════════════════════════════════════════════

    private void CompileLevel()
    {
        if (string.IsNullOrEmpty(sceneToCompile))
        {
            EditorUtility.DisplayDialog("Error", "Please specify a scene to compile.", "OK");
            return;
        }

        string scenePath = FindScenePath(sceneToCompile);
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Error", $"Scene '{sceneToCompile}' not found.", "OK");
            return;
        }

        AssetImporter sceneImporter = AssetImporter.GetAtPath(scenePath);
        if (sceneImporter == null || string.IsNullOrEmpty(sceneImporter.assetBundleName))
        {
            EditorUtility.DisplayDialog("Error",
                "Scene doesn't have an asset bundle assigned. Please assign one first.", "OK");
            return;
        }

        string bundleName     = sceneImporter.assetBundleName;
        string iconBundleName = bundleName + "_icon";

        var builds = new List<AssetBundleBuild>
        {
            new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames      = new[] { scenePath }
            }
        };

        if (levelIcon != null)
        {
            string iconPath = AssetDatabase.GetAssetPath(levelIcon);
            if (!string.IsNullOrEmpty(iconPath))
            {
                builds.Add(new AssetBundleBuild
                {
                    assetBundleName = iconBundleName,
                    assetNames      = new[] { iconPath }
                });
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AssetBundles"));
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            builds.ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("Build Failed",
                "Asset bundle build failed! Check the Console for errors.", "OK");
            return;
        }

        string[] createdBundles = manifest.GetAllAssetBundles();

        string successMessage =
            $"Level '{sceneToCompile}' compiled!\n\n" +
            $"Scene Bundle: {bundleName}\n" +
            $"Output: {outputPath}\n" +
            $"Files: {createdBundles.Length}";

        if (levelIcon != null)
            successMessage += $"\nIcon Bundle: {iconBundleName}";

        EditorUtility.DisplayDialog("Success", successMessage, "OK");
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════

    private string FindScenePath(string sName)
    {
        string directPath = $"Assets/Scenes/{sName}/{sName}.unity";
        if (File.Exists(directPath)) return directPath;

        string[] guids = AssetDatabase.FindAssets($"t:Scene {sName}", new[] { "Assets/Scenes" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sName)
                return path;
        }

        return null;
    }
}
#endif