using UnityEngine;

public class MouseLook1 : MonoBehaviour
{
    public enum RotationAxes
    {
        XandY,
        X,
        Y
    }

    [Header("Rotation Settings")]
    [SerializeField] private RotationAxes _axes = RotationAxes.XandY;
    [SerializeField] private float _rotationSpeedHor = 5.0f;
    [SerializeField] private float _rotationSpeedVer = 5.0f;

    [Header("Vertical Rotation Limits")]
    [SerializeField] private float _maxVert = 45.0f;
    [SerializeField] private float _minVert = -45.0f;

    [Header("Optional Settings")]
    [SerializeField] private bool _invertY = false;
    [SerializeField] private bool _smoothRotation = false;
    [SerializeField] private float _smoothSpeed = 10f;

    [Header("Camera Collision")]
    [SerializeField] private bool _enableCameraCollision = true;
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private float _collisionRadius = 0.3f;
    [SerializeField] private LayerMask _collisionLayers = -1;
    [SerializeField] private float _collisionOffset = 0.1f;
    [SerializeField] private bool _showCollisionSphere = false;

    // Кэшированные компоненты
    private Transform _transform;

    // Состояние поворота
    private float _rotationX = 0f;
    private Vector3 _targetRotation;
    private Vector3 _currentRotation;

    // Кэшированные значения для оптимизации
    private float _mouseX;
    private float _mouseY;

    // Камера коллизия
    private Vector3 _originalCameraPosition;
    private Transform _cameraTransform;

    private void Awake()
    {
        // Кэшируем transform для лучшей производительности
        _transform = transform;

        // Ищем камеру если не назначена
        if (_targetCamera == null)
        {
            _targetCamera = GetComponent<Camera>();
            if (_targetCamera == null)
                _targetCamera = GetComponentInChildren<Camera>();
        }

        if (_targetCamera != null)
        {
            _cameraTransform = _targetCamera.transform;
        }
    }

    private void Start()
    {
        InitializeComponent();

        // Сохраняем исходную позицию камеры
        if (_cameraTransform != null)
        {
            _originalCameraPosition = _cameraTransform.localPosition;
        }

        // Инициализируем текущие углы поворота
        _currentRotation = _transform.localEulerAngles;
        _targetRotation = _currentRotation;
    }

