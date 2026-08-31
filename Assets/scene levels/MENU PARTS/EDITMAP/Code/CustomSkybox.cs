using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB; // StandaloneFileBrowser

public class AdvancedSkyboxChanger : MonoBehaviour
{
    [Header("UI Elements")]
    public Button selectImageButton;
    public Text statusText;

    [Header("Skybox Settings")]
    public Material skyboxMaterial;
    public SkyboxType skyboxType = SkyboxType.Panoramic;

    [Header("Preview")]
    public RawImage previewImage;

    [Header("Skybox History")]
    public Transform historyPanel;
    public GameObject historyButtonPrefab;
    public int maxHistoryCount = 10;

    public enum SkyboxType
    {
        Panoramic,
        SixSided,
        Cubemap
    }

    [System.Serializable]
    public class SkyboxHistoryItem
    {
        public string fileName;
        public string filePath;
        public Texture2D texture;
        public Material material;
        public SkyboxType type;

        public SkyboxHistoryItem(string name, string path, Texture2D tex, Material mat, SkyboxType skyboxType)
        {
            fileName = name;
            filePath = path;
            texture = tex;
            material = mat;
            type = skyboxType;
        }
    }

    private List<SkyboxHistoryItem> skyboxHistory = new List<SkyboxHistoryItem>();
    private List<Button> historyButtons = new List<Button>();

    private void Start()
    {
        if (selectImageButton != null)
        {
            selectImageButton.onClick.AddListener(OpenFileDialog);
        }

        // Создаем skybox материал в зависимости от типа
        CreateSkyboxMaterial();

        // Инициализируем панель истории
        InitializeHistoryPanel();

        UpdateStatusText("Нажмите кнопку для выбора изображения");
    }

    private void CreateSkyboxMaterial()
    {
        if (skyboxMaterial == null)
        {
            string shaderName = skyboxType switch
            {
                SkyboxType.Panoramic => "Skybox/Panoramic",
                SkyboxType.SixSided => "Skybox/6 Sided",
                SkyboxType.Cubemap => "Skybox/Cubemap",
                _ => "Skybox/Panoramic"
            };

            skyboxMaterial = new Material(Shader.Find(shaderName));
        }
    }

