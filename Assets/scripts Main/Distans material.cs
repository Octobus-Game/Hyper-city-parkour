using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Distansmaterial : MonoBehaviour
{
    public Transform player;                // Игрок (камера или персонаж)
    public Renderer[] targetObjects;        // Объекты, у которых будет меняться материал
    public Material nearMaterial;           // Материал, когда игрок рядом
    public Material farMaterial;            // Материал, когда игрок далеко
    public float switchDistance = 10f;      // Расстояние, на котором происходит переключение

    void Update()
    {
        if (player == null || targetObjects == null || nearMaterial == null || farMaterial == null)
            return;

        foreach (Renderer obj in targetObjects)
        {
            float distance = Vector3.Distance(player.position, obj.transform.position);
            Material currentMaterial = distance > switchDistance ? farMaterial : nearMaterial;

            if (obj.sharedMaterial != currentMaterial)
                obj.sharedMaterial = currentMaterial;
        }
    }
}
