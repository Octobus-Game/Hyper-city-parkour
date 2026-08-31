using UnityEngine;
using UnityEngine.UI;

public class ESCPAUSEGAME01 : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button[] additionalResumeButtons;

    [Header("Camera References")]
    public Camera[] camerasToFreeze;
    public MonoBehaviour[] cameraScripts;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip pauseSound;
    public AudioClip resumeSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    // -----------------------------------------------
    [Header("Toggle Objects on ESC")]
    [Tooltip("ќбъекты, которые ќ“ Ћё„ј“—я при паузе и ¬ Ћё„ј“—я при возобновлении")]
    public GameObject[] objectsToDisableOnPause;

    [Tooltip("ќбъекты, которые ¬ Ћё„ј“—я при паузе и ќ“ Ћё„ј“—я при возобновлении")]
    public GameObject[] objectsToEnableOnPause;
    // -----------------------------------------------

    private bool isPaused = false;
    private float originalTimeScale = 1f;
    private bool[] originalCameraStates;
    private bool[] originalScriptStates;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        originalTimeScale = Time.timeScale;

        if (camerasToFreeze == null || camerasToFreeze.Length == 0)
            camerasToFreeze = FindObjectsOfType<Camera>();

        if (cameraScripts == null || cameraScripts.Length == 0)
        {
            var mouseScripts = FindObjectsOfType<MonoBehaviour>();
            System.Collections.Generic.List<MonoBehaviour> cameraControlScripts = new System.Collections.Generic.List<MonoBehaviour>();

            foreach (var script in mouseScripts)
            {
                string scriptName = script.GetType().Name.ToLower();
                if (scriptName.Contains("camera") || scriptName.Contains("mouse") ||
                    scriptName.Contains("look") || scriptName.Contains("orbit") ||
                    scriptName.Contains("fps") || scriptName.Contains("firstperson"))
                {
                    cameraControlScripts.Add(script);
                }
            }

            cameraScripts = cameraControlScripts.ToArray();
        }

        originalCameraStates = new bool[camerasToFreeze.Length];
        originalScriptStates = new bool[cameraScripts.Length];

        SetupResumeButtons();
        SetupAudioComponent();

        // »нициализируем начальное состо€ние объектов
        InitToggleObjects();
    }

    // -----------------------------------------------
    void InitToggleObjects()
    {
        // objectsToDisableOnPause Ч активны в игре, неактивны на паузе
        if (objectsToDisableOnPause != null)
            foreach (var obj in objectsToDisableOnPause)
                if (obj != null) obj.SetActive(true);

        // objectsToEnableOnPause Ч неактивны в игре, активны на паузе
        if (objectsToEnableOnPause != null)
            foreach (var obj in objectsToEnableOnPause)
                if (obj != null) obj.SetActive(false);
    }

    void ApplyToggleObjects(bool pausing)
    {
        if (objectsToDisableOnPause != null)
            foreach (var obj in objectsToDisableOnPause)
                if (obj != null) obj.SetActive(!pausing);

        if (objectsToEnableOnPause != null)
            foreach (var obj in objectsToEnableOnPause)
                if (obj != null) obj.SetActive(pausing);
    }
    // -----------------------------------------------

    void SetupAudioComponent()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.ignoreListenerPause = true;
        }
    }

    void SetupResumeButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeFromButton);
        }

        if (additionalResumeButtons != null)
        {
            foreach (Button button in additionalResumeButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(ResumeFromButton);
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
            PauseGame();
        else
            ResumeGame();
    }

    void PauseGame()
    {
        PlayPauseSound();
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        FreezeCameras();
        ApplyToggleObjects(true);   // < переключаем объекты

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Game Paused");
    }

    void ResumeGame()
    {
        PlayResumeSound();
        Time.timeScale = originalTimeScale;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        UnfreezeCameras();
        ApplyToggleObjects(false);  // < возвращаем объекты

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Game Resumed");
    }

    void PlayPauseSound()
    {
        if (audioSource != null && pauseSound != null)
        {
            audioSource.clip = pauseSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
    }

    void PlayResumeSound()
    {
        if (audioSource != null && resumeSound != null)
        {
            audioSource.clip = resumeSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
        else if (audioSource != null && pauseSound != null)
        {
            audioSource.clip = pauseSound;
            audioSource.volume = soundVolume * 0.8f;
            audioSource.Play();
        }
    }

    public void SetPauseSound(AudioClip newPauseSound) => pauseSound = newPauseSound;
    public void SetResumeSound(AudioClip newResumeSound) => resumeSound = newResumeSound;

    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null) audioSource.volume = soundVolume;
    }

    public void ResumeButton()
    {
        if (isPaused) TogglePause();
    }

    public void ResumeFromButton()
    {
        if (isPaused)
        {
            TogglePause();
            Debug.Log("Game resumed from UI button");
        }
    }

    public void AddResumeButton(Button newButton)
    {
        if (newButton != null)
        {
            newButton.onClick.RemoveAllListeners();
            newButton.onClick.AddListener(ResumeFromButton);

            if (additionalResumeButtons == null)
            {
                additionalResumeButtons = new Button[] { newButton };
            }
            else
            {
                var buttonList = new System.Collections.Generic.List<Button>(additionalResumeButtons);
                if (!buttonList.Contains(newButton))
                {
                    buttonList.Add(newButton);
                    additionalResumeButtons = buttonList.ToArray();
                }
            }
        }
    }

    public void RemoveResumeButton(Button buttonToRemove)
    {
        if (buttonToRemove != null && additionalResumeButtons != null)
        {
            var buttonList = new System.Collections.Generic.List<Button>(additionalResumeButtons);
            if (buttonList.Remove(buttonToRemove))
            {
                additionalResumeButtons = buttonList.ToArray();
                buttonToRemove.onClick.RemoveListener(ResumeFromButton);
            }
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool IsPaused() => isPaused;

    void FreezeCameras()
    {
        for (int i = 0; i < cameraScripts.Length; i++)
        {
            if (cameraScripts[i] != null)
            {
                originalScriptStates[i] = cameraScripts[i].enabled;
                cameraScripts[i].enabled = false;
            }
        }

        for (int i = 0; i < camerasToFreeze.Length; i++)
        {
            if (camerasToFreeze[i] != null)
            {
                AudioListener al = camerasToFreeze[i].GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }
        }
    }

    void UnfreezeCameras()
    {
        for (int i = 0; i < cameraScripts.Length; i++)
        {
            if (cameraScripts[i] != null)
                cameraScripts[i].enabled = originalScriptStates[i];
        }

        for (int i = 0; i < camerasToFreeze.Length; i++)
        {
            if (camerasToFreeze[i] != null)
            {
                AudioListener al = camerasToFreeze[i].GetComponent<AudioListener>();
                if (al != null) al.enabled = true;
            }
        }
    }

    public void AddCameraScript(MonoBehaviour script)
    {
        if (script != null)
        {
            var scriptList = new System.Collections.Generic.List<MonoBehaviour>(cameraScripts);
            if (!scriptList.Contains(script))
            {
                scriptList.Add(script);
                cameraScripts = scriptList.ToArray();
                originalScriptStates = new bool[cameraScripts.Length];
            }
        }
    }
}