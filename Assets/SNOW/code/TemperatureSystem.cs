using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ImageFillTimer : MonoBehaviour
{
    [Header("UI Components")]
    public Image fireImage;
    public Image coldImage;

    [Header("Fill Settings")]
    public float fillDuration = 2f;
    public float resetDuration = 1f;

    [Header("Timer Settings")]
    public float timerDuration = 20f;

    private bool isPlayerInTrigger = false;
    private bool isTimerRunning = false;
    private Coroutine fireCoroutine;
    private Coroutine coldCoroutine;

    void Start()
    {
        // Инициализация шкалы огня
        if (fireImage != null)
        {
            fireImage.type = Image.Type.Filled;
            fireImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("Fire Image not found!");
        }

        // Инициализация шкалы холода
        if (coldImage != null)
        {
            coldImage.type = Image.Type.Filled;
            coldImage.fillAmount = 1f; // Начинается заполненной
        }
        else
        {
            Debug.LogError("Cold Image not found!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это игрок (можно добавить проверку по тегу)
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            StartFillingFire();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            StartResetFire();
        }
    }

    private void StartFillingFire()
    {
        // Останавливаем предыдущие корутины если они есть
        if (fireCoroutine != null) StopCoroutine(fireCoroutine);
        if (coldCoroutine != null) StopCoroutine(coldCoroutine);

        // Запускаем заполнение огня и опустошение холода
        fireCoroutine = StartCoroutine(FillFireImage());
        coldCoroutine = StartCoroutine(EmptyColdImage());
    }

    private void StartResetFire()
    {
        // Останавливаем заполнение
        if (fireCoroutine != null) StopCoroutine(fireCoroutine);
        if (coldCoroutine != null) StopCoroutine(coldCoroutine);

        // Запускаем сброс огня
        fireCoroutine = StartCoroutine(ResetFireImage());
    }

    private IEnumerator FillFireImage()
    {
        float elapsedTime = 0f;
        float startFillAmount = fireImage.fillAmount;

        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fillDuration;
            fireImage.fillAmount = Mathf.Lerp(startFillAmount, 1f, progress);
            yield return null;
        }

        fireImage.fillAmount = 1f;
        Debug.Log("Fire шкала заполнена");

        // Запускаем таймер после полного заполнения
        StartTimer();
    }

    private IEnumerator EmptyColdImage()
    {
        float elapsedTime = 0f;
        float startFillAmount = coldImage.fillAmount;

        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fillDuration;
            coldImage.fillAmount = Mathf.Lerp(startFillAmount, 0f, progress);
            yield return null;
        }

        coldImage.fillAmount = 0f;
        Debug.Log("Cold шкала опустошена");
    }

    private IEnumerator ResetFireImage()
    {
        float elapsedTime = 0f;
        float startFillAmount = fireImage.fillAmount;

        Debug.Log("Сброс Fire шкалы...");

        while (elapsedTime < resetDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / resetDuration;
            fireImage.fillAmount = Mathf.Lerp(startFillAmount, 0f, progress);
            yield return null;
        }

        fireImage.fillAmount = 0f;
        Debug.Log("Fire шкала сброшена");
    }

    private void StartTimer()
    {
        if (!isTimerRunning)
        {
            StartCoroutine(CountdownTimer());
        }
    }

    private IEnumerator CountdownTimer()
    {
        isTimerRunning = true;
        Debug.Log("Таймер запущен на " + timerDuration + " секунд");

        float remainingTime = timerDuration;
        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        RestartScene();
        isTimerRunning = false;
    }

    private void RestartScene()
    {
        Debug.Log("Перезагрузка сцены...");
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void ForceRestart()
    {
        RestartScene();
    }

    // Метод для ручного сброса обеих шкал
    public void ResetAllScales()
    {
        StopAllCoroutines();
        isTimerRunning = false;
        isPlayerInTrigger = false;

        if (fireImage != null) fireImage.fillAmount = 0f;
        if (coldImage != null) coldImage.fillAmount = 1f;

        Debug.Log("Все шкалы сброшены");
    }
}