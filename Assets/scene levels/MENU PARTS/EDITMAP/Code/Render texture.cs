using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using SFB; // StandaloneFileBrowser

public class VideoToggleController : MonoBehaviour
{
    [Header("UI Components")]
    public Toggle videoToggle;
    public Button selectVideoButton;
    public Slider volumeSlider; // Новый слайдер для громкости

    [Header("Video Components")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource; // Новый аудио источник
    public RenderTexture renderTexture;
    public Renderer targetRenderer;

    [Header("Settings")]
    public int renderTextureWidth = 1920;
    public int renderTextureHeight = 1080;

    private Material originalMaterial;
    private string selectedVideoPath;

    void Start()
    {
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
        }

        if (videoToggle != null)
        {
            videoToggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        if (selectVideoButton != null)
        {
            selectVideoButton.onClick.AddListener(OpenVideoFileDialog);
            selectVideoButton.gameObject.SetActive(false);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = 1f; // Стартовая громкость — максимум
        }

        if (renderTexture == null)
        {
            CreateRenderTexture();
        }

        SetupVideoPlayer();
    }

    void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;

        // Настраиваем аудио через AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        if (videoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource && audioSource != null)
        {
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
            audioSource.playOnAwake = false;
        }
    }

    void OnVolumeSliderChanged(float value)
    {
        if (audioSource != null)
        {
            audioSource.volume = value;
            Debug.Log($"Volume changed: {value}");
        }
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            EnableVideoMode();
        }
        else
        {
            DisableVideoMode();
        }
    }

    void EnableVideoMode()
    {
        if (selectVideoButton != null)
        {
            selectVideoButton.gameObject.SetActive(true);
        }

        if (targetRenderer != null && renderTexture != null)
        {
            Material videoMaterial = new Material(originalMaterial);
            videoMaterial.mainTexture = renderTexture;
            targetRenderer.material = videoMaterial;
        }

        if (videoPlayer != null && !string.IsNullOrEmpty(selectedVideoPath))
        {
            videoPlayer.Play();
        }

        Debug.Log("Video mode enabled");
    }

    void DisableVideoMode()
    {
        if (selectVideoButton != null)
        {
            selectVideoButton.gameObject.SetActive(false);
        }

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (targetRenderer != null && originalMaterial != null)
        {
            targetRenderer.material = originalMaterial;
        }

        Debug.Log("Video mode disabled");
    }

    void OpenVideoFileDialog()
    {
        var extensions = new[] {
            new ExtensionFilter("Video Files", "mp4", "avi", "mov", "mkv", "wmv", "flv", "webm"),
            new ExtensionFilter("All Files", "*"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select Video File", "", extensions, false);

        if (paths != null && paths.Length > 0)
        {
            selectedVideoPath = paths[0];
            LoadVideo(selectedVideoPath);
        }
    }

    void LoadVideo(string videoPath)
    {
        if (videoPlayer != null && !string.IsNullOrEmpty(videoPath))
        {
            videoPlayer.Stop();
            videoPlayer.url = videoPath;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnVideoPrepared;

            Debug.Log($"Loading video: {videoPath}");
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();

        Debug.Log("Video prepared and playing");
    }

    void CreateRenderTexture()
    {
        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }

        if (videoToggle != null)
        {
            videoToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        if (selectVideoButton != null)
        {
            selectVideoButton.onClick.RemoveListener(OpenVideoFileDialog);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        }
    }

    public void PlayVideo()
    {
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            videoPlayer.Play();
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }
}
