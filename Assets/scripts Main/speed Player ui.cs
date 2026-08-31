using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private CharacterController targetObject;

    [Header("Settings")]
    [SerializeField] private string prefix = "Speed: ";
    [SerializeField] private string suffix = " m/s";
    [SerializeField] private int decimalPlaces = 1;

    private void Update()
    {
        if (targetObject != null && speedText != null)
        {
            // Получаем скорость объекта
            float speed = targetObject.velocity.magnitude;

            // Форматируем и отображаем скорость
            speedText.text = prefix + speed.ToString("F" + decimalPlaces) + suffix;
        }
    }
}