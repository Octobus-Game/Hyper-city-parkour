using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PAROLL : MonoBehaviour
{
    // Правильный пароль
    [SerializeField] private string correctPassword = "1234";

    // Объект, который должен исчезнуть
    [SerializeField] private GameObject targetObject;

    // Текстовое поле для отображения введенного пароля
    [SerializeField] private Text passwordDisplay;

    // Введённый пользователем пароль
    private string enteredPassword = "";

    // Проверка пароля
    public void CheckPassword()
    {
        if (enteredPassword == correctPassword)
        {
            Debug.Log("Password is correct!");

            // Если объект назначен, делаем его невидимым
            if (targetObject != null)
            {
                targetObject.SetActive(false);
            }

            ClearPassword();
        }
        else
        {
            Debug.Log("Incorrect password. Try again.");
            ClearPassword();
        }
    }

    // Добавить символ в пароль
    public void AddDigit(string digit)
    {
        enteredPassword += digit;
        UpdatePasswordDisplay();
    }

    // Очистить введённый пароль
    public void ClearPassword()
    {
        enteredPassword = "";
        UpdatePasswordDisplay();
    }

    // Обновление текстового отображения пароля
    private void UpdatePasswordDisplay()
    {
        passwordDisplay.text = new string('*', enteredPassword.Length); // Показываем только звёздочки
    }
}

