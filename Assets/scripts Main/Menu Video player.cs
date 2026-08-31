using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SFB; // StandaloneFileBrowser

public class VideoFileSelector : MonoBehaviour
{
    [Header("UI Components")]
    public Button selectFileButton;
    public RawImage videoDisplayPanel; // Panel для отображения видео

    [Header("Video Components")]
    public VideoPlayer _videoPlayer;

    private const string VIDEO_PATH_KEY = "SelectedVideoPath";
    private string currentVideoPath = "";

    public AudioSource _menuaudiooff;


    void Start()
    {
        // Настройка кнопки
        if (selectFileButton != null)
        {
            selectFileButton.onClick.AddListener(OpenFileDialog);
        }

        // Настройка видеоплеера
        if (_videoPlayer == null)
        {
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        _videoPlayer.playOnAwake = false;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Создаем RenderTexture для отображения видео
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 16);
        _videoPlayer.targetTexture = renderTexture;

        if (videoDisplayPanel != null)
        {
            videoDisplayPanel.texture = renderTexture;
        }

        // Загружаем сохраненный путь к видео
        LoadSavedVideoPath();

        // Подписываемся на события видеоплеера
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.errorReceived += OnVideoError;
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.errorReceived -= OnVideoError;
        }
    }

    public void OpenFileDialog()
    {
        // Поддерживаемые форматы видео
        ExtensionFilter[] extensions = new[]
        {
            new ExtensionFilter("Video Files", "mp4", "avi", "mov", "wmv", "flv", "webm", "mkv", "m4v", "3gp", "ogg", "ogv"),
            new ExtensionFilter("MP4", "mp4"),
            new ExtensionFilter("AVI", "avi"),
            new ExtensionFilter("MOV", "mov"),
            new ExtensionFilter("OGG Video", "ogg", "ogv"),
            new ExtensionFilter("All Files", "*")
        };

        // Открываем диалог выбора файла
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Выберите видеофайл",
            "",
            extensions,
            false
        );

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedPath = paths[0];
            LoadVideo(selectedPath);
        }
    }

    private void LoadVideo(string videoPath)
    {
        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogWarning("Путь к видео пустой!");
            return;
        }

        // Проверяем существование файла
        if (!System.IO.File.Exists(videoPath))
        {
            Debug.LogError($"Видеофайл не найден: {videoPath}");
            return;
        }

        currentVideoPath = videoPath;

        // Сохраняем путь
        SaveVideoPath(videoPath);

        // Подготавливаем видео к воспроизведению
        _videoPlayer.url = "file://" + videoPath;
        _videoPlayer.Prepare();

        Debug.Log($"Загружается видео: {videoPath}");
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Видео готово к воспроизведению");

        // Автоматически начинаем воспроизведение
        _videoPlayer.Play();

        // Показываем панель с видео
        if (videoDisplayPanel != null)
        {
                       
            videoDisplayPanel.gameObject.SetActive(true);
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"Ошибка при загрузке видео: {message}");
    }

    private void SaveVideoPath(string path)
    {
        PlayerPrefs.SetString(VIDEO_PATH_KEY, path);
        PlayerPrefs.Save();
        Debug.Log($"Сохранен путь к видео: {path}");
    }

    private void LoadSavedVideoPath()
    {
        if (PlayerPrefs.HasKey(VIDEO_PATH_KEY))
        {
            string savedPath = PlayerPrefs.GetString(VIDEO_PATH_KEY);
            if (!string.IsNullOrEmpty(savedPath) && System.IO.File.Exists(savedPath))
            {
                Debug.Log($"Загружается сохраненное видео: {savedPath}");
                LoadVideo(savedPath);
            }
            else
            {
                Debug.LogWarning("Сохраненный видеофайл не найден или поврежден");
                PlayerPrefs.DeleteKey(VIDEO_PATH_KEY);
            }
        }
    }

    // Дополнительные методы управления видео
    [Header("Video Controls")]
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;

    void Update()
    {
        // Настройка кнопок управления, если они назначены
        if (playButton != null && playButton.onClick.GetPersistentEventCount() == 0)
        {
            playButton.onClick.AddListener(() => {
                if (_videoPlayer.isPrepared)
                    _videoPlayer.Play();
            });
        }

        if (pauseButton != null && pauseButton.onClick.GetPersistentEventCount() == 0)
        {
            pauseButton.onClick.AddListener(() => {
                if (_videoPlayer.isPlaying)
                    _videoPlayer.Pause();
            });
        }

        if (stopButton != null && stopButton.onClick.GetPersistentEventCount() == 0)
        {
            stopButton.onClick.AddListener(() => {
                _videoPlayer.Stop();
            });
        }
    }

    // Публичные методы для управления видео
    public void PlayVideo()
    {
        if (_videoPlayer.isPrepared)
            _videoPlayer.Play();
    }

    public void PauseVideo()
    {
        if (_videoPlayer.isPlaying)
            _videoPlayer.Pause();
    }

    public void StopVideo()
    {
        _videoPlayer.Stop();
    }

    public void SetVolume(float volume)
    {
        _videoPlayer.SetDirectAudioVolume(0, volume);
    }

    public bool IsVideoLoaded()
    {
        return !string.IsNullOrEmpty(currentVideoPath) && _videoPlayer.isPrepared;
    }

    public string GetCurrentVideoPath()
    {
        return currentVideoPath;
    }
}