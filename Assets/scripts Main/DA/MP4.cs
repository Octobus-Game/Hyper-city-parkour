using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoOnClick : MonoBehaviour
{
    private Material originalMaterial;
    private VideoPlayer videoPlayer;
    private bool isVideoPlaying = false;

    public VideoClip videoClip; // Видео клип, который вы хотите проиграть

    private void Start()
    {
        // Получаем VideoPlayer и сохраняем оригинальный материал объекта
        videoPlayer = GetComponent<VideoPlayer>();
        originalMaterial = GetComponent<Renderer>().material;

        // Настраиваем VideoPlayer
        videoPlayer.clip = videoClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;

        // Создаем RenderTexture для отображения видео
        RenderTexture renderTexture = new RenderTexture(256, 256, 0);
        videoPlayer.targetTexture = renderTexture;

        // Назначаем RenderTexture в качестве материала
        GetComponent<Renderer>().material.mainTexture = renderTexture;
    }

    private void OnMouseDown()
    {
        if (isVideoPlaying)
        {
            // Останавливаем видео и возвращаем оригинальный материал
            videoPlayer.Stop();
            GetComponent<Renderer>().material = originalMaterial;
            isVideoPlaying = false;
        }
        else
        {
            // Воспроизводим видео и заменяем материал
            videoPlayer.Play();
            isVideoPlaying = true;
        }
    }
}


