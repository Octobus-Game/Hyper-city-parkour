using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CLOSETAB : MonoBehaviour
{
    // Массив UI элементов, которые будут скрываться/показываться
    public GameObject[] uiElements;

    // Кнопка для переключения
    public Button toggleButton;

    // Текущее состояние видимости элементов
    private bool areElementsVisible = true;

    private void Start()
    {
        // Привязываем метод ToggleElements к событию нажатия кнопки
        toggleButton.onClick.AddListener(ToggleElements);
    }

    // Метод для переключения видимости элементов
    private void ToggleElements()
    {
        areElementsVisible = !areElementsVisible;

        foreach (GameObject element in uiElements)
        {
            element.SetActive(areElementsVisible);
        }
    }
}
