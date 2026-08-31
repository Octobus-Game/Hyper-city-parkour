using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ulltrakillslovo : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load when the secret word is typed.")]
    public string targetSceneName = "YourSceneNameHere";

    [Header("Word Settings")]
    [Tooltip("The secret word that triggers the scene change.")]
    public string secretWord = "Puzzles";

    [Tooltip("Time window in seconds to reset input if no typing occurs.")]
    [Range(1f, 10f)]
    public float inputTimeout = 3f;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when the secret word is typed.")]
    public AudioClip transitionSound;

    [Tooltip("Volume for the transition sound.")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Visual Feedback")]
    [Tooltip("Optional UI Text to show typing progress (for debugging).")]
    public UnityEngine.UI.Text debugText;

    [Tooltip("Show visual feedback when word is recognized.")]
    public bool showVisualFeedback = true;

    // Private fields
    private string inputBuffer = "";
    private AudioSource audioSource;
    private bool isTriggered = false;
    private bool isTransitioning = false;
    private float lastInputTime;
    private Coroutine transitionCoroutine;

    // Events for extensibility
    public System.Action OnSecretWordEntered;
    public System.Action OnSceneTransitionStarted;

    void Start()
    {
        InitializeComponents();
        ValidateSettings();
    }

    void Update()
    {
        if (isTriggered || isTransitioning)
            return;

        HandleInput();
        CheckInputTimeout();
        UpdateDebugDisplay();
    }

    private void InitializeComponents()
    {
        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        lastInputTime = Time.time;
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("Target scene name is not set!");
        }

        if (string.IsNullOrEmpty(secretWord))
        {
            Debug.LogWarning("Secret word is not set!");
            secretWord = "Puzzles"; // Fallback
        }

        if (string.IsNullOrEmpty(targetSceneName) || targetSceneName == "YourSceneNameHere")
        {
            Debug.LogError("Please set a valid target scene name in the inspector!");
        }
    }

    private void HandleInput()
    {
        string currentInput = Input.inputString;

        if (string.IsNullOrEmpty(currentInput))
            return;

        foreach (char c in currentInput)
        {
            // Filter out non-letter characters for cleaner input
            if (char.IsLetter(c))
            {
                inputBuffer += char.ToLower(c);
                lastInputTime = Time.time;

                // Keep buffer size manageable
                if (inputBuffer.Length > secretWord.Length * 2)
                {
                    inputBuffer = inputBuffer.Substring(inputBuffer.Length - secretWord.Length);
                }

                CheckForSecretWord();
            }
        }
    }

    private void CheckInputTimeout()
    {
        if (Time.time - lastInputTime > inputTimeout && !string.IsNullOrEmpty(inputBuffer))
        {
            inputBuffer = "";
            UpdateDebugDisplay();
        }
    }

    private void CheckForSecretWord()
    {
        if (inputBuffer.EndsWith(secretWord.ToLower()))
        {
            TriggerSecretWord();
        }
    }

    private void TriggerSecretWord()
    {
        if (isTriggered)
            return;

        isTriggered = true;

        Debug.Log($"Secret word '{secretWord}' detected! Loading scene: {targetSceneName}");

        // Invoke event
        OnSecretWordEntered?.Invoke();

        // Start transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(HandleSceneTransition());
    }

    private IEnumerator HandleSceneTransition()
    {
        isTransitioning = true;
        OnSceneTransitionStarted?.Invoke();

        // Play sound if available
        if (transitionSound != null && audioSource != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(transitionSound);

            // Wait for sound to finish
            yield return new WaitForSeconds(transitionSound.length);
        }

        // Show visual feedback
        if (showVisualFeedback)
        {
            // Could add screen flash, fade, etc. here
            Debug.Log("Transitioning to new scene...");
        }

        // Load scene
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        try
        {
            if (Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogError($"Scene '{targetSceneName}' cannot be loaded. Check if it's added to Build Settings!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{targetSceneName}': {e.Message}");
        }
    }

    private void UpdateDebugDisplay()
    {
        if (debugText != null)
        {
            debugText.text = $"Input: {inputBuffer}\nTarget: {secretWord.ToLower()}";
        }
    }

    // Public methods for external control
    public void ResetTrigger()
    {
        isTriggered = false;
        isTransitioning = false;
        inputBuffer = "";

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    public void SetSecretWord(string newWord)
    {
        if (!string.IsNullOrEmpty(newWord))
        {
            secretWord = newWord;
            inputBuffer = ""; // Reset buffer when changing word
        }
    }

    public void TriggerImmediately()
    {
        if (!isTriggered && !isTransitioning)
        {
            TriggerSecretWord();
        }
    }

    // Cleanup
    void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
    }
}
