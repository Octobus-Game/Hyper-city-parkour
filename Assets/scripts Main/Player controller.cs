using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterMovement0 : MonoBehaviour
{
    // Ходьба, ускорения
    [SerializeField] public float _speedwalk;
    [SerializeField] private float _dashSpeedMultiplier = 2f;
    [SerializeField] private float _dashDuration = 4f;
    [SerializeField] public float _gravity;
    [SerializeField] public float _jumpPower;
    [SerializeField] private ParticleSystem _dashEffect;

    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _fallMultiplier = 3f;
    [SerializeField] private Scrollbar _dashScrollbar;
    [SerializeField] private float _dashScrollbarSize = 0.3f;


    // В секцию [Header] Inspector Fields добавь:
    [Header("Tilt Object Settings")]
    [SerializeField] private Transform _tiltObject;
    [SerializeField] private float _tiltDuration = 1f;
    [SerializeField] private float _tiltRightZ = -90f;
    [SerializeField] private float _tiltLeftZ = 90f;
    [SerializeField] private float _tiltBackZ = 180f;

    // Плавное ускорение/замедление
    [Header("Movement Smoothing")]
    [SerializeField] private float _accelerationTime = 0.2f;
    [SerializeField] private float _decelerationTime = 0.15f;
    [SerializeField] private AnimationCurve _accelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _decelerationCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Speed Display")]
    [SerializeField] private TextMeshProUGUI speedText;

    // Стрейф-моментум (сохранение скорости при A/D)
    [Header("Strafe Momentum Settings")]
    [SerializeField] private bool _strafeMomentumEnabled = true;
    [SerializeField] private float _strafeMaxSpeed = 20f;         // Максимальная скорость от стрейфа
    [SerializeField] private float _strafeAcceleration = 8f;      // Как быстро набирается скорость
    [SerializeField] private float _strafeMomentumDecay = 3f;     // Как быстро затухает импульс без ввода
    [SerializeField] private float _strafeMomentumDecayGround = 8f; // Затухание на земле
    [SerializeField] private float _strafeAirControl = 0.4f;      // Контроль в воздухе (0-1)

    // Бесконечный прыжок
    [Header("Infinite Jump Settings")]
    [SerializeField] private bool _infiniteJumpEnabled = false;   // Включить бесконечный прыжок
    [SerializeField] private float _infiniteJumpInterval = 0.3f;  // Интервал между авто-прыжками (сек)
    [SerializeField] private float _infiniteJumpPower = 0f;       // 0 = использует _jumpPower

    // Wall Jump параметры
    [Header("Wall Jump Settings")]
    [SerializeField] private float _wallJumpPower = 12f;
    [SerializeField] private float _wallJumpHorizontalForce = 8f;
    [SerializeField] private float _wallSlidingSpeed = 2f;
    [SerializeField] private float _wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask _wallLayerMask = 1;
    [SerializeField] private float _wallJumpCooldown = 0.2f;
    [SerializeField] private ParticleSystem _wallJumpEffect;

    // Double Jump параметры
    [Header("Double Jump Settings")]
    [SerializeField] private bool _doubleJumpEnabled = true;
    [SerializeField] private float _doubleJumpPower = 10f;
    [SerializeField] private ParticleSystem _doubleJumpEffect;
    [SerializeField] private AudioClip _doubleJumpSound;

    // Air Dash параметры
    [Header("Air Dash Settings")]
    [SerializeField] private bool _airDashEnabled = true;
    [SerializeField] private float _airDashForce = 15f;
    [SerializeField] private float _airDashUpwardForce = 5f;
    [SerializeField] private float _airDashCooldown = 1f;
    [SerializeField] private ParticleSystem _airDashEffect;
    [SerializeField] private AudioClip _airDashSound;
    [SerializeField] private float _airDashVolume = 0.8f;

    // Crouch параметры
    [Header("Crouch Settings")]
    [SerializeField] private bool _crouchEnabled = true;
    [SerializeField] private float _crouchHeight = 1f;
    [SerializeField] private float _crouchTransitionSpeed = 10f;
    [SerializeField] private float _crouchSpeedMultiplier = 0.7f;
    [SerializeField] private bool _maintainMomentumWhileCrouching = true;
    [SerializeField] private float _momentumDecayRate = 0.95f;
    [SerializeField] private Vector3 _crouchCameraOffset = new Vector3(0, -0.5f, 0);
    [SerializeField] private AudioClip _crouchSound;
    [SerializeField] private AudioClip _uncrouchSound;
    [SerializeField] private float _crouchSoundVolume = 0.4f;

    // Wheel Rotation Settings
    [Header("Wheel Settings")]
    [SerializeField] private Transform _wheel1;
    [SerializeField] private Transform _wheel2;
    [SerializeField] private float _wheelRotationSpeed = 360f;
    [SerializeField] private bool _reverseWheelRotation = false;
    [SerializeField] private float _wheel1StartDelay = 0f;
    [SerializeField] private float _wheel2StartDelay = 0f;
    [SerializeField] private float _wheelAccelerationTime = 0.5f;

    // Звуковая система
    [Header("Sound Settings")]
    [SerializeField] private AudioSource _footstepAudioSource;
    [SerializeField] private AudioClip[] _walkFootstepSounds;
    [SerializeField] private AudioClip[] _runFootstepSounds;
    [SerializeField] private float _walkStepInterval = 0.5f;
    [SerializeField] private float _runStepInterval = 0.3f;
    [SerializeField] private float _footstepVolume = 0.5f;

    [SerializeField] private AudioSource _jumpAudioSource;
    [SerializeField] private AudioClip _jumpSound;
    [SerializeField] private AudioClip _landSound;
    [SerializeField] private ParticleSystem _landEffect;
    [SerializeField] private AudioClip _wallJumpSound;
    [SerializeField] private float _jumpVolume = 0.7f;
    [SerializeField] private float _landVolume = 0.6f;
    [SerializeField] private float _minLandVelocity = -3f;

    [Header("Camera Shake on Land")]
    [SerializeField] private float _landShakeDuration  = 0.3f;
    [SerializeField] private float _landShakeMagnitude = 0.15f;

    // Анимации
    [Header("Animation Settings")]
    [SerializeField] private Animator _animator;
    [SerializeField] private float _animationSmoothTime = 0.1f;

    // Параметры анимаций
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsDashing = Animator.StringToHash("IsDashing");
    private static readonly int IsWallSliding = Animator.StringToHash("IsWallSliding");
    private static readonly int WallJumpTrigger = Animator.StringToHash("WallJump");
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int DoubleJumpTrigger = Animator.StringToHash("DoubleJump");
    private static readonly int AirDashTrigger = Animator.StringToHash("AirDash");
    private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int CrouchTrigger = Animator.StringToHash("Crouch");
    private static readonly int UncrouchTrigger = Animator.StringToHash("Uncrouch");
    private static readonly int IsStrafeLeft = Animator.StringToHash("IsStrafeLeft");
    private static readonly int IsStrafeRight = Animator.StringToHash("IsStrafeRight");

    // Planet Gravity
    [Header("Planet Gravity Settings")]
    [Tooltip("Включить режим ходьбы по планете (Sphere Collider)")]
    [SerializeField] private bool _planetGravityEnabled = false;
    [Tooltip("Скорость выравнивания игрока по нормали поверхности планеты")]
    [SerializeField] private float _planetAlignmentSpeed = 8f;
    [Tooltip("Дополнительный отступ SphereCast для определения «земли» на планете")]
    [SerializeField] private float _planetGroundCheckOffset = 0.2f;
    [SerializeField] private LayerMask _planetGroundLayerMask = ~0;

    private float _originalScrollbarSize;
    public CharacterController _characterController;
    private Vector3 _walkDirection;
    private Vector3 _targetWalkDirection;
    private float _currentSpeedMultiplier = 0f;
    private bool _isAccelerating = false;
    private bool _isDecelerating = false;
    private float _accelerationTimer = 0f;
    private float _decelerationTimer = 0f;

    private Coroutine _tiltCoroutine;
    private float _tiltTargetZ = 0f;
    private float _tiltOriginalZ = 0f;
    
    private Vector3 _velocity;
    private bool _isDashing = false;

    // Стрейф-моментум переменные
    private Vector3 _strafeMomentumVelocity = Vector3.zero;
    private bool _isStrafeInputActive = false;

    // Бесконечный прыжок переменные
    private float _infiniteJumpTimer = 0f;

    // Wall Jump переменные
    private bool _isTouchingWall = false;
    private bool _isWallSliding = false;
    private Vector3 _wallNormal;
    private float _lastWallJumpTime = 0f;

    // Double Jump переменные
    private bool _hasDoubleJumped = false;
    private bool _canDoubleJump = false;

    // Air Dash переменные
    private bool _hasAirDashed = false;
    private float _lastAirDashTime = 0f;

    // Crouch переменные
    private bool _isCrouching = false;
    private bool _wasCrouchingLastFrame = false;
    private float _originalHeight;
    private float _currentHeight;
    private Vector3 _crouchMomentum = Vector3.zero;
    private Transform _cameraTransform;
    private Vector3 _originalCameraPosition;

    // Анимационные переменные
    private bool _wasGrounded = true;
    private float _currentMovementSpeed = 0f;

    // Planet gravity state
    private Planet[] _cachedPlanets;
    private Planet _currentPlanet;
    private bool _isGroundedOnPlanet;
    private Vector3 _planetGravityDir = Vector3.down;

    /// <summary>Единая проверка «на земле» для обычного и планетарного режимов.</summary>
    private bool IsPlayerGrounded => _planetGravityEnabled
        ? _isGroundedOnPlanet
        : _characterController.isGrounded;

    // Звуковые переменные
    private float _nextFootstepTime = 0f;
    private float _velocityBeforeLanding = 0f;

    // Wheel timing переменные
    private float _wheel1RotationTimer = 0f;
    private float _wheel2RotationTimer = 0f;
    private bool _isWheelRotationActive = false;
    private float _wheel1CurrentSpeedMultiplier = 0f;
    private float _wheel2CurrentSpeedMultiplier = 0f;

    private Coroutine _fadeCoroutine;
    private Coroutine _cameraShakeCoroutine;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _cachedPlanets = FindObjectsOfType<Planet>();

        _originalHeight = _characterController.height;
        _currentHeight = _originalHeight;

        _cameraTransform = Camera.main?.transform;
        if (_cameraTransform != null)
            _originalCameraPosition = _cameraTransform.localPosition;

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        if (_animator == null)
            Debug.LogWarning("Animator не найден! Анимации не будут работать.");

        if (_wheel1 == null || _wheel2 == null)
            Debug.LogWarning("Wheel объекты не назначены! Вращение колес не будет работать.");

        if (_footstepAudioSource == null)
        {
            GameObject footstepObj = new GameObject("FootstepAudioSource");
            footstepObj.transform.SetParent(transform);
            footstepObj.transform.localPosition = Vector3.zero;
            _footstepAudioSource = footstepObj.AddComponent<AudioSource>();
            _footstepAudioSource.spatialBlend = 1f;
            _footstepAudioSource.minDistance = 1f;
            _footstepAudioSource.maxDistance = 15f;
        }

        if (_jumpAudioSource == null)
        {
            GameObject jumpObj = new GameObject("JumpAudioSource");
            jumpObj.transform.SetParent(transform);
            jumpObj.transform.localPosition = Vector3.zero;
            _jumpAudioSource = jumpObj.AddComponent<AudioSource>();
            _jumpAudioSource.spatialBlend = 1f;
            _jumpAudioSource.minDistance = 1f;
            _jumpAudioSource.maxDistance = 20f;
        }
    


	if (_tiltObject != null)
     {
    	_tiltOriginalZ = _tiltObject.eulerAngles.z;
    	if (_tiltOriginalZ > 180f) _tiltOriginalZ -= 360f;   // нормализация в [-180, 180]
    }


    


    }

    private void Update()
    {
        // Получаем актуальные клавиши из KeybindManager
        KeyCode keyForward = KeybindManager.Instance.GetKey("Forward");
        KeyCode keyBack = KeybindManager.Instance.GetKey("Back");
        KeyCode keyLeft = KeybindManager.Instance.GetKey("Left");
        KeyCode keyRight = KeybindManager.Instance.GetKey("Right");
        KeyCode keyJump = KeybindManager.Instance.GetKey("Jump");
        KeyCode keyDash = KeybindManager.Instance.GetKey("Dash");
        KeyCode keyCrouch = KeybindManager.Instance.GetKey("Crouch");

        // Заменяем все KeyCode.W → keyForward и т.д.
        // Пример:
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(keyRight)) z = 1f;
        if (Input.GetKey(keyLeft)) z = -1f;
        if (Input.GetKey(keyForward)) x = -1f;
        if (Input.GetKey(keyBack)) x = 1f;

        _isStrafeInputActive = Input.GetKey(keyRight) || Input.GetKey(keyLeft);

        if (Input.GetKeyDown(keyDash) && !_isDashing && IsPlayerGrounded)
            StartCoroutine(Dash());







        CheckWallCollision();

        // ── Бесконечный прыжок ──────────────────────────────────────
        if (_infiniteJumpEnabled && Input.GetKey(KeyCode.Space))
        {
            _infiniteJumpTimer -= Time.deltaTime;
            if (_infiniteJumpTimer <= 0f)
            {
                float power = _infiniteJumpPower > 0f ? _infiniteJumpPower : _jumpPower;
                _velocity.y = power;
                _infiniteJumpTimer = _infiniteJumpInterval;

                if (_jumpSound != null && _jumpAudioSource != null)
                {
                    _jumpAudioSource.volume = _jumpVolume * 0.8f;
                    _jumpAudioSource.pitch = Random.Range(0.95f, 1.05f);
                    _jumpAudioSource.PlayOneShot(_jumpSound);
                }
            }
        }
        // ── Обычные прыжки (только если бесконечный выключен) ────────
        else if (!_infiniteJumpEnabled)
        {
            if (IsPlayerGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }
            else if (!IsPlayerGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                if (_isTouchingWall && Time.time - _lastWallJumpTime > _wallJumpCooldown)
                    WallJump();
                else if (_doubleJumpEnabled && _canDoubleJump && !_hasDoubleJumped)
                    DoubleJump();
            }
        }




        if (Input.GetKey(keyForward) && Input.GetKey(keyDash) && Input.GetKeyDown(keyJump))
        {
            if (_airDashEnabled && !IsPlayerGrounded && !_hasAirDashed)
                if (Time.time - _lastAirDashTime > _airDashCooldown)
                    AirDash();
        }

        HandleCrouch();
        bool crouchInput = Input.GetKey(KeybindManager.Instance.GetKey("Crouch"));
        HandleTiltObject(keyForward, keyBack, keyLeft, keyRight);

        _targetWalkDirection = transform.right * x + transform.forward * z;

        UpdateMovementSmoothing();
        HandleFootstepSounds();

        if (!IsPlayerGrounded)
            _velocityBeforeLanding = _velocity.y;

        if (IsPlayerGrounded)
        {
            _hasDoubleJumped = false;
            _canDoubleJump = false;
            _hasAirDashed = false;
            _infiniteJumpTimer = 0f;

            if (!_wasGrounded)
                PlayLandingSound();
        }
        else
        {
            if (_wasGrounded)
                _canDoubleJump = true;
        }

        UpdateAnimations();
        RotateWheels();

        if (speedText != null)
        {
            // Показываем полную горизонтальную скорость включая стрейф-моментум
            Vector3 totalHorizontal = new Vector3(
                _characterController.velocity.x + _strafeMomentumVelocity.x,
                0,
                _characterController.velocity.z + _strafeMomentumVelocity.z
            );
            float speed = totalHorizontal.magnitude;
            speedText.text = "Speed: " + speed.ToString("F1") + " m/s";
        }
    }

    private void UpdateMovementSmoothing()
    {
        bool isInputActive = _targetWalkDirection.magnitude > 0.01f;

        if (isInputActive)
        {
            if (!_isAccelerating || _isDecelerating)
            {
                _isAccelerating = true;
                _isDecelerating = false;
                _accelerationTimer = _currentSpeedMultiplier * _accelerationTime;
            }

            _accelerationTimer += Time.deltaTime;
            float accelerationProgress = Mathf.Clamp01(_accelerationTimer / _accelerationTime);
            _currentSpeedMultiplier = _accelerationCurve.Evaluate(accelerationProgress);
            _walkDirection = _targetWalkDirection;
        }
        else
        {
            if (!_isDecelerating || _isAccelerating)
            {
                _isDecelerating = true;
                _isAccelerating = false;
                _decelerationTimer = (1f - _currentSpeedMultiplier) * _decelerationTime;
            }

            _decelerationTimer += Time.deltaTime;
            float decelerationProgress = Mathf.Clamp01(_decelerationTimer / _decelerationTime);
            _currentSpeedMultiplier = _decelerationCurve.Evaluate(decelerationProgress);
        }

        if (_currentSpeedMultiplier <= 0.001f && _isDecelerating)
        {
            _currentSpeedMultiplier = 0f;
            _walkDirection = Vector3.zero;
            _isDecelerating = false;
        }
    }


    private void HandleTiltObject(KeyCode keyForward, KeyCode keyBack, KeyCode keyLeft, KeyCode keyRight)
{
    if (_tiltObject == null) return;

    // Каждый GetKeyDown срабатывает ровно один раз — даже при зажатой клавише
    if (Input.GetKeyDown(keyForward))
        
	SetTilt(_tiltOriginalZ);          // ← вперёд = исходное положение объекта
    else if (Input.GetKeyDown(keyRight))
        
	SetTilt(_tiltRightZ);
    else if (Input.GetKeyDown(keyLeft))
        
	SetTilt(_tiltLeftZ);
    else if (Input.GetKeyDown(keyBack))
        
	SetTilt(_tiltBackZ);

    // GetKeyUp убран — поворот больше не сбрасывается при отпускании клавиши
}
    

    private void SetTilt(float targetZ)
    {
        if (Mathf.Approximately(_tiltTargetZ, targetZ)) return;
        _tiltTargetZ = targetZ;
        if (_tiltCoroutine != null) StopCoroutine(_tiltCoroutine);
        _tiltCoroutine = StartCoroutine(TiltObjectTo(targetZ));
    }

    private IEnumerator TiltObjectTo(float targetZ)
    {
        float startZ = _tiltObject.eulerAngles.z;
        // Нормализуем стартовый угол в диапазон [-180, 180]
        if (startZ > 180f) startZ -= 360f;

        float elapsed = 0f;
        while (elapsed < _tiltDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _tiltDuration);
            // Плавное смягчение (ease in-out)
            t = t * t * (3f - 2f * t);

            float currentZ = Mathf.LerpAngle(startZ, targetZ, t);
            Vector3 euler = _tiltObject.eulerAngles;
            euler.z = currentZ;
            _tiltObject.eulerAngles = euler;
            yield return null;
        }

        // Финальное значение
        Vector3 finalEuler = _tiltObject.eulerAngles;
        finalEuler.z = targetZ;
        _tiltObject.eulerAngles = finalEuler;
    





 }

















    // ── Стрейф-моментум ─────────────────────────────────────────────
    private void UpdateStrafeMomentum()
    {
        if (!_strafeMomentumEnabled) return;

        bool isGrounded = IsPlayerGrounded;

        if (_isStrafeInputActive)
        {
            // Направление стрейфа (только боковая ось)
            float strafeInput = 0f;
            if (Input.GetKey(KeyCode.D)) strafeInput = 1f;
            if (Input.GetKey(KeyCode.A)) strafeInput = -1f;

            Vector3 strafeDir = transform.forward * strafeInput;

            // В воздухе — уменьшенный контроль
            float accel = _strafeAcceleration * (isGrounded ? 1f : _strafeAirControl);

            // Добавляем скорость в направлении стрейфа (не превышая максимум)
            Vector3 desiredAdd = strafeDir * accel * Time.fixedDeltaTime;
            Vector3 newVelocity = _strafeMomentumVelocity + desiredAdd;

            if (newVelocity.magnitude > _strafeMaxSpeed)
                newVelocity = newVelocity.normalized * _strafeMaxSpeed;

            _strafeMomentumVelocity = newVelocity;
        }
        else
        {
            // Затухание: на земле быстрее, в воздухе медленнее
            float decay = isGrounded ? _strafeMomentumDecayGround : _strafeMomentumDecay;
            _strafeMomentumVelocity = Vector3.MoveTowards(
                _strafeMomentumVelocity,
                Vector3.zero,
                decay * Time.fixedDeltaTime
            );
        }

        // Применяем накопленный моментум
        if (_strafeMomentumVelocity.magnitude > 0.01f)
            _characterController.Move(_strafeMomentumVelocity * Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        if (_planetGravityEnabled)
            UpdatePlanetGravity();

        Walk(_walkDirection);
        UpdateStrafeMomentum();

        if (_planetGravityEnabled)
            DoGravityPlanet();
        else
            DoGravity(_characterController.isGrounded);
    }

    private void RotateWheels()
    {
        if (_wheel1 == null && _wheel2 == null) return;

        float movementMagnitude = _currentSpeedMultiplier;

        if (movementMagnitude > 0.01f)
        {
            if (!_isWheelRotationActive)
            {
                _isWheelRotationActive = true;
                _wheel1RotationTimer = 0f;
                _wheel2RotationTimer = 0f;
            }

            _wheel1RotationTimer += Time.deltaTime;
            _wheel2RotationTimer += Time.deltaTime;

            float baseRotationSpeed = _wheelRotationSpeed * movementMagnitude;

            if (_isDashing)
                baseRotationSpeed *= _dashSpeedMultiplier;

            float rotationDirection = _reverseWheelRotation ? 1f : -1f;

            if (_wheel1 != null && _wheel1RotationTimer >= _wheel1StartDelay)
            {
                float wheel1Progress = Mathf.Clamp01((_wheel1RotationTimer - _wheel1StartDelay) / _wheelAccelerationTime);
                _wheel1CurrentSpeedMultiplier = Mathf.Lerp(0f, 1f, wheel1Progress);
                float wheel1RotationAmount = baseRotationSpeed * _wheel1CurrentSpeedMultiplier * rotationDirection * Time.deltaTime;
                _wheel1.Rotate(0, 0, wheel1RotationAmount, Space.Self);
            }

            if (_wheel2 != null && _wheel2RotationTimer >= _wheel2StartDelay)
            {
                float wheel2Progress = Mathf.Clamp01((_wheel2RotationTimer - _wheel2StartDelay) / _wheelAccelerationTime);
                _wheel2CurrentSpeedMultiplier = Mathf.Lerp(0f, 1f, wheel2Progress);
                float wheel2RotationAmount = baseRotationSpeed * _wheel2CurrentSpeedMultiplier * rotationDirection * Time.deltaTime;
                _wheel2.Rotate(0, 0, wheel2RotationAmount, Space.Self);
            }
        }
        else
        {
            if (_isWheelRotationActive)
            {
                _isWheelRotationActive = false;
                _wheel1RotationTimer = 0f;
                _wheel2RotationTimer = 0f;
                _wheel1CurrentSpeedMultiplier = 0f;
                _wheel2CurrentSpeedMultiplier = 0f;
            }
        }
    }

    private void HandleFootstepSounds()
    {
        bool isMoving = _currentSpeedMultiplier > 0.1f;
        bool isGrounded = IsPlayerGrounded;

        if (isGrounded && isMoving && Time.time >= _nextFootstepTime)
        {
            PlayFootstepSound();
            float interval = _isDashing ? _runStepInterval : _walkStepInterval;
            _nextFootstepTime = Time.time + interval;
        }
    }

    private void PlayFootstepSound()
    {
        if (_footstepAudioSource == null) return;

        AudioClip[] soundArray = _isDashing ? _runFootstepSounds : _walkFootstepSounds;

        if (soundArray == null || soundArray.Length == 0)
            soundArray = _walkFootstepSounds;

        if (soundArray != null && soundArray.Length > 0)
        {
            AudioClip clip = soundArray[Random.Range(0, soundArray.Length)];
            if (clip != null)
            {
                _footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
                _footstepAudioSource.volume = _footstepVolume;
                _footstepAudioSource.PlayOneShot(clip);
            }
        }
    }

    private void PlayLandingSound()
    {
        if (_velocityBeforeLanding < _minLandVelocity)
        {
            if (_landSound != null && _jumpAudioSource != null)
            {
                float volumeMultiplier = Mathf.Clamp01(Mathf.Abs(_velocityBeforeLanding) / 15f);
                _jumpAudioSource.volume = _landVolume * volumeMultiplier;
                _jumpAudioSource.pitch = Random.Range(0.95f, 1.05f);
                _jumpAudioSource.PlayOneShot(_landSound);
            }

            if (_landEffect != null)
            {
                _landEffect.transform.position = transform.position;
                _landEffect.Play();
            }

            if (_cameraTransform != null && _landShakeDuration > 0f && _landShakeMagnitude > 0f)
            {
                float velocityFactor = Mathf.Clamp01(Mathf.Abs(_velocityBeforeLanding) / 20f);
                if (_cameraShakeCoroutine != null) StopCoroutine(_cameraShakeCoroutine);
                _cameraShakeCoroutine = StartCoroutine(CameraShakeLand(velocityFactor));
            }
        }
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
        float targetSpeed = horizontalVelocity.magnitude;
        _currentMovementSpeed = Mathf.Lerp(_currentMovementSpeed, targetSpeed, Time.deltaTime / _animationSmoothTime);

        bool isGrounded = IsPlayerGrounded;
        bool isMoving = _currentSpeedMultiplier > 0.1f;
        bool isWalking = isGrounded && isMoving && !_isDashing && !_isCrouching;
        bool isRunning = isGrounded && isMoving && _isDashing && !_isCrouching;
        bool isJumping = !isGrounded && _velocity.y > 0;
        bool isFalling = !isGrounded && _velocity.y < -0.1f && !_isWallSliding;
        bool isStrafeLeft = Input.GetKey(KeyCode.A) && isGrounded;
        bool isStrafeRight = Input.GetKey(KeyCode.D) && isGrounded;

        _animator.SetBool(IsStrafeLeft, isStrafeLeft);
        _animator.SetBool(IsStrafeRight, isStrafeRight);

        if (_wasGrounded && !isGrounded && _velocity.y > 0)
            _animator.SetTrigger("JumpTrigger");

        _animator.SetBool(IsWalking, isWalking);
        _animator.SetBool(IsRunning, isRunning);
        _animator.SetBool(IsJumping, isJumping);
        _animator.SetBool(IsGrounded, isGrounded);
        _animator.SetBool(IsDashing, _isDashing);
        _animator.SetBool(IsWallSliding, _isWallSliding);
        _animator.SetBool(IsFalling, isFalling);
        _animator.SetBool(IsCrouching, _isCrouching);
        _animator.SetFloat(MovementSpeed, _currentMovementSpeed);
        _animator.SetFloat(VerticalVelocity, _velocity.y);




        _wasGrounded = isGrounded;
    }

    private void CheckWallCollision()
    {
        Vector3[] directions = { transform.right, -transform.right, transform.forward, -transform.forward };
        _isTouchingWall = false;

        foreach (Vector3 direction in directions)
        {
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(rayOrigin, direction, out hit, _wallCheckDistance, _wallLayerMask))
            {
                _isTouchingWall = true;
                _wallNormal = hit.normal;
                break;
            }
        }

        _isWallSliding = _isTouchingWall && !IsPlayerGrounded && _velocity.y < 0;
    }

    private void Walk(Vector3 direction)
    {
        float speed = _isDashing ? _speedwalk * _dashSpeedMultiplier : _speedwalk;

        if (_isCrouching && !_maintainMomentumWhileCrouching)
            speed *= _crouchSpeedMultiplier;

        Vector3 movement = direction * speed * _currentSpeedMultiplier * Time.fixedDeltaTime;

        if (_isCrouching && _maintainMomentumWhileCrouching)
        {
            movement += _crouchMomentum * Time.fixedDeltaTime;
            _crouchMomentum *= _momentumDecayRate;
        }

        _characterController.Move(movement);
    }

    private void DoGravity(bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0)
            _velocity.y = -1f;

        float gravityToApply = _gravity;

        if (Input.GetKey(KeyCode.LeftShift))
            gravityToApply *= _fallMultiplier;

        if (_isWallSliding)
            _velocity.y = Mathf.Max(_velocity.y, -_wallSlidingSpeed);
        else
            _velocity.y -= gravityToApply * Time.fixedDeltaTime;

        _characterController.Move(_velocity * Time.fixedDeltaTime);
    }

    private void Jump()
    {
        _velocity.y = _jumpPower;

        if (_jumpSound != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _jumpVolume;
            _jumpAudioSource.pitch = Random.Range(0.95f, 1.05f);
            _jumpAudioSource.PlayOneShot(_jumpSound);
        }

        if (_animator != null)
            _animator.SetTrigger("JumpTrigger");
    }

    private void DoubleJump()
    {
        _hasDoubleJumped = true;
        _velocity.y = _doubleJumpPower;

        AudioClip doubleJumpClip = _doubleJumpSound != null ? _doubleJumpSound : _jumpSound;
        if (doubleJumpClip != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _jumpVolume * 0.9f;
            _jumpAudioSource.pitch = Random.Range(1.1f, 1.2f);
            _jumpAudioSource.PlayOneShot(doubleJumpClip);
        }

        if (_doubleJumpEffect != null)
        {
            _doubleJumpEffect.transform.position = transform.position;
            _doubleJumpEffect.Play();
        }

        if (_animator != null)
            _animator.SetTrigger(DoubleJumpTrigger);

        Debug.Log("Double Jump!");
    }

    private void AirDash()
    {
        _hasAirDashed = true;
        _lastAirDashTime = Time.time;

        Vector3 dashDirection = -transform.right;
        _velocity.y = _airDashUpwardForce;
        _characterController.Move(dashDirection * _airDashForce * Time.fixedDeltaTime);

        AudioClip airDashClip = _airDashSound != null ? _airDashSound : _jumpSound;
        if (airDashClip != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _airDashVolume;
            _jumpAudioSource.pitch = Random.Range(1.15f, 1.25f);
            _jumpAudioSource.PlayOneShot(airDashClip);
        }

        if (_airDashEffect != null)
        {
            _airDashEffect.transform.position = transform.position;
            _airDashEffect.Play();
        }

        if (_animator != null)
            _animator.SetTrigger(AirDashTrigger);

        Debug.Log("Air Dash!");
    }

    private void WallJump()
    {
        _lastWallJumpTime = Time.time;
        _velocity.y = _wallJumpPower;

        _characterController.Move(_wallNormal * _wallJumpHorizontalForce * Time.fixedDeltaTime);

        _hasDoubleJumped = false;
        _canDoubleJump = true;

        AudioClip wallJumpClip = _wallJumpSound != null ? _wallJumpSound : _jumpSound;
        if (wallJumpClip != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _jumpVolume * 1.1f;
            _jumpAudioSource.pitch = Random.Range(1.05f, 1.15f);
            _jumpAudioSource.PlayOneShot(wallJumpClip);
        }

        if (_wallJumpEffect != null)
        {
            _wallJumpEffect.transform.position = transform.position;
            _wallJumpEffect.Play();
        }

        if (_animator != null)
            _animator.SetTrigger(WallJumpTrigger);
    }

    private IEnumerator Dash()
    {
        _isDashing = true;
        _dashEffect.Play();

        if (_animator != null)
            _animator.SetTrigger("DashTrigger");

        yield return new WaitForSeconds(_dashDuration);
        _isDashing = false;
        _dashEffect.Stop();
    }

    public void OnLandAnimationComplete()
    {
        if (_animator != null)
            _animator.SetBool("IsLanding", false);
    }

    public void OnDashAnimationComplete()
    {
        Debug.Log("Dash animation completed");
    }

    private void HandleCrouch()
    {
        if (!_crouchEnabled) return;

        bool crouchInput = Input.GetKey(KeyCode.CapsLock);

        if (crouchInput && !_isCrouching)
            StartCrouch();
        else if (!crouchInput && _isCrouching)
        {
            if (CanStandUp())
                EndCrouch();
        }

        if (_isCrouching)
            _currentHeight = Mathf.Lerp(_currentHeight, _crouchHeight, Time.deltaTime * _crouchTransitionSpeed);
        else
            _currentHeight = Mathf.Lerp(_currentHeight, _originalHeight, Time.deltaTime * _crouchTransitionSpeed);

        _characterController.height = _currentHeight;

        Vector3 center = _characterController.center;
        center.y = _currentHeight / 2f;
        _characterController.center = center;

        if (_cameraTransform != null)
        {
            Vector3 targetCameraPos = _isCrouching
                ? _originalCameraPosition + _crouchCameraOffset
                : _originalCameraPosition;

            _cameraTransform.localPosition = Vector3.Lerp(
                _cameraTransform.localPosition,
                targetCameraPos,
                Time.deltaTime * _crouchTransitionSpeed
            );
        }

        _wasCrouchingLastFrame = _isCrouching;
    }

    private void StartCrouch()
    {
        _isCrouching = true;

        if (_maintainMomentumWhileCrouching && IsPlayerGrounded)
        {
            _crouchMomentum = new Vector3(
                _characterController.velocity.x,
                0,
                _characterController.velocity.z
            );
        }

        if (_crouchSound != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _crouchSoundVolume;
            _jumpAudioSource.pitch = Random.Range(0.95f, 1.05f);
            _jumpAudioSource.PlayOneShot(_crouchSound);
        }

        if (_animator != null)
            _animator.SetTrigger(CrouchTrigger);
    }

    private void EndCrouch()
    {
        _isCrouching = false;
        _crouchMomentum = Vector3.zero;

        if (_uncrouchSound != null && _jumpAudioSource != null)
        {
            _jumpAudioSource.volume = _crouchSoundVolume;
            _jumpAudioSource.pitch = Random.Range(0.95f, 1.05f);
            _jumpAudioSource.PlayOneShot(_uncrouchSound);
        }

        if (_animator != null)
            _animator.SetTrigger(UncrouchTrigger);
    }

    private bool CanStandUp()
    {
        float checkDistance = _originalHeight - _crouchHeight + 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * _currentHeight;

        bool hasObstacle = Physics.SphereCast(
            rayOrigin,
            _characterController.radius * 0.9f,
            transform.up,
            out RaycastHit hit,
            checkDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        return !hasObstacle;
    }

    // ══════════════════════════════════════════════════════════════════
    //  ПЛАНЕТАРНАЯ ГРАВИТАЦИЯ
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Обновляет список всех планет на сцене (вызывать при динамическом спавне).</summary>
    public void FindPlanets()
    {
        _cachedPlanets = FindObjectsOfType<Planet>();
    }

    private void UpdatePlanetGravity()
    {
        // Ищем ближайшую планету в радиусе влияния
        _currentPlanet = null;
        float closestDist = float.MaxValue;

        if (_cachedPlanets == null) FindPlanets();

        foreach (Planet planet in _cachedPlanets)
        {
            if (planet == null) continue;
            float dist = Vector3.Distance(transform.position, planet.transform.position);
            if (dist < planet.GetInfluenceRadius() && dist < closestDist)
            {
                closestDist = dist;
                _currentPlanet = planet;
            }
        }

        if (_currentPlanet != null)
        {
            // «Вниз» — от игрока к центру планеты
            _planetGravityDir = (_currentPlanet.transform.position - transform.position).normalized;

            // Плавно выравниваем «вверх» игрока по нормали поверхности планеты
            Vector3 targetUp = -_planetGravityDir;
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot,
                _planetAlignmentSpeed * Time.fixedDeltaTime
            );
        }
        else
        {
            // Вне зоны влияния — возвращаемся к стандартному «вниз»
            _planetGravityDir = Vector3.down;
        }

        _isGroundedOnPlanet = CheckPlanetGround();
    }

    private bool CheckPlanetGround()
    {
        if (_currentPlanet == null)
            return _characterController.isGrounded;

        // SphereCast в сторону центра планеты
        float checkDistance = _characterController.height * 0.5f
                              + _characterController.radius * 0.1f
                              + _planetGroundCheckOffset;

        return Physics.SphereCast(
            transform.position,
            _characterController.radius * 0.85f,
            _planetGravityDir,
            out _,
            checkDistance,
            _planetGroundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void DoGravityPlanet()
    {
        bool grounded = IsPlayerGrounded;

        if (grounded && _velocity.y < 0f)
            _velocity.y = -1f;

        float gravityStrength = _currentPlanet != null ? _currentPlanet.gravityStrength : _gravity;

        if (_isWallSliding)
        {
            _velocity.y = Mathf.Max(_velocity.y, -_wallSlidingSpeed);
        }
        else if (!grounded)
        {
            float multiplier = Input.GetKey(KeyCode.LeftShift) ? _fallMultiplier : 1f;
            _velocity.y -= gravityStrength * multiplier * Time.fixedDeltaTime;
        }

        // Движение вдоль локальной оси Y (выровненной по нормали планеты)
        _characterController.Move(transform.up * _velocity.y * Time.fixedDeltaTime);
    }

    private IEnumerator CameraShakeLand(float velocityFactor = 1f)
    {
        float elapsed = 0f;
        while (elapsed < _landShakeDuration)
        {
            float strength = Mathf.Lerp(_landShakeMagnitude * velocityFactor, 0f, elapsed / _landShakeDuration);
            Vector3 basePos = _isCrouching
                ? _originalCameraPosition + _crouchCameraOffset
                : _originalCameraPosition;

            _cameraTransform.localPosition = basePos + new Vector3(
                Random.Range(-1f, 1f) * strength,
                Random.Range(-1f, 1f) * strength,
                0f
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        _cameraTransform.localPosition = _isCrouching
            ? _originalCameraPosition + _crouchCameraOffset
            : _originalCameraPosition;

        _cameraShakeCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = _isTouchingWall ? Color.red : Color.green;
            Vector3[] directions = { transform.right, -transform.right, transform.forward, -transform.forward };
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

            foreach (Vector3 direction in directions)
                Gizmos.DrawRay(rayOrigin, direction * _wallCheckDistance);
        }
    }
}