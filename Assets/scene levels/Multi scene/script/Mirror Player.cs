using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MirrorPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speedwalk = 5f;
    [SerializeField] private float _dashSpeedMultiplier = 2f;
    [SerializeField] private float _dashDuration = 4f;
    [SerializeField] private float _gravity = 9.81f;
    [SerializeField] private float _jumpPower = 8f;
    [SerializeField] private float _fallMultiplier = 3f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem _dashEffect;
    [SerializeField] private AudioSource _footstepAudio;
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("UI")]
    [SerializeField] private Scrollbar _dashScrollbar;
    [SerializeField] private float _dashScrollbarSize = 0.3f;

    // Network synchronized variables
    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 _networkPosition;

    [SyncVar(hook = nameof(OnRotationChanged))]
    private Quaternion _networkRotation;

    [SyncVar(hook = nameof(OnDashStateChanged))]
    private bool _networkIsDashing = false;

    [SyncVar(hook = nameof(OnMovingStateChanged))]
    private bool _networkIsMoving = false;

    // Local variables
    private CharacterController _characterController;
    private Vector3 _walkDirection;
    private Vector3 _velocity;
    private bool _isDashing = false;
    private bool _wasMovingLastFrame = false;
    private float _originalScrollbarSize;

    private Coroutine _fadeCoroutine;

    // Network interpolation
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private float _lerpRate = 15f;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();

        // Initialize network position for server
        if (isServer)
        {
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
        }

        // Store original scrollbar size
        if (_dashScrollbar != null)
        {
            _originalScrollbarSize = _dashScrollbar.size;
        }
    }

    private void Update()
    {
        // Only process input for local player
        if (!isLocalPlayer)
        {
            // Interpolate position for non-local players
            InterpolatePosition();
            return;
        }

        // Handle input only for local player
        HandleInput();
        HandleFootstepAudio(_networkIsMoving);
    }

    private void HandleInput()
    {
        // Jump input
        if (_characterController.isGrounded && Input.GetKey(KeyCode.Space))
        {
            CmdJump();
        }

        // Movement input
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.D)) z = 1f;
        if (Input.GetKey(KeyCode.A)) z = -1f;
        if (Input.GetKey(KeyCode.W)) x = -1f;
        if (Input.GetKey(KeyCode.S)) x = 1f;
        if (Input.GetKey(KeyCode.LeftAlt)) z = -1f;

        // Dash input
        if (Input.GetKeyDown(KeyCode.LeftControl) && !_isDashing)
        {
            CmdStartDash();
        }

        // Send movement to server
        if (x != 0f || z != 0f)
        {
            Vector3 moveDirection = transform.right * x + transform.forward * z;
            CmdMove(moveDirection, Input.GetKey(KeyCode.LeftShift));
        }
        else
        {
            CmdMove(Vector3.zero, Input.GetKey(KeyCode.LeftShift));
        }
    }

    private void FixedUpdate()
    {
        // Only process physics on server
        if (!isServer) return;

        // Apply gravity
        DoGravity(_characterController.isGrounded);

        // Update network position
        if (Vector3.Distance(transform.position, _networkPosition) > 0.1f)
        {
            _networkPosition = transform.position;
        }

        if (Quaternion.Angle(transform.rotation, _networkRotation) > 1f)
        {
            _networkRotation = transform.rotation;
        }
    }

    #region Commands (Client to Server)

    [Command]
    private void CmdMove(Vector3 direction, bool fastFall)
    {
        _walkDirection = direction;

        // Update moving state
        bool isMoving = direction.magnitude > 0.1f && _characterController.isGrounded;
        if (_networkIsMoving != isMoving)
        {
            _networkIsMoving = isMoving;
        }

        // Apply movement
        Walk(direction);

        // Store fast fall state for gravity calculation
        _isFastFalling = fastFall;
    }

    [Command]
    private void CmdJump()
    {
        if (_characterController.isGrounded)
        {
            _velocity.y = _jumpPower;
        }
    }

    [Command]
    private void CmdStartDash()
    {
        if (!_isDashing)
        {
            StartCoroutine(ServerDash());
        }
    }

    #endregion

    #region Server Methods

    private bool _isFastFalling = false;

    private void Walk(Vector3 direction)
    {
        float speed = _isDashing ? _speedwalk * _dashSpeedMultiplier : _speedwalk;
        _characterController.Move(direction * speed * Time.fixedDeltaTime);
    }

    private void DoGravity(bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0)
            _velocity.y = -1f;

        float gravityToApply = _gravity;

        // Apply fall multiplier if client is holding shift
        if (_isFastFalling)
        {
            gravityToApply *= _fallMultiplier;
        }

        _velocity.y -= gravityToApply * Time.fixedDeltaTime;
        _characterController.Move(_velocity * Time.fixedDeltaTime);
    }

    private IEnumerator ServerDash()
    {
        _isDashing = true;
        _networkIsDashing = true;

        // Trigger dash effect on all clients
        RpcPlayDashEffect();

        yield return new WaitForSeconds(_dashDuration);

        _isDashing = false;
        _networkIsDashing = false;

        // Stop dash effect on all clients
        RpcStopDashEffect();
    }

    #endregion

    #region ClientRpc (Server to All Clients)

    [ClientRpc]
    private void RpcPlayDashEffect()
    {
        if (_dashEffect != null)
        {
            _dashEffect.Play();
        }

        // Update UI for local player
        if (isLocalPlayer && _dashScrollbar != null)
        {
            _dashScrollbar.size = _dashScrollbarSize;
        }
    }

    [ClientRpc]
    private void RpcStopDashEffect()
    {
        if (_dashEffect != null)
        {
            _dashEffect.Stop();
        }

        // Update UI for local player
        if (isLocalPlayer && _dashScrollbar != null)
        {
            _dashScrollbar.size = _originalScrollbarSize;
        }
    }

    #endregion

    #region SyncVar Hooks

    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        if (!isLocalPlayer)
        {
            _lastPosition = transform.position;
        }
    }

    private void OnRotationChanged(Quaternion oldRot, Quaternion newRot)
    {
        if (!isLocalPlayer)
        {
            _lastRotation = transform.rotation;
        }
    }

    private void OnDashStateChanged(bool oldState, bool newState)
    {
        _isDashing = newState;
    }

    private void OnMovingStateChanged(bool oldState, bool newState)
    {
        // Audio will be handled in Update for local player
    }

    #endregion

    #region Position Interpolation

    private void InterpolatePosition()
    {
        // Smooth position interpolation for non-local players
        if (Vector3.Distance(transform.position, _networkPosition) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _lerpRate);
        }

        if (Quaternion.Angle(transform.rotation, _networkRotation) > 1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * _lerpRate);
        }
    }

    #endregion

    #region Audio Handling

    private void HandleFootstepAudio(bool isMoving)
    {
        // Only handle audio for local player
        if (!isLocalPlayer) return;

        if (isMoving)
        {
            if (!_footstepAudio.isPlaying)
            {
                _footstepAudio.Play();
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _footstepAudio.volume = 1f;
            _wasMovingLastFrame = true;
        }
        else
        {
            if (_wasMovingLastFrame && _footstepAudio.isPlaying)
            {
                if (_fadeCoroutine == null)
                {
                    _fadeCoroutine = StartCoroutine(FadeOutFootsteps());
                }
            }
            _wasMovingLastFrame = false;
        }
    }

    private IEnumerator FadeOutFootsteps()
    {
        float startVolume = _footstepAudio.volume;
        float elapsed = 0f;

        while (elapsed < _fadeDuration && _footstepAudio != null)
        {
            if (_footstepAudio.volume <= 0f)
                break;

            _footstepAudio.volume = Mathf.Lerp(startVolume, 0f, elapsed / _fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_footstepAudio != null)
        {
            _footstepAudio.volume = 0f;
            _footstepAudio.Stop();
        }

        _fadeCoroutine = null;
    }

    #endregion

    #region Public Methods

    public void TriggerExternalDash()
    {
        if (isLocalPlayer && !_isDashing)
        {
            CmdStartDash();
        }
    }

    public void ForceJump(float force)
    {
        if (isLocalPlayer)
        {
            // Send custom jump force to server
            CmdForceJump(force);
        }
    }

    [Command]
    private void CmdForceJump(float force)
    {
        _velocity.y = force;
    }

    public void StopFootstepAudio()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (_footstepAudio != null && _footstepAudio.isPlaying)
        {
            _footstepAudio.Stop();
            _footstepAudio.volume = 0f;
        }

        _wasMovingLastFrame = false;
    }

    #endregion

}
