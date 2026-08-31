using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Кастомный NetworkManager для Mirror, который умеет переключаться
/// между множеством "игровых" сцен (карт/уровней), а не только
/// между Offline Scene и одной Online Scene.
///
/// Как использовать:
/// 1. Замени стандартный компонент NetworkManager на этой сцене
///    на этот скрипт (LevelNetworkManager).
/// 2. В инспекторе заполни список levelScenes именами сцен
///    (точно как они называются в Build Settings).
/// 3. Все эти сцены должны быть добавлены в
///    File -> Build Settings -> Scenes In Build.
/// 4. Вызывай LoadLevel/LoadNextLevel/LoadRandomLevel ТОЛЬКО на сервере
///    (например, из логики окончания матча или из UI хоста).
/// </summary>
public class LevelNetworkManager : NetworkManager
{
    [Header("Список игровых сцен (карт/уровней)")]
    [Tooltip("Имена сцен должны совпадать с именами файлов сцен в Build Settings")]
    public List<string> levelScenes = new List<string>();

    private int currentLevelIndex = -1;

    /// <summary>Текущая загруженная игровая сцена (для UI/логики).</summary>
    public string CurrentLevel =>
        currentLevelIndex >= 0 && currentLevelIndex < levelScenes.Count
            ? levelScenes[currentLevelIndex]
            : null;

    /// <summary>Загрузить конкретную сцену по имени. Вызывать только на сервере.</summary>
    [Server]
    public void LoadLevel(string sceneName)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("LoadLevel можно вызывать только на сервере/хосте.");
            return;
        }

        int index = levelScenes.IndexOf(sceneName);
        if (index < 0)
            Debug.LogWarning($"Сцена '{sceneName}' не найдена в списке levelScenes, " +
                              "но попытка загрузки всё равно будет выполнена.");
        else
            currentLevelIndex = index;

        // Это и есть ключевой вызов: он меняет сцену на сервере
        // и синхронно подгружает её у всех подключённых клиентов.
        ServerChangeScene(sceneName);
    }

    /// <summary>Переключиться на следующую сцену из списка (по кругу).</summary>
    [Server]
    public void LoadNextLevel()
    {
        if (levelScenes.Count == 0)
        {
            Debug.LogWarning("levelScenes пуст — добавь сцены в инспекторе.");
            return;
        }

        currentLevelIndex = (currentLevelIndex + 1) % levelScenes.Count;
        ServerChangeScene(levelScenes[currentLevelIndex]);
    }

    /// <summary>Переключиться на случайную сцену из списка.</summary>
    [Server]
    public void LoadRandomLevel()
    {
        if (levelScenes.Count == 0)
        {
            Debug.LogWarning("levelScenes пуст — добавь сцены в инспекторе.");
            return;
        }

        currentLevelIndex = Random.Range(0, levelScenes.Count);
        ServerChangeScene(levelScenes[currentLevelIndex]);
    }

    // ----- Хуки Mirror, можно дополнить своей логикой -----

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        Debug.Log($"[Server] Сцена сменилась на: {sceneName}");

        // Сюда удобно поставить, например, расстановку игроков
        // по точкам спауна новой сцены, если она не происходит
        // автоматически через твою логику спавна игрока.
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        Debug.Log("[Client] Локальная сцена синхронизирована с сервером.");
    }
}
