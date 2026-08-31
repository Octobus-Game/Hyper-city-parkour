using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Testcontroller : MonoBehaviour
{
    [Header("Basic Movement")]
    [SerializeField] private float _speedWalk = 5f;
    [SerializeField] private float _speedRun = 8f;
    [SerializeField] private float _speedCrouch = 2f;
    [SerializeField] private float _speedCrawl = 1f;
    [SerializeField] private float _speedSwim = 3f;

    [Header("Jump System")]
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private float _variableJumpMultiplier = 0.5f;
    [SerializeField] private int _maxJumps = 2;
    [SerializeField] private float _coyoteTime = 0.2f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private float _wallJumpForce = 8f;
    [SerializeField] private float _wallJumpSideForce = 5f;

    [Header("Dash System")]
    [SerializeField] private float _dashSpeedMultiplier = 2f;
    [SerializeField] private float _dashDuration = 0.3f;
    [SerializeField] private float _dashCooldown = 2f;
    [SerializeField] private int _maxDashes = 2;
    [SerializeField] private bool _canAirDash = true;
    [SerializeField] private float _dashInvulnerabilityTime = 0.1f;

    [Header("Slide System")]
    [SerializeField] private float _slideSpeed = 8f;
    [SerializeField] private float _slideDuration = 1f;
    [SerializeField] private float _slideDeceleration = 5f;

    [Header("Wall Climbing")]
    [SerializeField] private float _wallClimbSpeed = 3f;
    [SerializeField] private float _wallCheckDistance = 1f;
    [SerializeField] private LayerMask _wallLayer = 1;

    [Header("Stamina System")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 20f;
    [SerializeField] private float _runStaminaCost = 10f;
    [SerializeField] private float _dashStaminaCost = 30f;
    [SerializeField] private float _climbStaminaCost = 15f;

    [Header("Physics")]
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _fallMultiplier = 3f;
    [SerializeField] private float _lowJumpMultiplier = 2f;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask _groundLayer = 1;

    [Header("Terrain Effects")]
    [SerializeField] private float _iceSpeedMultiplier = 1.5f;
    [SerializeField] private float _mudSpeedMultiplier = 0.5f;
    [SerializeField] private float _waterSpeedMultiplier = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource _footstepAudio;
    [SerializeField] private AudioClip[] _footstepSounds;
    [SerializeField] private AudioClip[] _landingSounds;
    [SerializeField] private AudioClip _dashSound;
    [SerializeField] private AudioClip _jumpSound;
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem _dashEffect;
    [SerializeField] private ParticleSystem _landingEffect;
    [SerializeField] private ParticleSystem _runningEffect;
    [SerializeField] private ParticleSystem _wallSlideEffect;
    [SerializeField] private TrailRenderer _speedTrail;

    [Header("UI Elements")]
    [SerializeField] private Scrollbar _dashScrollbar;
    [SerializeField] private Scrollbar _staminaBar;
    [SerializeField] private Text _speedText;
    [SerializeField] private Text _statsText;

    [Header("Camera Effects")]
    [SerializeField] private float _walkBobAmount = 0.05f;
    [SerializeField] private float _walkBobSpeed = 14f;
    [SerializeField] private float _landingShakeIntensity = 0.3f;
    [SerializeField] private float _landingShakeDuration = 0.2f;

    // Components
    private CharacterController _characterController;
    private Transform _cameraTransform;

    // Movement State
    private Vector3 _walkDirection;
    private Vector3 _velocity;
    private MovementState _currentState = MovementState.Walking;
    private TerrainType _currentTerrain = TerrainType.Normal;

    // Jumping
    private int _jumpCount = 0;
    private float _coyoteTimeCounter = 0f;
    private float _jumpBufferCounter = 0f;
    private bool _isJumpPressed = false;
    private bool _wasGrounded = false;

    // Dashing
    private bool _isDashing = false;
    private int _dashCount = 0;
    private float _dashCooldownTimer = 0f;
    private Vector3 _dashDirection;
    private bool _isInvulnerable = false;

    // Sliding
    private bool _isSliding = false;
    private float _slideTimer = 0f;
    private Vector3 _slideDirection;

    // Wall mechanics
    private bool _isWallSliding = false;
    private bool _isClimbing = false;
    private Vector3 _wallNormal;
    private RaycastHit _wallHit;

    // Stamina
    private float _currentStamina;
    private bool _isExhausted = false;

    // Audio
    private Coroutine _fadeCoroutine;
    private int _currentFootstepIndex = 0;

    // Camera bobbing
    private Vector3 _originalCameraPos;
    private float _bobTimer = 0f;

    // Statistics
    private float _totalDistance = 0f;
    private int _totalJumps = 0;
    private int _totalDashes = 0;
    private Vector3 _lastPosition;

    // Interaction
    private List<GameObject> _pushableObjects = new List<GameObject>();

    public enum MovementState
    {
        Walking, Running, Crouching, Crawling, Swimming, Sliding, WallSliding, Climbing
    }

    public enum TerrainType
    {
        Normal, Ice, Mud, Water, Metal, Grass, Sand
    }

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _cameraTransform = Camera.main.transform;
        _originalCameraPos = _cameraTransform.localPosition;
        _currentStamina = _maxStamina;
        _lastPosition = transform.position;

        if (_speedTrail) _speedTrail.enabled = false;
    }

    private void Update()
    {
        UpdateInput();
        UpdateTimers();
        UpdateMovementState();
        UpdateStamina();
        UpdateUI();
        UpdateCameraBobbing();
        UpdateStatistics();

        HandleJumping();
        HandleDashing();
        HandleSliding();
        HandleWallInteraction();
        HandleAudio();
        HandlePushing();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        CheckWall();
        HandleMovement();
        HandleGravity();
        CheckTerrain();
    }

    private void UpdateInput()
    {
        // Jump input with buffering
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isJumpPressed = true;
            _jumpBufferCounter = _jumpBufferTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isJumpPressed = false;
        }

        // Dash input
        if (Input.GetKeyDown(KeyCode.LeftControl) && CanDash())
        {
            StartDash();
        }

        // Slide input
        if (Input.GetKeyDown(KeyCode.C) && CanSlide())
        {
            StartSlide();
        }

        // Movement state changes
        if (Input.GetKey(KeyCode.LeftShift) && _currentStamina > 0 && !_isExhausted)
        {
            if (_currentState != MovementState.Running && _currentState != MovementState.Sliding)
                _currentState = MovementState.Running;
        }
        else if (Input.GetKey(KeyCode.LeftAlt))
        {
            _currentState = MovementState.Crouching;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            _currentState = MovementState.Crawling;
        }
        else if (_currentState != MovementState.Sliding && _currentState != MovementState.WallSliding)
        {
            _currentState = MovementState.Walking;
        }
    }

    private void UpdateTimers()
    {
        if (_jumpBufferCounter > 0) _jumpBufferCounter -= Time.deltaTime;
        if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;
        if (_slideTimer > 0) _slideTimer -= Time.deltaTime;

        // Coyote time
        if (_characterController.isGrounded)
        {
            _coyoteTimeCounter = _coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        float x = 0f, z = 0f;

        // Get input
        if (Input.GetKey(KeyCode.D)) z = 1f;
        if (Input.GetKey(KeyCode.A)) z = -1f;
        if (Input.GetKey(KeyCode.W)) x = -1f;
        if (Input.GetKey(KeyCode.S)) x = 1f;

        _walkDirection = transform.right * x + transform.forward * z;
        _walkDirection.Normalize();

        if (_isSliding)
        {
            HandleSlideMovement();
        }
        else if (_isClimbing)
        {
            HandleClimbingMovement();
        }
        else if (_isWallSliding)
        {
            HandleWallSlideMovement();
        }
        else
        {
            HandleNormalMovement();
        }
    }

    private void HandleNormalMovement()
    {
        if (_walkDirection.magnitude < 0.1f) return;

        float speed = GetCurrentSpeed();
        Vector3 movement = _walkDirection * speed;

        // Apply terrain effects
        movement *= GetTerrainSpeedMultiplier();

        // Apply dash multiplier
        if (_isDashing)
        {
            movement = _dashDirection * speed * _dashSpeedMultiplier;
        }

        _characterController.Move(movement * Time.fixedDeltaTime);

        // Consume stamina for running
        if (_currentState == MovementState.Running && _walkDirection.magnitude > 0.1f)
        {
            ConsumeStamina(_runStaminaCost * Time.fixedDeltaTime);
        }
    }

    private void HandleSlideMovement()
    {
        float slideSpeedCurrent = Mathf.Lerp(_slideSpeed, 0f, (_slideDuration - _slideTimer) / _slideDuration);
        _characterController.Move(_slideDirection * slideSpeedCurrent * Time.fixedDeltaTime);

        if (_slideTimer <= 0f)
        {
            EndSlide();
        }
    }

    private void HandleClimbingMovement()
    {
        if (!Input.GetKey(KeyCode.W)) return;

        Vector3 climbMove = Vector3.up * _wallClimbSpeed;
        _characterController.Move(climbMove * Time.fixedDeltaTime);
        ConsumeStamina(_climbStaminaCost * Time.fixedDeltaTime);

        if (_currentStamina <= 0)
        {
            _isClimbing = false;
        }
    }

    private void HandleWallSlideMovement()
    {
        // Slow fall when wall sliding
        if (_velocity.y < 0)
        {
            _velocity.y = Mathf.Max(_velocity.y, -2f);
        }
    }

    private void HandleJumping()
    {
        bool isGrounded = _characterController.isGrounded || _coyoteTimeCounter > 0f;

        if (_jumpBufferCounter > 0f && CanJump(isGrounded))
        {
            PerformJump();
            _jumpBufferCounter = 0f;
        }

        // Variable jump height
        if (!_isJumpPressed && _velocity.y > 0f)
        {
            _velocity.y *= _variableJumpMultiplier;
        }

        // Reset jump count when grounded
        if (_characterController.isGrounded && !_wasGrounded)
        {
            _jumpCount = 0;
            _dashCount = 0; // Reset air dash count
            PlayLandingEffects();
        }

        _wasGrounded = _characterController.isGrounded;
    }

    private void PerformJump()
    {
        if (_isWallSliding)
        {
            // Wall jump
            _velocity.y = _wallJumpForce;
            Vector3 wallJumpDirection = _wallNormal + Vector3.up;
            _characterController.Move(wallJumpDirection * _wallJumpSideForce * Time.fixedDeltaTime);
            _isWallSliding = false;
        }
        else
        {
            _velocity.y = _jumpPower;
        }

        _jumpCount++;
        _totalJumps++;
        _coyoteTimeCounter = 0f;

        PlayJumpEffects();
    }

    private void HandleDashing()
    {
        if (_isDashing)
        {
            if (_dashDuration <= 0f)
            {
                EndDash();
            }
            else
            {
                _dashDuration -= Time.deltaTime;
            }
        }
    }

    private void StartDash()
    {
        if (!CanDash()) return;

        _isDashing = true;
        _dashCount++;
        _totalDashes++;
        _dashDuration = 0.3f; // Reset duration
        _dashCooldownTimer = _dashCooldown;

        // Set dash direction
        if (_walkDirection.magnitude > 0.1f)
        {
            _dashDirection = _walkDirection.normalized;
        }
        else
        {
            _dashDirection = transform.forward;
        }

        ConsumeStamina(_dashStaminaCost);
        StartCoroutine(DashInvulnerability());
        PlayDashEffects();
    }

    private void EndDash()
    {
        _isDashing = false;
        if (_dashEffect) _dashEffect.Stop();
        if (_speedTrail) _speedTrail.enabled = false;
    }

    private void HandleSliding()
    {
        if (_isSliding && _slideTimer <= 0f)
        {
            EndSlide();
        }
    }

    private void StartSlide()
    {
        _isSliding = true;
        _slideTimer = _slideDuration;
        _slideDirection = _walkDirection.magnitude > 0.1f ? _walkDirection.normalized : transform.forward;
        _currentState = MovementState.Sliding;
    }

    private void EndSlide()
    {
        _isSliding = false;
        _currentState = MovementState.Walking;
    }

    private void HandleWallInteraction()
    {
        if (_isWallSliding)
        {
            if (_wallSlideEffect && !_wallSlideEffect.isPlaying)
            {
                _wallSlideEffect.Play();
            }
        }
        else
        {
            if (_wallSlideEffect && _wallSlideEffect.isPlaying)
            {
                _wallSlideEffect.Stop();
            }
        }
    }

    private void HandleGravity()
    {
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -1f;
        }

        float gravityToApply = _gravity;

        // Enhanced gravity when falling fast or holding shift
        if (Input.GetKey(KeyCode.LeftShift) || _velocity.y < 0)
        {
            gravityToApply *= _velocity.y > 0 ? _lowJumpMultiplier : _fallMultiplier;
        }

        _velocity.y -= gravityToApply * Time.fixedDeltaTime;
        _characterController.Move(_velocity * Time.fixedDeltaTime);
    }

    private void CheckGrounded()
    {
        // More accurate ground checking
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _characterController.height / 2, transform.position.z);
        if (Physics.CheckSphere(spherePosition, _characterController.radius + _groundCheckDistance, _groundLayer))
        {
            if (!_characterController.isGrounded)
            {
                _characterController.enabled = false;
                _characterController.enabled = true;
            }
        }
    }

    private void CheckWall()
    {
        Vector3 forward = transform.forward;

        if (Physics.Raycast(transform.position, forward, out _wallHit, _wallCheckDistance, _wallLayer))
        {
            _wallNormal = _wallHit.normal;

            if (!_characterController.isGrounded && _velocity.y < 0 && Input.GetKey(KeyCode.W))
            {
                if (Input.GetKey(KeyCode.LeftShift) && _currentStamina > 0)
                {
                    _isClimbing = true;
                    _isWallSliding = false;
                }
                else
                {
                    _isWallSliding = true;
                    _isClimbing = false;
                }
            }
        }
        else
        {
            _isWallSliding = false;
            _isClimbing = false;
        }
    }

    private void CheckTerrain()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, _characterController.height / 2 + 0.5f))
        {
            string terrainTag = hit.collider.tag;
            switch (terrainTag)
            {
                case "Ice": _currentTerrain = TerrainType.Ice; break;
                case "Mud": _currentTerrain = TerrainType.Mud; break;
                case "Water": _currentTerrain = TerrainType.Water; break;
                case "Metal": _currentTerrain = TerrainType.Metal; break;
                case "Grass": _currentTerrain = TerrainType.Grass; break;
                case "Sand": _currentTerrain = TerrainType.Sand; break;
                default: _currentTerrain = TerrainType.Normal; break;
            }
        }
    }

    private void HandlePushing()
    {
        // Simple pushing mechanic
        if (Input.GetKey(KeyCode.E))
        {
            Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, 2f);
            foreach (Collider col in nearbyObjects)
            {
                if (col.CompareTag("Pushable"))
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb)
                    {
                        Vector3 pushDirection = (col.transform.position - transform.position).normalized;
                        rb.AddForce(pushDirection * 5f, ForceMode.Force);
                    }
                }
            }
        }
    }

    private void UpdateStamina()
    {
        if (_currentStamina < _maxStamina && !IsConsumingStamina())
        {
            _currentStamina = Mathf.Min(_maxStamina, _currentStamina + _staminaRegenRate * Time.deltaTime);
        }

        if (_currentStamina <= 0 && !_isExhausted)
        {
            _isExhausted = true;
            StartCoroutine(ExhaustionRecovery());
        }
    }

    private void ConsumeStamina(float amount)
    {
        _currentStamina = Mathf.Max(0, _currentStamina - amount);
    }

    private bool IsConsumingStamina()
    {
        return _currentState == MovementState.Running || _isClimbing;
    }

    private IEnumerator ExhaustionRecovery()
    {
        yield return new WaitForSeconds(2f);
        _isExhausted = false;
    }

    private void UpdateMovementState()
    {
        if (_isSliding)
            _currentState = MovementState.Sliding;
        else if (_isWallSliding)
            _currentState = MovementState.WallSliding;
        else if (_isClimbing)
            _currentState = MovementState.Climbing;
        else if (_currentTerrain == TerrainType.Water)
            _currentState = MovementState.Swimming;
    }

    private void UpdateCameraBobbing()
    {
        if (_characterController.isGrounded && _walkDirection.magnitude > 0.1f && _currentState != MovementState.Sliding)
        {
            _bobTimer += Time.deltaTime * _walkBobSpeed;
            float bobOffset = Mathf.Sin(_bobTimer) * _walkBobAmount;
            _cameraTransform.localPosition = _originalCameraPos + Vector3.up * bobOffset;
        }
        else
        {
            _bobTimer = 0f;
            _cameraTransform.localPosition = Vector3.Lerp(_cameraTransform.localPosition, _originalCameraPos, Time.deltaTime * 5f);
        }
    }

    private void UpdateUI()
    {
        if (_staminaBar)
            _staminaBar.size = _currentStamina / _maxStamina;

        if (_dashScrollbar)
            _dashScrollbar.size = _dashCooldownTimer > 0 ? _dashCooldownTimer / _dashCooldown : 1f;

        if (_speedText)
        {
            float currentSpeed = _characterController.velocity.magnitude;
            _speedText.text = $"Speed: {currentSpeed:F1} m/s";
        }
    }

    private void UpdateStatistics()
    {
        _totalDistance += Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if (_statsText)
        {
            _statsText.text = $"Distance: {_totalDistance:F1}m\nJumps: {_totalJumps}\nDashes: {_totalDashes}";
        }
    }

    private void HandleAudio()
    {
        bool isMoving = _walkDirection.magnitude > 0.1f && _characterController.isGrounded;

        if (isMoving && _currentState != MovementState.Sliding)
        {
            if (!_footstepAudio.isPlaying)
            {
                PlayFootstepSound();
            }
        }
        else
        {
            if (_footstepAudio.isPlaying && _fadeCoroutine == null)
            {
                _fadeCoroutine = StartCoroutine(FadeOutFootsteps());
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (_footstepSounds.Length > 0)
        {
            _footstepAudio.clip = _footstepSounds[_currentFootstepIndex % _footstepSounds.Length];
            _footstepAudio.pitch = Random.Range(0.9f, 1.1f);
            _footstepAudio.Play();
            _currentFootstepIndex++;
        }
    }

    private void PlayJumpEffects()
    {
        if (_jumpSound)
        {
            AudioSource.PlayClipAtPoint(_jumpSound, transform.position);
        }
    }

    private void PlayDashEffects()
    {
        if (_dashEffect) _dashEffect.Play();
        if (_speedTrail) _speedTrail.enabled = true;
        if (_dashSound)
        {
            AudioSource.PlayClipAtPoint(_dashSound, transform.position);
        }
    }

    private void PlayLandingEffects()
    {
        if (_landingEffect) _landingEffect.Play();
        if (_landingSounds.Length > 0)
        {
            AudioClip landingClip = _landingSounds[Random.Range(0, _landingSounds.Length)];
            AudioSource.PlayClipAtPoint(landingClip, transform.position);
        }

        // Camera shake
        StartCoroutine(CameraShake());
    }

    private IEnumerator CameraShake()
    {
        float elapsed = 0f;
        Vector3 originalPos = _cameraTransform.localPosition;

        while (elapsed < _landingShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * _landingShakeIntensity;
            float y = Random.Range(-1f, 1f) * _landingShakeIntensity;

            _cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _cameraTransform.localPosition = originalPos;
    }

    private IEnumerator DashInvulnerability()
    {
        _isInvulnerable = true;
        yield return new WaitForSeconds(_dashInvulnerabilityTime);
        _isInvulnerable = false;
    }

    private IEnumerator FadeOutFootsteps()
    {
        float startVolume = _footstepAudio.volume;
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            _footstepAudio.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _footstepAudio.volume = 0f;
        _footstepAudio.Stop();
        _fadeCoroutine = null;
    }

    // Helper Methods
    private float GetCurrentSpeed()
    {
        switch (_currentState)
        {
            case MovementState.Running: return _speedRun;
            case MovementState.Crouching: return _speedCrouch;
            case MovementState.Crawling: return _speedCrawl;
            case MovementState.Swimming: return _speedSwim;
            case MovementState.Sliding: return _slideSpeed;
            default: return _speedWalk;
        }
    }

    private float GetTerrainSpeedMultiplier()
    {
        switch (_currentTerrain)
        {
            case TerrainType.Ice: return _iceSpeedMultiplier;
            case TerrainType.Mud: return _mudSpeedMultiplier;
            case TerrainType.Water: return _waterSpeedMultiplier;
            default: return 1f;
        }
    }

    private bool CanJump(bool isGrounded)
    {
        return (isGrounded && _jumpCount == 0) || (_jumpCount < _maxJumps && !isGrounded);
    }

    private bool CanDash()
    {
        if (_currentStamina < _dashStaminaCost) return false;
        if (_dashCooldownTimer > 0) return false;

        if (_characterController.isGrounded) return true;
        if (_canAirDash && _dashCount < _maxDashes) return true;

        return false;
    }

    private bool CanSlide()
    {
        return _characterController.isGrounded && !_isSliding && _walkDirection.magnitude > 0.1f;
    }

    // Public Methods for external access
    public void TriggerExternalDash()
    {
        if (CanDash()) StartDash();
    }

    public void ForceJump(float force)
    {
        _velocity.y = force;
    }

    public void AddStamina(float amount)
    {
        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + amount);
    }

    public void SetTerrainType(TerrainType terrain)
    {
        _currentTerrain = terrain;
    }

    public bool IsInvulnerable()
    {
        return _isInvulnerable;
    }

    public MovementState GetCurrentState()
    {
        return _currentState;
    }

    public float GetCurrentStamina()
    {
        return _currentStamina;
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _wallCheckDistance);

        Gizmos.color = Color.green;
        Vector3 groundCheck = new Vector3(transform.position.x, transform.position.y - GetComponent<CharacterController>().height / 2, transform.position.z);
        Gizmos.DrawWireSphere(groundCheck, GetComponent<CharacterController>().radius + _groundCheckDistance);
    }
}
