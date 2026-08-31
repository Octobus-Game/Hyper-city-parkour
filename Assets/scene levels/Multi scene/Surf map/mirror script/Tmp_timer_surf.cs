using System.Collections;
using TMPro;
using UnityEngine;

public class StopwatchUI : MonoBehaviour
{
    public static StopwatchUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startSound;

    private Coroutine _timerCoroutine;
    private float _elapsed;
    private bool _running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartTimer()
    {
        if (_running) return;
        _elapsed = 0f;
        _running = true;

        PlayStartSound();

        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        _timerCoroutine = StartCoroutine(TimerRoutine());
    }

    public void StopTimer()
    {
        _running = false;
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    public void ResetTimer()
    {
        StopTimer();
        _elapsed = 0f;
        UpdateDisplay();
    }

    private void PlayStartSound()
    {
        if (audioSource == null || startSound == null) return;
        audioSource.PlayOneShot(startSound);
    }

    private IEnumerator TimerRoutine()
    {
        while (_running)
        {
            _elapsed += Time.deltaTime;
            UpdateDisplay();
            yield return null;
        }
    }

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        int minutes  = (int)(_elapsed / 60f);
        int seconds  = (int)(_elapsed % 60f);
        int centisec = (int)((_elapsed % 1f) * 100f);

        timerText.text = $"{minutes:00}:{seconds:00}.{centisec:00}";
    }
}