using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class SeparateWindowCreator : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button createWindowButton;
    [SerializeField] private GameObject panelToMove;

    [Header("Window Settings")]
    [SerializeField] private string windowTitle = "External Panel";
    [SerializeField] private int windowWidth = 800;
    [SerializeField] private int windowHeight = 600;

    // Windows API функции
    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y,
        int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool UpdateWindow(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Константы для создания окна
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    private const uint WS_VISIBLE = 0x10000000;
    private const int SW_SHOW = 5;

    private IntPtr windowHandle;
    private Camera separateCamera;
    private Canvas separateCanvas;
    private bool windowCreated = false;

    void Start()
    {
        if (createWindowButton != null)
        {
            createWindowButton.onClick.AddListener(CreateSeparateWindow);
        }
        else
        {
            Debug.LogError("Create Window Button не назначена!");
        }

        if (panelToMove == null)
        {
            Debug.LogError("Panel To Move не назначена!");
        }
    }

    void CreateSeparateWindow()
    {
        if (windowCreated)
        {
            Debug.Log("Окно уже создано!");
            return;
        }

        try
        {
            // Создаем новое окно Windows
            CreateExternalWindow();

            // Создаем отдельную камеру для нового окна
            CreateSeparateCamera();

            // Перемещаем панель в новое окно
            MovePanelToSeparateWindow();

            windowCreated = true;
            Debug.Log("Отдельное окно успешно создано!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при создании окна: {e.Message}");
        }
    }

    void CreateExternalWindow()
    {
        // Получаем handle модуля
        IntPtr hInstance = GetModuleHandle(null);

        // Создаем окно
        windowHandle = CreateWindowEx(
            0,
            "STATIC",
            windowTitle,
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            100, 100, // позиция окна
            windowWidth, windowHeight,
            IntPtr.Zero,
            IntPtr.Zero,
            hInstance,
            IntPtr.Zero);

        if (windowHandle == IntPtr.Zero)
        {
            throw new Exception("Не удалось создать окно");
        }

        // Показываем окно
        ShowWindow(windowHandle, SW_SHOW);
        UpdateWindow(windowHandle);
    }

    void CreateSeparateCamera()
    {
        // Создаем новый GameObject для камеры
        GameObject cameraObject = new GameObject("SeparateWindowCamera");
        separateCamera = cameraObject.AddComponent<Camera>();

        // Настраиваем камеру
        separateCamera.clearFlags = CameraClearFlags.SolidColor;
        separateCamera.backgroundColor = Color.gray;
        separateCamera.orthographic = true;
        separateCamera.orthographicSize = 5;

        // Устанавливаем разрешение камеры
        RenderTexture renderTexture = new RenderTexture(windowWidth, windowHeight, 24);
        separateCamera.targetTexture = renderTexture;

        // Создаем Canvas для UI элементов
        CreateSeparateCanvas();
    }

    void CreateSeparateCanvas()
    {
        // Создаем новый Canvas
        GameObject canvasObject = new GameObject("SeparateWindowCanvas");
        separateCanvas = canvasObject.AddComponent<Canvas>();

        // Настраиваем Canvas
        separateCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        separateCanvas.worldCamera = separateCamera;
        separateCanvas.sortingOrder = 1;

        // Добавляем CanvasScaler
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(windowWidth, windowHeight);

        // Добавляем GraphicRaycaster для взаимодействия
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    void MovePanelToSeparateWindow()
    {
        if (panelToMove != null && separateCanvas != null)
        {
            // Сохраняем исходную позицию панели
            Vector3 originalPosition = panelToMove.transform.position;
            Vector3 originalScale = panelToMove.transform.localScale;

            // Перемещаем панель к новому Canvas
            panelToMove.transform.SetParent(separateCanvas.transform, false);

            // Настраиваем RectTransform для правильного отображения
            RectTransform rectTransform = panelToMove.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            Debug.Log($"Панель перемещена в отдельное окно");
        }
        else
        {
            Debug.LogError("Не удалось переместить панель - отсутствуют необходимые компоненты");
        }
    }

    void OnDestroy()
    {
        // Очищаем ресурсы при уничтожении объекта
        if (separateCamera != null && separateCamera.targetTexture != null)
        {
            separateCamera.targetTexture.Release();
        }
    }

    // Альтернативный метод для платформ, где Windows API недоступен
    void CreateSeparateWindowFallback()
    {
        Debug.Log("Создание отдельного окна через Unity (fallback метод)");

        // Создаем отдельную камеру
        CreateSeparateCamera();

        // Перемещаем панель
        MovePanelToSeparateWindow();

        // В этом случае "окно" будет отображаться в том же Unity окне,
        // но на отдельной камере, что может быть полезно для тестирования
        windowCreated = true;
    }

    // Публичный метод для создания окна (можно вызвать из других скриптов)
    public void CreateWindow()
    {
        CreateSeparateWindow();
    }

    // Метод для закрытия окна
    public void CloseWindow()
    {
        if (windowCreated && separateCanvas != null)
        {
            // Возвращаем панель обратно (если нужно)
            // panelToMove.transform.SetParent(originalParent, false);

            // Уничтожаем созданные объекты
            if (separateCanvas != null)
                DestroyImmediate(separateCanvas.gameObject);

            if (separateCamera != null)
                DestroyImmediate(separateCamera.gameObject);

            windowCreated = false;
            Debug.Log("Отдельное окно закрыто");
        }
    }
}