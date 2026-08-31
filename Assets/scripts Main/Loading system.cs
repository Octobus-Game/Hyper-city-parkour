using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider loadingSlider;    // Reference to the UI slider for progress
    public Text loadingText;        // Reference to the UI text for progress percentage
    public Button loadButton;       // Reference to the button that triggers loading (legacy)
    public Button[] sceneButtons;   // Array of buttons for individual scene loading
    public GameObject loadingPanel; // Reference to the loading panel to activate

    [Header("Scene Settings")]
    public string[] scenesToLoad;   // Array of scene names to load
    public bool loadScenesAdditively = false; // Whether to load scenes additively or replace current scene

    [Header("Optional: Single Scene (Legacy Support)")]
    public string sceneToLoad;      // Single scene name (for backward compatibility)

    void Start()
    {
        // Set up button click event for legacy single button
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(() => StartLoading(-1)); // -1 indicates load all scenes
        }

        // Set up individual scene buttons
        SetupSceneButtons();

        // Initially hide the loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    // Method called when button is clicked
    public void StartLoading()
    {
        StartLoading(-1); // Load all scenes
    }

    // Method called when specific scene button is clicked
    public void StartLoading(int sceneIndex)
    {
        // Activate loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // Disable all buttons to prevent multiple clicks
        DisableAllButtons();

        // Start the loading process in a coroutine to allow UI updates
        StartCoroutine(LoadScenesAsync(sceneIndex));
    }

    IEnumerator LoadScenesAsync()
    {
        yield return LoadScenesAsync(-1); // Load all scenes
    }

    IEnumerator LoadScenesAsync(int specificSceneIndex)
    {
        // Determine which scenes to load
        string[] targetScenes = GetScenesToLoad(specificSceneIndex);

        if (targetScenes == null || targetScenes.Length == 0)
        {
            Debug.LogError("No scenes specified to load!");
            ResetLoadingUI();
            yield break;
        }

        float totalProgress = 0f;
        AsyncOperation[] loadOperations = new AsyncOperation[targetScenes.Length];

        // Start loading all scenes
        for (int i = 0; i < targetScenes.Length; i++)
        {
            if (string.IsNullOrEmpty(targetScenes[i]))
            {
                Debug.LogWarning($"Empty scene name at index {i}, skipping...");
                continue;
            }

            LoadSceneMode loadMode = (i == 0 && !loadScenesAdditively) ?
                LoadSceneMode.Single : LoadSceneMode.Additive;

            loadOperations[i] = SceneManager.LoadSceneAsync(targetScenes[i], loadMode);

            // Prevent scene activation until all scenes are loaded (optional)
            if (loadOperations[i] != null)
            {
                loadOperations[i].allowSceneActivation = false;
            }
        }

        // Monitor loading progress
        bool allScenesLoaded = false;
        while (!allScenesLoaded)
        {
            totalProgress = 0f;
            allScenesLoaded = true;

            // Calculate combined progress from all loading operations
            int validOperations = 0;
            for (int i = 0; i < loadOperations.Length; i++)
            {
                if (loadOperations[i] != null)
                {
                    // Normalize progress (Unity reports 0.9 as complete for async loading)
                    float normalizedProgress = Mathf.Clamp01(loadOperations[i].progress / 0.9f);
                    totalProgress += normalizedProgress;
                    validOperations++;

                    // Check if this operation is still loading
                    if (loadOperations[i].progress < 0.9f)
                    {
                        allScenesLoaded = false;
                    }
                }
            }

            // Calculate average progress
            if (validOperations > 0)
            {
                totalProgress /= validOperations;
            }

            // Update UI elements
            UpdateLoadingUI(totalProgress, targetScenes.Length, specificSceneIndex);

            yield return null; // Wait for the next frame
        }

        // Activate all loaded scenes
        for (int i = 0; i < loadOperations.Length; i++)
        {
            if (loadOperations[i] != null)
            {
                loadOperations[i].allowSceneActivation = true;
            }
        }

        // Wait for actual scene activation
        while (!AllScenesActivated(loadOperations))
        {
            yield return null;
        }

        // Final UI update
        UpdateLoadingUI(1f, targetScenes.Length, specificSceneIndex);
    }

    private string[] GetScenesToLoad()
    {
        return GetScenesToLoad(-1); // Load all scenes
    }

    private string[] GetScenesToLoad(int specificSceneIndex)
    {
        // If specific scene index is provided, load only that scene
        if (specificSceneIndex >= 0)
        {
            if (scenesToLoad != null && specificSceneIndex < scenesToLoad.Length)
            {
                if (!string.IsNullOrEmpty(scenesToLoad[specificSceneIndex]))
                {
                    return new string[] { scenesToLoad[specificSceneIndex] };
                }
            }

            Debug.LogError($"Invalid scene index {specificSceneIndex} or empty scene name!");
            return null;
        }

        // Priority: use scenesToLoad array if it has valid entries
        if (scenesToLoad != null && scenesToLoad.Length > 0)
        {
            return scenesToLoad;
        }

        // Fallback: use single scene for backward compatibility
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            return new string[] { sceneToLoad };
        }

        return null;
    }

    private void UpdateLoadingUI(float progress, int sceneCount)
    {
        UpdateLoadingUI(progress, sceneCount, -1); // Default behavior
    }

    private void UpdateLoadingUI(float progress, int sceneCount, int specificSceneIndex)
    {
        // Update slider
        if (loadingSlider != null)
        {
            loadingSlider.value = progress;
        }

        // Update text
        if (loadingText != null)
        {
            string progressText = $"Loading: {Mathf.Round(progress * 100)}%";

            if (specificSceneIndex >= 0)
            {
                // Loading specific scene
                if (scenesToLoad != null && specificSceneIndex < scenesToLoad.Length)
                {
                    progressText += $" - {scenesToLoad[specificSceneIndex]}";
                }
            }
            else if (sceneCount > 1)
            {
                // Loading multiple scenes
                progressText += $" ({sceneCount} scenes)";
            }

            loadingText.text = progressText;
        }
    }

    private bool AllScenesActivated(AsyncOperation[] operations)
    {
        for (int i = 0; i < operations.Length; i++)
        {
            if (operations[i] != null && !operations[i].isDone)
            {
                return false;
            }
        }
        return true;
    }

    private void ResetLoadingUI()
    {
        // Hide loading panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        // Re-enable all buttons
        EnableAllButtons();
    }

    private void SetupSceneButtons()
    {
        if (sceneButtons == null || scenesToLoad == null) return;

        // Set up listeners for each button that corresponds to a scene
        for (int i = 0; i < sceneButtons.Length && i < scenesToLoad.Length; i++)
        {
            if (sceneButtons[i] != null && !string.IsNullOrEmpty(scenesToLoad[i]))
            {
                int sceneIndex = i; // Capture the index for the closure
                sceneButtons[i].onClick.AddListener(() => StartLoading(sceneIndex));
            }
        }
    }

    private void DisableAllButtons()
    {
        // Disable legacy button
        if (loadButton != null)
        {
            loadButton.interactable = false;
        }

        // Disable all scene buttons
        if (sceneButtons != null)
        {
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                if (sceneButtons[i] != null)
                {
                    sceneButtons[i].interactable = false;
                }
            }
        }
    }

    private void EnableAllButtons()
    {
        // Enable legacy button
        if (loadButton != null)
        {
            loadButton.interactable = true;
        }

        // Enable all scene buttons
        if (sceneButtons != null)
        {
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                if (sceneButtons[i] != null)
                {
                    sceneButtons[i].interactable = true;
                }
            }
        }
    }

    void OnDestroy()
    {
        // Remove legacy button listener when object is destroyed
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
        }

        // Remove all scene button listeners
        if (sceneButtons != null)
        {
            for (int i = 0; i < sceneButtons.Length; i++)
            {
                if (sceneButtons[i] != null)
                {
                    sceneButtons[i].onClick.RemoveAllListeners();
                }
            }
        }
    }
}