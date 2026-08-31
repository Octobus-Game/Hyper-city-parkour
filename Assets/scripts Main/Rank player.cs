using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TIMERSECOND : MonoBehaviour
{
    public Text timerText; // Ссылка на UI-элемент для отображения времени
    private float elapsedTime; // Прошедшее время в секундах
    private bool isRunning = true; // Флаг, идет ли таймер
    [Header("Rank Images")]
    public Image sRankImage;
    public Image aRankImage;
    public Image bRankImage;
    public Image cRankImage;
    public Image dRankImage;
    private enum RankStage { None, S, A, B, C, D }
    private RankStage currentStage = RankStage.None;
    void Start()
    {
        elapsedTime = 0f;
        // Показать только S-rank в начале
        ShowOnlyImage(sRankImage);
        currentStage = RankStage.S;
    }
    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText();
            UpdateRankImage();
        }
    }
    void UpdateTimerText()
    {
        int hours = Mathf.FloorToInt(elapsedTime / 3600);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 1000) % 1000);
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}:{3:000}", hours, minutes, seconds, milliseconds);
    }
    void UpdateRankImage()
    {
        if (elapsedTime >= 300 && currentStage != RankStage.D)
        {
            ShowOnlyImage(dRankImage);
            currentStage = RankStage.D;
        }
        else if (elapsedTime >= 180 && currentStage != RankStage.C)
        {
            ShowOnlyImage(cRankImage);
            currentStage = RankStage.C;
        }
        else if (elapsedTime >= 120 && currentStage != RankStage.B)
        {
            ShowOnlyImage(bRankImage);
            currentStage = RankStage.B;
        }
        else if (elapsedTime >= 60 && currentStage != RankStage.A)
        {
            ShowOnlyImage(aRankImage);
            currentStage = RankStage.A;
        }
    }
    void ShowOnlyImage(Image imageToShow)
    {
        sRankImage.gameObject.SetActive(imageToShow == sRankImage);
        aRankImage.gameObject.SetActive(imageToShow == aRankImage);
        bRankImage.gameObject.SetActive(imageToShow == bRankImage);
        cRankImage.gameObject.SetActive(imageToShow == cRankImage);
        dRankImage.gameObject.SetActive(imageToShow == dRankImage);
    }
    // Методы для управления таймером
    public void StartTimer()
    {
        isRunning = true;
    }
    public void StopTimer()
    {
        isRunning = false;
    }
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
        ShowOnlyImage(sRankImage);
        currentStage = RankStage.S;
    }
}