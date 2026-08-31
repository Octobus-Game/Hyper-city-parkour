using UnityEngine;

public class YGARLENIS : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speedwalk = 5f;
    [SerializeField] private float _gravity = 9.81f;
    [SerializeField] private float _jumpPower = 8f;
   
    [SerializeField] private AnimationCurve _landingSpeedCurve;

    [Header("Wall Jump Settings")]
    [SerializeField] private float _wallJumpForce = 8f;
    [SerializeField] private float _wallJumpHorizontalForce = 5f;
    [SerializeField] private float _wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask _wallLayerMask = -1;
    [SerializeField] private int _maxWallJumps = 3;

    [Header("Animation Settings")]
    [SerializeField] private Animator _animator;
    [SerializeField] private float _animationSmoothTime = 0.1f;

    [Header("Idle Animation Settings")]
    [SerializeField] private float _idleTimeThreshold = 10f;
    [SerializeField] private string _idleAnimationClip = "IdleSpecial";

    private CharacterController _cc;
    private Vector3 _dir, _vel;

    // Wall jump variables
    private int _currentWallJumps;
    private bool _isWallJumping;
    private Vector3 _wallNormal;
    private bool _canWallJump;

    // Idle animation variables
    private float _idleTimer = 0f;
    private bool _isSpecialIdle = false;
    private Vector3 _lastPosition;
    private bool _isMoving = false;

    // Animation parameter hashes for performance
    private int _speedHash;
    private int _isGroundedHash;
    private int _isJumpingHash;
    private int _velocityYHash;
    private int _isWallJumpingHash;
    private int _isSpecialIdleHash;

    // Animation smoothing
    private float _currentSpeed;
    private float _speedVelocity;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        // Get Animator component if not assigned
        if (_animator == null)
            _animator = GetComponent<Animator>();

        // Cache animation parameter hashes
        _speedHash = Animator.StringToHash("Speed");
        _isGroundedHash = Animator.StringToHash("IsGrounded");
        _isJumpingHash = Animator.StringToHash("IsJumping");
        _velocityYHash = Animator.StringToHash("VelocityY");
        _isWallJumpingHash = Animator.StringToHash("IsWallJumping");
        _isSpecialIdleHash = Animator.StringToHash("IsSpecialIdle");

        // Initialize idle tracking
        _lastPosition = transform.position;
    }

    void Update()
    {
        CheckWallJump();
        HandleInput();
        CheckIdleTime();
        UpdateAnimations();
    }

    void CheckWallJump()
    {
        // Проверяем, есть ли стена рядом
        _canWallJump = false;

        if (!_cc.isGrounded && _vel.y < 0 && _currentWallJumps < _maxWallJumps)
        {
            // Проверяем стены в четырех направлениях
            Vector3[] directions = { transform.right, -transform.right, transform.forward, -transform.forward };

            foreach (Vector3 direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, _wallCheckDistance, _wallLayerMask))
                {
                    _canWallJump = true;
                    _wallNormal = hit.normal;
                    break;
                }
            }
        }

        // Сбрасываем счетчик прыжков от стены при приземлении
        if (_cc.isGrounded)
        {
            _currentWallJumps = 0;
            _isWallJumping = false;
        }
    }

    void HandleInput()
    {
        // Jump input - обычный прыжок или прыжок от стены
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_cc.isGrounded)
            {
                // Обычный прыжок
                _vel.y = _jumpPower * _landingSpeedCurve.Evaluate(Time.time);
            }
            else if (_canWallJump)
            {
                // Прыжок от стены
                PerformWallJump();
            }
        }

        // Movement input
        float x = 0, z = 0;
        if (Input.GetKey(KeyCode.W)) z = 1;
        if (Input.GetKey(KeyCode.S)) z = -1;
        if (Input.GetKey(KeyCode.A)) x = -1;
        if (Input.GetKey(KeyCode.D)) x = 1;
        

        _dir = (transform.right * x + transform.forward * z).normalized;

        // Проверяем, есть ли ввод движения
        _isMoving = (x != 0 || z != 0) && z != 100; // Исключаем LeftControl как движение
    }

    void PerformWallJump()
    {
        // Увеличиваем счетчик прыжков от стены
        _currentWallJumps++;
        _isWallJumping = true;

        // Применяем вертикальную силу прыжка
        _vel.y = _wallJumpForce;

        // Применяем горизонтальную силу от стены
        Vector3 jumpDirection = _wallNormal;
        jumpDirection.y = 0;
        jumpDirection = jumpDirection.normalized;

        // Добавляем горизонтальный импульс от стены
        _vel += jumpDirection * _wallJumpHorizontalForce;
    }

    void CheckIdleTime()
    {
        // Проверяем, движется ли персонаж
        bool hasMovement = _isMoving || !_cc.isGrounded || _isWallJumping;

        // Проверяем изменение позиции (для дополнительной точности)
        Vector3 currentPosition = transform.position;
        bool positionChanged = Vector3.Distance(currentPosition, _lastPosition) > 0.01f;
        _lastPosition = currentPosition;

        if (hasMovement || positionChanged)
        {
            // Сбрасываем таймер простоя
            _idleTimer = 0f;

            // Выключаем специальную анимацию простоя, если она была активна
            if (_isSpecialIdle)
            {
                _isSpecialIdle = false;
                if (_animator != null)
                {
                    _animator.SetBool(_isSpecialIdleHash, false);
                }
            }
        }
        else if (_cc.isGrounded)
        {
            // Увеличиваем таймер простоя только если персонаж на земле
            _idleTimer += Time.deltaTime;

            // Активируем специальную анимацию простоя
            if (_idleTimer >= _idleTimeThreshold && !_isSpecialIdle)
            {
                _isSpecialIdle = true;
                if (_animator != null)
                {
                    _animator.SetBool(_isSpecialIdleHash, true);

                    // Дополнительно можно воспроизвести конкретный клип
                    if (!string.IsNullOrEmpty(_idleAnimationClip))
                    {
                        _animator.Play(_idleAnimationClip, -1, 0f);
                    }
                }
            }
        }
    }

    void UpdateAnimations()
    {
        if (_animator == null) return;

        // Calculate movement speed for animation
        Vector3 horizontalVelocity = new Vector3(_dir.x, 0, _dir.z) * _speedwalk;
        float targetSpeed = horizontalVelocity.magnitude;

        // Smooth the speed value for animations
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, _animationSmoothTime);

        // Set animation parameters
        _animator.SetFloat(_speedHash, _currentSpeed);
        _animator.SetBool(_isGroundedHash, _cc.isGrounded);
        _animator.SetBool(_isJumpingHash, !_cc.isGrounded && _vel.y > 0);
        _animator.SetFloat(_velocityYHash, _vel.y);
        _animator.SetBool(_isWallJumpingHash, _isWallJumping);
        _animator.SetBool(_isSpecialIdleHash, _isSpecialIdle);
    }

    void FixedUpdate()
    {
        // Apply horizontal movement
        _cc.Move(_dir * _speedwalk * Time.fixedDeltaTime);

        // Apply gravity and vertical movement
        if (_cc.isGrounded && _vel.y < 0)
            _vel.y = -1f; // Small downward force to keep grounded

        _vel.y -= _gravity * Time.fixedDeltaTime;

        // Apply velocity (including wall jump forces)
        _cc.Move(_vel * Time.fixedDeltaTime);

        // Gradually reduce horizontal momentum from wall jumps
        if (_isWallJumping)
        {
            _vel.x = Mathf.Lerp(_vel.x, 0, Time.fixedDeltaTime * 2f);
            _vel.z = Mathf.Lerp(_vel.z, 0, Time.fixedDeltaTime * 2f);
        }
    }

    // Публичные методы для управления idle анимацией
    public void ResetIdleTimer()
    {
        _idleTimer = 0f;
        _isSpecialIdle = false;
        if (_animator != null)
        {
            _animator.SetBool(_isSpecialIdleHash, false);
        }
    }

    public void ForceSpecialIdle()
    {
        _isSpecialIdle = true;
        _idleTimer = _idleTimeThreshold;
        if (_animator != null)
        {
            _animator.SetBool(_isSpecialIdleHash, true);
            if (!string.IsNullOrEmpty(_idleAnimationClip))
            {
                _animator.Play(_idleAnimationClip, -1, 0f);
            }
        }
    }

    public bool IsInSpecialIdle()
    {
        return _isSpecialIdle;
    }

    public float GetIdleTime()
    {
        return _idleTimer;
    }

    // Отладочная информация
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Показываем зону проверки стен
            Gizmos.color = _canWallJump ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, _wallCheckDistance);

            // Показываем нормаль стены, если она есть
            if (_canWallJump)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, _wallNormal * 2f);
            }
        }
    }
}