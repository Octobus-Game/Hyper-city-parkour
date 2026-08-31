using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro; 

namespace P90brush
{
    public class PlayerLogic_mirror : NetworkBehaviour, ISurfControllable
    {
        public float _walkSpeed = 80f;
        public float _jumpForce = 40f;

        [Header("Physics Settings")]
        public int    _tickRate  = 128;
        public Camera _fpsCamera;

        [Header("UI")]
        public Text _debugText;

        [Header("Speed Display")]
        [SerializeField] private TextMeshProUGUI _speedText;

        [Header("Movement Config")]
        [SerializeField]
        public MovementConfig moveConfig = new MovementConfig();

        [Header("Hookshot")]
        [SerializeField]
        public Hookshot _hookshot;

        [Header("Slow Motion")]
        public SlowMotion _slowMotion;

        [Header("Movement Sounds")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip   _jumpSound;
        [SerializeField] private AudioClip[] _footstepSounds;   // масив — рандомний крок
        [SerializeField] private float       _footstepInterval = 0.35f; // секунд між кроками

        #region Private

        private Rigidbody      _rb;
        private Collider       _collider;
        private Vector3        _startPosition;
        private Quaternion     _originalRotation;
        private SurfController _controller = new SurfController();

        // Звуки
        private bool  _wasGroundedLastFrame;
        private float _footstepTimer;

        #endregion

        #region SyncVars

        [SyncVar] private Vector3 _syncedPosition;
        [SyncVar] private Vector3 _syncedVelocity;
        [SyncVar] private Vector3 _syncedViewAngles;
        [SyncVar] private bool    _syncedIsHooked;
        [SyncVar] private Vector3 _syncedHookedPosition;

        #endregion

        #region ISurfControllable

        public MovementConfig MoveConfig    => moveConfig;
        public PlayerData     PlayerData    { get; } = new PlayerData();
        public InputData      InputData     { get; } = new InputData();
        public Collider       Collider      => _collider;
        public Vector3        BaseVelocity  { get; } = Vector3.zero;
        public Camera         FpsCamera     => _fpsCamera;

        #endregion

        // ================================================================ Unity

        private void Awake()
        {
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount  = 1;
            Time.fixedDeltaTime         = 1f / _tickRate;
        }

        void Start()
        {
            if (_hookshot   == null) _hookshot   = GetComponentInChildren<Hookshot>();
            if (_slowMotion == null) _slowMotion  = GetComponentInChildren<SlowMotion>();
            if (_fpsCamera  == null) _fpsCamera   = GetComponentInChildren<Camera>();

            // Авто-пошук AudioSource якщо не призначено
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.isKinematic    = true;
                _rb.freezeRotation = true;
            }

            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;

            PlayerData.Origin = transform.position;
            _startPosition    = transform.position;
            _originalRotation = transform.localRotation;

            if (isLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                if (_fpsCamera != null) _fpsCamera.gameObject.SetActive(true);
            }
            else
            {
                if (_fpsCamera != null) _fpsCamera.gameObject.SetActive(false);
                if (_speedText != null) _speedText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (!isLocalPlayer)
            {
                ApplyRemoteState();
                return;
            }

            UpdateUI();
            UpdateSpeedText();
            UpdateMovementSounds();  // <-- додано

            if (PauseMenu.GameIsPaused) return;

            InputData.Update(moveConfig);
            UpdateHook();
            UpdateSlowMotion();
            UpdateViewAngle();
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer) return;

            SyncExternalPositionChange();

            if (InputData.ResetPressed)
            {
                Teleport(_startPosition);
                if (_slowMotion != null) _slowMotion.IsSlowMotion = false;
            }

            float dt = Time.fixedDeltaTime;

            if (_hookshot != null)
                _hookshot.CatchMovement(this, dt);

            _controller.ProcessMovement(this, moveConfig, dt);
            ApplyPlayerMovement();

            CmdSendState(
                PlayerData.Origin,
                PlayerData.Velocity,
                PlayerData.ViewAngles,
                PlayerData.IsHooked(),
                PlayerData.HookedPosition);
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer) return;
            ApplyMouseMovement();
        }

