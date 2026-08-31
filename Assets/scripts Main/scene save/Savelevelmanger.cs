using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class LevelProgressSystem : MonoBehaviour
{
    // ------------------ ДАННЫЕ ------------------
    [System.Serializable]
    public class LevelData
    {
        public int levelindexLOL = 0;
    }

    private static string savePath => Path.Combine(Application.persistentDataPath, "Levels complete.json");

    // ------------------ СОХРАНЕНИЕ ------------------
    public static void SaveProgress(int index)
    {
        LevelData data = new LevelData { levelindexLOL = index };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Сохранено: " + json);
    }

    public static LevelData LoadProgress()
    {
        if (!File.Exists(savePath))
        {
            SaveProgress(0); // создаём начальный файл
        }

        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<LevelData>(json);
    }

    // ------------------ ТРИГГЕР (вешается на объект) ------------------
    public bool actAsTrigger = false; // true если это триггер-сохранялка
    public int progressToSave = 1;    // какое значение сохранить при входе

    private void OnTriggerEnter(Collider other)
    {
        if (actAsTrigger && other.CompareTag("Player"))
        {
            SaveProgress(progressToSave);
        }
    }

    // ------------------ ПРОВЕРКА ЗАГРУЗКИ ------------------
    public bool actAsLevelLoader = false; // true если это загрузчик сцены
    public string sceneToLoad = "Level2"; // имя сцены

    public void TryLoadLevel()
    {
        if (!actAsLevelLoader) return;

        LevelData data = LoadProgress();

        if (data.levelindexLOL == 1)
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("Вы не прошли предыдущий уровень!");
        }
    }
}
