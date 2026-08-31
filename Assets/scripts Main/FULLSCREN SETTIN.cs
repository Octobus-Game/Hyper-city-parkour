using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FULLSCRENSETTIN : MonoBehaviour
{
    public Toggle fullscreenToggle;

    void Start()
    {
        // Устанавливаем текущее состояние toggle в зависимости от режима экрана
        fullscreenToggle.isOn = Screen.fullScreen;

        // Добавляем слушатель на изменение состояния toggle
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    void OnDestroy()
    {
        // Убираем слушатель при уничтожении объекта
        fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
    }
}
