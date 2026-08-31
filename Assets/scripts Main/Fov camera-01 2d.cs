using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fovcamera_01_2d : MonoBehaviour
{
    [Header("FOV Settings")]
    [SerializeField] private float targetFOV = 90f;
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float lerpSpeed = 2f;

    private const string SaveKey = "CameraOrthographicSize";

    private Camera _cam;
    private Coroutine _fovCoroutine;

    private void Awake()
    {
        _cam = Camera.main;
        LoadFOV(); // Загружаем сохранённое значение при старте сцены
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StartFovTransition(targetFOV);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StartFovTransition(defaultFOV);
    }

    private void StartFovTransition(float newFOV)
    {
        if (_fovCoroutine != null)
            StopCoroutine(_fovCoroutine);
        _fovCoroutine = StartCoroutine(LerpFOV(newFOV));
    }

    private IEnumerator LerpFOV(float targetValue)
    {
        while (!Mathf.Approximately(_cam.orthographicSize, targetValue))
        {
            _cam.orthographicSize = Mathf.Lerp(
                _cam.orthographicSize,
                targetValue,
                Time.deltaTime * lerpSpeed
            );
            yield return null;
        }

        _cam.orthographicSize = targetValue;
        SaveFOV(); // Сохраняем после завершения перехода
    }

    private void SaveFOV()
    {
        PlayerPrefs.SetFloat(SaveKey, _cam.orthographicSize);
        PlayerPrefs.Save(); // Принудительная запись на диск
    }

    private void LoadFOV()
    {
        // Если сохранения нет — используем defaultFOV
        float saved = PlayerPrefs.GetFloat(SaveKey, defaultFOV);
        _cam.orthographicSize = saved;
    }

    // Вызови вручную если нужно сбросить сохранение
    public void ResetFOV()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        _cam.orthographicSize = defaultFOV;
    }
}