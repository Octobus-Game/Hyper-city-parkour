using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rot : MonoBehaviour
{


    public float rotationSpeed = 100f;

    void Update()
    {
        // Вращение объекта по оси Y
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }















}

   




