using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class Postprossesing_off : MonoBehaviour
{
    [Header("Player")]
    public GameObject playerObject;         // Перетащи сюда объект/префаб игрока

    [Header("Post Processing")]
    public PostProcessVolume postProcessVolume; // Если задан — используется он, иначе ищется в playerObject

    [Header("UI")]
    public Toggle postProcessingToggle;

    private const string SaveKey = "PostProcessingEnabled";

    void Start()
    {
        // 1. Если Volume не задан вручную — ищем в объекте игрока
        if (postProcessVolume == null && playerObject != null)
            postProcessVolume = playerObject.GetComponentInChildren<PostProcessVolume>();

        // 2. Финальный фолбэк — ищем любой Volume на сцене
        if (postProcessVolume == null)
            postProcessVolume = FindObjectOfType<PostProcessVolume>();

        if (postProcessingToggle != null && postProcessVolume != null)
        {
            bool saved = PlayerPrefs.GetInt(SaveKey, 1) == 1;
            postProcessVolume.enabled = saved;
            postProcessingToggle.isOn = saved;

            postProcessingToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    void OnToggleChanged(bool isOn)
    {
        if (postProcessVolume != null)
        {
            postProcessVolume.enabled = isOn;
            PlayerPrefs.SetInt(SaveKey, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}