using UnityEngine;
using Discord;
using UnityEngine.SceneManagement;

public class DiscordController : MonoBehaviour
{
    private Discord.Discord discord;
    private long clientId = 1386045527942103110; // Replace with your Discord application client ID

    [Header("Level Settings")]
    public string currentLevel = "Level 1 - City"; // Set this from your game logic
    public string currentIcon = "game_icon"; // Current scene icon key

    [Header("Scene Icon Mapping")]
    public SceneIconMapping[] sceneIcons; // Array to map scenes to icons

    [System.Serializable]
    public class SceneIconMapping
    {
        public string sceneName;
        public string iconKey; // Key name from Discord Developer Portal
        public string iconText; // Hover text for the icon
    }

    void Start()
    {
        // Initialize Discord
        try
        {
            discord = new Discord.Discord(clientId, (ulong)Discord.CreateFlags.Default);
            Debug.Log("Discord SDK initialized successfully");

            // Get current scene info
            UpdateSceneInfo();

            // Set up initial activity
            UpdateActivity();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Discord: {e.Message}");
        }
    }

    void Update()
    {
        // Run callbacks to process Discord events
        if (discord != null)
        {
            discord.RunCallbacks();
        }
    }

    void UpdateSceneInfo()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Find matching scene icon mapping
        foreach (var mapping in sceneIcons)
        {
            if (mapping.sceneName == currentSceneName)
            {
                currentIcon = mapping.iconKey;
                currentLevel = mapping.sceneName;
                return;
            }
        }

        // Default values if no mapping found
        currentIcon = "game_icon";
        currentLevel = currentSceneName;
    }

    void UpdateActivity()
    {
        if (discord == null) return;

        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = currentLevel, // Display the current level
            Details = "Playing",
            Timestamps =
            {
                Start = System.DateTimeOffset.Now.ToUnixTimeSeconds()
            },
            Assets =
            {
                LargeImage = currentIcon, // Dynamic icon based on scene
                LargeText = GetIconText(),
            }
        };

        activityManager.UpdateActivity(activity, (result) =>
        {
            if (result == Discord.Result.Ok)
            {
                Debug.Log("Discord activity updated successfully");
            }
            else
            {
                Debug.LogError($"Failed to update activity: {result}");
            }
        });
    }

    string GetIconText()
    {
        // Find matching scene for icon text
        foreach (var mapping in sceneIcons)
        {
            if (mapping.iconKey == currentIcon)
            {
                return mapping.iconText;
            }
        }
        return "My Unity Game";
    }

    void OnApplicationQuit()
    {
        // Clean up Discord connection
        if (discord != null)
        {
            discord.Dispose();
        }
    }

    // Call this method when the player changes levels
    public void SetLevel(string levelName)
    {
        currentLevel = levelName;
        UpdateActivity();
    }

    // Call this method to change both level and icon
    public void SetLevelWithIcon(string levelName, string iconKey)
    {
        currentLevel = levelName;
        currentIcon = iconKey;
        UpdateActivity();
    }

    // Automatically update when scene changes
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneInfo();
        UpdateActivity();
    }
}