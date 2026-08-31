using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    [Header("Настройки активации")]
    [Tooltip("Клавиша для включения/выключения объекта")]
    public KeyCode activationKey = KeyCode.E; // Клавиша по умолчанию — 'E'

    [Tooltip("Объект, которым мы хотим управлять")]
    public GameObject objectToActivate;

    void Update()
    {
        // Проверяем, была ли нажата назначенная клавиша
        if (Input.GetKeyDown(activationKey))
        {
            // Убеждаемся, что объект назначен в инспекторе, чтобы избежать ошибок (NullReferenceException)
            if (objectToActivate != null)
            {
                // Узнаем текущее состояние объекта (включен он или выключен)
                bool isCurrentlyActive = objectToActivate.activeSelf;
                
                // Переключаем на противоположное состояние
                objectToActivate.SetActive(!isCurrentlyActive);
            }
            else
            {
                Debug.LogWarning("Объект для управления не назначен в скрипте ObjectActivator!");
            }
        }
    }
}