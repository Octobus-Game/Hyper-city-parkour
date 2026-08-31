using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
public class ColorSpaceEffect : PostProcessEffectSettings
{
    [Range(0f, 1f)]
    public FloatParameter gammaCorrection = new FloatParameter { value = 1f };

    [Range(0.1f, 3f)]
    public FloatParameter gamma = new FloatParameter { value = 2.2f };

    public BoolParameter simulateLinear = new BoolParameter { value = false };
}

public class ColorSpaceEffectRenderer : PostProcessEffectRenderer<ColorSpaceEffect>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/ColorSpaceEffect"));
        sheet.properties.SetFloat("_Gamma", settings.gamma);
        sheet.properties.SetFloat("_GammaCorrection", settings.gammaCorrection);
        sheet.properties.SetFloat("_SimulateLinear", settings.simulateLinear ? 1f : 0f);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}

public class RuntimeColorSpaceSwitcher : MonoBehaviour
{
    [Header("Post Process Volume")]
    public PostProcessVolume postProcessVolume;

    [Header("Color Space Settings")]
    [Range(0f, 1f)]
    public float gammaCorrection = 1f;

    [Range(0.1f, 3f)]
    public float gamma = 2.2f;

    public bool simulateLinearSpace = false;

    private ColorSpaceEffect colorSpaceEffect;

    void Start()
    {
        // Найти или создать Post Process Volume
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<PostProcessVolume>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            // Добавить эффект если его нет
            if (!postProcessVolume.profile.TryGetSettings(out colorSpaceEffect))
            {
                colorSpaceEffect = postProcessVolume.profile.AddSettings<ColorSpaceEffect>();
            }
        }

        UpdateColorSpaceEffect();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), $"Actual Color Space: {QualitySettings.activeColorSpace}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Simulating Linear: {simulateLinearSpace}");

        if (GUI.Button(new Rect(10, 60, 200, 30), "Toggle Linear Simulation"))
        {
            ToggleLinearSimulation();
        }

        if (GUI.Button(new Rect(10, 100, 200, 30), "Reset to Default"))
        {
            ResetToDefault();
        }

        // Слайдер для gamma коррекции
        GUI.Label(new Rect(10, 140, 100, 20), "Gamma:");
        gamma = GUI.HorizontalSlider(new Rect(110, 145, 200, 20), gamma, 0.1f, 3f);
        GUI.Label(new Rect(320, 140, 50, 20), gamma.ToString("F2"));

        // Обновить эффект при изменении параметров
        if (GUI.changed)
        {
            UpdateColorSpaceEffect();
        }
    }

    /// <summary>
    /// Переключить симуляцию Linear Color Space
    /// </summary>
    public void ToggleLinearSimulation()
    {
        simulateLinearSpace = !simulateLinearSpace;
        UpdateColorSpaceEffect();
        Debug.Log($"Linear simulation: {simulateLinearSpace}");
    }

    /// <summary>
    /// Сбросить к настройкам по умолчанию
    /// </summary>
    public void ResetToDefault()
    {
        simulateLinearSpace = false;
        gamma = 2.2f;
        gammaCorrection = 1f;
        UpdateColorSpaceEffect();
    }

    /// <summary>
    /// Обновить Post Process эффект
    /// </summary>
    private void UpdateColorSpaceEffect()
    {
        if (colorSpaceEffect != null)
        {
            colorSpaceEffect.simulateLinear.value = simulateLinearSpace;
            colorSpaceEffect.gamma.value = gamma;
            colorSpaceEffect.gammaCorrection.value = gammaCorrection;
        }
    }

    /// <summary>
    /// Применить эффект Linear Color Space
    /// </summary>
    public void ApplyLinearEffect()
    {
        simulateLinearSpace = true;
        gamma = 2.2f;
        gammaCorrection = 0.45f; // 1/2.2
        UpdateColorSpaceEffect();
    }

    /// <summary>
    /// Применить эффект Gamma Color Space
    /// </summary>
    public void ApplyGammaEffect()
    {
        simulateLinearSpace = false;
        gamma = 1f;
        gammaCorrection = 1f;
        UpdateColorSpaceEffect();
    }
}