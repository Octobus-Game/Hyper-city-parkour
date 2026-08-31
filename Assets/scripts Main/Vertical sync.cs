using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Verticalsync : MonoBehaviour
{
    public Toggle vsyncToggle;

    void Start()
    {
        if (vsyncToggle != null)
        {
            // Устанавливаем начальное значение тумблера на основе текущей настройки VSync
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    void OnToggleChanged(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
        Debug.Log("VSync: " + (isOn ? "Включен" : "Выключен"));
    }
}
