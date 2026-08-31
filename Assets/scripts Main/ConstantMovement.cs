using System.Collections;
using System.Collections.Generic;
using UnityEngine;



   

public class ConstantMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        // Двигаем объект вперед по его локальной оси
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
}


