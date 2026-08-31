using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

public class WarmZoneTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Image coldBar;

    [Header("Cold Settings")]
    [SerializeField] private float decreaseSpeed = 0.5f; // Скорость снижения холода (в зоне тепла)
    [SerializeField] private float increaseSpeed = 0.3f; // Скорость повышения холода (вне зоны)
    [SerializeField] private int timerAmount = 20000;
    private bool isPlayerInside = false;

    private void Start()
    {
        // Устанавливаем начальное значение шкалы
        if (coldBar != null)
        {
            coldBar.fillAmount = 0f;
            
        }
    }

    private void Update()
    {
        if (coldBar == null) return;

        if (isPlayerInside)
        {
            // Игрок внутри триггера - уменьшаем холод
            coldBar.fillAmount = Mathf.MoveTowards(coldBar.fillAmount, 0f, decreaseSpeed * Time.deltaTime);
        }
        else
        {
            // Игрок вне триггера - увеличиваем холод
            coldBar.fillAmount = Mathf.MoveTowards(coldBar.fillAmount, 1f, increaseSpeed * Time.deltaTime);
        }

        if (coldBar.fillAmount >= 1f)
        {
            Thread.Sleep(timerAmount);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что вошедший объект - игрок
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Проверяем, что вышедший объект - игрок
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}