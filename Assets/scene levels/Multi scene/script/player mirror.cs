using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mirrorplayer
{
    [RequireComponent(typeof(CharacterController))]
    public class playermirror : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float walkingSpeed = 7.5f;
        public float runningSpeed = 11.5f;
        public float jumpSpeed = 8.0f;
        public float gravity = 20.0f;

        [Header("Inertia Settings")]
        public bool inertiaEnabled = true;
        [Tooltip("Насколько быстро игрок разгоняется на земле (больше = резче старт)")]
        public float groundAcceleration = 15.0f;
        [Tooltip("Насколько быстро игрок тормозит на земле (больше = резче стоп)")]
        public float groundDeceleration = 8.0f;
        [Tooltip("Ускорение в воздухе (меньше = «плавающее» управление)")]
        public float airAcceleration = 4.0f;
        [Tooltip("Торможение в воздухе (меньше = дольше летит)")]
        public float airDeceleration = 1.5f;

        [Header("Admin Abilities")]
        [Tooltip("Назначается автоматически в Start(): Host -> true, Client -> false. Ручное значение в инспекторе будет перезаписано при запуске.")]
        public bool isAdmin = false;
        public KeyCode flyKey = KeyCode.B;
        public KeyCode noclipKey = KeyCode.N;
        public float flySpeed = 15.0f;
        public float flySpeedMultiplier = 2.0f;
        public float flyAcceleration = 5.0f;
        public float flyDeceleration = 8.0f;

        [Header("Instant Landing Settings")]
        public bool instantLandingEnabled = true;
        public KeyCode instantLandingKey = KeyCode.LeftControl;
        public float instantLandingSpeed = 50.0f;
        public float instantLandingRayDistance = 100.0f;

        [Header("Wall Jump Settings")]
        public bool wallJumpEnabled = true;
        public float wallJumpForce = 15.0f;
        public float wallJumpSideForce = 10.0f;
        public float wallSlideSpeed = 2.0f;
        public float wallStickTime = 0.25f;
        public float wallJumpCooldown = 0.1f;
        public LayerMask wallLayerMask = -1;
        public float wallCheckDistance = 0.6f;
        public float wallCheckHeight = 1.5f;
        public int maxWallJumps = 2;
        public bool resetWallJumpsOnGroundTouch = true;

        [Header("Wall Jump Audio (Optional)")]
        public AudioClip wallSlideSound;
        public AudioClip wallJumpSound;
        public AudioSource audioSource;

        [Header("Moving Platform")]
        [Tooltip("Слои, на которых находятся движущиеся платформы. Игрок будет «прилипать» к ним, пока стоит сверху.")]
        public LayerMask platformLayerMask = -1;
        [Tooltip("Дистанция проверки лучом вниз от ног игрока, чтобы обнаружить платформу под ним")]
        public float platformCheckDistance = 0.3f;

        [Header("Camera Settings")]
        public Camera playerCamera;
        public float lookSpeed = 2.0f;
        public float lookXLimit = 45.0f;

        [Header("Camera Tilt Settings")]
        public bool cameraTiltEnabled = true;
        public float maxTiltAngle = 15.0f;
        // tiltSpeed УДАЛЁН из заголовка — он вызывал умножение угла и давал ±75°+
        // Используйте tiltSmoothness для управления скоростью Lerp.
        [Tooltip("Скорость сглаживания наклона (только Lerp, НЕ умножается в целевой угол)")]
        public float tiltSmoothness = 8.0f;

        [Header("UI Interaction (Local Only)")]
        public KeyCode uiToggleKey = KeyCode.Tab;
        public GameObject uiCanvas;
        public bool startWithUIEnabled = false;

        [Header("Local Only Objects")]
        [Tooltip("Объекты, которые должны быть активны ТОЛЬКО у локального игрока " +
                 "(не синхронизируются по сети, никогда не видны на remote-копиях этого игрока " +
                 "на других клиентах). Например: собственная модель головы/шлема, локальные маркеры, " +
                 "локальный HUD-объект в мировом пространстве и т.п.")]
        public GameObject[] localOnlyObjects;

        [Header("Speed Display UI")]
        public Text speedText;
        public bool showSpeedUI = true;
        [Tooltip("Показывать полную 3D-скорость (включая вертикаль / Ctrl-спуск)")]
        public bool showFullSpeed = true;
        public string speedFormat = "Speed: {0:F1} m/s";
        [Tooltip("Дополнительно показывать вертикальную составляющую")]
        public bool showVerticalSpeed = false;
        public string verticalSpeedFormat = "  ↕ {0:F1} m/s";
        public int speedUpdateRate = 10;

        [Header("Network Camera Objects")]
        public Transform cameraObjectsParent;
        public GameObject[] cameraAttachedObjects;

        [Header("Static Network Objects")]
        public GameObject[] staticNetworkObjects;
        public bool staticObjectsVisibleByDefault = true;

        [Header("Debug")]
        public bool showCursorState = false;
        public bool showWallJumpDebug = false;
        public bool showAdminDebug = false;

        // ─── Components ────────────────────────────────────────────────
        CharacterController characterController;

        // ─── Movement ──────────────────────────────────────────────────
        Vector3 moveDirection = Vector3.zero;
        float rotationX = 0;

        // ─── Camera rotation guard ─────────────────────────────────────
        // FIX: пропускаем первые кадры считывания мыши, чтобы избежать
        // сплайка дельты при блокировке курсора (была причина авто-поворота).
        private int inputWarmupFrames = 2;
        private int inputFrameCounter = 0;

        // ─── Inertia ───────────────────────────────────────────────────
        private Vector3 smoothedHorizontalVelocity = Vector3.zero;

        // ─── Admin ─────────────────────────────────────────────────────
        private bool isFlyMode = false;
        private bool isNoclipMode = false;
        private Vector3 flyVelocity = Vector3.zero;

        // ─── Instant Landing ───────────────────────────────────────────
        private bool isInstantLanding = false;

        // ─── Ground stick (анти-дрожание CharacterController.isGrounded) ─
        // Служебное значение moveDirection.y, которое держит контроллер прижатым
        // к земле на спусках/ступеньках. Это НЕ настоящая скорость падения, поэтому
        // его нужно исключать из отображаемой пользователю скорости (см. GetDisplayVerticalSpeed).
        private const float GroundStickVelocity = -2f;

        // ─── Camera Tilt ───────────────────────────────────────────────
        private float currentTiltZ = 0f;
        private float targetTiltZ = 0f;

        // ─── Wall Jump ─────────────────────────────────────────────────
        private bool isTouchingWall = false;
        private bool isWallSliding = false;
        private Vector3 wallNormal = Vector3.zero;
        private float wallStickTimer = 0f;
        private float wallJumpTimer = 0f;
        private int currentWallJumps = 0;
        private bool wasGrounded = false;
        private Vector3 lastWallNormal = Vector3.zero;
        private bool canWallJump = true;

        // ─── Moving Platform ───────────────────────────────────────────
        private Transform currentPlatform = null;
        private Vector3 platformLastPosition = Vector3.zero;

        // ─── Network SyncVars ──────────────────────────────────────────
        [SyncVar] private float networkRotationX = 0;
        [SyncVar] private Quaternion networkBodyRotation = Quaternion.identity;
        [SyncVar] private float networkTiltZ = 0;

        // ─── State ─────────────────────────────────────────────────────
        // canMove  — разрешает ходьбу/прыжок/гравитацию/wall-jump (HandleMovement и т.д.)
        // canLook  — разрешает поворот камеры и тела игрока (HandleCameraRotation/Tilt)
        // Флаги независимы: например, во время лазания по лестнице можно
        // выключить canMove, оставив canLook = true, чтобы игрок мог осматриваться.
        [HideInInspector] public bool canMove = true;
        [HideInInspector] public bool canLook = true;
        private bool isUIMode = false;
        private bool wasUIMode = false;

        [SyncVar(hook = nameof(OnCameraObjectsVisibilityChanged))]
        private bool cameraObjectsVisible = false;

        [SyncVar(hook = nameof(OnStaticObjectsVisibilityChanged))]
        private bool staticObjectsVisible = true;

        // ═══════════════════════════════════════════════════════════════
        void Start()
        {
            characterController = GetComponent<CharacterController>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            Debug.Log($"[{gameObject.name}] Start called. IsLocalPlayer: {isLocalPlayer}, IsServer: {isServer}");

            if (!isLocalPlayer)
            {
                playerCamera.gameObject.SetActive(false);
                if (uiCanvas != null) uiCanvas.SetActive(false);

                // На remote-копиях этого игрока (у других клиентов) локальные объекты должны быть выключены.
                SetLocalOnlyObjectsActive(false);
            }
            else
            {
                // У локального игрока (у него самого) локальные объекты включены.
                SetLocalOnlyObjectsActive(true);

                // FIX 1: инициализируем rotationX из реального угла камеры в префабе,
                //         чтобы не было прыжка если камера стоит не горизонтально.
                float rawX = playerCamera.transform.localEulerAngles.x;
                rotationX = rawX > 180f ? rawX - 360f : rawX;   // нормализуем в [-180, 180]
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                currentTiltZ = 0f;
                targetTiltZ  = 0f;

                isUIMode = startWithUIEnabled;
                if (uiCanvas != null) uiCanvas.SetActive(isUIMode);
                Cursor.lockState = isUIMode ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = isUIMode;

                if (isServer)
                {
                    cameraObjectsVisible = !isUIMode;
                    staticObjectsVisible  = staticObjectsVisibleByDefault;
                }

                // Проверяем, является ли локальный игрок хостом или клиентом,
                // и выдаём/снимаем права Admin Abilities соответственно.
                DetermineAdminRights();
            }

            SetupCameraObjects();
            SetupStaticObjects();
        }

        // ═══════════════════════════════════════════════════════════════
        void Update()
        {
            if (!isLocalPlayer)
            {
                UpdateNetworkCameraObjects();
                return;
            }

            HandleUIToggle();

            if (showCursorState)
                Debug.Log($"UI Mode: {isUIMode}, Cursor Visible: {Cursor.visible}, Cursor Lock: {Cursor.lockState}");

            if (wasUIMode != isUIMode)
            {
                UpdateCursorState();
                wasUIMode = isUIMode;

                // Сбрасываем прогрев — после смены состояния курсора снова может быть сплайк мыши.
                inputFrameCounter = 0;
            }

            if (!isUIMode)
            {
                if (isAdmin) HandleAdminAbilities();

                if (isFlyMode || isNoclipMode)
                {
                    HandleFlyMovement();
                }
                else
                {
                    if (instantLandingEnabled) HandleInstantLanding();
                    if (wallJumpEnabled)       HandleWallJump();
                    HandleMovement();
                }

                HandleCameraRotation();

                if (cameraTiltEnabled) HandleCameraTilt();
            }
            else
            {
                // FIX: раньше при открытии UI (Tab) весь блок движения, включая гравитацию
                // и CharacterController.Move, полностью пропускался — игрок буквально
                // "зависал" в воздухе, если Tab нажимался в прыжке/падении.
                // Здесь мы НЕ читаем игровой ввод (WASD/мышь/прыжок и т.д.) и НЕ двигаем
                // игрока по горизонтали, но продолжаем применять гравитацию и вызывать
                // CharacterController.Move, чтобы падение / приземление работали как обычно.
                if (!isFlyMode && !isNoclipMode)
                {
                    ApplyGravityOnly();
                }
            }

            UpdateLocalCameraObjects();

            if (showWallJumpDebug && wallJumpEnabled) DebugWallJumpInfo();
            if (showAdminDebug    && isAdmin)         DebugAdminInfo();
        }

        // ═══════════════════════════════════════════════════════════════
        // Admin
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Определяет права администратора по сетевой роли игрока.
        /// Host — это игрок, который одновременно является сервером,
        /// поэтому у его локального игрока isServer == true.
        /// "Чистый" клиент, подключившийся к чужому хосту, никогда
        /// не получает isServer == true, поэтому права не выдаются.
        /// </summary>
        void DetermineAdminRights()
        {
            bool isHost = isServer; // для локального игрока isServer истинен только в режиме хоста

            isAdmin = isHost;

            Debug.Log(isHost
                ? $"[{gameObject.name}] Игрок — ХОСТ → права Admin Abilities ВЫДАНЫ."
                : $"[{gameObject.name}] Игрок — КЛИЕНТ → права Admin Abilities НЕ выданы.");
        }

        void HandleAdminAbilities()
        {
            if (!isAdmin) return; // дополнительная защита: без прав админа клавиши не обрабатываются

            if (Input.GetKeyDown(flyKey))    ToggleFlyMode();
            if (Input.GetKeyDown(noclipKey)) ToggleNoclipMode();
        }

        void ToggleFlyMode()
        {
            isFlyMode = !isFlyMode;

            if (isFlyMode)
            {
                if (isNoclipMode) { isNoclipMode = false; characterController.enabled = true; }
                flyVelocity                = Vector3.zero;
                moveDirection              = Vector3.zero;
                smoothedHorizontalVelocity = Vector3.zero;
                Debug.Log("Fly Mode: ENABLED");
            }
            else
            {
                flyVelocity = Vector3.zero;
                Debug.Log("Fly Mode: DISABLED");
            }
        }

        void ToggleNoclipMode()
        {
            isNoclipMode = !isNoclipMode;

            if (isNoclipMode)
            {
                if (isFlyMode) isFlyMode = false;
                characterController.enabled = false;
                flyVelocity                = Vector3.zero;
                moveDirection              = Vector3.zero;
                smoothedHorizontalVelocity = Vector3.zero;
                Debug.Log("Noclip Mode: ENABLED");
            }
            else
            {
                characterController.enabled = true;
                flyVelocity = Vector3.zero;
                Debug.Log("Noclip Mode: DISABLED");
            }
        }

        void HandleFlyMovement()
        {
            if (!canMove) return;

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right   = transform.TransformDirection(Vector3.right);
            Vector3 up      = Vector3.up;

            float currentFlySpeed = flySpeed;
            if (Input.GetKey(KeyCode.LeftShift)) currentFlySpeed *= flySpeedMultiplier;

            Vector3 targetVelocity = Vector3.zero;
            targetVelocity += forward * Input.GetAxis("Vertical")   * currentFlySpeed;
            targetVelocity += right   * Input.GetAxis("Horizontal") * currentFlySpeed;

            if (Input.GetKey(KeyCode.Space))                                            targetVelocity += up * currentFlySpeed;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) targetVelocity -= up * currentFlySpeed;

            flyVelocity = targetVelocity.magnitude > 0.1f
                ? Vector3.Lerp(flyVelocity, targetVelocity, Time.deltaTime * flyAcceleration)
                : Vector3.Lerp(flyVelocity, Vector3.zero,   Time.deltaTime * flyDeceleration);

            if (isNoclipMode) transform.position += flyVelocity * Time.deltaTime;
            else              characterController.Move(flyVelocity * Time.deltaTime);
        }

        // ═══════════════════════════════════════════════════════════════
        // Instant Landing
        // ═══════════════════════════════════════════════════════════════

        void HandleInstantLanding()
        {
            if (!canMove) return;

            if (Input.GetKey(instantLandingKey) && !characterController.isGrounded)
            {
                isInstantLanding = true;
                moveDirection.y  = -instantLandingSpeed;
            }
            else if (isInstantLanding && characterController.isGrounded)
            {
                isInstantLanding = false;
                moveDirection.y  = 0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Camera Tilt  ← ИСПРАВЛЕНО
        // ═══════════════════════════════════════════════════════════════

        void HandleCameraTilt()
        {
            // ВАЖНО: гейтится через canLook, а не canMove. Если оставить canMove —
            // то при cameraTiltEnabled == true именно этот метод вызывает
            // ApplyCameraTilt(), которая применяет playerCamera.transform.localRotation.
            // При заблокированном canMove (например, во время лазания по лестнице)
            // поворот камеры из HandleCameraRotation() просто не отображался бы.
            if (!canLook) return;

            float horizontalInput = Input.GetAxis("Horizontal");

            // FIX 2: tiltSpeed убран из формулы целевого угла.
            //   ДО (баг): targetTiltZ = -horizontalInput * maxTiltAngle * tiltSpeed
            //             при tiltSpeed=5, maxTiltAngle=15 → целевой угол ±75°, почти -90°
            //   ПОСЛЕ:    targetTiltZ = -horizontalInput * maxTiltAngle
            //             → максимум ±maxTiltAngle (15°), скорость Lerp = tiltSmoothness
            targetTiltZ  = -horizontalInput * maxTiltAngle;
            currentTiltZ = Mathf.Lerp(currentTiltZ, targetTiltZ, Time.deltaTime * tiltSmoothness);

            ApplyCameraTilt();

            if (isServer) networkTiltZ = currentTiltZ;
            else          CmdUpdateCameraTilt(currentTiltZ);
        }

        void ApplyCameraTilt()
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, currentTiltZ);
        }

        [Command]
        void CmdUpdateCameraTilt(float tiltZ) => networkTiltZ = tiltZ;

        // ═══════════════════════════════════════════════════════════════
        // Wall Jump
        // ═══════════════════════════════════════════════════════════════

        void HandleWallJump()
        {
            if (!canMove) return;

            if (characterController.isGrounded && !wasGrounded && resetWallJumpsOnGroundTouch)
            {
                currentWallJumps = 0;
                canWallJump      = true;
            }
            wasGrounded = characterController.isGrounded;

            if (wallStickTimer > 0) wallStickTimer -= Time.deltaTime;
            if (wallJumpTimer  > 0) wallJumpTimer  -= Time.deltaTime;
            else                    canWallJump = true;

            CheckWallContact();
            HandleWallSliding();

            if (Input.GetButtonDown("Jump") && CanWallJump())
                PerformWallJump();
        }

        void CheckWallContact()
        {
            isTouchingWall = false;
            wallNormal     = Vector3.zero;

            Vector3[] directions = { transform.right, -transform.right, transform.forward, -transform.forward };

            foreach (Vector3 direction in directions)
            {
                for (int i = 0; i < 3; i++)
                {
                    float   height    = (i - 1) * (wallCheckHeight / 2f);
                    Vector3 rayOrigin = transform.position + Vector3.up * (1.0f + height);

                    RaycastHit hit;
                    if (Physics.Raycast(rayOrigin, direction, out hit, wallCheckDistance, wallLayerMask))
                    {
                        float angle = Vector3.Angle(hit.normal, Vector3.up);
                        if (angle > 60f && angle < 120f)
                        {
                            isTouchingWall = true;
                            wallNormal     = hit.normal;

                            if (showWallJumpDebug)
                            {
                                Debug.DrawRay(rayOrigin, direction * wallCheckDistance, Color.red);
                                Debug.DrawRay(hit.point, hit.normal, Color.blue);
                            }
                            return;
                        }
                    }

                    if (showWallJumpDebug)
                        Debug.DrawRay(rayOrigin, direction * wallCheckDistance, Color.green);
                }
            }
        }

        void HandleWallSliding()
        {
            bool wasWallSliding = isWallSliding;

            isWallSliding = isTouchingWall &&
                            !characterController.isGrounded &&
                            moveDirection.y < 0 &&
                            IsMovingIntoWall() &&
                            !isInstantLanding;

            if (isWallSliding && !wasWallSliding && wallSlideSound != null && audioSource != null)
            {
                audioSource.clip = wallSlideSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!isWallSliding && wasWallSliding && audioSource != null && audioSource.clip == wallSlideSound)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }

            if (isWallSliding)
            {
                moveDirection.y = Mathf.Max(moveDirection.y, -wallSlideSpeed);
                wallStickTimer  = wallStickTime;
                lastWallNormal  = wallNormal;
            }
        }

        bool IsMovingIntoWall()
        {
            if (wallNormal == Vector3.zero) return false;
            return Vector3.Dot(GetInputDirection(), -wallNormal) > 0.1f;
        }

        Vector3 GetInputDirection()
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right   = transform.TransformDirection(Vector3.right);
            return (forward * Input.GetAxis("Vertical")) + (right * Input.GetAxis("Horizontal"));
        }

        bool CanWallJump()
        {
            return canWallJump &&
                   (isTouchingWall || wallStickTimer > 0) &&
                   !characterController.isGrounded &&
                   currentWallJumps < maxWallJumps &&
                   wallJumpTimer <= 0;
        }

        void PerformWallJump()
        {
            Vector3 jumpDirection;

            if (isTouchingWall)          jumpDirection = wallNormal;
            else if (wallStickTimer > 0) jumpDirection = lastWallNormal;
            else                         return;

            Vector3 wallJumpVelocity   = jumpDirection * wallJumpSideForce + Vector3.up * wallJumpForce;
            moveDirection              = wallJumpVelocity;
            smoothedHorizontalVelocity = new Vector3(wallJumpVelocity.x, 0f, wallJumpVelocity.z);

            currentWallJumps++;
            wallJumpTimer    = wallJumpCooldown;
            canWallJump      = false;
            wallStickTimer   = 0f;
            isInstantLanding = false;

            if (wallJumpSound != null && audioSource != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = wallJumpSound;
                audioSource.Play();
            }

            if (showWallJumpDebug)
                Debug.Log($"Wall Jump! Direction: {jumpDirection}, Jumps used: {currentWallJumps}/{maxWallJumps}");
        }

        // ═══════════════════════════════════════════════════════════════
        // Movement + Inertia
        // ═══════════════════════════════════════════════════════════════

        void HandleMovement()
        {
            if (!canMove) return;

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right   = transform.TransformDirection(Vector3.right);

            bool  isRunning      = Input.GetKey(KeyCode.LeftShift);
            float speed          = isRunning ? runningSpeed : walkingSpeed;
            Vector3 targetHorizontal = (forward * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal")) * speed;

            float movementDirectionY = moveDirection.y;

            if (!isWallSliding || wallStickTimer <= 0)
            {
                if (inertiaEnabled)
                {
                    bool  grounded   = characterController.isGrounded;
                    bool  hasInput   = targetHorizontal.magnitude > 0.1f;
                    float lerpFactor = grounded
                        ? (hasInput ? groundAcceleration : groundDeceleration)
                        : (hasInput ? airAcceleration    : airDeceleration);

                    smoothedHorizontalVelocity = Vector3.Lerp(
                        smoothedHorizontalVelocity, targetHorizontal, Time.deltaTime * lerpFactor);

                    moveDirection   = smoothedHorizontalVelocity;
                }
                else
                {
                    moveDirection              = targetHorizontal;
                    smoothedHorizontalVelocity = moveDirection;
                }

                moveDirection.y = movementDirectionY;
            }
            else
            {
                moveDirection.x = Mathf.Lerp(moveDirection.x, targetHorizontal.x, Time.deltaTime * 2f);
                moveDirection.z = Mathf.Lerp(moveDirection.z, targetHorizontal.z, Time.deltaTime * 2f);
                smoothedHorizontalVelocity = new Vector3(moveDirection.x, 0f, moveDirection.z);
            }

            if (Input.GetButton("Jump") && characterController.isGrounded && wallJumpTimer <= 0)
            {
                moveDirection.y  = jumpSpeed;
                isInstantLanding = false;
            }
            else if (characterController.isGrounded && moveDirection.y < 0f)
            {
                // FIX: без этого moveDirection.y "замораживался" на последнем отрицательном
                // значении гравитации после приземления (не сбрасывался), пока не был выполнен
                // прыжок. Небольшое отрицательное значение (а не 0) держит CharacterController
                // прижатым к земле на склонах/ступеньках, как рекомендует документация Unity.
                moveDirection.y = GroundStickVelocity;
            }

            if (!characterController.isGrounded && !isWallSliding && !isInstantLanding)
                moveDirection.y -= gravity * Time.deltaTime;

            characterController.Move(moveDirection * Time.deltaTime + GetPlatformDelta());
        }

        // ═══════════════════════════════════════════════════════════════
        // Moving Platform
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// CharacterController.Move() двигает transform.position напрямую и никак
        /// не зависит от того, что происходит с объектами под игроком — поэтому если
        /// платформа под ногами едет, игрок сам по себе остаётся на месте (проваливается
        /// сквозь неё или "соскальзывает"). Этот метод лучом вниз проверяет, стоит ли
        /// игрок на платформе, запоминает её позицию в прошлом кадре и возвращает то,
        /// на сколько она сдвинулась с тех пор — это смещение нужно ПРИБАВИТЬ к обычному
        /// движению игрока в этом же вызове Move(), чтобы игрок "ехал" вместе с платформой.
        /// </summary>
        Vector3 GetPlatformDelta()
        {
            Transform detectedPlatform = null;

            if (characterController.isGrounded)
            {
                Vector3 feet      = transform.position + characterController.center
                                     + Vector3.down * (characterController.height * 0.5f);
                Vector3 rayOrigin = feet + Vector3.up * 0.1f;

                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit,
                                     platformCheckDistance + 0.1f, platformLayerMask,
                                     QueryTriggerInteraction.Ignore))
                {
                    detectedPlatform = hit.transform;
                }
            }

            Vector3 delta = Vector3.zero;

            if (detectedPlatform != null)
            {
                if (detectedPlatform == currentPlatform)
                {
                    // Стояли на этой же платформе и в прошлом кадре — считаем, насколько она уехала
                    delta = detectedPlatform.position - platformLastPosition;
                }
                // Если платформа обнаружена только что (сменилась или игрок только приземлился) —
                // в этом кадре delta не считаем (иначе будет "скачок"), просто запоминаем стартовую позицию

                currentPlatform       = detectedPlatform;
                platformLastPosition  = detectedPlatform.position;
            }
            else
            {
                currentPlatform = null;
            }

            return delta;
        }

        /// <summary>
        /// Урезанная версия движения, используемая, когда открыт UI (isUIMode == true).
        /// Игнорирует любой игровой ввод (ходьба/бег/прыжок/wall-jump/наклон камеры),
        /// но продолжает:
        ///   1) плавно гасить горизонтальную скорость (чтобы не "скользить" с открытым меню),
        ///   2) применять гравитацию,
        ///   3) вызывать CharacterController.Move,
        /// иначе персонаж, открывший UI в воздухе, зависает на месте до закрытия UI.
        /// </summary>
        void ApplyGravityOnly()
        {
            if (!canMove) return;

            // Гасим горизонтальную инерцию, пока открыто меню — без этого игрок продолжил
            // бы лететь в направлении, которое было задано до открытия UI.
            smoothedHorizontalVelocity = Vector3.Lerp(
                smoothedHorizontalVelocity, Vector3.zero, Time.deltaTime * groundDeceleration);

            moveDirection.x = smoothedHorizontalVelocity.x;
            moveDirection.z = smoothedHorizontalVelocity.z;

            if (!characterController.isGrounded)
                moveDirection.y -= gravity * Time.deltaTime;
            else if (moveDirection.y < 0f)
                moveDirection.y = 0f;

            characterController.Move(moveDirection * Time.deltaTime + GetPlatformDelta());
        }

        // ═══════════════════════════════════════════════════════════════
        // Camera Rotation  ← ИСПРАВЛЕНО
        // ═══════════════════════════════════════════════════════════════

        void HandleCameraRotation()
        {
            if (!canLook) return;

            // FIX 3: пропускаем первые inputWarmupFrames кадров после старта
            //        (или после переключения курсора), чтобы не поймать сплайк
            //        дельты мыши в момент CursorLockMode.Locked.
            if (inputFrameCounter < inputWarmupFrames)
            {
                inputFrameCounter++;
                return;
            }

            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX  = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            // Если тилт выключен — ставим локальный поворот камеры здесь.
            // Если включён — ApplyCameraTilt() сделает это сам через Euler(rotationX, 0, tiltZ).
            if (!cameraTiltEnabled)
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

            transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);

            if (isServer)
            {
                networkRotationX    = rotationX;
                networkBodyRotation = transform.rotation;
            }
            else
            {
                CmdUpdateCameraRotation(rotationX, transform.rotation);
            }
        }

        [Command]
        void CmdUpdateCameraRotation(float rotX, Quaternion bodyRot)
        {
            networkRotationX    = rotX;
            networkBodyRotation = bodyRot;
        }

        // ═══════════════════════════════════════════════════════════════
        // Camera Objects
        // ═══════════════════════════════════════════════════════════════

        void SetupCameraObjects()
        {
            if (cameraObjectsParent == null)
            {
                Debug.LogWarning($"[{gameObject.name}] cameraObjectsParent is null!");
                return;
            }

            Debug.Log($"[{gameObject.name}] Setting up camera objects. IsLocalPlayer: {isLocalPlayer}, Objects count: {(cameraAttachedObjects != null ? cameraAttachedObjects.Length : 0)}");

            if (cameraAttachedObjects != null)
            {
                for (int i = 0; i < cameraAttachedObjects.Length; i++)
                {
                    if (cameraAttachedObjects[i] != null)
                    {
                        Debug.Log($"[{gameObject.name}] Setting up object {i}: {cameraAttachedObjects[i].name}");

                        if (cameraAttachedObjects[i].transform.parent != cameraObjectsParent)
                        {
                            cameraAttachedObjects[i].transform.SetParent(cameraObjectsParent, false);
                            Debug.Log($"[{gameObject.name}] Moved {cameraAttachedObjects[i].name} to cameraObjectsParent");
                        }

                        cameraAttachedObjects[i].SetActive(true);
                    }
                    else Debug.LogWarning($"[{gameObject.name}] Camera attached object [{i}] is null!");
                }
            }
            else Debug.LogWarning($"[{gameObject.name}] cameraAttachedObjects array is null!");
        }

        void UpdateLocalCameraObjects()
        {
            if (!isLocalPlayer || cameraObjectsParent == null) return;

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{gameObject.name}] Camera pos: {playerCamera.transform.position}, rot: {playerCamera.transform.rotation}");
                Debug.Log($"[{gameObject.name}] CameraParent pos: {cameraObjectsParent.position}, rot: {cameraObjectsParent.rotation}");
            }

            cameraObjectsParent.position = playerCamera.transform.position;
            cameraObjectsParent.rotation = playerCamera.transform.rotation;

            bool shouldBeVisible = !isUIMode;
            if (cameraObjectsVisible != shouldBeVisible)
            {
                Debug.Log($"[{gameObject.name}] Changing camera objects visibility to: {shouldBeVisible}");
                if (isServer) cameraObjectsVisible = shouldBeVisible;
                else          CmdSetCameraObjectsVisibility(shouldBeVisible);
            }
        }

        void UpdateNetworkCameraObjects()
        {
            if (cameraObjectsParent == null || playerCamera == null) return;

            Quaternion cameraRotation      = Quaternion.Euler(networkRotationX, 0f, networkTiltZ);
            Quaternion worldCameraRotation = networkBodyRotation * cameraRotation;
            Vector3    cameraPosition      = transform.position + networkBodyRotation * (Vector3.up * 1.8f);

            cameraObjectsParent.position = cameraPosition;
            cameraObjectsParent.rotation = worldCameraRotation;
        }

        [Command]
        void CmdSetCameraObjectsVisibility(bool visible) => cameraObjectsVisible = visible;

        void OnCameraObjectsVisibilityChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"[{gameObject.name}] Camera objects visibility → {newValue}");
            if (cameraAttachedObjects == null) return;
            foreach (var obj in cameraAttachedObjects)
                if (obj != null) obj.SetActive(newValue);
        }

        // ═══════════════════════════════════════════════════════════════
        // Static Objects
        // ═══════════════════════════════════════════════════════════════

        void SetupStaticObjects()
        {
            if (staticNetworkObjects == null)
            {
                Debug.LogWarning($"[{gameObject.name}] staticNetworkObjects is null!");
                return;
            }

            Debug.Log($"[{gameObject.name}] Setting up static objects. IsLocalPlayer: {isLocalPlayer}, Objects count: {staticNetworkObjects.Length}");

            for (int i = 0; i < staticNetworkObjects.Length; i++)
            {
                if (staticNetworkObjects[i] != null)
                {
                    Debug.Log($"[{gameObject.name}] Static object {i}: {staticNetworkObjects[i].name} → {staticObjectsVisible}");
                    staticNetworkObjects[i].SetActive(staticObjectsVisible);
                }
                else Debug.LogWarning($"[{gameObject.name}] Static network object [{i}] is null!");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Local Only Objects
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Включает/выключает объекты из localOnlyObjects. Не синхронизируется по сети —
        /// каждый клиент сам решает для СВОЕГО экземпляра playermirror, что показывать.
        /// true  → вызывается для isLocalPlayer  (объекты видны у себя)
        /// false → вызывается для remote-копий   (объекты скрыты у других)
        /// </summary>
        void SetLocalOnlyObjectsActive(bool active)
        {
            if (localOnlyObjects == null) return;

            foreach (var obj in localOnlyObjects)
                if (obj != null) obj.SetActive(active);
        }

        [Command]
        public void CmdSetStaticObjectsVisibility(bool visible) => staticObjectsVisible = visible;

        void OnStaticObjectsVisibilityChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"[{gameObject.name}] Static objects visibility → {newValue}");
            if (staticNetworkObjects == null) return;
            foreach (var obj in staticNetworkObjects)
                if (obj != null) obj.SetActive(newValue);
        }

        [Command]
        public void CmdToggleStaticObject(int objectIndex)
        {
            if (staticNetworkObjects != null && objectIndex >= 0 && objectIndex < staticNetworkObjects.Length)
                RpcToggleStaticObject(objectIndex);
        }

        [ClientRpc]
        void RpcToggleStaticObject(int objectIndex)
        {
            if (staticNetworkObjects != null && objectIndex >= 0 && objectIndex < staticNetworkObjects.Length)
            {
                GameObject obj = staticNetworkObjects[objectIndex];
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                    Debug.Log($"[{gameObject.name}] Toggled static object {obj.name} → {obj.activeSelf}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // UI Toggle
        // ═══════════════════════════════════════════════════════════════

        void HandleUIToggle()
        {
            if (Input.GetKeyDown(uiToggleKey))              ToggleUIMode();
            if (Input.GetKeyDown(KeyCode.Escape) && isUIMode) SetUIMode(false);
        }

        public void ToggleUIMode() => SetUIMode(!isUIMode);

        public void SetUIMode(bool enabled)
        {
            isUIMode = enabled;
            if (uiCanvas != null) uiCanvas.SetActive(isUIMode);
        }


        void UpdateCursorState()
        {
            if (isUIMode)
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

        // ═══════════════════════════════════════════════════════════════
        // Speed Display UI
        // ═══════════════════════════════════════════════════════════════

        void LateUpdate()
        {
            if (!isLocalPlayer) return;

            if (showSpeedUI && speedText != null && Time.frameCount % speedUpdateRate == 0)
            {
                float displaySpeed;

                if (isFlyMode || isNoclipMode)
                {
                    displaySpeed = showFullSpeed
                        ? flyVelocity.magnitude
                        : new Vector3(flyVelocity.x, 0f, flyVelocity.z).magnitude;
                }
                else
                {
                    displaySpeed = showFullSpeed
                        ? GetDisplayMoveDirection().magnitude
                        : new Vector3(moveDirection.x, 0f, moveDirection.z).magnitude;
                }

                string text = string.Format(speedFormat, displaySpeed);

                if (showVerticalSpeed)
                {
                    float vertSpeed = (isFlyMode || isNoclipMode) ? flyVelocity.y : GetDisplayVerticalSpeed();
                    text += "\n" + string.Format(verticalSpeedFormat, vertSpeed);
                }

                speedText.text = text;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Debug
        // ═══════════════════════════════════════════════════════════════

        void DebugWallJumpInfo()
        {
            if (Time.frameCount % 30 == 0)
                Debug.Log($"WallJump — Touching: {isTouchingWall}, Sliding: {isWallSliding}, " +
                          $"Jumps: {currentWallJumps}/{maxWallJumps}, Grounded: {characterController.isGrounded}, " +
                          $"StickTimer: {wallStickTimer:F2}, JumpTimer: {wallJumpTimer:F2}, InstantLanding: {isInstantLanding}");
        }

        void DebugAdminInfo()
        {
            if (Time.frameCount % 30 == 0)
                Debug.Log($"Admin — FlyMode: {isFlyMode}, NoclipMode: {isNoclipMode}, " +
                          $"FlyVelocity: {flyVelocity.magnitude:F2}, Pos: {transform.position}");
        }

        // ═══════════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════════

        public void SetCanMove(bool value) => canMove = value;
        public void SetCanLook(bool value) => canLook = value;
        public void SetLocalOnlyObjectsVisible(bool visible) { if (isLocalPlayer) SetLocalOnlyObjectsActive(visible); }
        public bool IsUIMode()     => isUIMode;
        public bool IsFlyMode()    => isFlyMode;
        public bool IsNoclipMode() => isNoclipMode;

        public void EnableFlyMode()     { if (isAdmin && !isFlyMode)    ToggleFlyMode(); }
        public void DisableFlyMode()    { if (isAdmin &&  isFlyMode)    ToggleFlyMode(); }
        public void EnableNoclipMode()  { if (isAdmin && !isNoclipMode) ToggleNoclipMode(); }
        public void DisableNoclipMode() { if (isAdmin &&  isNoclipMode) ToggleNoclipMode(); }

        /// <summary>
        /// Вертикальная скорость для отображения/расчётов "видимой" скорости.
        /// Отличается от сырого moveDirection.y тем, что скрывает служебное
        /// значение GroundStickVelocity — оно нужно только физике CharacterController
        /// (чтобы не терять isGrounded на спусках) и не является реальным падением.
        /// </summary>
        private float GetDisplayVerticalSpeed()
        {
            return (characterController.isGrounded && moveDirection.y == GroundStickVelocity)
                ? 0f
                : moveDirection.y;
        }

        private Vector3 GetDisplayMoveDirection()
        {
            Vector3 v = moveDirection;
            v.y = GetDisplayVerticalSpeed();
            return v;
        }

        public float GetCurrentSpeed()
        {
            if (isFlyMode || isNoclipMode) return flyVelocity.magnitude;
            return showFullSpeed
                ? GetDisplayMoveDirection().magnitude
                : new Vector3(moveDirection.x, 0f, moveDirection.z).magnitude;
        }

        public Vector3 GetVelocity() => (isFlyMode || isNoclipMode) ? flyVelocity : GetDisplayMoveDirection();

        public void ResetWallJumpState()
        {
            currentWallJumps = 0; wallStickTimer = 0f; wallJumpTimer = 0f;
            isWallSliding = false; isTouchingWall = false; canWallJump = true; isInstantLanding = false;

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == wallSlideSound)
            { audioSource.Stop(); audioSource.loop = false; }
        }

        public void ResetAdminAbilities()
        {
            if (isFlyMode)    { isFlyMode    = false; flyVelocity = Vector3.zero; }
            if (isNoclipMode) { isNoclipMode = false; characterController.enabled = true; flyVelocity = Vector3.zero; }
        }

        // ═══════════════════════════════════════════════════════════════
        // Lifecycle
        // ═══════════════════════════════════════════════════════════════

        void OnDestroy()
        {
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        }

        void OnDisable()
        {
            if (isLocalPlayer) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        void OnDrawGizmosSelected()
        {
            if (!wallJumpEnabled) return;

            Vector3[] directions = { transform.right, -transform.right, transform.forward, -transform.forward };
            Gizmos.color = isTouchingWall ? Color.red : Color.green;

            foreach (Vector3 direction in directions)
                for (int i = 0; i < 3; i++)
                {
                    float   height    = (i - 1) * (wallCheckHeight / 2f);
                    Vector3 rayOrigin = transform.position + Vector3.up * (1.0f + height);
                    Gizmos.DrawLine(rayOrigin, rayOrigin + direction * wallCheckDistance);
                }

            if (isTouchingWall)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + wallNormal * 2f);
            }
        }
    }
}