using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class LevelUnlocker : MonoBehaviour
{
    public int currentLevel = 1; // Set this in the Inspector for each level

    private string saveFolder;
    private string saveFile;

    private void Start()
    {
        saveFolder = Path.Combine(Application.persistentDataPath, "level save");
        saveFile = Path.Combine(saveFolder, "progress.txt");

        // Create save folder if it doesn't exist
        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        // If no save exists, unlock only level 1
        if (!File.Exists(saveFile))
            SaveProgress(1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            int unlockedLevel = LoadProgress();
            if (currentLevel >= unlockedLevel)
            {
                SaveProgress(currentLevel + 1); // Unlock next level
            }

            // Load next level if it exists
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
        }
    }

    private void SaveProgress(int levelToUnlock)
    {
        File.WriteAllText(saveFile, levelToUnlock.ToString());
        PlayerPrefs.SetInt("UnlockedLevel", levelToUnlock); // Optional for UI menus
        PlayerPrefs.Save();
    }

    private int LoadProgress()
    {
        if (File.Exists(saveFile))
        {
            string data = File.ReadAllText(saveFile);
            if (int.TryParse(data, out int level))
                return level;
        }
        return 1;
    }

    public static bool IsLevelUnlocked(int level)
    {
        int unlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
        return level <= unlocked;
    }
}
