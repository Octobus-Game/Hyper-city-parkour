using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class OBCAMERAt : MonoBehaviour
{
    // Объект, который нужно показать
    public GameObject objectToShow;

    // Камера, которая будет следить за объектом
    public Camera objectCamera;

    // UI-кнопка
    public Button showObjectButton;

    void Start()
    {
        // Убедитесь, что объект и камера скрыты при старте
        objectToShow.SetActive(false);
        objectCamera.enabled = false;

        // Добавляем слушатель для нажатия кнопки
        showObjectButton.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        // Показать объект и активировать камеру
        objectToShow.SetActive(true);
        objectCamera.enabled = true;

        // Камера будет следить за объектом
        objectCamera.transform.position = objectToShow.transform.position + new Vector3(0, 5, -10);
        objectCamera.transform.LookAt(objectToShow.transform);
    }
}
