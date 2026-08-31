using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class ScriptSaver : MonoBehaviour
{
    [Header("UI Elements")]
    public InputField scriptInputField;
    public Button saveButton;
    public Button loadButton;
    public Button clearButton;
    public Dropdown savedScriptsDropdown;

    [Header("Save Settings")]
    public string saveFolder = "SavedScripts";
    public string defaultFileName = "NewScript";
    public InputField fileNameInputField;

    [Header("Feedback")]
    public Text statusText;
    public float statusDisplayTime = 2f;

    private string savePath;
    private List<string> savedScripts = new List<string>();

    void Start()
    {
        InitializeComponents();
        CreateSaveDirectory();
        LoadScriptsList();
        SetDefaultScript();
    }

    void InitializeComponents()
    {
        // Настройка кнопок
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveScript);

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadSelectedScript);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearInputField);

        // Настройка dropdown
        if (savedScriptsDropdown != null)
            savedScriptsDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // Установка пути сохранения
        savePath = Path.Combine(Application.persistentDataPath, saveFolder);
    }

    void CreateSaveDirectory()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            ShowStatus("Папка для сохранения создана: " + savePath);
        }
    }

    void SetDefaultScript()
    {
        if (scriptInputField != null)
        {
            scriptInputField.text = @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewScript : MonoBehaviour
{
    void Start()
    {
        Debug.Log(""Hello World!"");
    }
    
    void Update()
    {
        
    }
}";
        }
    }

    public void SaveScript()
    {
        if (scriptInputField == null)
        {
            ShowStatus("Input Field не найден!", true);
            return;
        }

        if (string.IsNullOrEmpty(scriptInputField.text))
        {
            ShowStatus("Нечего сохранять - поле пустое!", true);
            return;
        }

        string fileName = GetFileName();
        string fullPath = Path.Combine(savePath, fileName + ".cs");

        try
        {
            // Сохранение файла
            File.WriteAllText(fullPath, scriptInputField.text);

            // Обновление списка сохраненных скриптов
            LoadScriptsList();

            ShowStatus($"Скрипт сохранен: {fileName}.cs");

            Debug.Log($"Скрипт сохранен по пути: {fullPath}");
        }
        catch (Exception e)
        {
            ShowStatus($"Ошибка сохранения: {e.Message}", true);
            Debug.LogError($"Ошибка сохранения скрипта: {e.Message}");
        }
    }

    public void LoadSelectedScript()
    {
        if (savedScriptsDropdown == null || savedScriptsDropdown.options.Count == 0)
        {
            ShowStatus("Нет сохраненных скриптов для загрузки!", true);
            return;
        }

        string selectedScript = savedScriptsDropdown.options[savedScriptsDropdown.value].text;
        LoadScript(selectedScript);
    }

    public void LoadScript(string scriptName)
    {
        string fullPath = Path.Combine(savePath, scriptName + ".cs");

        if (!File.Exists(fullPath))
        {
            ShowStatus($"Файл не найден: {scriptName}.cs", true);
            return;
        }

        try
        {
            string scriptContent = File.ReadAllText(fullPath);

            if (scriptInputField != null)
            {
                scriptInputField.text = scriptContent;
                ShowStatus($"Скрипт загружен: {scriptName}.cs");
            }
        }
        catch (Exception e)
        {
            ShowStatus($"Ошибка загрузки: {e.Message}", true);
            Debug.LogError($"Ошибка загрузки скрипта: {e.Message}");
        }
    }

    public void ClearInputField()
    {
        if (scriptInputField != null)
        {
            scriptInputField.text = "";
            ShowStatus("Поле очищено");
        }
    }

    public void DeleteSelectedScript()
    {
        if (savedScriptsDropdown == null || savedScriptsDropdown.options.Count == 0)
        {
            ShowStatus("Нет скриптов для удаления!", true);
            return;
        }

        string selectedScript = savedScriptsDropdown.options[savedScriptsDropdown.value].text;
        string fullPath = Path.Combine(savePath, selectedScript + ".cs");

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                LoadScriptsList();
                ShowStatus($"Скрипт удален: {selectedScript}.cs");
            }
        }
        catch (Exception e)
        {
            ShowStatus($"Ошибка удаления: {e.Message}", true);
        }
    }

    void LoadScriptsList()
    {
        savedScripts.Clear();

        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "*.cs");

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                savedScripts.Add(fileName);
            }
        }

        UpdateDropdown();
    }

    void UpdateDropdown()
    {
        if (savedScriptsDropdown != null)
        {
            savedScriptsDropdown.ClearOptions();

            if (savedScripts.Count > 0)
            {
                savedScriptsDropdown.AddOptions(savedScripts);
            }
            else
            {
                savedScriptsDropdown.AddOptions(new List<string> { "Нет сохраненных скриптов" });
            }
        }
    }

    void OnDropdownValueChanged(int index)
    {
        // Можно добавить предварительный просмотр или другую логику
    }

    string GetFileName()
    {
        if (fileNameInputField != null && !string.IsNullOrEmpty(fileNameInputField.text))
        {
            return SanitizeFileName(fileNameInputField.text);
        }

        return defaultFileName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    string SanitizeFileName(string fileName)
    {
        // Удаление недопустимых символов из имени файла
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }

    void ShowStatus(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;

            // Автоматическое скрытие статуса
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(HideStatusAfterDelay());
            }
        }

        Debug.Log(message);
    }

    IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSeconds(statusDisplayTime);

        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    // Дополнительные методы для удобства
    public void SaveScriptWithCustomName(string customName)
    {
        if (fileNameInputField != null)
        {
            string originalName = fileNameInputField.text;
            fileNameInputField.text = customName;
            SaveScript();
            fileNameInputField.text = originalName;
        }
    }

    public string GetCurrentScript()
    {
        return scriptInputField != null ? scriptInputField.text : "";
    }

    public void SetScript(string scriptContent)
    {
        if (scriptInputField != null)
        {
            scriptInputField.text = scriptContent;
        }
    }

    public List<string> GetSavedScriptsList()
    {
        return new List<string>(savedScripts);
    }

    void OnDestroy()
    {
        // Очистка слушателей событий
        if (saveButton != null)
            saveButton.onClick.RemoveListener(SaveScript);

        if (loadButton != null)
            loadButton.onClick.RemoveListener(LoadSelectedScript);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearInputField);

        if (savedScriptsDropdown != null)
            savedScriptsDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }
}