        // ================================================================ Commands & RPCs

        [Command]
        private void CmdSendState(Vector3 origin, Vector3 velocity, Vector3 viewAngles,
            bool isHooked, Vector3 hookedPosition)
        {
            _syncedPosition       = origin;
            _syncedVelocity       = velocity;
            _syncedViewAngles     = viewAngles;
            _syncedIsHooked       = isHooked;
            _syncedHookedPosition = hookedPosition;
        }

        [Command]
        private void CmdTriggerHook() => RpcTriggerHook();

        [Command]
        private void CmdSetSlowMotion(bool value) => RpcSetSlowMotion(value);

        [ClientRpc]
        private void RpcTriggerHook()
        {
            if (isLocalPlayer || _hookshot == null) return;
            _hookshot.TriggerHook(this);
        }

        [ClientRpc]
        private void RpcSetSlowMotion(bool value)
        {
            if (isLocalPlayer || _slowMotion == null) return;
            _slowMotion.IsSlowMotion = value;
        }

        [Server]
        public void ServerTeleport(Vector3 position)
        {
            PlayerData.Origin = position;
            PlayerData.Velocity = Vector3.zero;
            PlayerData.HookedPosition = Vector3.zero;

            _syncedPosition = position;
            _syncedVelocity = Vector3.zero;
            _syncedIsHooked = false;
            _syncedHookedPosition = Vector3.zero;

            if (_rb != null)
            {
                _rb.position = position;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            transform.position = position;

            if (connectionToClient != null)
                TargetTeleport(connectionToClient, position);
        }

        [TargetRpc]
        private void TargetTeleport(NetworkConnectionToClient target, Vector3 position)
        {
            Teleport(position);
        }

        // ================================================================ Private helpers

        public void Teleport(Vector3 position, bool resetVelocity = true)
        {
            PlayerData.Origin = position;

            if (resetVelocity)
                PlayerData.Velocity = Vector3.zero;

            if (_hookshot != null)
                _hookshot.ForceRelease(this);
            else
                PlayerData.HookedPosition = Vector3.zero;

            if (_rb != null)
            {
                _rb.position = position;
                _rb.velocity = resetVelocity ? Vector3.zero : _rb.velocity;
                _rb.angularVelocity = Vector3.zero;
            }

            transform.position = position;
        }

        private void ApplyRemoteState()
        {
            transform.position = Vector3.Lerp(
                transform.position, _syncedPosition, Time.deltaTime * 20f);

            var xQ = Quaternion.AngleAxis(_syncedViewAngles.y, Vector3.up);
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation, xQ, Time.deltaTime * 20f);

            if (_fpsCamera != null)
            {
                var yQ = Quaternion.AngleAxis(_syncedViewAngles.x, Vector3.right);
                _fpsCamera.transform.localRotation = Quaternion.Lerp(
                    _fpsCamera.transform.localRotation, yQ, Time.deltaTime * 20f);
            }
        }

        private void UpdateUI()
        {
            if (_debugText == null) return;

            float mag = PlayerData.Velocity.x * PlayerData.Velocity.x
                      + PlayerData.Velocity.z * PlayerData.Velocity.z;

            _debugText.text = string.Format(
                "Velocity: {0}\n  x {1}\n  y {2}\n  z {3}\nFlyingStatus: {4}\nHooked: {5} {6}",
                mag,
                PlayerData.Velocity.x.ToString("F2"),
                PlayerData.Velocity.y.ToString("F2"),
                PlayerData.Velocity.z.ToString("F2"),
                PlayerData.IsGrounded() ? "OnGround" : "Fly/Surfing",
                PlayerData.IsHooked()
                    ? Vector3.Distance(PlayerData.Origin, PlayerData.HookedPosition).ToString("F2")
                    : "-1",
                _hookshot != null ? _hookshot.GetStatus().ToString() : "N/A");
        }

