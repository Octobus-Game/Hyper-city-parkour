using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sceneloader : MonoBehaviour
{
    [Tooltip("Name of the scene to load. Make sure it's added to Build Settings.")]
    public string sceneName;

    [Tooltip("Lock and hide cursor after scene load (for 3D games)")]
    public bool lockCursorOnLoad = true;

    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(LoadSceneAsync());
        }
        else
        {
            Debug.LogWarning("Scene name is not set!");
        }
    }

    private IEnumerator LoadSceneAsync()
    {
        // Гарантируем нормальный timeScale перед загрузкой
        Time.timeScale = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Ждём пока сцена полностью загрузится
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Восстанавливаем состояние курсора после загрузки
        if (lockCursorOnLoad)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}