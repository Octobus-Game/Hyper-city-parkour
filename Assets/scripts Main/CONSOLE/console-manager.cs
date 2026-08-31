using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;

public class GameConsole : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI commandResultText;
    [SerializeField] private Button executeButton;

    [Header("Script References")]
    [SerializeField] private MonoBehaviour mainScript;
    [SerializeField] private MonoBehaviour script;

    private void Start()
    {
        // Подписываемся на нажатие кнопки
        executeButton.onClick.AddListener(ExecuteCommand);

        // Подписываемся на Enter в InputField
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        // Очищаем результат
        commandResultText.text = "Готов к работе...";
    }

    private void OnInputEndEdit(string text)
    {
        // Выполняем команду при нажатии Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ExecuteCommand();
        }
    }

    public void ExecuteCommand()
    {
        string command = inputField.text.Trim();

        if (string.IsNullOrEmpty(command))
        {
            commandResultText.text = "Ошибка: команда пустая";
            return;
        }

        try
        {
            // Парсим команду
            string[] parts = command.Split(' ');
            string scriptName = parts[0].ToLower();
            string methodName = parts.Length > 1 ? parts[1] : "";

            MonoBehaviour targetScript = null;

            // Выбираем скрипт
            if (scriptName == "main")
            {
                targetScript = mainScript;
            }
            else if (scriptName == "script")
            {
                targetScript = script;
            }
            else
            {
                commandResultText.text = $"Ошибка: скрипт '{scriptName}' не найден. Доступно: main, script";
                return;
            }

            if (targetScript == null)
            {
                commandResultText.text = $"Ошибка: скрипт '{scriptName}' не назначен в инспекторе";
                return;
            }

            // Если указан только скрипт, показываем доступные методы
            if (string.IsNullOrEmpty(methodName))
            {
                ListMethods(targetScript);
                return;
            }

            // Получаем параметры (если есть)
            object[] parameters = null;
            if (parts.Length > 2)
            {
                parameters = new object[parts.Length - 2];
                for (int i = 2; i < parts.Length; i++)
                {
                    parameters[i - 2] = ParseParameter(parts[i]);
                }
            }

            // Выполняем метод
            ExecuteMethod(targetScript, methodName, parameters);
        }
        catch (Exception e)
        {
            commandResultText.text = $"Ошибка: {e.Message}";
        }

        // Очищаем поле ввода
        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void ExecuteMethod(MonoBehaviour targetScript, string methodName, object[] parameters)
    {
        Type scriptType = targetScript.GetType();
        MethodInfo method = null;

        // Ищем метод
        if (parameters == null || parameters.Length == 0)
        {
            method = scriptType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        }
        else
        {
            // Получаем все методы с нужным именем
            MethodInfo[] methods = scriptType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                if (m.Name == methodName)
                {
                    ParameterInfo[] methodParams = m.GetParameters();
                    if (methodParams.Length == parameters.Length)
                    {
                        method = m;
                        break;
                    }
                }
            }
        }

        if (method == null)
        {
            commandResultText.text = $"Ошибка: метод '{methodName}' не найден";
            return;
        }

        // Вызываем метод
        object result = method.Invoke(targetScript, parameters);

        // Показываем результат
        if (result != null)
        {
            commandResultText.text = $"Результат: {result}";
        }
        else
        {
            commandResultText.text = $"Команда '{methodName}' выполнена успешно";
        }
    }

    private void ListMethods(MonoBehaviour targetScript)
    {
        Type scriptType = targetScript.GetType();
        MethodInfo[] methods = scriptType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        string methodList = $"Доступные методы в {scriptType.Name}:\n";
        foreach (var method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            string paramStr = "";
            foreach (var param in parameters)
            {
                paramStr += $"{param.ParameterType.Name} {param.Name}, ";
            }
            if (paramStr.Length > 0)
                paramStr = paramStr.Substring(0, paramStr.Length - 2);

            methodList += $"- {method.Name}({paramStr})\n";
        }

        commandResultText.text = methodList;
    }

    private object ParseParameter(string param)
    {
        // Пробуем распарсить как int
        if (int.TryParse(param, out int intValue))
            return intValue;

        // Пробуем распарсить как float
        if (float.TryParse(param, out float floatValue))
            return floatValue;

        // Пробуем распарсить как bool
        if (bool.TryParse(param, out bool boolValue))
            return boolValue;

        // Иначе возвращаем как string
        return param;
    }
}