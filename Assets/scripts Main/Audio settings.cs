using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Music")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TextMeshProUGUI musicText;

    [Header("Sound")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI soundText;

    // PlayerPrefs keys
    private const string SaveKeyMusic = "MusicVolume";
    private const string SaveKeySound = "SoundVolume";

    // Exposed parameter names in AudioMixer (должны совпадать с Mixer)
    private const string MixerMusicParam = "MusicVolume";
    private const string MixerSoundParam = "SoundVolume";

    void Start()
    {
        SetupSlider(musicSlider, SaveKeyMusic, ApplyMusic);
        SetupSlider(soundSlider, SaveKeySound, ApplySound);
    }

    void SetupSlider(Slider slider, string saveKey, System.Action<float> applyFunc)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;

        float saved = PlayerPrefs.GetFloat(saveKey, 1f);
        slider.value = saved;
        applyFunc(saved);

        slider.onValueChanged.AddListener(value =>
        {
            applyFunc(value);
            PlayerPrefs.SetFloat(saveKey, value);
            PlayerPrefs.Save();
        });
    }

    void ApplyMusic(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MixerMusicParam, LinearToDecibel(value));

        UpdateText(musicText, value);
    }

    void ApplySound(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MixerSoundParam, LinearToDecibel(value));

        UpdateText(soundText, value);
    }

    // AudioMixer работает в децибелах, а слайдер — 0..1
    // Без этой конвертации громкость будет вести себя нелинейно на слух
    static float LinearToDecibel(float linear)
    {
        // Clamp в 0.0001 чтобы не было log(0) = -infinity
        return Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
    }

    void UpdateText(TextMeshProUGUI tmp, float value)
    {
        if (tmp != null)
            tmp.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}