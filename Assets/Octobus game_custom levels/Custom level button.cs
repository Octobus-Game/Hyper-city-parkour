using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LevelLoaderUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform levelButtonContainer; // Parent object for buttons (e.g., Grid Layout Group)
    public GameObject levelButtonPrefab; // Prefab with Button, Image (icon), and Text (level name)
    public Button selectFolderButton; // Button to open folder selection
    public Text currentFolderText; // Text to display current folder path

    [Header("Settings")]
    public string assetBundleFolderPath = "AssetBundles"; // Relative to project root
    public bool autoRefreshOnStart = true;

    [Header("Optional Loading UI")]
    public GameObject loadingPanel; // Optional loading indicator
    public Text loadingText; // Optional loading progress text

    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private bool isLoading = false;
    private string currentBundlePath;

    void Start()
    {
        // Set initial path
        currentBundlePath = Path.Combine(Application.dataPath, "..", assetBundleFolderPath);
        currentBundlePath = Path.GetFullPath(currentBundlePath);
        UpdateCurrentFolderDisplay();

        // Setup folder selection button
        if (selectFolderButton != null)
        {
            selectFolderButton.onClick.AddListener(OpenFolderSelection);
        }

        if (autoRefreshOnStart)
        {
            LoadAllLevels();
        }
    }

    /// <summary>
    /// Opens folder selection dialog
    /// </summary>
    public void OpenFolderSelection()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string selectedPath = OpenFolderPanel("Select AssetBundles Folder", currentBundlePath);

        if (!string.IsNullOrEmpty(selectedPath))
        {
            currentBundlePath = selectedPath;
            UpdateCurrentFolderDisplay();
            LoadAllLevels();
        }
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        string selectedPath = OpenFolderPanel("Select AssetBundles Folder", currentBundlePath);
        
        if (!string.IsNullOrEmpty(selectedPath))
        {
            currentBundlePath = selectedPath;
            UpdateCurrentFolderDisplay();
            LoadAllLevels();
        }
#elif UNITY_ANDROID
        // For Android, you might want to use a different approach or plugin
        Debug.LogWarning("Folder selection not supported on Android. Using default path.");
        NativeAndroidFolderPicker();
#elif UNITY_IOS
        // For iOS, folder selection is limited. You might need a plugin
        Debug.LogWarning("Folder selection not supported on iOS. Using default path.");
#else
        Debug.LogWarning("Folder selection not supported on this platform.");
#endif
    }

    /// <summary>
    /// Platform-specific folder picker for Windows/Mac/Linux
    /// </summary>
    private string OpenFolderPanel(string title, string defaultPath)
    {
#if UNITY_EDITOR
        return UnityEditor.EditorUtility.OpenFolderPanel(title, defaultPath, "");
#elif UNITY_STANDALONE_WIN
        return WindowsFolderPicker(title, defaultPath);
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        return UnixFolderPicker(title, defaultPath);
#else
        return "";
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    /// <summary>
    /// Windows folder picker using native dialog
    /// </summary>
    private string WindowsFolderPicker(string title, string defaultPath)
    {
        try
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = title;
            dialog.SelectedPath = defaultPath;
            
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error opening folder picker: {e.Message}");
        }
        
        return "";
    }
#endif

