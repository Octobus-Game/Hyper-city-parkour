using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScreenSettings : MonoBehaviour
{
    [Header("Screen Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Resolution[] resolutions;

    private const string SaveKeyResolution = "ResolutionIndex";

    void Start()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // Загружаем сохранённый индекс, по умолчанию — текущее разрешение экрана
        int saved = PlayerPrefs.GetInt(SaveKeyResolution, currentResolutionIndex);
        saved = Mathf.Clamp(saved, 0, resolutions.Length - 1);

        resolutionDropdown.value = saved;
        resolutionDropdown.RefreshShownValue();
        SetResolution(saved);

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        PlayerPrefs.SetInt(SaveKeyResolution, resolutionIndex);
        PlayerPrefs.Save();
    }
}