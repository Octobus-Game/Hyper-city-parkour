#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using Steamworks;

public class OctopusGameLevelCompiler : EditorWindow
{
    private string sceneName = "NewLevel";
    private string assetBundleName = "level_bundle";
    private string sceneToCompile = "";
    private SceneAsset sceneAsset;
    private Texture2D levelIcon;
    private Vector2 scrollPosition;

    // --- Steam Workshop upload settings ---
    // Valve's public test AppID ("Spacewar"). It's provided so developers can
    // build and test Steamworks features -- including Workshop uploads -- before
    // they have their own AppID approved. See:
    // https://partner.steamgames.com/doc/features/workshop/implementation
    private static readonly AppId_t SpacewarAppId = new AppId_t(480);

    private string workshopTitle = "";
    private string workshopDescription = "";
    private string workshopTags = "";
    private ERemoteStoragePublishedFileVisibility workshopVisibility =
        ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
    private ulong existingPublishedFileId = 0;
    private string steamStatusMessage = "";
    private bool steamInitialized = false;

    private string lastCompiledBundleName;
    private string pendingContentFolder;
    private string pendingPreviewImage;

    private CallResult<CreateItemResult_t> m_createItemCallResult;
    private CallResult<SubmitItemUpdateResult_t> m_submitItemCallResult;

