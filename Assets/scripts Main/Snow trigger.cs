using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Snowtrigger : MonoBehaviour
{
    public float countdownTime = 120f; // Время таймера в секундах (2 минуты)
    public Text countdownText;         // UI Text (опционально, если хочешь видеть таймер)
    private bool timerStarted = false;

    void OnTriggerEnter(Collider other)
    {
        if (!timerStarted && other.CompareTag("Player"))
        {
            timerStarted = true;
            StartCoroutine(StartCountdown());
        }
    }

    IEnumerator StartCountdown()
    {
        float timeLeft = countdownTime;

        while (timeLeft > 0)
        {
            if (countdownText != null)
            {
                int minutes = Mathf.FloorToInt(timeLeft / 60);
                int seconds = Mathf.FloorToInt(timeLeft % 60);
                countdownText.text = $"{minutes:00}:{seconds:00}";
            }

            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        // Перезапуск сцены
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}



