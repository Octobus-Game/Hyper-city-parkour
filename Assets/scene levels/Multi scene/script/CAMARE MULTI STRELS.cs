    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLEVEL8 : MonoBehaviour
{
    public Camera camera1; // Первая камера
    public Camera camera2; // Вторая камера

    void Start()
    {
        // Включаем первую камеру по умолчанию, выключаем вторую
        camera1.enabled = true;
        camera2.enabled = false;
    }

    void Update()
    {
        // Если нажата клавиша 1, включаем первую камеру и выключаем вторую
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            SwitchToCamera1();
        }

        // Если нажата клавиша 2, включаем вторую камеру и выключаем первую
        if (Input.GetKeyDown(KeyCode.Mouse3))
        {
            SwitchToCamera2();
        }
    }

    void SwitchToCamera1()
    {
        camera1.enabled = true;
        camera2.enabled = false;
    }

    void SwitchToCamera2()
    {
        camera1.enabled = false;
        camera2.enabled = true;
    }
}
