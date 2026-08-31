using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class _FpsLimitSlider : MonoBehaviour
{
    [Header("UI References")]
    public Slider fpsSlider;
    public TextMeshProUGUI fpsText;

    [Header("Settings")]
    public float minFPS = 1f;
    public float maxFPS = 360f;        // Верхняя граница слайдера
    public float unlockThreshold = 355f; // Выше этого значения = безлимитный FPS

    private const string SaveKey = "TargetFPS";

    void Start()
    {
        if (fpsSlider != null)
        {
            fpsSlider.minValue = minFPS;
            fpsSlider.maxValue = maxFPS;

            float saved = PlayerPrefs.GetFloat(SaveKey, 60f);
            fpsSlider.value = saved;
            ApplyFPS(saved);

            fpsSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void OnSliderChanged(float value)
    {
        ApplyFPS(value);
        PlayerPrefs.SetFloat(SaveKey, value);
        PlayerPrefs.Save();
    }

    void ApplyFPS(float value)
    {
        if (value >= unlockThreshold)
        {
            Application.targetFrameRate = -1; // Безлимитный
            UpdateText("∞");
        }
        else
        {
            int fps = Mathf.RoundToInt(value);
            Application.targetFrameRate = fps;
            UpdateText(fps.ToString());
        }
    }

    void UpdateText(string value)
    {
        if (fpsText != null)
            fpsText.text = value + " FPS";
    }
}