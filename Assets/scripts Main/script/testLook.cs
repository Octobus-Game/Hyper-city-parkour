using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testLook : MonoBehaviour
{
    public enum RotationAxes
    {
        XandY, X, Y
    }

    [Header("Rotation Settings")]
    public RotationAxes _axes = RotationAxes.XandY;
    public float _rotationSpeedHor = 5.0f;
    public float _rotationSpeedVer = 5.0f;
    public float maxVert = 45.0f;
    public float minVert = -45.0f;

    [Header("Smoothing")]
    public bool enableSmoothing = true;
    public float smoothingSpeed = 5.0f;

    [Header("Sensitivity")]
    [Range(0.1f, 10.0f)]
    public float mouseSensitivity = 1.0f;

    [Header("Lock Cursor")]
    public bool lockCursor = true;
    public KeyCode unlockKey = KeyCode.Escape;

    [Header("Invert Controls")]
    public bool invertVertical = false;
    public bool invertHorizontal = false;

    [Header("Auto-Center")]
    public bool autoCenter = false;
    public float autoCenterSpeed = 2.0f;
    public float autoCenterDelay = 3.0f;

    private float _rotationX = 0;
    private Vector3 targetRotation;
    private Vector3 currentRotation;
    private float lastInputTime;
    private bool isAutoCentering = false;

    private void Start()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.freezeRotation = true;

        // Инициализация текущего поворота
        currentRotation = transform.localEulerAngles;
        targetRotation = currentRotation;

        // Блокировка курсора
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleCursorLock();
        HandleMouseLook();
        HandleAutoCenter();
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(unlockKey))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Инвертирование осей
        if (invertHorizontal) mouseX = -mouseX;
        if (invertVertical) mouseY = -mouseY;

        // Проверка на ввод для автоцентрирования
        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            lastInputTime = Time.time;
            isAutoCentering = false;
        }

        if (_axes == RotationAxes.XandY)
        {
            _rotationX -= mouseY * _rotationSpeedVer;
            _rotationX = Mathf.Clamp(_rotationX, minVert, maxVert);

            float delta = mouseX * _rotationSpeedHor;
            float _rotationY = targetRotation.y + delta;

            targetRotation = new Vector3(_rotationX, _rotationY, 0);
        }
        else if (_axes == RotationAxes.X)
        {
            float delta = mouseX * _rotationSpeedHor;
            targetRotation.y += delta;
        }
        else if (_axes == RotationAxes.Y)
        {
            _rotationX -= mouseY * _rotationSpeedVer;
            _rotationX = Mathf.Clamp(_rotationX, minVert, maxVert);
            targetRotation.x = _rotationX;
        }

        // Применение сглаживания
        if (enableSmoothing)
        {
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, smoothingSpeed * Time.deltaTime);
            transform.localEulerAngles = currentRotation;
        }
        else
        {
            transform.localEulerAngles = targetRotation;
            currentRotation = targetRotation;
        }
    }

    private void HandleAutoCenter()
    {
        if (!autoCenter) return;

        if (Time.time - lastInputTime > autoCenterDelay && !isAutoCentering)
        {
            isAutoCentering = true;
        }

        if (isAutoCentering)
        {
            // Плавное возвращение к центру по вертикали
            float centerX = Mathf.LerpAngle(_rotationX, 0, autoCenterSpeed * Time.deltaTime);
            _rotationX = centerX;

            targetRotation.x = _rotationX;

            // Остановка автоцентрирования при достижении центра
            if (Mathf.Abs(_rotationX) < 0.1f)
            {
                _rotationX = 0;
                targetRotation.x = 0;
                isAutoCentering = false;
            }
        }
    }

    // Публичные методы для управления извне
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10.0f);
    }

    public void SetRotationSpeed(float horizontal, float vertical)
    {
        _rotationSpeedHor = horizontal;
        _rotationSpeedVer = vertical;
    }

    public void ResetRotation()
    {
        _rotationX = 0;
        targetRotation = Vector3.zero;
        currentRotation = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
    }

    public void SetInvertControls(bool invertHor, bool invertVer)
    {
        invertHorizontal = invertHor;
        invertVertical = invertVer;
    }
}
