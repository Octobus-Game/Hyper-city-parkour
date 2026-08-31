using UnityEngine;
using System.Collections.Generic;

public class TimeSlowTrigger_01 : MonoBehaviour
{
    [Header("Настройки замедления времени")]
    [Tooltip("Скорость времени при замедлении (0.5 = в 2 раза медленнее)")]
    [Range(0f, 1f)]
    public float slowMotionScale = 0.3f;
    
    [Tooltip("Длительность замедления в секундах")]
    public float slowMotionDuration = 3f;
    
    [Tooltip("Скорость перехода к замедлению")]
    public float transitionSpeed = 5f;
    
    [Header("Настройки звука")]
    [Tooltip("Замедлять все AudioSource в сцене?")]
    public bool affectAllAudioSources = true;
    
    [Tooltip("Минимальный pitch для звуков (насколько низким будет звук)")]
    [Range(0.1f, 1f)]
    public float minPitch = 0.5f;
    
    [Tooltip("Список AudioSource для исключения из замедления")]
    public List<AudioSource> excludedAudioSources = new List<AudioSource>();
    
    [Header("Опции")]
    [Tooltip("Уничтожить триггер после активации?")]
    public bool destroyAfterUse = false;
    
    [Tooltip("Можно ли активировать повторно?")]
    public bool canReactivate = true;
    
    private bool isActivated = false;
    private bool isSlowMotionActive = false;
    private Dictionary<AudioSource, float> originalPitches = new Dictionary<AudioSource, float>();
    
    private void OnTriggerEnter(Collider other)
    {
        // Проверяем тег игрока
        if (other.CompareTag("Player"))
        {
            // Проверяем, можно ли активировать
            if (!isActivated || canReactivate)
            {
                ActivateSlowMotion();
            }
        }
    }
    
    private void ActivateSlowMotion()
    {
        if (!isSlowMotionActive)
        {
            isActivated = true;
            StartCoroutine(SlowMotionCoroutine());
        }
    }
    
    private System.Collections.IEnumerator SlowMotionCoroutine()
    {
        isSlowMotionActive = true;
        
        // Сохраняем оригинальные pitch всех AudioSource
        SaveOriginalPitches();
        
        // Плавный переход к замедлению
        float currentTimeScale = Time.timeScale;
        while (Mathf.Abs(Time.timeScale - slowMotionScale) > 0.01f)
        {
            float t = Time.timeScale;
            Time.timeScale = Mathf.Lerp(Time.timeScale, slowMotionScale, transitionSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            // Плавно изменяем pitch звуков
            UpdateAudioPitches(Time.timeScale);
            
            yield return null;
        }
        
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        UpdateAudioPitches(slowMotionScale);
        
        // Ждём указанное время (в реальных секундах)
        yield return new WaitForSecondsRealtime(slowMotionDuration);
        
        // Плавный возврат к нормальной скорости
        while (Mathf.Abs(Time.timeScale - 1f) > 0.01f)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, transitionSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            // Плавно восстанавливаем pitch звуков
            UpdateAudioPitches(Time.timeScale);
            
            yield return null;
        }
        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Полностью восстанавливаем оригинальные pitch
        RestoreOriginalPitches();
        
        isSlowMotionActive = false;
        
        // Уничтожаем объект если нужно
        if (destroyAfterUse)
        {
            Destroy(gameObject);
        }
    }
    
    private void SaveOriginalPitches()
    {
        originalPitches.Clear();
        
        if (affectAllAudioSources)
        {
            // Находим все AudioSource в сцене
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
            
            foreach (AudioSource audioSource in allAudioSources)
            {
                // Пропускаем исключённые источники
                if (excludedAudioSources.Contains(audioSource))
                    continue;
                
                // Сохраняем оригинальный pitch
                if (!originalPitches.ContainsKey(audioSource))
                {
                    originalPitches[audioSource] = audioSource.pitch;
                }
            }
        }
    }
    
    private void UpdateAudioPitches(float timeScale)
    {
        // Вычисляем целевой pitch на основе скорости времени
        float targetPitchMultiplier = Mathf.Max(minPitch, timeScale);
        
        foreach (var kvp in originalPitches)
        {
            AudioSource audioSource = kvp.Key;
            float originalPitch = kvp.Value;
            
            if (audioSource != null)
            {
                // Применяем новый pitch относительно оригинального
                audioSource.pitch = originalPitch * targetPitchMultiplier;
            }
        }
    }
    
    private void RestoreOriginalPitches()
    {
        foreach (var kvp in originalPitches)
        {
            AudioSource audioSource = kvp.Key;
            float originalPitch = kvp.Value;
            
            if (audioSource != null)
            {
                audioSource.pitch = originalPitch;
            }
        }
        
        originalPitches.Clear();
    }
    
    private void OnDestroy()
    {
        // Восстанавливаем нормальное время и звуки при уничтожении объекта
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        RestoreOriginalPitches();
    }
    
    private void OnDisable()
    {
        // Восстанавливаем при отключении компонента
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            RestoreOriginalPitches();
            isSlowMotionActive = false;
        }
    }
}