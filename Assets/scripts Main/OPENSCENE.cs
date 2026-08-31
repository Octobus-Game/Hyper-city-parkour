using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OPENSCENE : MonoBehaviour
{
    public string sceneName; // Имя сцены для загрузки
    public Button loadSceneButton; // Кнопка для запуска сцены

    void Start()
    {
        // Привязка метода к событию нажатия кнопки
        loadSceneButton.onClick.AddListener(LoadScene);
    }

    void LoadScene()
    {
        // Проверка, указано ли имя сцены
        if (!string.IsNullOrEmpty(sceneName))
        {
            // Загрузка сцены с указанным именем
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not set!");
        }
    }


}