    [MenuItem("Tools/Octopus Game/Level Manager")]
    public static void ShowWindow()
    {
        GetWindow<OctopusGameLevelCompiler>("Octopus Game");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Octopus Game - Level Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // New Level Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Create New Level", EditorStyles.boldLabel);
        GUILayout.Space(5);

        sceneName = EditorGUILayout.TextField("Scene Name:", sceneName);
        assetBundleName = EditorGUILayout.TextField("Asset Bundle Name:", assetBundleName);

        GUILayout.Space(10);

        if (GUILayout.Button("New Level", GUILayout.Height(30)))
        {
            CreateNewLevel();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // Compile Level Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Compile Level", EditorStyles.boldLabel);
        GUILayout.Space(5);

        sceneAsset = (SceneAsset)EditorGUILayout.ObjectField("Scene to Compile:",
            sceneAsset, typeof(SceneAsset), false);

        if (sceneAsset != null)
        {
            sceneToCompile = sceneAsset.name;
        }

        GUILayout.Space(5);
        levelIcon = (Texture2D)EditorGUILayout.ObjectField("Level Icon:", levelIcon, typeof(Texture2D), false);

        if (levelIcon != null)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Icon Preview:", EditorStyles.boldLabel);
            Rect iconRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(iconRect, levelIcon);
        }

        EditorGUILayout.HelpBox("Select a scene and an icon texture to compile into the asset bundle.", MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Compile This Level", GUILayout.Height(30)))
        {
            CompileLevel();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // Steam Workshop Upload Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Upload to Steam Workshop (Spacewar / AppID 480)", EditorStyles.boldLabel);
        GUILayout.Space(5);

        workshopTitle = EditorGUILayout.TextField("Item Title:", workshopTitle);

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Description:");
        workshopDescription = EditorGUILayout.TextArea(workshopDescription, GUILayout.Height(70));

        GUILayout.Space(5);
        workshopTags = EditorGUILayout.TextField("Tags (comma-separated):", workshopTags);

        GUILayout.Space(5);
        workshopVisibility = (ERemoteStoragePublishedFileVisibility)EditorGUILayout.EnumPopup(
            "Visibility:", workshopVisibility);

        GUILayout.Space(5);
        string publishedIdField = EditorGUILayout.TextField(
            "Existing Published File ID (0 = new item):", existingPublishedFileId.ToString());
        ulong.TryParse(publishedIdField, out existingPublishedFileId);

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Uploads the bundle compiled above. Requires Steamworks.NET, the Steam client running " +
            "and logged in, and a 'steam_appid.txt' file containing '480' in the project's root " +
            "folder (next to Assets) so the Editor authenticates as the Spacewar test app.",
            MessageType.Info);

        if (!string.IsNullOrEmpty(steamStatusMessage))
        {
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(steamStatusMessage, MessageType.None);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Upload Compiled Bundle to Steam Workshop", GUILayout.Height(30)))
        {
            UploadToSteamWorkshop();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        // Quick Actions
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Open Asset Bundle Output Folder"))
        {
            string path = Application.dataPath + "/../AssetBundles";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.RevealInFinder(path);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

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

        // Create Scenes folder if it doesn't exist
        string scenesPath = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // Create a folder for this specific level
        string levelFolderPath = $"{scenesPath}/{sceneName}";
        if (!AssetDatabase.IsValidFolder(levelFolderPath))
        {
            string guid = AssetDatabase.CreateFolder(scenesPath, sceneName);
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create folder: {levelFolderPath}", "OK");
                return;
            }
            Debug.Log($"Created level folder: {levelFolderPath}");
        }
        else
        {
            EditorUtility.DisplayDialog("Warning",
                $"Folder '{sceneName}' already exists. Scene will be created in existing folder.",
                "OK");
        }

        // Create new scene in the level folder
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        string scenePath = $"{levelFolderPath}/{sceneName}.unity";

        // Save the scene
        EditorSceneManager.SaveScene(newScene, scenePath);

        // Set asset bundle name for the scene
        AssetImporter importer = AssetImporter.GetAtPath(scenePath);
        if (importer != null)
        {
            importer.assetBundleName = assetBundleName;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Success",
            $"Level '{sceneName}' created successfully!\n\nFolder: {levelFolderPath}\nAsset Bundle: {assetBundleName}\nScene: {scenePath}",
            "OK");

        Debug.Log($"New level created: {scenePath} with asset bundle: {assetBundleName}");

        // Highlight the created folder in Project window
        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(levelFolderPath);
        if (folderObj != null)
        {
            EditorGUIUtility.PingObject(folderObj);
            Selection.activeObject = folderObj;
        }
    }

    private void CompileLevel()
    {
        if (string.IsNullOrEmpty(sceneToCompile))
        {
            EditorUtility.DisplayDialog("Error", "Please specify a scene to compile.", "OK");
            return;
        }

        // Try to find the scene in level folders first
        string scenePath = FindScenePath(sceneToCompile);

        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Error",
                $"Scene '{sceneToCompile}' not found in any level folder.",
                "OK");
            return;
        }

        Debug.Log($"Found scene at: {scenePath}");

        // Get asset bundle name from scene
        AssetImporter sceneImporter = AssetImporter.GetAtPath(scenePath);
        if (sceneImporter == null || string.IsNullOrEmpty(sceneImporter.assetBundleName))
        {
            EditorUtility.DisplayDialog("Error",
                "Scene doesn't have an asset bundle assigned. Please assign one first.",
                "OK");
            return;
        }

        string bundleName = sceneImporter.assetBundleName;
        string iconBundleName = bundleName + "_icon";

        // Remember this so the Steam Workshop upload section below knows
        // exactly which bundle file corresponds to what was just built.
        lastCompiledBundleName = bundleName;

        // Create list of asset bundle builds
        List<AssetBundleBuild> builds = new List<AssetBundleBuild>();

        // Build for scene
        AssetBundleBuild sceneBuild = new AssetBundleBuild();
        sceneBuild.assetBundleName = bundleName;
        sceneBuild.assetNames = new string[] { scenePath };
        builds.Add(sceneBuild);

        // Build for icon if provided
        if (levelIcon != null)
        {
            string iconPath = AssetDatabase.GetAssetPath(levelIcon);
            if (!string.IsNullOrEmpty(iconPath))
            {
                AssetBundleBuild iconBuild = new AssetBundleBuild();
                iconBuild.assetBundleName = iconBundleName;
                iconBuild.assetNames = new string[] { iconPath };
                builds.Add(iconBuild);

                Debug.Log($"Level icon '{levelIcon.name}' will be built into: {iconBundleName}");
            }
        }

        // Force save all assets before building
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Create output directory
        string outputPath = Path.Combine(Application.dataPath, "..", "AssetBundles");
        outputPath = Path.GetFullPath(outputPath);

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log($"Created AssetBundles directory at: {outputPath}");
        }

        Debug.Log($"Building asset bundles to: {outputPath}");
        Debug.Log($"Scene bundle name: {bundleName}");
        if (levelIcon != null)
        {
            Debug.Log($"Icon bundle name: {iconBundleName}");
        }
        Debug.Log($"Target platform: {EditorUserBuildSettings.activeBuildTarget}");
        Debug.Log($"Building {builds.Count} bundle(s)");

        // Build ONLY the specified asset bundles
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            builds.ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("Build Failed",
                "Asset bundle build failed! Check the Console for errors.",
                "OK");
            Debug.LogError("Asset bundle build returned null manifest!");
            return;
        }

