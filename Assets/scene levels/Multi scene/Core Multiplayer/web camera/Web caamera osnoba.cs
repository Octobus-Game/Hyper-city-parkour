using UnityEngine;
using Mirror;
using System.Collections;

public class WebcamStreamer : NetworkBehaviour
{
    [Header("Webcam Settings")]
    public string webcamName = "";
    public int webcamWidth = 640;
    public int webcamHeight = 480;
    public int webcamFPS = 30;

    [Header("Display Settings")]
    public Renderer targetRenderer;
    public string materialPropertyName = "_MainTex";

    [Header("Network Settings")]
    [SyncVar(hook = nameof(OnTextureDataChanged))]
    private byte[] textureData;

    [SyncVar(hook = nameof(OnTextureSizeChanged))]
    private Vector2Int textureSize;

    private WebCamTexture webcamTexture;
    private Texture2D networkTexture;
    private Material targetMaterial;

    // Частота отправки кадров по сети (для оптимизации)
    [Header("Performance")]
    public float networkUpdateRate = 10f; // кадров в секунду по сети
    private float lastNetworkUpdate;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        InitializeWebcam();
    }

    void InitializeWebcam()
    {
        if (!isLocalPlayer) return;

        // Находим доступные камеры
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("Веб-камера не найдена!");
            return;
        }

        // Используем указанную камеру или первую доступную
        string selectedDevice = string.IsNullOrEmpty(webcamName) ? devices[0].name : webcamName;

        Debug.Log($"Инициализация веб-камеры: {selectedDevice}");

        // Создаем WebCamTexture
        webcamTexture = new WebCamTexture(selectedDevice, webcamWidth, webcamHeight, webcamFPS);

        // Применяем к локальному объекту
        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            targetMaterial.SetTexture(materialPropertyName, webcamTexture);
        }

        // Запускаем камеру
        webcamTexture.Play();

        // Начинаем отправку данных по сети
        StartCoroutine(NetworkUpdateCoroutine());
    }

    IEnumerator NetworkUpdateCoroutine()
    {
        while (webcamTexture != null && webcamTexture.isPlaying)
        {
            yield return new WaitForSeconds(1f / networkUpdateRate);

            if (isLocalPlayer && webcamTexture != null && webcamTexture.didUpdateThisFrame)
            {
                SendWebcamDataToNetwork();
            }
        }
    }

    void SendWebcamDataToNetwork()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying) return;

        // Создаем Texture2D из WebCamTexture
        Texture2D tempTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);

        // Копируем пиксели
        tempTexture.SetPixels(webcamTexture.GetPixels());
        tempTexture.Apply();

        // Сжимаем в JPEG для экономии трафика
        byte[] imageData = tempTexture.EncodeToJPG(75);

        // Освобождаем временную текстуру
        Destroy(tempTexture);

        // Отправляем данные через SyncVar
        CmdUpdateWebcamTexture(imageData, new Vector2Int(webcamTexture.width, webcamTexture.height));
    }

    [Command]
    void CmdUpdateWebcamTexture(byte[] data, Vector2Int size)
    {
        // Обновляем SyncVar на сервере, что автоматически синхронизирует с клиентами
        textureData = data;
        textureSize = size;
    }

    void OnTextureDataChanged(byte[] oldData, byte[] newData)
    {
        if (isLocalPlayer || newData == null || newData.Length == 0) return;

        UpdateRemoteTexture(newData);
    }

    void OnTextureSizeChanged(Vector2Int oldSize, Vector2Int newSize)
    {
        if (isLocalPlayer) return;

        // Пересоздаем текстуру с новым размером
        if (networkTexture != null)
        {
            Destroy(networkTexture);
        }

        networkTexture = new Texture2D(newSize.x, newSize.y, TextureFormat.RGB24, false);

        if (targetRenderer != null)
        {
            if (targetMaterial == null)
                targetMaterial = targetRenderer.material;

            targetMaterial.SetTexture(materialPropertyName, networkTexture);
        }
    }

    void UpdateRemoteTexture(byte[] imageData)
    {
        if (networkTexture == null) return;

        // Загружаем JPEG данные в текстуру
        if (networkTexture.LoadImage(imageData))
        {
            networkTexture.Apply();
        }
    }

    void Start()
    {
        // Настраиваем компоненты при старте
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null && targetMaterial == null)
        {
            targetMaterial = targetRenderer.material;
        }
    }

    void OnDestroy()
    {
        // Очищаем ресурсы
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
        }

        if (networkTexture != null)
        {
            Destroy(networkTexture);
        }
    }

    // Методы для управления камерой
    [ContextMenu("Start Webcam")]
    public void StartWebcam()
    {
        if (isLocalPlayer && webcamTexture != null && !webcamTexture.isPlaying)
        {
            webcamTexture.Play();
            StartCoroutine(NetworkUpdateCoroutine());
        }
    }

    [ContextMenu("Stop Webcam")]
    public void StopWebcam()
    {
        if (isLocalPlayer && webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
            StopAllCoroutines();
        }
    }

    // Дополнительные утилиты
    public void SetTargetRenderer(Renderer renderer)
    {
        targetRenderer = renderer;
        if (renderer != null)
        {
            targetMaterial = renderer.material;
        }
    }

    public void SetWebcamResolution(int width, int height)
    {
        webcamWidth = width;
        webcamHeight = height;

        if (isLocalPlayer && webcamTexture != null)
        {
            bool wasPlaying = webcamTexture.isPlaying;
            if (wasPlaying) webcamTexture.Stop();

            Destroy(webcamTexture);
            InitializeWebcam();
        }
    }

    // Информация о состоянии
    public bool IsWebcamActive()
    {
        return webcamTexture != null && webcamTexture.isPlaying;
    }

    public Vector2Int GetWebcamResolution()
    {
        if (webcamTexture != null)
            return new Vector2Int(webcamTexture.width, webcamTexture.height);
        return Vector2Int.zero;
    }
}