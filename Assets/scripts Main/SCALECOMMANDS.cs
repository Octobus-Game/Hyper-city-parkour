using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SCALECOMMANDS : MonoBehaviour
{
    public InputField inputField;

    void Start()
    {
        // Подписываемся на событие окончания ввода текста
        inputField.onEndEdit.AddListener(OnInputSubmit);
    }

    void OnInputSubmit(string input)
    {
        // Проверяем введённый текст
        if (input == "//scaleallobject0.5 nj")
        {
            ScaleAll();
        }
    }

    void ScaleAll()
    {
        // Находим все объекты на сцене
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Пропускаем объекты, у которых нет трансформации (например, камеры или света)
            if (obj.transform != null)
            {
                // Увеличиваем масштаб объекта на 5%
                obj.transform.localScale *= 1.05f;
            }
        }
    }
}

