using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Camerasliderfov : MonoBehaviour
{
    public Camera targetCamera;
    public Slider fovSlider;
    public TextMeshProUGUI fovText;
    public float minFov = 30f;
    public float maxFov = 100f;

    private const string FOV_KEY = "CameraFOV";

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (fovSlider != null)
        {
            fovSlider.minValue = minFov;
            fovSlider.maxValue = maxFov;

            // Загружаем сохранённый FOV, или берём текущий камеры как дефолт
            float savedFov = PlayerPrefs.GetFloat(FOV_KEY, targetCamera.fieldOfView);
            savedFov = Mathf.Clamp(savedFov, minFov, maxFov);

            fovSlider.value = savedFov;
            targetCamera.fieldOfView = savedFov;

            fovSlider.onValueChanged.AddListener(OnFovChanged);
            UpdateFovText(savedFov);
        }
    }

    void OnFovChanged(float value)
    {
        if (targetCamera != null)
            targetCamera.fieldOfView = value;

        // Сохраняем новое значение
        PlayerPrefs.SetFloat(FOV_KEY, value);
        PlayerPrefs.Save();

        UpdateFovText(value);
    }

    void UpdateFovText(float value)
    {
        if (fovText != null)
            fovText.text = Mathf.RoundToInt(value).ToString();
    }
}