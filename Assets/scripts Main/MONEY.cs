using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Для работы с UI элементами
public class MONEY : MonoBehaviour
{
    public GameObject legacyTextObject; // Укажите объект с текстом (Legacy Text)

    private void OnCollisionEnter(Collision collision)
    {
        // Найдем компонент Text на объекте
        Text textComponent = legacyTextObject.GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.text = "1"; // Изменяем текст на "1"
        }
        else
        {
            Debug.LogWarning("Компонент Text не найден на объекте Legacy Text!");
        }
    }
}
