using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skyboxdark : MonoBehaviour
{
    public Material skyboxMaterial;
    public float darkenSpeed = 0.1f; // Скорость затемнения
    private float targetExposure = 0.5f; // Минимальное значение экспозиции

    void Update()
    {
        if (skyboxMaterial != null)
        {
            float currentExposure = skyboxMaterial.GetFloat("_Exposure");
            float newExposure = Mathf.Lerp(currentExposure, targetExposure, Time.deltaTime * darkenSpeed);
            skyboxMaterial.SetFloat("_Exposure", newExposure);
        }
    }
}

