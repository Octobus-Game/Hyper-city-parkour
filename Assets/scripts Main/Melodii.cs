using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melodii : MonoBehaviour
{
    public AudioSource audioSource; // Источник звука
    public AudioClip[] audioClips;  // Массив мелодий

    private int currentClipIndex = 0;

    void Start()
    {
        if (audioClips.Length > 0 && audioSource != null)
        {
            // Воспроизводим первую мелодию
            PlayNextClip();
        }
    }

    void Update()
    {
        // Проверяем, если аудио остановилось, воспроизводим следующую мелодию
        if (!audioSource.isPlaying && currentClipIndex < audioClips.Length)
        {
            PlayNextClip();
        }
    }

    private void PlayNextClip()
    {
        if (currentClipIndex < audioClips.Length)
        {
            audioSource.clip = audioClips[currentClipIndex];
            audioSource.Play();
            currentClipIndex++;
        }
    }
}
