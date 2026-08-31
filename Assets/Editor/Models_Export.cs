using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;

public class AssetBundleExporter : EditorWindow
{
    private GameObject selectedPrefab;
    private Material selectedSkyboxMaterial;
    private GameObject selectedUIPrefab;
    private SceneAsset selectedScene;
    private string bundleName = "MyBundle";
    private BuildTarget buildTarget = BuildTarget.StandaloneWindows64;
    private string exportPath = "";

    private enum ExportType
    {
        Model,
        Skybox,
        UI,
        Scene
    }
    private ExportType exportType = ExportType.Model;

    [MenuItem("Tools/AssetBundle Exporter")]
    public static void ShowWindow()
    {
        GetWindow<AssetBundleExporter>("AssetBundle Exporter");
    }

    void OnEnable()
    {
        // Устанавливаем путь по умолчанию
        exportPath = Path.Combine(Application.dataPath, "../AssetBundles");
    }

    void OnGUI()
    {
        GUILayout.Label("AssetBundle Exporter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Выбор типа экспорта
        exportType = (ExportType)EditorGUILayout.EnumPopup("Тип экспорта:", exportType);
        GUILayout.Space(10);

        if (exportType == ExportType.Model)
        {
            DrawModelExportUI();
        }
        else if (exportType == ExportType.Skybox)
        {
            DrawSkyboxExportUI();
        }
        else if (exportType == ExportType.UI)
        {
            DrawUIExportUI();
        }
        else if (exportType == ExportType.Scene)
        {
            DrawSceneExportUI();
        }

        GUILayout.Space(20);

        // Общие настройки
        GUILayout.Label("Общие настройки:", EditorStyles.boldLabel);
        bundleName = EditorGUILayout.TextField("Имя Bundle:", bundleName);
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Платформа:", buildTarget);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Путь экспорта:", exportPath);
        if (GUILayout.Button("Выбрать", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Выберите папку для экспорта", exportPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                exportPath = path;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        // Кнопка экспорта
        GUI.enabled = CanExport();
        if (GUILayout.Button("Экспортировать AssetBundle", GUILayout.Height(40)))
        {
            if (exportType == ExportType.Model)
            {
                ExportModelBundle();
            }
            else if (exportType == ExportType.Skybox)
            {
                ExportSkyboxBundle();
            }
            else if (exportType == ExportType.UI)
            {
                ExportUIBundle();
            }
            else if (exportType == ExportType.Scene)
            {
                ExportSceneBundle();
            }
        }
        GUI.enabled = true;

        GUILayout.Space(10);

        // Инструкции
        EditorGUILayout.HelpBox(GetInstructions(), MessageType.Info);
    }

    void DrawModelExportUI()
    {
        GUILayout.Label("Экспорт модели/префаба:", EditorStyles.boldLabel);
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab/Модель:",
            selectedPrefab,
            typeof(GameObject),
            false
        );

        if (selectedPrefab != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Информация о модели:");
            EditorGUILayout.LabelField($"Имя: {selectedPrefab.name}");

            MeshFilter[] meshes = selectedPrefab.GetComponentsInChildren<MeshFilter>();
            EditorGUILayout.LabelField($"Количество мешей: {meshes.Length}");

            Renderer[] renderers = selectedPrefab.GetComponentsInChildren<Renderer>();
            EditorGUILayout.LabelField($"Количество рендереров: {renderers.Length}");
        }
    }

    void DrawSkyboxExportUI()
    {
        GUILayout.Label("Экспорт Skybox материала:", EditorStyles.boldLabel);
        selectedSkyboxMaterial = (Material)EditorGUILayout.ObjectField(
            "Skybox Material:",
            selectedSkyboxMaterial,
            typeof(Material),
            false
        );

        if (selectedSkyboxMaterial != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Информация о материале:");
            EditorGUILayout.LabelField($"Имя: {selectedSkyboxMaterial.name}");
            EditorGUILayout.LabelField($"Шейдер: {selectedSkyboxMaterial.shader.name}");

            if (!selectedSkyboxMaterial.shader.name.Contains("Skybox"))
            {
                EditorGUILayout.HelpBox("ВНИМАНИЕ: Выбранный материал не использует Skybox шейдер!", MessageType.Warning);
            }
        }
    }

    void DrawUIExportUI()
    {
        GUILayout.Label("Экспорт UI префаба:", EditorStyles.boldLabel);
        selectedUIPrefab = (GameObject)EditorGUILayout.ObjectField(
            "UI Prefab:",
            selectedUIPrefab,
            typeof(GameObject),
            false
        );

        if (selectedUIPrefab != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Информация о UI:");
            EditorGUILayout.LabelField($"Имя: {selectedUIPrefab.name}");

            Canvas canvas = selectedUIPrefab.GetComponent<Canvas>();
            Canvas[] canvases = selectedUIPrefab.GetComponentsInChildren<Canvas>();
            EditorGUILayout.LabelField($"Canvas компонентов: {canvases.Length}");

            Graphic[] graphics = selectedUIPrefab.GetComponentsInChildren<Graphic>();
            EditorGUILayout.LabelField($"UI элементов (Image/Text/etc): {graphics.Length}");

            Button[] buttons = selectedUIPrefab.GetComponentsInChildren<Button>();
            EditorGUILayout.LabelField($"Кнопок: {buttons.Length}");

            InputField[] inputFields = selectedUIPrefab.GetComponentsInChildren<InputField>();
            EditorGUILayout.LabelField($"Input Fields: {inputFields.Length}");

            if (canvas == null && canvases.Length == 0)
            {
                EditorGUILayout.HelpBox("ВНИМАНИЕ: Префаб не содержит Canvas компонента!", MessageType.Warning);
            }

            // Проверка на EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = selectedUIPrefab.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                EditorGUILayout.HelpBox("ИНФОРМАЦИЯ: Префаб содержит EventSystem. Убедитесь, что он не конфликтует с существующим в сцене.", MessageType.Info);
            }
        }
    }

