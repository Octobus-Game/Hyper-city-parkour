using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HAPKU : MonoBehaviour
{
    // Перетаскиваем нужные объекты в эти поля в инспекторе
    public GameObject originalObject;      // Исходный объект
    public GameObject replacementObject;   // Объект замены
    public Button yourButton;              // Кнопка UI

    private void Start()
    {
        // Прячем объект замены при старте
        if (replacementObject != null)
        {
            replacementObject.SetActive(false);
        }

        // Подписываемся на событие нажатия кнопки
        if (yourButton != null)
        {
            yourButton.onClick.AddListener(OnButtonClick);
        }
    }

    // Метод, который вызывается при нажатии на кнопку
    void OnButtonClick()
    {
        // Проверяем, есть ли исходный объект
        if (originalObject != null)
        {
            // Прячем исходный объект
            originalObject.SetActive(false);
        }

        // Показываем объект замены
        if (replacementObject != null)
        {
            replacementObject.SetActive(true);
        }
    }
}