    private void OpenFileDialog()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp", "tga"),
            new ExtensionFilter("PNG", "png"),
            new ExtensionFilter("JPG", "jpg", "jpeg"),
            new ExtensionFilter("BMP", "bmp"),
            new ExtensionFilter("TGA", "tga")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Выберите изображение для skybox", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            UpdateStatusText("Загрузка изображения...");
            StartCoroutine(LoadImageAndApplySkybox(paths[0]));
        }
    }

    private IEnumerator LoadImageAndApplySkybox(string path)
    {
        byte[] fileData;

        try
        {
            fileData = File.ReadAllBytes(path);
        }
        catch (System.Exception e)
        {
            UpdateStatusText($"Ошибка чтения файла: {e.Message}");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        bool loaded = texture.LoadImage(fileData);

        if (loaded)
        {
            // Проверяем, не дублируется ли новый skybox перед сменой
            if (!IsSkyboxDuplicate(texture))
            {
                // Сохраняем текущий skybox в историю перед сменой
                SaveCurrentSkyboxToHistory();
            }

            // Показываем превью
            if (previewImage != null)
            {
                previewImage.texture = texture;
            }

            ApplyTextureToSkybox(texture, Path.GetFileName(path));
            UpdateStatusText($"Skybox обновлен: {Path.GetFileName(path)}");
        }
        else
        {
            UpdateStatusText("Не удалось загрузить изображение");
            Destroy(texture);
        }

        yield return null;
    }

    private void ApplyTextureToSkybox(Texture2D texture, string fileName = "")
    {
        switch (skyboxType)
        {
            case SkyboxType.Panoramic:
                skyboxMaterial.SetTexture("_MainTex", texture);
                break;

            case SkyboxType.SixSided:
                // Применяем ко всем сторонам
                skyboxMaterial.SetTexture("_FrontTex", texture);
                skyboxMaterial.SetTexture("_BackTex", texture);
                skyboxMaterial.SetTexture("_LeftTex", texture);
                skyboxMaterial.SetTexture("_RightTex", texture);
                skyboxMaterial.SetTexture("_UpTex", texture);
                skyboxMaterial.SetTexture("_DownTex", texture);
                break;

            case SkyboxType.Cubemap:
                // Для кубемапа нужно создать кубическую текстуру
                Cubemap cubemap = CreateCubemapFromTexture(texture);
                skyboxMaterial.SetTexture("_Tex", cubemap);
                break;
        }

        // Устанавливаем материал как skybox
        RenderSettings.skybox = skyboxMaterial;

        // Обновляем освещение
        DynamicGI.UpdateEnvironment();
    }

    private Cubemap CreateCubemapFromTexture(Texture2D source)
    {
        Cubemap cubemap = new Cubemap(source.width, source.format, false);

        // Простое решение - используем одну текстуру для всех сторон
        Color[] pixels = source.GetPixels();

        cubemap.SetPixels(pixels, CubemapFace.PositiveX);
        cubemap.SetPixels(pixels, CubemapFace.NegativeX);
        cubemap.SetPixels(pixels, CubemapFace.PositiveY);
        cubemap.SetPixels(pixels, CubemapFace.NegativeY);
        cubemap.SetPixels(pixels, CubemapFace.PositiveZ);
        cubemap.SetPixels(pixels, CubemapFace.NegativeZ);

        cubemap.Apply();
        return cubemap;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }

    // Метод для изменения типа skybox из UI
    public void ChangeSkyboxType(int typeIndex)
    {
        skyboxType = (SkyboxType)typeIndex;
        CreateSkyboxMaterial();
        UpdateStatusText($"Тип skybox изменен на: {skyboxType}");
    }

    // Инициализация панели истории
    private void InitializeHistoryPanel()
    {
        if (historyPanel == null)
        {
            Debug.LogWarning("History Panel не назначен!");
            return;
        }

        if (historyButtonPrefab == null)
        {
            Debug.LogWarning("History Button Prefab не назначен!");
            return;
        }
    }

    // Сохранение текущего skybox в историю
    private void SaveCurrentSkyboxToHistory()
    {
        if (skyboxMaterial == null) return;

        Texture2D currentTexture = GetCurrentSkyboxTexture();
        if (currentTexture == null) return;

        // Создаем копию материала для истории
        Material historyMaterial = new Material(skyboxMaterial);

        // Создаем элемент истории
        string fileName = currentTexture.name != "" ? currentTexture.name : $"Skybox_{skyboxHistory.Count + 1}";
        SkyboxHistoryItem historyItem = new SkyboxHistoryItem(
            fileName,
            "", // путь неизвестен для текущего skybox
            currentTexture,
            historyMaterial,
            skyboxType
        );

        // Добавляем в историю
        skyboxHistory.Add(historyItem);

        // Создаем UI кнопку для истории
        CreateHistoryButton(historyItem);

        // Ограничиваем размер истории
        if (skyboxHistory.Count > maxHistoryCount)
        {
            RemoveOldestHistoryItem();
        }
    }

    // Получение текущей текстуры skybox
    private Texture2D GetCurrentSkyboxTexture()
    {
        if (skyboxMaterial == null) return null;

        switch (skyboxType)
        {
            case SkyboxType.Panoramic:
                return skyboxMaterial.GetTexture("_MainTex") as Texture2D;
            case SkyboxType.SixSided:
                return skyboxMaterial.GetTexture("_FrontTex") as Texture2D;
            case SkyboxType.Cubemap:
                // Для кубемапа сложнее получить исходную текстуру
                return null;
            default:
                return null;
        }
    }

    // Создание UI кнопки для элемента истории
    private void CreateHistoryButton(SkyboxHistoryItem historyItem)
    {
        if (historyPanel == null || historyButtonPrefab == null) return;

        GameObject buttonObj = Instantiate(historyButtonPrefab, historyPanel);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // Настраиваем кнопку
            button.onClick.AddListener(() => LoadSkyboxFromHistory(historyItem));

            // Устанавливаем иконку (если есть RawImage в prefab)
            RawImage icon = buttonObj.GetComponentInChildren<RawImage>();
            if (icon != null && historyItem.texture != null)
            {
                icon.texture = historyItem.texture;
            }

            // Устанавливаем текст (если есть Text в prefab)
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = historyItem.fileName;
            }

            // Добавляем tooltip или другую информацию
            buttonObj.name = $"HistoryButton_{historyItem.fileName}";

            historyButtons.Add(button);
        }
    }

    // Загрузка skybox из истории
    private void LoadSkyboxFromHistory(SkyboxHistoryItem historyItem)
    {
        if (historyItem == null || historyItem.material == null) return;

        // Сохраняем текущий skybox перед загрузкой из истории
        SaveCurrentSkyboxToHistory();

        // Применяем материал из истории
        skyboxMaterial = new Material(historyItem.material);
        skyboxType = historyItem.type;

        // Устанавливаем как текущий skybox
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment();

        // Обновляем превью
        if (previewImage != null && historyItem.texture != null)
        {
            previewImage.texture = historyItem.texture;
        }

        UpdateStatusText($"Загружен из истории: {historyItem.fileName}");
    }

    // Удаление самого старого элемента истории
    private void RemoveOldestHistoryItem()
    {
        if (skyboxHistory.Count == 0) return;

        // Удаляем первый элемент (самый старый)
        SkyboxHistoryItem oldestItem = skyboxHistory[0];
        skyboxHistory.RemoveAt(0);

        // Удаляем соответствующую кнопку
        if (historyButtons.Count > 0)
        {
            Button oldestButton = historyButtons[0];
            historyButtons.RemoveAt(0);

            if (oldestButton != null)
            {
                DestroyImmediate(oldestButton.gameObject);
            }
        }

        // Освобождаем память
        if (oldestItem.material != null)
        {
            DestroyImmediate(oldestItem.material);
        }
    }

    // Очистка всей истории
    public void ClearHistory()
    {
        // Удаляем все кнопки
        foreach (Button button in historyButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        historyButtons.Clear();

        // Освобождаем материалы
        foreach (SkyboxHistoryItem item in skyboxHistory)
        {
            if (item.material != null)
            {
                DestroyImmediate(item.material);
            }
        }
        skyboxHistory.Clear();

        UpdateStatusText("История skybox очищена");
    }

    // Получение количества элементов в истории
    public int GetHistoryCount()
    {
        return skyboxHistory.Count;
    }

    // Проверка на дублирование skybox
    private bool IsSkyboxDuplicate(Texture2D currentTexture)
    {
        if (currentTexture == null) return false;

        foreach (SkyboxHistoryItem item in skyboxHistory)
        {
            if (item.texture == null) continue;

            // Сравниваем по названию текстуры и типу skybox
            if (item.texture.name == currentTexture.name && item.type == skyboxType)
            {
                return true;
            }

            // Дополнительная проверка по хешу пикселей (более надежно)
            if (AreTexturesIdentical(item.texture, currentTexture))
            {
                return true;
            }
        }

        return false;
    }

    // Сравнение текстур по содержимому (пикселям)
    private bool AreTexturesIdentical(Texture2D texture1, Texture2D texture2)
    {
        if (texture1 == null || texture2 == null) return false;
        if (texture1 == texture2) return true; // Та же ссылка

        // Быстрая проверка по размерам
        if (texture1.width != texture2.width || texture1.height != texture2.height)
        {
            return false;
        }

        // Проверка по формату
        if (texture1.format != texture2.format)
        {
            return false;
        }

        try
        {
            // Сравниваем хеши пикселей для оптимизации
            Color32[] pixels1 = texture1.GetPixels32();
            Color32[] pixels2 = texture2.GetPixels32();

            if (pixels1.Length != pixels2.Length) return false;

            // Сравниваем хеш-коды массивов пикселей
            int hash1 = GetPixelArrayHash(pixels1);
            int hash2 = GetPixelArrayHash(pixels2);

            return hash1 == hash2;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Не удалось сравнить текстуры: {e.Message}");
            // Fallback - сравниваем по названию
            return texture1.name == texture2.name;
        }
    }

    // Вычисление хеша для массива пикселей
    private int GetPixelArrayHash(Color32[] pixels)
    {
        if (pixels == null || pixels.Length == 0) return 0;

        unchecked
        {
            int hash = 17;
            // Берем каждый 100-й пиксель для оптимизации
            int step = Mathf.Max(1, pixels.Length / 1000);

            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                hash = hash * 31 + pixel.r;
                hash = hash * 31 + pixel.g;
                hash = hash * 31 + pixel.b;
                hash = hash * 31 + pixel.a;
            }

            return hash;
        }
    }

    // Поиск и удаление дубликата из истории (если нужно)
    public void RemoveDuplicatesFromHistory()
    {
        List<SkyboxHistoryItem> uniqueItems = new List<SkyboxHistoryItem>();
        List<Button> uniqueButtons = new List<Button>();

        for (int i = 0; i < skyboxHistory.Count; i++)
        {
            SkyboxHistoryItem currentItem = skyboxHistory[i];
            bool isDuplicate = false;

            // Проверяем среди уже отобранных уникальных элементов
            foreach (SkyboxHistoryItem uniqueItem in uniqueItems)
            {
                if (AreTexturesIdentical(currentItem.texture, uniqueItem.texture) &&
                    currentItem.type == uniqueItem.type)
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                uniqueItems.Add(currentItem);
                if (i < historyButtons.Count)
                {
                    uniqueButtons.Add(historyButtons[i]);
                }
            }
            else
            {
                // Удаляем дубликат
                if (i < historyButtons.Count && historyButtons[i] != null)
                {
                    DestroyImmediate(historyButtons[i].gameObject);
                }
                if (currentItem.material != null)
                {
                    DestroyImmediate(currentItem.material);
                }
            }
        }

        // Обновляем списки
        skyboxHistory = uniqueItems;
        historyButtons = uniqueButtons;

        UpdateStatusText($"Удалено дубликатов: {skyboxHistory.Count} уникальных элементов осталось");
    }
}