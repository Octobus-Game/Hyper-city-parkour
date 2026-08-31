using UnityEngine;
using UnityEngine.UI;

public class VsyncToggle : MonoBehaviour
{
    [SerializeField] private Toggle vsyncToggle;

    private const string SaveKey = "Vsync";

    void Start()
    {
        bool saved = PlayerPrefs.GetInt(SaveKey, 1) == 1;

        QualitySettings.vSyncCount = saved ? 1 : 0;
        vsyncToggle.isOn = saved;

        vsyncToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;

        PlayerPrefs.SetInt(SaveKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}