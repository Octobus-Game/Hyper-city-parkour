// Far Camera Slider.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Farcameraslideer : MonoBehaviour
{
    public Camera targetCamera;
    public Slider farSlider;
    public TextMeshProUGUI farText;

    public float minFar = 50f;
    public float maxFar = 1000f;

    private const string SaveKey = "CameraFarClipPlane";

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (farSlider != null)
        {
            farSlider.minValue = minFar;
            farSlider.maxValue = maxFar;

            //                               ,                        farClipPlane       
            float saved = PlayerPrefs.GetFloat(SaveKey, targetCamera.farClipPlane);
            saved = Mathf.Clamp(saved, minFar, maxFar); //                                     

            targetCamera.farClipPlane = saved;
            farSlider.value = saved;

            farSlider.onValueChanged.AddListener(OnFarChanged);
            UpdateFarText(saved);
        }
    }

    void OnFarChanged(float value)
    {
        if (targetCamera != null)
        {
            targetCamera.farClipPlane = value;
            PlayerPrefs.SetFloat(SaveKey, value);
            PlayerPrefs.Save();
        }

        UpdateFarText(value);
    }

    void UpdateFarText(float value)
    {
        if (farText != null)
            farText.text = " " + Mathf.RoundToInt(value).ToString();
    }
}