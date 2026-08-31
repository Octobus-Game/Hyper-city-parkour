using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB; // StandaloneFileBrowser
using TMPro;
public class AdvancedMusicPlayer : MonoBehaviour
{
    [Header("UI Elements")]
    public Button selectMusicButton;
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Button nextButton;
    public Button prevButton;
    public Text statusText;
    public Text timeText;
    public Slider volumeSlider;
    public Slider progressSlider;
    public Toggle loopToggle;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool loopMusic = true;

    [Header("Hotkeys")]
    public KeyCode playPauseKey = KeyCode.Tab;
    public KeyCode stopKey = KeyCode.Escape;
    public KeyCode nextKey = KeyCode.RightArrow;
    public KeyCode prevKey = KeyCode.LeftArrow;

    [Header("Music History")]
    public Transform historyPanel;
    public GameObject historyButtonPrefab;
    public int maxHistoryCount = 15;

    private List<string> playlist = new List<string>();
    private List<AudioClip> loadedClips = new List<AudioClip>();
    private int currentTrackIndex = 0;
    private bool isLoading = false;
    private bool isDraggingProgress = false;

    private void Start()
    {
        SetupUI();
        SetupAudioSource();
        UpdateStatusText("Выберите музыкальные файлы");
    }

    private void SetupUI()
    {
        if (selectMusicButton != null)
            selectMusicButton.onClick.AddListener(OpenFileDialog);

        if (playButton != null)
            playButton.onClick.AddListener(PlayMusic);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseMusic);

        if (stopButton != null)
            stopButton.onClick.AddListener(StopMusic);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextTrack);

        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousTrack);

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (progressSlider != null)
        {
            progressSlider.onValueChanged.AddListener(OnProgressSliderChanged);
        }

        if (loopToggle != null)
        {
            loopToggle.isOn = loopMusic;
            loopToggle.onValueChanged.AddListener(SetLoop);
        }
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = volume;
        audioSource.loop = loopMusic;
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        HandleHotkeys();
        UpdateUI();
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(playPauseKey))
        {
            ToggleMusic();
        }

        if (Input.GetKeyDown(stopKey))
        {
            StopMusic();
        }

        if (Input.GetKeyDown(nextKey))
        {
            NextTrack();
        }

        if (Input.GetKeyDown(prevKey))
        {
            PreviousTrack();
        }
    }

    private void UpdateUI()
    {
        // Обновляем прогресс бар
        if (audioSource.clip != null && !isDraggingProgress && progressSlider != null)
        {
            float progress = audioSource.time / audioSource.clip.length;
            progressSlider.value = progress;
        }

        // Обновляем время
        if (timeText != null && audioSource.clip != null)
        {
            float currentTime = audioSource.time;
            float totalTime = audioSource.clip.length;
            timeText.text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
        }

        // Проверяем окончание трека для автоматического перехода к следующему
        if (audioSource.isPlaying && audioSource.clip != null &&
            audioSource.time >= audioSource.clip.length - 0.1f && !loopMusic)
        {
            NextTrack();
        }
    }

    private void OpenFileDialog()
    {
        if (isLoading)
        {
            UpdateStatusText("Подождите, загружается файл...");
            return;
        }

        var extensions = new[] {
            new ExtensionFilter("Audio Files", "mp3", "ogg", "wav"),
            new ExtensionFilter("MP3", "mp3"),
            new ExtensionFilter("OGG", "ogg"),
            new ExtensionFilter("WAV", "wav")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Выберите музыкальные файлы", "", extensions, true);

        if (paths.Length > 0)
        {
            StartCoroutine(LoadMultipleAudioFiles(paths));
        }
    }

    private IEnumerator LoadMultipleAudioFiles(string[] paths)
    {
        isLoading = true;

        // Очищаем предыдущий плейлист
        ClearPlaylist();

        UpdateStatusText($"Загружается {paths.Length} файлов...");

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            string fileName = Path.GetFileName(path);

            UpdateStatusText($"Загружается: {fileName} ({i + 1}/{paths.Length})");

            yield return StartCoroutine(LoadSingleAudioFile(path));
        }

        if (playlist.Count > 0)
        {
            currentTrackIndex = 0;
            SetCurrentTrack(0);
            UpdateStatusText($"Загружено {playlist.Count} файлов. Tab - воспроизведение");
        }
        else
        {
            UpdateStatusText("Не удалось загрузить файлы");
        }

        isLoading = false;
    }

    private IEnumerator LoadSingleAudioFile(string path)
    {
        AudioType audioType = GetAudioType(path);
        string url = "file://" + path;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = Path.GetFileName(path);

                playlist.Add(path);
                loadedClips.Add(clip);
            }
        }
    }

    private AudioType GetAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLower();
        return extension switch
        {
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            ".wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN
        };
    }

    private void SetCurrentTrack(int index)
    {
        if (index >= 0 && index < loadedClips.Count)
        {
            currentTrackIndex = index;
            audioSource.clip = loadedClips[index];

            string fileName = Path.GetFileName(playlist[index]);
            UpdateStatusText($"Трек: {fileName} ({index + 1}/{playlist.Count})");
        }
    }

    private void ToggleMusic()
    {
        if (audioSource.clip == null)
        {
            UpdateStatusText("Сначала выберите музыкальные файлы");
            return;
        }

        if (audioSource.isPlaying)
        {
            PauseMusic();
        }
        else
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            string fileName = Path.GetFileName(playlist[currentTrackIndex]);
            UpdateStatusText($"Играет: {fileName}");
        }
    }

    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            string fileName = Path.GetFileName(playlist[currentTrackIndex]);
            UpdateStatusText($"Пауза: {fileName}");
        }
    }

    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            string fileName = Path.GetFileName(playlist[currentTrackIndex]);
            UpdateStatusText($"Остановлено: {fileName}");
        }
    }

    public void NextTrack()
    {
        if (playlist.Count == 0) return;

        int nextIndex = (currentTrackIndex + 1) % playlist.Count;
        SetCurrentTrack(nextIndex);

        if (audioSource.isPlaying)
        {
            PlayMusic();
        }
    }

    public void PreviousTrack()
    {
        if (playlist.Count == 0) return;

        int prevIndex = (currentTrackIndex - 1 + playlist.Count) % playlist.Count;
        SetCurrentTrack(prevIndex);

        if (audioSource.isPlaying)
        {
            PlayMusic();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }

    public void SetLoop(bool loop)
    {
        loopMusic = loop;
        audioSource.loop = loopMusic;
    }

    public void OnProgressSliderChanged(float value)
    {
        if (audioSource.clip != null)
        {
            isDraggingProgress = true;
            audioSource.time = value * audioSource.clip.length;
            StartCoroutine(ResetProgressDrag());
        }
    }

    private IEnumerator ResetProgressDrag()
    {
        yield return new WaitForEndOfFrame();
        isDraggingProgress = false;
    }

    private void ClearPlaylist()
    {
        // Освобождаем память от предыдущих клипов
        foreach (var clip in loadedClips)
        {
            if (clip != null)
                Destroy(clip);
        }

        playlist.Clear();
        loadedClips.Clear();
        currentTrackIndex = 0;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}