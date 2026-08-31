using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    [Header("Compass Settings")]
    [Tooltip("UI Image элемент, который будет вращаться как стрелка компаса")]
    public Image compassImage;

    [Tooltip("Объект, на который будет указывать компас")]
    public Transform targetObject;

    [Tooltip("Скорость вращения компаса (0 = мгновенно)")]
    [Range(0f, 20f)]
    public float rotationSpeed = 5f;

    [Tooltip("Камера игрока (если не указана, будет использована Camera.main)")]
    public Camera playerCamera;

    [Header("Direction Check Settings")]
    [Tooltip("Допустимый угол отклонения для правильного направления (в градусах)")]
    [Range(1f, 45f)]
    public float angleThreshold = 10f;

    [Tooltip("Цвет при правильном направлении")]
    public Color correctColor = Color.green;

    [Tooltip("Цвет при неправильном направлении")]
    public Color incorrectColor = Color.red;

    [Tooltip("Обычный цвет компаса")]
    public Color normalColor = Color.white;

    private float correctDirectionTimer = 0f;
    private float incorrectDirectionTimer = 0f;
    private bool wasCorrect = false;
    private bool wasIncorrect = false;

    private void Start()
    {
        // Если камера не указана, используем главную камеру
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Проверка на наличие необходимых компонентов
        if (compassImage == null)
        {
            Debug.LogError("Compass Image не указан в инспекторе!");
        }

        if (targetObject == null)
        {
            Debug.LogError("Target Object не указан в инспекторе!");
        }
    }

    private void Update()
    {
        if (compassImage == null || targetObject == null || playerCamera == null)
            return;

        UpdateCompass();
        CheckDirection();
        UpdateColor();
    }

    private void UpdateCompass()
    {
        // Получаем направление от камеры к целевому объекту
        Vector3 direction = targetObject.position - playerCamera.transform.position;

        // Убираем компонент Y для расчета только в горизонтальной плоскости
        direction.y = 0;

        // Нормализуем вектор
        direction.Normalize();

        // Получаем forward направление камеры (также в горизонтальной плоскости)
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        // Вычисляем угол между направлением камеры и направлением к цели
        float angle = Vector3.SignedAngle(cameraForward, direction, Vector3.up);

        // Целевой угол поворота для стрелки компаса
        Quaternion targetRotation = Quaternion.Euler(0, 0, -angle);

        // Плавное вращение или мгновенное
        if (rotationSpeed > 0)
        {
            compassImage.rectTransform.rotation = Quaternion.Lerp(
                compassImage.rectTransform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
        else
        {
            compassImage.rectTransform.rotation = targetRotation;
        }
    }

    private void CheckDirection()
    {
        // Получаем направление от камеры к целевому объекту
        Vector3 direction = targetObject.position - playerCamera.transform.position;
        direction.y = 0;
        direction.Normalize();

        // Получаем forward направление камеры
        Vector3 cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        // Вычисляем угол
        float angle = Vector3.Angle(cameraForward, direction);

        // Проверяем, смотрим ли мы в правильном направлении
        if (angle <= angleThreshold)
        {
            // Правильное направление
            correctDirectionTimer += Time.deltaTime;
            incorrectDirectionTimer = 0f;
            wasIncorrect = false;

            // Если прошла 1 секунда правильного направления
            if (correctDirectionTimer >= 1f && !wasCorrect)
            {
                wasCorrect = true;
            }
        }
        else
        {
            // Неправильное направление
            incorrectDirectionTimer += Time.deltaTime;
            correctDirectionTimer = 0f;
            wasCorrect = false;

            // Если прошла 1 секунда неправильного направления
            if (incorrectDirectionTimer >= 1f && !wasIncorrect)
            {
                wasIncorrect = true;
            }
        }
    }

    private void UpdateColor()
    {
        if (wasCorrect && correctDirectionTimer >= 1f)
        {
            // Зеленый цвет при правильном направлении
            compassImage.color = correctColor;
        }
        else if (wasIncorrect && incorrectDirectionTimer >= 1f)
        {
            // Красный цвет при неправильном направлении
            compassImage.color = incorrectColor;
        }
        else
        {
            // Обычный цвет
            compassImage.color = normalColor;
        }
    }
}