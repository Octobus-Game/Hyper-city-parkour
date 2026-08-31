using System.Collections;
using UnityEngine;

public class MusicFadeOut : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float fadeOutDuration = 2f; // Время затухания в секундах
    [SerializeField] private float fadeStartBeforeEnd = 3f; // За сколько секунд до конца начинать затухание

    private float originalVolume;
    private bool isFading = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        originalVolume = audioSource.volume;
    }

    void Update()
    {
        // Проверяем, нужно ли начинать затухание
        if (!isFading && audioSource.isPlaying)
        {
            float timeLeft = audioSource.clip.length - audioSource.time;

            if (timeLeft <= fadeStartBeforeEnd)
            {
                StartCoroutine(FadeOut());
            }
        }
    }

    private IEnumerator FadeOut()
    {
        isFading = true;
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();

        // Восстанавливаем громкость для следующего воспроизведения
        audioSource.volume = originalVolume;
        isFading = false;
    }
}