using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Puzzlessclovo : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load when 'Puzzles' is typed.")]
    public string targetSceneName = "YourSceneNameHere";

    [Header("Audio Settings")]
    [Tooltip("Sound to play when the word 'Puzzles' is typed.")]
    public AudioClip transitionSound;

    private string inputBuffer = "";
    private string targetWord = "Puzzles";
    private AudioSource audioSource;
    private bool triggered = false;

    void Start()
    {
        // Add AudioSource if not present
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (triggered)
            return;

        foreach (char c in Input.inputString)
        {
            inputBuffer += c;

            if (inputBuffer.Length > targetWord.Length)
                inputBuffer = inputBuffer.Substring(inputBuffer.Length - targetWord.Length);

            if (inputBuffer.Equals(targetWord, System.StringComparison.OrdinalIgnoreCase))
            {
                triggered = true;

                if (transitionSound != null)
                {
                    audioSource.PlayOneShot(transitionSound);
                    Invoke(nameof(LoadTargetScene), transitionSound.length);
                }
                else
                {
                    LoadTargetScene();
                }
            }
        }
    }

    void LoadTargetScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}
