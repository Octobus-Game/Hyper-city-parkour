using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Screen_Events_mode_01 : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] public GameObject[] activeObjects; // Массив объектов для активации
    [SerializeField] public TextMeshProUGUI infoText; // TextMeshPro для отображения информации
    [SerializeField] public float activationInterval = 20f; // Интервал активации (20 секунд)
    [SerializeField] public float activeDuration = 30f; // Длительность активности объекта (30 секунд)

    public int currentIndex = 0;
    public List<Coroutine> activeCoroutines = new List<Coroutine>();

    void Start()
    {
        // Деактивируем все объекты в начале
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Запускаем корутину активации
        StartCoroutine(ActivationLoop());
    }

    IEnumerator ActivationLoop()
    {
        while (true)
        {
            if (activeObjects.Length > 0 && activeObjects[currentIndex] != null)
            {
                // Активируем текущий объект
                GameObject objToActivate = activeObjects[currentIndex];
                objToActivate.SetActive(true);

                // Обновляем текст
                UpdateInfoText(objToActivate.name);

                // Запускаем корутину деактивации через 30 секунд
                StartCoroutine(DeactivateAfterDelay(objToActivate, activeDuration));

                // Переходим к следующему объекту
                currentIndex = (currentIndex + 1) % activeObjects.Length;
            }

            // Ждем 20 секунд до следующей активации
            yield return new WaitForSeconds(activationInterval);
        }
    }

    IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    void UpdateInfoText(string objectName)
    {
        if (infoText != null)
        {
            infoText.text = $"Активирован: {objectName}\nВремя активности: {activeDuration} сек";
        }
    }
}