        // ---------------------------------------------------------------- Speed TMP
        private void UpdateSpeedText()
        {
            if (_speedText == null) return;

            float horizontalSpeed = new Vector2(
                PlayerData.Velocity.x,
                PlayerData.Velocity.z).magnitude;

            int speed = Mathf.Clamp(Mathf.RoundToInt(horizontalSpeed), 0, 999);
            _speedText.text = speed.ToString("D3");
        }

        // ---------------------------------------------------------------- Movement sounds
        private void UpdateMovementSounds()
        {
            if (_audioSource == null) return;

            bool isGrounded = PlayerData.IsGrounded();

            // Звук стрибка: гравець щойно відірвався від землі
            if (_wasGroundedLastFrame && !isGrounded && InputData.JumpPressed)
                PlayJumpSound();

            // Кроки: тільки на землі і з горизонтальним рухом
            if (isGrounded)
            {
                float horizontalSpeed = new Vector2(
                    PlayerData.Velocity.x,
                    PlayerData.Velocity.z).magnitude;

                bool isMoving = horizontalSpeed > 0.5f;

                if (isMoving)
                {
                    _footstepTimer -= Time.deltaTime;
                    if (_footstepTimer <= 0f)
                    {
                        PlayFootstep();
                        // Інтервал скорочується з швидкістю — чим швидше, тим частіше
                        float speedFactor = Mathf.Clamp01(horizontalSpeed / _walkSpeed);
                        _footstepTimer = Mathf.Lerp(_footstepInterval, _footstepInterval * 0.4f, speedFactor);
                    }
                }
                else
                {
                    // Стоїмо — скидаємо таймер щоб перший крок грав одразу
                    _footstepTimer = 0f;
                }
            }
            else
            {
                _footstepTimer = 0f;
            }

            _wasGroundedLastFrame = isGrounded;
        }

        private void PlayJumpSound()
        {
            if (_jumpSound == null) return;
            _audioSource.PlayOneShot(_jumpSound);
        }

        private void PlayFootstep()
        {
            if (_footstepSounds == null || _footstepSounds.Length == 0) return;
            var clip = _footstepSounds[Random.Range(0, _footstepSounds.Length)];
            if (clip != null) _audioSource.PlayOneShot(clip);
        }
        // ----------------------------------------------------------------

        private void UpdateHook()
        {
            if (_hookshot == null) return;

            if (!InputData.HookPressedLastUpdate && InputData.HookPressed)
            {
                _hookshot.TriggerHook(this);
                CmdTriggerHook();
            }
            _hookshot.CheckForRelease(this);
        }

        private void UpdateSlowMotion()
        {
            if (_slowMotion == null) return;

            if (!_slowMotion.IsSlowMotion && InputData.SlowMotionPressed)
            {
                _slowMotion.IsSlowMotion = true;
                CmdSetSlowMotion(true);
            }
        }

        private void UpdateViewAngle()
        {
            var rot = PlayerData.ViewAngles + new Vector3(-InputData.MouseY, InputData.MouseX, 0f);
            rot.x = GameUtils.ClampAngle(rot.x, -85f, 85f);
            PlayerData.ViewAngles = rot;
        }

        private void ApplyPlayerMovement()
        {
            if (_rb != null)
                _rb.position = PlayerData.Origin;

            transform.position = PlayerData.Origin;
        }

        private void SyncExternalPositionChange()
        {
            if ((transform.position - PlayerData.Origin).sqrMagnitude < 0.0001f)
                return;

            PlayerData.Origin = transform.position;

            if (_rb != null)
                _rb.position = transform.position;
        }

        private void ApplyMouseMovement()
        {
            if (_fpsCamera == null) return;

            var yQ = Quaternion.AngleAxis(PlayerData.ViewAngles.x, Vector3.right);
            var xQ = Quaternion.AngleAxis(PlayerData.ViewAngles.y, Vector3.up);
            transform.localRotation            = xQ;
            _fpsCamera.transform.localRotation = yQ;
        }
    }
}
