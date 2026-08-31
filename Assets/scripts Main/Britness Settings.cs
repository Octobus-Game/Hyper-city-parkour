using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class _BrightnessSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessText;

    [Header("Post Processing")]
    [SerializeField] private PostProcessVolume postProcessVolume;

    [Header("Settings")]
    [SerializeField] private float minExposure = -3f;
    [SerializeField] private float maxExposure = 3f;

    private AutoExposure _autoExposure;
    private const string SaveKey = "ExposureCompensation";

    void Start()
    {
        // Достаём AutoExposure из Volume
        if (postProcessVolume != null)
            postProcessVolume.profile.TryGetSettings(out _autoExposure);

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = minExposure;
            brightnessSlider.maxValue = maxExposure;

            float saved = PlayerPrefs.GetFloat(SaveKey, 0f);
            brightnessSlider.value = saved;
            ApplyExposure(saved);

            brightnessSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void OnSliderChanged(float value)
    {
        ApplyExposure(value);
        PlayerPrefs.SetFloat(SaveKey, value);
        PlayerPrefs.Save();
    }

    void ApplyExposure(float value)
    {
        if (_autoExposure != null)
            _autoExposure.keyValue.value = value;

        UpdateText(value);
    }

    void UpdateText(float value)
    {
        if (brightnessText != null)
            brightnessText.text = (value >= 0 ? "+" : "") + value.ToString("F1");
    }
}