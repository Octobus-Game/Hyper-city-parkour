using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CAMERA12 : MonoBehaviour
{
    public Camera mainCamera; // Основная камера
    public Camera secondaryCamera; // Вторая камера
    public Button switchButton; // Кнопка для переключения камер

    void Start()
    {
        // Убедитесь, что только одна камера активна в начале
        mainCamera.gameObject.SetActive(true);
        secondaryCamera.gameObject.SetActive(false);

        // Назначьте метод для нажатия кнопки
        switchButton.onClick.AddListener(SwitchCamera);
    }

    void SwitchCamera()
    {
        // Переключаем активную камеру
        bool isMainCameraActive = mainCamera.gameObject.activeSelf;
        mainCamera.gameObject.SetActive(!isMainCameraActive);
        secondaryCamera.gameObject.SetActive(isMainCameraActive);
    }
}