    void DrawSceneExportUI()
    {
        GUILayout.Label("Экспорт Unity сцены:", EditorStyles.boldLabel);
        selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Scene:",
            selectedScene,
            typeof(SceneAsset),
            false
        );

        if (selectedScene != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Информация о сцене:");
            EditorGUILayout.LabelField($"Имя: {selectedScene.name}");

            string scenePath = AssetDatabase.GetAssetPath(selectedScene);
            EditorGUILayout.LabelField($"Путь: {scenePath}");

            // Попытка получить информацию о сцене без её загрузки
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorGUILayout.LabelField($"Корневых объектов: {scene.rootCount}");
            }
            else
            {
                EditorGUILayout.LabelField("Сцена не загружена (информация ограничена)");
            }

            EditorGUILayout.HelpBox(
                "ВАЖНО: Убедитесь, что все ассеты, используемые в сцене, " +
                "находятся в проекте и не являются внешними зависимостями.",
                MessageType.Warning
            );
        }
    }

    bool CanExport()
    {
        if (string.IsNullOrEmpty(bundleName))
            return false;

        if (exportType == ExportType.Model)
        {
            return selectedPrefab != null;
        }
        else if (exportType == ExportType.Skybox)
        {
            return selectedSkyboxMaterial != null;
        }
        else if (exportType == ExportType.UI)
        {
            return selectedUIPrefab != null;
        }
        else if (exportType == ExportType.Scene)
        {
            return selectedScene != null;
        }

        return false;
    }

    string GetInstructions()
    {
        if (exportType == ExportType.Model)
        {
            return "ИНСТРУКЦИЯ:\n" +
                   "1. Перетащите ваш Prefab или FBX модель в поле 'Prefab/Модель'\n" +
                   "2. Укажите имя для bundle файла\n" +
                   "3. Выберите целевую платформу\n" +
                   "4. Нажмите 'Экспортировать AssetBundle'\n" +
                   "5. Скопируйте созданный .bundle файл в папку BepInEx/plugins/SkyboxManager/Models/";
        }
        else if (exportType == ExportType.Skybox)
        {
            return "ИНСТРУКЦИЯ:\n" +
                   "1. Создайте материал с Skybox шейдером (Shader -> Skybox -> ...)\n" +
                   "2. Перетащите материал в поле 'Skybox Material'\n" +
                   "3. Укажите имя для bundle файла\n" +
                   "4. Выберите целевую платформу\n" +
                   "5. Нажмите 'Экспортировать AssetBundle'\n" +
                   "6. Скопируйте созданный .bundle файл в папку BepInEx/plugins/SkyboxManager/Skyboxes/";
        }
        else if (exportType == ExportType.UI)
        {
            return "ИНСТРУКЦИЯ:\n" +
                   "1. Создайте UI префаб с Canvas (GameObject -> UI -> Canvas)\n" +
                   "2. Добавьте необходимые UI элементы (Button, Text, Image и т.д.)\n" +
                   "3. Перетащите префаб в поле 'UI Prefab'\n" +
                   "4. Укажите имя для bundle файла\n" +
                   "5. Выберите целевую платформу\n" +
                   "6. Нажмите 'Экспортировать AssetBundle'\n" +
                   "7. Скопируйте созданный .bundle файл в папку BepInEx/plugins/SkyboxManager/UI/";
        }
        else if (exportType == ExportType.Scene)
        {
            return "ИНСТРУКЦИЯ:\n" +
                   "1. Создайте или выберите существующую Unity сцену (.unity)\n" +
                   "2. Убедитесь, что сцена добавлена в Build Settings (File -> Build Settings)\n" +
                   "3. Перетащите сцену в поле 'Scene'\n" +
                   "4. Укажите имя для bundle файла\n" +
                   "5. Выберите целевую платформу\n" +
                   "6. Нажмите 'Экспортировать AssetBundle'\n" +
                   "7. Скопируйте созданный .bundle файл в папку BepInEx/plugins/SkyboxManager/Scenes/";
        }

        return "";
    }

    void ExportModelBundle()
    {
        ExportGenericBundle(selectedPrefab, "Models");
    }

    void ExportSkyboxBundle()
    {
        ExportGenericBundle(selectedSkyboxMaterial, "Skyboxes");
    }

    void ExportUIBundle()
    {
        ExportGenericBundle(selectedUIPrefab, "UI");
    }

    void ExportSceneBundle()
    {
        ExportSceneAssetBundle(selectedScene, "Scenes");
    }

    void ExportGenericBundle(Object asset, string targetFolder)
    {
        try
        {
            // Создаем папку если не существует
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            // Получаем путь к ассету
            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Ошибка", "Не удалось получить путь к ассету!", "OK");
                return;
            }

            // Устанавливаем AssetBundle name
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            string tempBundleName = $"temp_{targetFolder.ToLower()}_bundle";
            importer.assetBundleName = tempBundleName;

            // Создаем AssetBundle
            string outputPath = Path.Combine(exportPath, bundleName + ".bundle");

            BuildPipeline.BuildAssetBundles(
                exportPath,
                BuildAssetBundleOptions.None,
                buildTarget
            );

            // Переименовываем bundle
            string tempBundlePath = Path.Combine(exportPath, tempBundleName);
            if (File.Exists(tempBundlePath))
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.Move(tempBundlePath, outputPath);
            }

            // Очищаем AssetBundle name
            importer.assetBundleName = "";

            // Удаляем временные файлы
            CleanupTempFiles(exportPath, tempBundleName);

            EditorUtility.DisplayDialog(
                "Успех!",
                $"AssetBundle успешно создан!\n\nПуть: {outputPath}\n\nТеперь скопируйте файл {bundleName}.bundle в папку:\nBepInEx/plugins/SkyboxManager/{targetFolder}/",
                "OK"
            );

            // Открываем папку с экспортом
            EditorUtility.RevealInFinder(outputPath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось создать AssetBundle:\n{e.Message}", "OK");
            Debug.LogError($"AssetBundle Export Error: {e.Message}\n{e.StackTrace}");
        }
    }

    void ExportSceneAssetBundle(SceneAsset scene, string targetFolder)
    {
        try
        {
            // Создаем папку если не существует
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            // Получаем путь к сцене
            string scenePath = AssetDatabase.GetAssetPath(scene);

            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Ошибка", "Не удалось получить путь к сцене!", "OK");
                return;
            }

            // Устанавливаем AssetBundle name для сцены
            AssetImporter importer = AssetImporter.GetAtPath(scenePath);
            string tempBundleName = $"temp_{targetFolder.ToLower()}_bundle";
            importer.assetBundleName = tempBundleName;

            // Создаем массив для билда
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = tempBundleName;
            buildMap[0].assetNames = new string[] { scenePath };

            // Создаем AssetBundle
            string outputPath = Path.Combine(exportPath, bundleName + ".bundle");

            BuildPipeline.BuildAssetBundles(
                exportPath,
                buildMap,
                BuildAssetBundleOptions.None,
                buildTarget
            );

            // Переименовываем bundle
            string tempBundlePath = Path.Combine(exportPath, tempBundleName);
            if (File.Exists(tempBundlePath))
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.Move(tempBundlePath, outputPath);
            }

            // Очищаем AssetBundle name
            importer.assetBundleName = "";

            // Удаляем временные файлы
            CleanupTempFiles(exportPath, tempBundleName);

            EditorUtility.DisplayDialog(
                "Успех!",
                $"AssetBundle сцены успешно создан!\n\nПуть: {outputPath}\n\nТеперь скопируйте файл {bundleName}.bundle в папку:\nBepInEx/plugins/SkyboxManager/{targetFolder}/\n\nДля загрузки сцены используйте:\nSceneManager.LoadScene(\"{scene.name}\", LoadSceneMode.Additive);",
                "OK"
            );

            // Открываем папку с экспортом
            EditorUtility.RevealInFinder(outputPath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось создать AssetBundle сцены:\n{e.Message}", "OK");
            Debug.LogError($"Scene AssetBundle Export Error: {e.Message}\n{e.StackTrace}");
        }
    }

    void CleanupTempFiles(string directory, string bundleName)
    {
        // Удаляем .manifest файлы
        string manifestPath = Path.Combine(directory, bundleName + ".manifest");
        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }

        string mainManifest = Path.Combine(directory, "AssetBundles");
        if (File.Exists(mainManifest))
        {
            File.Delete(mainManifest);
        }

        string mainManifestMeta = mainManifest + ".manifest";
        if (File.Exists(mainManifestMeta))
        {
            File.Delete(mainManifestMeta);
        }
    }
}

