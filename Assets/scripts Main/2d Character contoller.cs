using UnityEngine;
using UnityEngine.EventSystems;

#if CONTROLLER_3D
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
#else
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
#endif
public class PlayerController_2D_3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Physics")]
    [SerializeField] private float fallMultiplier    = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime     = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    // ── Components ─────────────────────────────────────────────────────────
#if CONTROLLER_3D
    private Rigidbody    rb;
    private BoxCollider  col;
#else
    private Rigidbody2D   rb;
    private BoxCollider2D col;
#endif
    private Animator animator;

    // ── State ──────────────────────────────────────────────────────────────
    private float horizontalInput;
    private float verticalInput;
    private bool  isGrounded;
    private bool  isFacingRight = true;

    // FIX (infinite jump): флаг сбрасывается только при приземлении.
    // Пока он true — coyoteTimer не даёт новый прыжок.
    private bool  hasJumped;

    // Jump timers
    private float coyoteTimer;
    private float jumpBufferTimer;

    // Facing sign отдельно — не трогаем scale.x вне FlipSprite
    private float facingSign = 1f;

    // ── Animation hashes ──────────────────────────────────────────────────
    private static readonly int AnimSpeed      = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimJump       = Animator.StringToHash("Jump");

    // ══════════════════════════════════════════════════════════════════════
    // UI Focus
    // ══════════════════════════════════════════════════════════════════════

    private bool IsUIFocused()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.currentSelectedGameObject != null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Unity Messages
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
#if CONTROLLER_3D
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
#else
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
#endif
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        GatherInput();
        CheckGround();
        UpdateJumpTimers();
        HandleJump();
#if !CONTROLLER_3D
        FlipSprite();
#endif
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
        ApplyBetterGravity();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Input
    // ══════════════════════════════════════════════════════════════════════

    private void GatherInput()
    {
        if (IsUIFocused())
        {
            horizontalInput = 0f;
#if CONTROLLER_3D
            verticalInput = 0f;
#endif
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
#if CONTROLLER_3D
        verticalInput = Input.GetAxisRaw("Vertical");
#endif
    }

    // ══════════════════════════════════════════════════════════════════════
    // Jump Timers: Coyote Time + Jump Buffer
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateJumpTimers()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            hasJumped   = false;   // FIX: приземлились — разрешаем следующий прыжок
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Jump
    // FIX (infinite jump): canJump требует !hasJumped.
    //   hasJumped = true  сразу при прыжке → повторный прыжок невозможен.
    //   hasJumped = false только когда isGrounded снова станет true.
    // ══════════════════════════════════════════════════════════════════════

    private void HandleJump()
    {
        bool canJump   = coyoteTimer > 0f && !hasJumped;
        bool wantsJump = jumpBufferTimer > 0f;

        if (!canJump || !wantsJump) return;

        hasJumped       = true;   // FIX: блокируем до приземления
        jumpBufferTimer = 0f;
        coyoteTimer     = 0f;

#if CONTROLLER_3D
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
#else
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
#endif
        animator?.SetTrigger(AnimJump);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Movement
    // ══════════════════════════════════════════════════════════════════════

    private void Move()
    {
#if CONTROLLER_3D
        Vector3 camForward = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
            : Vector3.forward;
        Vector3 camRight = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized
            : Vector3.right;

        Vector3 moveDir        = (camRight * horizontalInput + camForward * verticalInput).normalized;
        Vector3 targetVelocity = moveDir * moveSpeed;
        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);

        if (moveDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                Time.fixedDeltaTime * 15f);
#else
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
#endif
    }

    private void ApplyBetterGravity()
    {
#if CONTROLLER_3D
        float vy = rb.velocity.y;
        if (vy < 0f)
            rb.velocity += Vector3.up * (Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
        else if (vy > 0f && !Input.GetButton("Jump"))
            rb.velocity += Vector3.up * (Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime);
#else
        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
        else if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
            rb.velocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime);
#endif
    }

    // ══════════════════════════════════════════════════════════════════════
    // Ground Detection
    // ══════════════════════════════════════════════════════════════════════

    private void CheckGround()
    {
#if CONTROLLER_3D
        Vector3 origin = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * (col.size.y * 0.5f);
        isGrounded = Physics.CheckSphere(origin, groundCheckDistance, groundLayer);
#else
        Vector2 origin = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + Vector2.down * (col.size.y * 0.5f);
        isGrounded = Physics2D.OverlapCircle(origin, groundCheckDistance, groundLayer);
#endif
    }

    // ══════════════════════════════════════════════════════════════════════
    // Sprite Flipping (2D only)
    // ══════════════════════════════════════════════════════════════════════

#if !CONTROLLER_3D
    private void FlipSprite()
    {
        if ((horizontalInput > 0f && !isFacingRight) ||
            (horizontalInput < 0f &&  isFacingRight))
        {
            isFacingRight = !isFacingRight;
            facingSign    = isFacingRight ? 1f : -1f;

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * facingSign;
            transform.localScale = s;
        }
    }
#endif

    // ══════════════════════════════════════════════════════════════════════
    // Animator
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateAnimator()
    {
        if (animator == null) return;

#if CONTROLLER_3D
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        animator.SetFloat(AnimSpeed, flatVel.magnitude);
#else
        animator.SetFloat(AnimSpeed, Mathf.Abs(horizontalInput));
#endif
        animator.SetBool(AnimIsGrounded, isGrounded);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Editor Gizmos
    // ══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (col == null)
        {
#if CONTROLLER_3D
            col = GetComponent<BoxCollider>();
#else
            col = GetComponent<BoxCollider2D>();
#endif
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
#if CONTROLLER_3D
        Vector3 gOrigin = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * (col.size.y * 0.5f);
        Gizmos.DrawWireSphere(gOrigin, groundCheckDistance);
#else
        Vector2 gOrigin = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + Vector2.down * (col.size.y * 0.5f);
        Gizmos.DrawWireSphere(gOrigin, groundCheckDistance);
#endif
    }
#endif
}