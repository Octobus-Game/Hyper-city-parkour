using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class waTERSoundTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip soundClip;
    public AudioSource audioSource;

    [Header("Barrel Effect Settings")]
    [Range(0.1f, 3.0f)]
    public float reverbLevel = 0.8f;

    [Range(0.1f, 2.0f)]
    public float lowPassCutoff = 0.4f;

    [Range(0.5f, 1.5f)]
    public float pitchVariation = 0.8f;

    [Range(0.1f, 1.0f)]
    public float volume = 0.7f;

    private AudioReverbFilter reverbFilter;
    private AudioLowPassFilter lowPassFilter;
    private bool hasTriggered = false;

    void Start()
    {
        // Получаем или создаем AudioSource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Настраиваем базовые параметры AudioSource
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 3D звук
        audioSource.volume = volume;

        // Добавляем эффекты для имитации звука из бочки
        SetupBarrelEffects();
    }

    void SetupBarrelEffects()
    {
        // Добавляем реверб для эффекта эха
        reverbFilter = gameObject.GetComponent<AudioReverbFilter>();
        if (reverbFilter == null)
            reverbFilter = gameObject.AddComponent<AudioReverbFilter>();

        reverbFilter.reverbPreset = AudioReverbPreset.Cave;
        reverbFilter.reverbLevel = reverbLevel;
        reverbFilter.decayTime = 2.0f;

        // Добавляем низкочастотный фильтр для приглушенного звука
        lowPassFilter = gameObject.GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null)
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();

        lowPassFilter.cutoffFrequency = 22000 * lowPassCutoff;
        lowPassFilter.lowpassResonanceQ = 1.0f;
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что столкнулись с игроком
        if (other.CompareTag("Player") && !hasTriggered)
        {
            PlayBarrelSound();
            hasTriggered = true;

            // Сбрасываем флаг через некоторое время для повторного воспроизведения
            Invoke("ResetTrigger", 1.0f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Альтернативный вариант для Rigidbody объектов
        if (collision.gameObject.CompareTag("Player") && !hasTriggered)
        {
            PlayBarrelSound();
            hasTriggered = true;
            Invoke("ResetTrigger", 1.0f);
        }
    }

    void PlayBarrelSound()
    {
        if (soundClip != null && audioSource != null)
        {
            // Добавляем случайную вариацию высоты тона
            audioSource.pitch = pitchVariation + Random.Range(-0.1f, 0.1f);

            // Воспроизводим звук
            audioSource.clip = soundClip;
            audioSource.Play();

            Debug.Log("Играет звук из бочки!");
        }
        else
        {
            Debug.LogWarning("AudioClip или AudioSource не назначен!");
        }
    }

    void ResetTrigger()
    {
        hasTriggered = false;
    }

    // Метод для настройки эффектов в реальном времени
    public void UpdateBarrelEffects()
    {
        if (reverbFilter != null)
            reverbFilter.reverbLevel = reverbLevel;

        if (lowPassFilter != null)
            lowPassFilter.cutoffFrequency = 22000 * lowPassCutoff;

        if (audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.pitch = pitchVariation;
        }
    }

    // Вызывается при изменении значений в инспекторе
    void OnValidate()
    {
        if (Application.isPlaying)
            UpdateBarrelEffects();
    }
}