        // Verify the files were created
        string[] createdBundles = manifest.GetAllAssetBundles();
        Debug.Log($"Created {createdBundles.Length} asset bundle(s):");
        foreach (string bundle in createdBundles)
        {
            string bundlePath = Path.Combine(outputPath, bundle);
            if (File.Exists(bundlePath))
            {
                FileInfo fileInfo = new FileInfo(bundlePath);
                Debug.Log($"  - {bundle} ({fileInfo.Length} bytes)");
            }
        }

        string successMessage = $"Level '{sceneToCompile}' compiled successfully!\n\nScene Bundle: {bundleName}\nOutput: {outputPath}\n\nFiles created: {createdBundles.Length}";

        if (levelIcon != null)
        {
            successMessage += $"\n\nIcon Bundle: {iconBundleName}\nIcon: {levelIcon.name}";
        }

        EditorUtility.DisplayDialog("Success", successMessage, "OK");

        // Refresh asset database
        AssetDatabase.Refresh();

        // Open the folder
        EditorUtility.RevealInFinder(outputPath);
    }

    private string FindScenePath(string sceneName)
    {
        // First try direct path in level folder
        string directPath = $"Assets/Scenes/{sceneName}/{sceneName}.unity";
        if (File.Exists(directPath))
        {
            return directPath;
        }

        // Search all scene files in Scenes folder
        string[] sceneGuids = AssetDatabase.FindAssets($"t:Scene {sceneName}", new[] { "Assets/Scenes" });

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName == sceneName)
            {
                return path;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------
    // Steam Workshop upload (Steamworks.NET)
    // ---------------------------------------------------------------

    private void UploadToSteamWorkshop()
    {
        if (string.IsNullOrEmpty(lastCompiledBundleName))
        {
            EditorUtility.DisplayDialog("Error",
                "No bundle has been compiled yet in this Editor session.\n\n" +
                "Click \"Compile This Level\" above first, then upload.",
                "OK");
            return;
        }

        if (string.IsNullOrEmpty(workshopTitle))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a title for the Workshop item.", "OK");
            return;
        }

        string outputPath = Path.Combine(Application.dataPath, "..", "AssetBundles");
        outputPath = Path.GetFullPath(outputPath);
        string bundlePath = Path.Combine(outputPath, lastCompiledBundleName);

        if (!File.Exists(bundlePath))
        {
            EditorUtility.DisplayDialog("Error",
                $"Compiled bundle not found at:\n{bundlePath}\n\nPlease compile the level again.",
                "OK");
            return;
        }

        if (!EnsureSteamInitialized())
        {
            return;
        }

        try
        {
            // Steam Workshop content is uploaded as a whole folder, so stage the
            // compiled bundle (and its manifest, if present) into their own folder
            // rather than pointing at the shared AssetBundles output directory,
            // which may also contain bundles from other levels.
            string stagingFolder = Path.Combine(outputPath, "WorkshopUpload", lastCompiledBundleName);
            Directory.CreateDirectory(stagingFolder);

            File.Copy(bundlePath, Path.Combine(stagingFolder, lastCompiledBundleName), true);

            string manifestPath = bundlePath + ".manifest";
            if (File.Exists(manifestPath))
            {
                File.Copy(manifestPath, Path.Combine(stagingFolder, lastCompiledBundleName + ".manifest"), true);
            }

            pendingContentFolder = stagingFolder;
            pendingPreviewImage = levelIcon != null ? AssetDatabase.GetAssetPath(levelIcon) : null;
        }
        catch (IOException e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to stage Workshop content folder:\n{e.Message}", "OK");
            return;
        }

        if (existingPublishedFileId == 0)
        {
            steamStatusMessage = "Creating new Steam Workshop item...";
            Repaint();

            SteamAPICall_t call = SteamUGC.CreateItem(SpacewarAppId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            m_createItemCallResult.Set(call);
        }
        else
        {
            steamStatusMessage = "Preparing update for existing Workshop item...";
            Repaint();

            StartItemUpdate(new PublishedFileId_t(existingPublishedFileId));
        }
    }

    private void StartItemUpdate(PublishedFileId_t fileId)
    {
        UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(SpacewarAppId, fileId);

        SteamUGC.SetItemTitle(updateHandle, workshopTitle);
        SteamUGC.SetItemDescription(updateHandle, workshopDescription);
        SteamUGC.SetItemVisibility(updateHandle, workshopVisibility);

        if (!SteamUGC.SetItemContent(updateHandle, pendingContentFolder))
        {
            Debug.LogWarning("[Steam Workshop] SetItemContent returned false.");
        }

        if (!string.IsNullOrEmpty(pendingPreviewImage))
        {
            SteamUGC.SetItemPreview(updateHandle, pendingPreviewImage);
        }

        string[] tags = ParseTags(workshopTags);
        if (tags != null)
        {
            SteamUGC.SetItemTags(updateHandle, tags);
        }

        steamStatusMessage = "Uploading to Steam Workshop...";
        Repaint();

        SteamAPICall_t call = SteamUGC.SubmitItemUpdate(updateHandle, "Updated via Octopus Game Level Manager");
        m_submitItemCallResult.Set(call);
    }

    private string[] ParseTags(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return null;
        }

        List<string> tags = new List<string>();
        foreach (string tag in csv.Split(','))
        {
            string trimmed = tag.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                tags.Add(trimmed);
            }
        }

        return tags.Count > 0 ? tags.ToArray() : null;
    }

    private void OnCreateItemResult(CreateItemResult_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_eResult != EResult.k_EResultOK)
        {
            steamStatusMessage = $"Failed to create Workshop item: {pCallback.m_eResult}";
            Repaint();
            EditorUtility.DisplayDialog("Steam Upload Failed", steamStatusMessage, "OK");
            return;
        }

        existingPublishedFileId = pCallback.m_nPublishedFileId.m_PublishedFileId;

        if (pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            EditorUtility.DisplayDialog("Workshop Agreement Required",
                "You need to accept the Steam Workshop Legal Agreement before this item can be " +
                "published publicly. Accept it via the Steam client or the Workshop website, then " +
                "upload again.",
                "OK");
        }

        StartItemUpdate(pCallback.m_nPublishedFileId);
    }

    private void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_eResult != EResult.k_EResultOK)
        {
            steamStatusMessage = $"Upload failed: {pCallback.m_eResult}";
            Repaint();
            EditorUtility.DisplayDialog("Steam Upload Failed", steamStatusMessage, "OK");
            return;
        }

        steamStatusMessage = $"Upload complete! Published File ID: {existingPublishedFileId}";
        Repaint();

        if (pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            EditorUtility.DisplayDialog("Workshop Agreement Required",
                $"Item uploaded (Published File ID: {existingPublishedFileId}), but you still need " +
                "to accept the Steam Workshop Legal Agreement before it becomes visible to others.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Success",
                $"Workshop item published!\n\nPublished File ID: {existingPublishedFileId}",
                "OK");
        }
    }

    private bool EnsureSteamInitialized()
    {
        if (steamInitialized)
        {
            return true;
        }

        bool initialized;
        try
        {
            initialized = SteamAPI.Init();
        }
        catch (System.DllNotFoundException e)
        {
            EditorUtility.DisplayDialog("Steamworks Not Found",
                "Could not load the native Steamworks library:\n" + e.Message +
                "\n\nMake sure Steamworks.NET is installed in this project.",
                "OK");
            return false;
        }

        if (!initialized)
        {
            EditorUtility.DisplayDialog("Steam Init Failed",
                "SteamAPI.Init() failed. Make sure:\n\n" +
                "1. The Steam client is running and you are logged in.\n" +
                "2. A file named 'steam_appid.txt' containing '480' exists in your project's root " +
                "folder (the folder that contains Assets), so the Editor authenticates as the " +
                "Spacewar test app.",
                "OK");
            return false;
        }

        m_createItemCallResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
        m_submitItemCallResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);

        EditorApplication.update += OnEditorSteamUpdate;
        steamInitialized = true;
        return true;
    }

    private void OnEditorSteamUpdate()
    {
        if (steamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        // Note: closing this window mid-upload stops callbacks from being pumped,
        // so let an upload finish (watch the status message above) before closing.
        if (steamInitialized)
        {
            EditorApplication.update -= OnEditorSteamUpdate;
            SteamAPI.Shutdown();
            steamInitialized = false;
        }
    }
}
#endif