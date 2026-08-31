using UnityEngine;
using System.Collections;

public class Fog_SKYBOX_ColorTrigger : MonoBehaviour
{
    [Header("Fog Settings")]
    [SerializeField] private Color newFogColor = Color.red;
    [SerializeField] private float transitionDuration = 1f;
    
    [Header("Skybox Settings")]
    [SerializeField] private bool changeSkybox = false;
    [SerializeField] private Material newSkybox;
    [SerializeField] private float skyboxFadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;
    
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;
    
    private Color originalFogColor;
    private Material originalSkybox;
    private bool isTriggered = false;
    private bool isTransitioning = false;
    private float transitionProgress = 0f;
    private Color currentColor;
    private Color targetColor;
    
    // Для fade эффекта
    private Texture2D fadeTexture;
    private float fadeAlpha = 0f;
    private bool isFading = false;

    void Start()
    {
        // Сохраняем оригинальный цвет тумана
        originalFogColor = RenderSettings.fogColor;
        
        // Сохраняем оригинальный skybox
        originalSkybox = RenderSettings.skybox;
        
        // Включаем туман если он выключен
        if (!RenderSettings.fog)
        {
            RenderSettings.fog = true;
        }
        
        // Создаем текстуру для fade эффекта
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем что это игрок
        if (other.CompareTag(playerTag))
        {
            // Если триггер уже сработал и включен режим одного срабатывания
            if (isTriggered && triggerOnce)
                return;

            // Начинаем переход к новому цвету
            StartColorTransition(newFogColor);
            
            // Начинаем переход к новому skybox
            if (changeSkybox && newSkybox != null)
            {
                StartCoroutine(FadeSkybox(newSkybox));
            }
            
            isTriggered = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Возвращаем оригинальный цвет и skybox когда игрок выходит из триггера
        if (other.CompareTag(playerTag) && !triggerOnce)
        {
            StartColorTransition(originalFogColor);
            
            if (changeSkybox && originalSkybox != null)
            {
                StartCoroutine(FadeSkybox(originalSkybox));
            }
        }
    }

    void Update()
    {
        // Плавный переход между цветами тумана
        if (isTransitioning)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            RenderSettings.fogColor = Color.Lerp(currentColor, targetColor, transitionProgress);

            if (transitionProgress >= 1f)
            {
                isTransitioning = false;
                transitionProgress = 0f;
            }
        }
    }
    
    void OnGUI()
    {
        // Отрисовываем fade эффект
        if (isFading && fadeAlpha > 0f)
        {
            Color color = fadeColor;
            color.a = fadeAlpha;
            GUI.color = color;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
        }
    }

    void StartColorTransition(Color newColor)
    {
        currentColor = RenderSettings.fogColor;
        targetColor = newColor;
        transitionProgress = 0f;
        isTransitioning = true;
    }
    
    IEnumerator FadeSkybox(Material targetSkybox)
    {
        if (isFading) yield break;
        
        isFading = true;
        float halfDuration = skyboxFadeDuration / 2f;
        
        // Fade out (затемнение)
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
            yield return null;
        }
        
        // Меняем skybox в момент полного затемнения
        RenderSettings.skybox = targetSkybox;
        DynamicGI.UpdateEnvironment();
        
        // Fade in (осветление)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
            yield return null;
        }
        
        fadeAlpha = 0f;
        isFading = false;
    }

    // Метод для изменения цвета из других скриптов
    public void SetFogColor(Color color)
    {
        StartColorTransition(color);
    }

    // Метод для изменения skybox из других скриптов
    public void SetSkybox(Material skyboxMaterial)
    {
        if (skyboxMaterial != null && changeSkybox)
        {
            StartCoroutine(FadeSkybox(skyboxMaterial));
        }
    }

    // Метод для сброса к оригинальному цвету
    public void ResetFogColor()
    {
        StartColorTransition(originalFogColor);
    }
    
    // Метод для сброса к оригинальному skybox
    public void ResetSkybox()
    {
        if (originalSkybox != null && changeSkybox)
        {
            StartCoroutine(FadeSkybox(originalSkybox));
        }
    }
    
    // Метод для сброса всего
    public void ResetAll()
    {
        ResetFogColor();
        ResetSkybox();
    }
    
    void OnDestroy()
    {
        // Очищаем текстуру
        if (fadeTexture != null)
        {
            Destroy(fadeTexture);
        }
    }
}