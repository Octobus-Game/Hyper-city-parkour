using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class COMMANDS : MonoBehaviour
{
    public TMP_InputField inputField; // Ссылка на TMP_InputField
    public string sceneName; // Имя сцены для загрузки

    void Start()
    {
        // Проверяем, что поле ввода установлено
        if (inputField != null)
        {
            // Подписываемся на событие изменения текста
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }
    }

    // Метод, вызываемый при изменении текста в InputField
    private void OnInputValueChanged(string text)
    {
        if (text.Equals("loader_Dieeee", System.StringComparison.OrdinalIgnoreCase))
        {
            LoadScene();
        }
    }

    // Метод для загрузки сцены
    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("DIEEEEEE");
        }
    }
}