// Альтернативный метод - быстрый экспорт через контекстное меню
public class QuickAssetBundleExporter
{
    [MenuItem("Assets/Export to AssetBundle (Model)", false, 2000)]
    private static void ExportModelToAssetBundle()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите GameObject для экспорта!", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanel(
            "Сохранить AssetBundle",
            Application.dataPath,
            selectedObject.name + ".bundle",
            "bundle"
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        QuickExport(selectedObject, savePath);
    }

    [MenuItem("Assets/Export to AssetBundle (Material)", false, 2001)]
    private static void ExportMaterialToAssetBundle()
    {
        Material selectedMaterial = Selection.activeObject as Material;

        if (selectedMaterial == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите Material для экспорта!", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanel(
            "Сохранить AssetBundle",
            Application.dataPath,
            selectedMaterial.name + ".bundle",
            "bundle"
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        QuickExport(selectedMaterial, savePath);
    }

    [MenuItem("Assets/Export to AssetBundle (UI)", false, 2002)]
    private static void ExportUIToAssetBundle()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите UI GameObject для экспорта!", "OK");
            return;
        }

        // Проверка на наличие UI компонентов
        Canvas canvas = selectedObject.GetComponentInChildren<Canvas>();
        Graphic[] graphics = selectedObject.GetComponentsInChildren<Graphic>();

        if (canvas == null && graphics.Length == 0)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Предупреждение",
                "Выбранный объект не содержит Canvas или UI элементов. Продолжить экспорт?",
                "Да",
                "Нет"
            );

            if (!proceed)
                return;
        }

        string savePath = EditorUtility.SaveFilePanel(
            "Сохранить AssetBundle",
            Application.dataPath,
            selectedObject.name + ".bundle",
            "bundle"
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        QuickExport(selectedObject, savePath);
    }

    [MenuItem("Assets/Export to AssetBundle (Scene)", false, 2003)]
    private static void ExportSceneToAssetBundle()
    {
        SceneAsset selectedScene = Selection.activeObject as SceneAsset;

        if (selectedScene == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите Scene для экспорта!", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanel(
            "Сохранить AssetBundle",
            Application.dataPath,
            selectedScene.name + ".bundle",
            "bundle"
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        QuickExportScene(selectedScene, savePath);
    }

    private static void QuickExport(Object asset, string savePath)
    {
        try
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            string tempBundleName = "temp_quick_bundle";
            importer.assetBundleName = tempBundleName;

            string directory = Path.GetDirectoryName(savePath);

            BuildPipeline.BuildAssetBundles(
                directory,
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64
            );

            string tempBundlePath = Path.Combine(directory, tempBundleName);
            if (File.Exists(tempBundlePath))
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                File.Move(tempBundlePath, savePath);
            }

            importer.assetBundleName = "";

            // Cleanup
            string manifestPath = Path.Combine(directory, tempBundleName + ".manifest");
            if (File.Exists(manifestPath)) File.Delete(manifestPath);

            EditorUtility.DisplayDialog("Успех!", $"AssetBundle создан:\n{savePath}", "OK");
            EditorUtility.RevealInFinder(savePath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось создать AssetBundle:\n{e.Message}", "OK");
        }
    }

    private static void QuickExportScene(SceneAsset scene, string savePath)
    {
        try
        {
            string scenePath = AssetDatabase.GetAssetPath(scene);
            AssetImporter importer = AssetImporter.GetAtPath(scenePath);

            string tempBundleName = "temp_quick_scene_bundle";
            importer.assetBundleName = tempBundleName;

            string directory = Path.GetDirectoryName(savePath);

            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = tempBundleName;
            buildMap[0].assetNames = new string[] { scenePath };

            BuildPipeline.BuildAssetBundles(
                directory,
                buildMap,
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows64
            );

            string tempBundlePath = Path.Combine(directory, tempBundleName);
            if (File.Exists(tempBundlePath))
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
                File.Move(tempBundlePath, savePath);
            }

            importer.assetBundleName = "";

            // Cleanup
            string manifestPath = Path.Combine(directory, tempBundleName + ".manifest");
            if (File.Exists(manifestPath)) File.Delete(manifestPath);

            EditorUtility.DisplayDialog("Успех!", $"Scene AssetBundle создан:\n{savePath}", "OK");
            EditorUtility.RevealInFinder(savePath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось создать Scene AssetBundle:\n{e.Message}", "OK");
        }
    }

    [MenuItem("Assets/Export to AssetBundle (Model)", true)]
    private static bool ValidateExportModel()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem("Assets/Export to AssetBundle (Material)", true)]
    private static bool ValidateExportMaterial()
    {
        return Selection.activeObject is Material;
    }

    [MenuItem("Assets/Export to AssetBundle (UI)", true)]
    private static bool ValidateExportUI()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null) return false;

        // Проверяем, есть ли UI компоненты
        Canvas canvas = selectedObject.GetComponentInChildren<Canvas>();
        Graphic[] graphics = selectedObject.GetComponentsInChildren<Graphic>();

        return canvas != null || graphics.Length > 0;
    }

    [MenuItem("Assets/Export to AssetBundle (Scene)", true)]
    private static bool ValidateExportScene()
    {
        return Selection.activeObject is SceneAsset;
    }
}