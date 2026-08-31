using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FLYCAMERA : MonoBehaviour
{
    public float speed = 10.0f; // Скорость движения
    public float sensitivity = 2.0f; // Чувствительность мыши

    private float rotationX = 0.0f;
    private float rotationY = 0.0f;

    private bool isFrozen = false; // Флаг заморозки камеры

    void Update()
    {
        // Переключаем заморозку при нажатии F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            isFrozen = !isFrozen;
            Debug.Log("Camera frozen: " + isFrozen);
        }

        // Если управление заблокировано — выход из Update
        if (isFrozen) return;

        // Управление движением (WASD)
        float moveX = Input.GetAxis("Horizontal"); // A, D
        float moveZ = Input.GetAxis("Vertical");   // W, S

        Vector3 movement = new Vector3(moveX, 0.0f, moveZ);
        movement = transform.TransformDirection(movement);
        transform.position += movement * speed * Time.deltaTime;

        // Управление поворотом камеры мышью
        rotationX += Input.GetAxis("Mouse X") * sensitivity;
        rotationY += Input.GetAxis("Mouse Y") * sensitivity;

        rotationY = Mathf.Clamp(rotationY, -90.0f, 90.0f);

        transform.localRotation = Quaternion.Euler(-rotationY, rotationX, 0);
    }
}