#if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
    /// <summary>
    /// Unix-based folder picker (fallback using file browser)
    /// </summary>
    private string UnixFolderPicker(string title, string defaultPath)
    {
        // On non-editor builds, we need a different approach
        // You might want to use a third-party plugin like StandaloneFileBrowser
        Debug.LogWarning("Native folder picker not available. Please use a plugin like StandaloneFileBrowser.");
        return "";
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android folder picker (requires additional plugin)
    /// </summary>
    private void NativeAndroidFolderPicker()
    {
        // This would require a native Android plugin or Unity plugin
        // Example: NativeFilePicker or similar asset store plugin
        Debug.LogWarning("Android folder picking requires additional plugin. Using default path.");
    }
#endif

    /// <summary>
    /// Updates the UI text showing current folder path
    /// </summary>
    private void UpdateCurrentFolderDisplay()
    {
        if (currentFolderText != null)
        {
            // Show only the last 2 parts of the path for readability
            string[] pathParts = currentBundlePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int partsToShow = Mathf.Min(3, pathParts.Length);
            string displayPath = string.Join("/", pathParts, pathParts.Length - partsToShow, partsToShow);

            currentFolderText.text = $"Folder: .../{displayPath}";
        }
    }

    /// <summary>
    /// Scans the AssetBundles folder and creates a button for each level found
    /// </summary>
    public void LoadAllLevels()
    {
        // Clear existing buttons
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"Scanning for asset bundles in: {currentBundlePath}");

        if (!Directory.Exists(currentBundlePath))
        {
            Debug.LogWarning($"AssetBundles folder not found at: {currentBundlePath}");
            Debug.LogWarning("Please select a valid folder using the 'Select Folder' button");
            return;
        }

        // Get all files in AssetBundles folder
        string[] bundleFiles = Directory.GetFiles(currentBundlePath);

        // Track which level bundles we've found
        Dictionary<string, LevelData> levels = new Dictionary<string, LevelData>();

        foreach (string file in bundleFiles)
        {
            string fileName = Path.GetFileName(file);

            // Skip .manifest files and the main AssetBundles manifest file
            if (fileName.EndsWith(".manifest") || fileName == "AssetBundles")
            {
                Debug.Log($"Skipping manifest/index file: {fileName}");
                continue;
            }

            // Check if this is an icon bundle (ends with _icon)
            if (fileName.EndsWith("_icon"))
            {
                string levelName = fileName.Replace("_icon", "");

                if (!levels.ContainsKey(levelName))
                {
                    levels[levelName] = new LevelData { levelName = levelName };
                }

                levels[levelName].iconBundlePath = file;
                Debug.Log($"Found icon bundle for level: {levelName}");
            }
            else
            {
                // This is a scene bundle
                if (!levels.ContainsKey(fileName))
                {
                    levels[fileName] = new LevelData { levelName = fileName };
                }

                levels[fileName].sceneBundlePath = file;
                Debug.Log($"Found scene bundle: {fileName}");
            }
        }

        // Create buttons for each level that has a scene bundle
        int levelCount = 0;
        foreach (var levelData in levels.Values)
        {
            if (!string.IsNullOrEmpty(levelData.sceneBundlePath))
            {
                CreateLevelButton(levelData);
                levelCount++;
            }
        }

        if (levelCount == 0)
        {
            Debug.LogWarning("No valid level bundles found! Make sure the selected folder contains compiled levels.");
        }
        else
        {
            Debug.Log($"Successfully loaded {levelCount} level(s) from AssetBundles folder");
        }
    }

    /// <summary>
    /// Creates a UI button for a level
    /// </summary>
    void CreateLevelButton(LevelData levelData)
    {
        // Instantiate button prefab
        GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);

        // Get button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Level button prefab must have a Button component!");
            Destroy(buttonObj);
            return;
        }

        // Set button click listener
        button.onClick.AddListener(() => LoadLevel(levelData));

        // Set level name text (finds Text component in children)
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            // Clean up the level name for display
            string displayName = levelData.levelName.Replace("_", " ");
            buttonText.text = displayName;
        }
        else
        {
            Debug.LogWarning($"No Text component found in level button for: {levelData.levelName}");
        }

        // Load and set icon if available
        if (!string.IsNullOrEmpty(levelData.iconBundlePath))
        {
            StartCoroutine(LoadLevelIcon(levelData.iconBundlePath, buttonObj));
        }
        else
        {
            Debug.Log($"No icon found for level: {levelData.levelName}");
        }
    }

    /// <summary>
    /// Loads the icon for a level button asynchronously
    /// </summary>
    IEnumerator LoadLevelIcon(string iconBundlePath, GameObject buttonObj)
    {
        AssetBundle iconBundle = AssetBundle.LoadFromFile(iconBundlePath);

        if (iconBundle == null)
        {
            Debug.LogError($"Failed to load icon bundle: {iconBundlePath}");
            yield break;
        }

        // Load the first texture from the bundle
        string[] assetNames = iconBundle.GetAllAssetNames();

        if (assetNames.Length > 0)
        {
            AssetBundleRequest request = iconBundle.LoadAssetAsync<Texture2D>(assetNames[0]);
            yield return request;

            Texture2D iconTexture = request.asset as Texture2D;

            if (iconTexture != null)
            {
                // Find Image component in button (usually a child named "Icon" or similar)
                Image iconImage = buttonObj.GetComponentInChildren<Image>();

                if (iconImage != null)
                {
                    // Convert texture to sprite
                    Sprite iconSprite = Sprite.Create(
                        iconTexture,
                        new Rect(0, 0, iconTexture.width, iconTexture.height),
                        new Vector2(0.5f, 0.5f)
                    );

                    iconImage.sprite = iconSprite;
                    Debug.Log($"Icon loaded successfully for button");
                }
                else
                {
                    Debug.LogWarning("No Image component found in button for icon display");
                }
            }
            else
            {
                Debug.LogError("Failed to load texture from icon bundle");
            }
        }
        else
        {
            Debug.LogWarning($"Icon bundle is empty: {iconBundlePath}");
        }

        // Optionally unload icon bundle to save memory
        // iconBundle.Unload(false);
    }

    /// <summary>
    /// Loads a level when its button is clicked
    /// </summary>
    void LoadLevel(LevelData levelData)
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading a level, please wait...");
            return;
        }

        StartCoroutine(LoadLevelAsync(levelData));
    }

    /// <summary>
    /// Asynchronously loads a level scene from its asset bundle
    /// </summary>
    IEnumerator LoadLevelAsync(LevelData levelData)
    {
        isLoading = true;

        // Show loading panel if available
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        Debug.Log($"Loading level: {levelData.levelName}");

        // Update loading text
        if (loadingText != null)
        {
            loadingText.text = $"Loading {levelData.levelName}...";
        }

        // Load the scene bundle
        AssetBundle sceneBundle = AssetBundle.LoadFromFile(levelData.sceneBundlePath);

        if (sceneBundle == null)
        {
            Debug.LogError($"Failed to load scene bundle: {levelData.sceneBundlePath}");
            isLoading = false;

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            yield break;
        }

        // Get scene paths from bundle
        string[] scenePaths = sceneBundle.GetAllScenePaths();

        if (scenePaths.Length == 0)
        {
            Debug.LogError($"No scenes found in bundle: {levelData.levelName}");
            sceneBundle.Unload(false);
            isLoading = false;

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            yield break;
        }

        // Load the first scene from the bundle
        string sceneName = Path.GetFileNameWithoutExtension(scenePaths[0]);

        Debug.Log($"Loading scene: {sceneName} from path: {scenePaths[0]}");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the scene finishes loading
        while (!asyncLoad.isDone)
        {
            float progress = asyncLoad.progress * 100f;
            Debug.Log($"Loading progress: {progress:F1}%");

            if (loadingText != null)
            {
                loadingText.text = $"Loading {levelData.levelName}... {progress:F0}%";
            }

            yield return null;
        }

        Debug.Log($"Level {levelData.levelName} loaded successfully!");

        // Store loaded bundle for cleanup later
        if (!loadedBundles.ContainsKey(levelData.levelName))
        {
            loadedBundles.Add(levelData.levelName, sceneBundle);
        }

        // Hide loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        isLoading = false;
    }

    /// <summary>
    /// Public method to refresh the level list (can be called from a UI button)
    /// </summary>
    public void RefreshLevelList()
    {
        Debug.Log("Refreshing level list...");

        // Unload all bundles first
        foreach (var bundle in loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(false);
            }
        }
        loadedBundles.Clear();

        // Reload levels
        LoadAllLevels();
    }

    void OnDestroy()
    {
        // Cleanup: Unload all bundles when this object is destroyed
        foreach (var bundle in loadedBundles.Values)
        {
            if (bundle != null)
            {
                bundle.Unload(false);
            }
        }
        loadedBundles.Clear();
    }
}

/// <summary>
/// Helper class to store level data
/// </summary>
[System.Serializable]
public class LevelData
{
    public string levelName;
    public string sceneBundlePath;
    public string iconBundlePath;
}