    private void InitializeComponent()
    {
        // Замораживаем поворот у Rigidbody если он есть
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.freezeRotation = true;
        }
    }

    private void Update()
    {
        HandleMouseInput();
        ProcessRotation();
    }

    private void LateUpdate()
    {
        // Обрабатываем коллизию камеры после всех поворотов
        if (_enableCameraCollision && _cameraTransform != null)
        {
            HandleCameraCollision();
        }
    }

    private void HandleMouseInput()
    {
        // Кэшируем входные данные мыши
        _mouseX = Input.GetAxis("Mouse X") * _rotationSpeedHor;
        _mouseY = Input.GetAxis("Mouse Y") * _rotationSpeedVer;

        // Инвертируем Y если нужно
        if (_invertY)
            _mouseY = -_mouseY;
    }

    private void ProcessRotation()
    {
        switch (_axes)
        {
            case RotationAxes.XandY:
                HandleBothAxes();
                break;
            case RotationAxes.X:
                HandleHorizontalOnly();
                break;
            case RotationAxes.Y:
                HandleVerticalOnly();
                break;
        }

        ApplyRotation();
    }

    private void HandleBothAxes()
    {
        // Вертикальный поворот (X axis)
        _rotationX -= _mouseY;
        _rotationX = Mathf.Clamp(_rotationX, _minVert, _maxVert);

        // Горизонтальный поворот (Y axis)
        float rotationY = _currentRotation.y + _mouseX;

        _targetRotation = new Vector3(_rotationX, rotationY, 0f);
    }

    private void HandleHorizontalOnly()
    {
        float rotationY = _currentRotation.y + _mouseX;
        _targetRotation = new Vector3(_currentRotation.x, rotationY, _currentRotation.z);
    }

    private void HandleVerticalOnly()
    {
        _rotationX -= _mouseY;
        _rotationX = Mathf.Clamp(_rotationX, _minVert, _maxVert);

        _targetRotation = new Vector3(_rotationX, _currentRotation.y, _currentRotation.z);
    }

    private void ApplyRotation()
    {
        if (_smoothRotation)
        {
            // Плавное вращение
            _currentRotation = Vector3.Lerp(_currentRotation, _targetRotation,
                _smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Мгновенное вращение
            _currentRotation = _targetRotation;
        }

        _transform.localEulerAngles = _currentRotation;
    }

    private void HandleCameraCollision()
    {
        // Желаемая позиция камеры в мировых координатах
        Vector3 desiredWorldPos = _transform.TransformPoint(_originalCameraPosition);

        // Направление от центра объекта к камере
        Vector3 direction = (desiredWorldPos - _transform.position).normalized;
        float desiredDistance = Vector3.Distance(_transform.position, desiredWorldPos);

        RaycastHit hit;

        // Проверяем коллизию между центром объекта и желаемой позицией камеры
        if (Physics.SphereCast(_transform.position, _collisionRadius, direction, out hit, desiredDistance, _collisionLayers))
        {
            // Если есть коллизия, размещаем камеру ближе
            float safeDistance = hit.distance - _collisionOffset;
            safeDistance = Mathf.Max(safeDistance, _collisionRadius + 0.1f); // Минимальное расстояние

            Vector3 safePosition = _transform.position + direction * safeDistance;
            _cameraTransform.position = safePosition;
        }
        else
        {
            // Если коллизии нет, возвращаем в исходную позицию
            _cameraTransform.position = desiredWorldPos;
        }
    }

    // Публичные методы для внешнего управления
    public void SetSensitivity(float horizontal, float vertical)
    {
        _rotationSpeedHor = Mathf.Max(0f, horizontal);
        _rotationSpeedVer = Mathf.Max(0f, vertical);
    }

    public void SetVerticalLimits(float min, float max)
    {
        _minVert = Mathf.Clamp(min, -90f, 0f);
        _maxVert = Mathf.Clamp(max, 0f, 90f);

        // Применяем новые ограничения к текущему углу
        _rotationX = Mathf.Clamp(_rotationX, _minVert, _maxVert);
    }

    public void ResetRotation()
    {
        _rotationX = 0f;
        _currentRotation = Vector3.zero;
        _targetRotation = Vector3.zero;
        _transform.localEulerAngles = Vector3.zero;

        // Возвращаем камеру в исходную позицию
        if (_cameraTransform != null)
        {
            _cameraTransform.localPosition = _originalCameraPosition;
        }
    }

    public void SetRotationMode(RotationAxes newAxes)
    {
        _axes = newAxes;
    }

    public void SetInvertY(bool invert)
    {
        _invertY = invert;
    }

    public void SetSmoothRotation(bool smooth, float speed = 10f)
    {
        _smoothRotation = smooth;
        _smoothSpeed = Mathf.Max(0.1f, speed);
    }

    // Методы для управления коллизией камеры
    public void EnableCameraCollision(bool enable)
    {
        _enableCameraCollision = enable;

        // Если отключили, возвращаем камеру в исходную позицию
        if (!enable && _cameraTransform != null)
        {
            _cameraTransform.localPosition = _originalCameraPosition;
        }
    }

    public void SetCollisionRadius(float radius)
    {
        _collisionRadius = Mathf.Max(0.1f, radius);
    }

    public void SetCollisionLayers(LayerMask layers)
    {
        _collisionLayers = layers;
    }

    public void SetCollisionOffset(float offset)
    {
        _collisionOffset = Mathf.Max(0f, offset);
    }

    public void SetTargetCamera(Camera camera)
    {
        _targetCamera = camera;
        if (camera != null)
        {
            _cameraTransform = camera.transform;
            _originalCameraPosition = _cameraTransform.localPosition;
        }
    }

    // Геттеры для получения текущих значений
    public float CurrentVerticalRotation => _rotationX;
    public float CurrentHorizontalRotation => _currentRotation.y;
    public Vector3 CurrentRotation => _currentRotation;
    public RotationAxes CurrentAxes => _axes;
    public bool IsCameraCollisionEnabled => _enableCameraCollision;
    public float CollisionRadius => _collisionRadius;

    // Для отладки
    private void OnValidate()
    {
        // Валидация значений в Inspector
        _rotationSpeedHor = Mathf.Max(0f, _rotationSpeedHor);
        _rotationSpeedVer = Mathf.Max(0f, _rotationSpeedVer);
        _smoothSpeed = Mathf.Max(0.1f, _smoothSpeed);

        // Проверка корректности вертикальных ограничений
        if (_minVert > _maxVert)
        {
            float temp = _minVert;
            _minVert = _maxVert;
            _maxVert = temp;
        }

        _minVert = Mathf.Clamp(_minVert, -90f, 90f);
        _maxVert = Mathf.Clamp(_maxVert, -90f, 90f);

        // Валидация параметров коллизии
        _collisionRadius = Mathf.Max(0.1f, _collisionRadius);
        _collisionOffset = Mathf.Max(0f, _collisionOffset);
    }

    // Для визуализации коллизионной сферы в Scene View
    private void OnDrawGizmosSelected()
    {
        if (!_enableCameraCollision || !_showCollisionSphere) return;

        Gizmos.color = Color.yellow;

        if (_cameraTransform != null)
        {
            // Показываем сферу в текущей позиции камеры
            Gizmos.DrawWireSphere(_cameraTransform.position, _collisionRadius);

            // Показываем линию от центра объекта к камере
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _cameraTransform.position);
        }
        else if (Application.isPlaying == false)
        {
            // В режиме редактора показываем где будет сфера
            Vector3 cameraPos = transform.TransformPoint(_originalCameraPosition);
            Gizmos.DrawWireSphere(cameraPos, _collisionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, cameraPos);
        }
    }
}