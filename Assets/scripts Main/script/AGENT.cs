using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AGENT : MonoBehaviour
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    public GameObject player;

    [Header("Animation Settings")]
    public Animator animator;
    public string walkAnimationName = "Walk";
    public string idleAnimationName = "Idle";
    public string shootAnimationName = "Shoot";
    public string dashAnimationName = "Dash";
    public string jumpAnimationName = "Jump";
    public string meleeAnimationName = "Melee";

    [Header("Movement Settings")]
    public float minMoveSpeed = 0.1f;
    public bool useRadius = false;
    public float followRadius = 10f;
    public float stopRadius = 2f;

    [Header("Jump Settings")]
    public bool canJump = true;
    public float jumpForce = 15f;
    public float jumpCooldown = 2f;
    public float jumpTriggerDistance = 8f;
    public float maxJumpTriggerDistance = 12f;
    [Range(0f, 1f)]
    public float jumpChance = 0.2f;
    public float jumpCheckInterval = 1f;
    public bool jumpTowardsPlayer = true;
    public Vector3 jumpCenterOffset = Vector3.zero;
    public float jumpHorizontalForce = 5f;

    [Header("Ground Check Settings")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 0.3f;

    [Header("Dash Settings")]
    public bool canDash = true;
    public float dashDistance = 5f;
    public float dashSpeed = 20f;
    public float dashCooldown = 3f;
    public float dashDuration = 0.5f;
    public LayerMask dashObstacleLayer = -1;
    public float minDashTriggerDistance = 8f;
    public float maxDashTriggerDistance = 15f;
    [Range(0f, 1f)]
    public float dashChance = 0.3f;
    public Vector3 dashCenterOffset = Vector3.zero;

    [Header("Shooting Settings")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float maxShootDistance = 20f;
    public float shootCooldown = 2f;
    public float fireRate = 1f;
    public int burstCount = 1;
    public float burstDelay = 0.1f;
    public float projectileSpeed = 10f;
    public float pathCheckTime = 3f;

    [Header("Melee Attack Settings")]
    public bool canMelee = true;
    public float meleeRange = 3f;
    public float meleeDamage = 25f;
    public float meleeCooldown = 1.5f;
    public float meleeAttackDelay = 0.3f;
    public float meleeAngle = 90f;
    public LayerMask meleeTargetLayer = -1;
    public Vector3 meleeAttackOffset = Vector3.zero;
    [Range(0f, 1f)]
    public float meleeChance = 0.5f;
    public bool meleeKnockback = true;
    public float meleeKnockbackForce = 5f;

    [Header("Melee Visual & Audio")]
    public ParticleSystem meleeHitEffect;
    public AudioClip meleeSwingSound;
    public AudioClip meleeHitSound;
    public bool showMeleeDebugGizmos = true;

    [Header("Melee Animation Timing")]
    public float meleeAnimationDuration = 1f;
    public bool useAnimationEvents = false;

    [Header("Berserk Mode Settings")]
    [Range(0f, 1f)]
    public float berserkChance = 0.1f;
    public float berserkDuration = 10f;
    public float berserkCooldown = 30f;
    public float berserkStatMultiplier = 4f;
    public ParticleSystem berserkParticles;
    public AudioClip berserkActivationSound;
    private AudioSource audioSource;

    [Header("Material Settings - Class 1")]
    public Material class1_NormalMaterial;
    public Material class1_BerserkMaterial;
    public Renderer class1_Renderer;

    [Header("Material Settings - Class 2")]
    public Material class2_NormalMaterial;
    public Material class2_BerserkMaterial;
    public Renderer class2_Renderer;

    [Header("Камера для дебага")]
    public Camera camera;

    [Header("Attack Toggle Settings")]
    
    private bool attackMode = false;
    private List<GameObject> attackTargets = new List<GameObject>();
    private int currentTargetIndex = 0;

    [Header("Behavior Mode Settings")]
    public bool isFriendly = false;
    [Tooltip("When friendly, agent protects player and attacks other agents")]
    public bool attackEnemyAgents = true;





    private bool isMoving = false;
    private bool isShooting = false;
    private bool isDashing = false;
    private bool isJumping = false;
    private bool isMeleeAttacking = false;
    private bool isGrounded = true;
    private Vector3 startPosition;
    private float lastShootTime = 0f;
    private float lastDashTime = 0f;
    private float lastJumpTime = 0f;
    private float lastJumpCheckTime = 0f;
    private float lastMeleeTime = 0f;
    private float pathCheckTimer = 0f;
    private Vector3 lastDestination;
    private bool targetUnreachable = false;
    private bool isInBurstMode = false;
    private Vector3 dashTarget;
    private float originalAgentSpeed;

    private bool isBerserk = false;
    private float lastBerserkTime = 0f;
    private float berserkEndTime = 0f;

    private float originalDashDistance;
    private float originalDashSpeed;
    private float originalDashCooldown;
    private float originalFireRate;
    private float originalProjectileSpeed;
    private float originalDashChance;
    private float originalJumpForce;
    private float originalJumpCooldown;
    private float originalMeleeDamage;
    private float originalMeleeCooldown;
    private float originalMeleeRange;

   

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        rb.isKinematic = false;

        startPosition = transform.position;
        originalAgentSpeed = agent.speed;

        SaveOriginalStats();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (shootPoint == null)
        {
            shootPoint = transform;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupRenderersIfNeeded();
    }

    private void SetupRenderersIfNeeded()
    {
        if (class1_Renderer == null || class2_Renderer == null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0 && class1_Renderer == null)
            {
                class1_Renderer = renderers[0];
            }

            if (renderers.Length > 1 && class2_Renderer == null)
            {
                class2_Renderer = renderers[1];
            }
            else if (renderers.Length == 1 && class2_Renderer == null)
            {
                class2_Renderer = renderers[0];
            }
        }
    }

    private void SaveOriginalStats()
    {
        originalDashDistance = dashDistance;
        originalDashSpeed = dashSpeed;
        originalDashCooldown = dashCooldown;
        originalFireRate = fireRate;
        originalProjectileSpeed = projectileSpeed;
        originalDashChance = dashChance;
        originalJumpForce = jumpForce;
        originalJumpCooldown = jumpCooldown;
        originalMeleeDamage = meleeDamage;
        originalMeleeCooldown = meleeCooldown;
        originalMeleeRange = meleeRange;
    }

    private void Update()
    {
       

        if (attackMode && attackTargets.Count > 0)
        {
            if (attackTargets[currentTargetIndex] == null)
            {
                FindAttackTargets();
                if (attackTargets.Count == 0)
                {
                    attackMode = false;
                    return;
                }
                currentTargetIndex = 0;
            }
            player = attackTargets[currentTargetIndex];
        }

        CheckGrounded();
        CheckBerserkActivation();
        UpdateBerserkMode();

        if (!isShooting && !isDashing && !isJumping && !isMeleeAttacking && isGrounded && canJump)
        {
            CheckJumpConditions();
        }

        if (!isShooting && !isDashing && !isMeleeAttacking && canDash)
        {
            CheckDashConditions();
        }

        if (isDashing || isJumping || isMeleeAttacking) return;

        Vector3 targetDestination = GetTargetDestination();

        CheckTargetReachability(targetDestination);
        CheckCombatConditions();

        if (!isShooting && !isMeleeAttacking)
        {
            agent.destination = targetDestination;
        }

        bool currentlyMoving = agent.velocity.magnitude > minMoveSpeed && !isShooting && !isMeleeAttacking;

        if (currentlyMoving != isMoving)
        {
            isMoving = currentlyMoving;
            UpdateAnimation();
        }
    }

    private void FindAttackTargets()
    {
        attackTargets.Clear();
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agent");

        foreach (GameObject agent in allAgents)
        {
            if (agent != gameObject)
            {
                attackTargets.Add(agent);
            }
        }
    }

    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }

    private void CheckJumpConditions()
    {
        if (player == null) return;

        Vector3 jumpCenter = transform.position + transform.TransformDirection(jumpCenterOffset);
        float distanceToPlayer = Vector3.Distance(jumpCenter, player.transform.position);

        bool canJumpNow = Time.time >= lastJumpTime + jumpCooldown;
        bool checkTimeReached = Time.time >= lastJumpCheckTime + jumpCheckInterval;

        if (canJumpNow && checkTimeReached &&
            distanceToPlayer >= jumpTriggerDistance &&
            distanceToPlayer <= maxJumpTriggerDistance &&
            Random.Range(0f, 1f) <= jumpChance)
        {
            lastJumpCheckTime = Time.time;
            PerformJump();
        }
    }

    private void PerformJump()
    {
        if (!isGrounded || isJumping) return;

        isJumping = true;
        lastJumpTime = Time.time;

        Vector3 jumpCenter = transform.position + transform.TransformDirection(jumpCenterOffset);

        agent.enabled = false;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (jumpTowardsPlayer && player != null)
        {
            Vector3 directionToPlayer = (player.transform.position - jumpCenter).normalized;
            Vector3 horizontalDirection = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized;
            rb.AddForce(horizontalDirection * jumpHorizontalForce, ForceMode.Impulse);
        }

        if (animator != null && !string.IsNullOrEmpty(jumpAnimationName))
        {
            animator.Play(jumpAnimationName);
        }

        StartCoroutine(JumpCoroutine());
    }

    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        while (!isGrounded)
        {
            yield return new WaitForFixedUpdate();
        }

        isJumping = false;

        if (agent != null)
        {
            agent.enabled = true;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    private void CheckBerserkActivation()
    {
        if (isBerserk || Time.time < lastBerserkTime + berserkCooldown) return;

        if (Time.time % 1f < Time.deltaTime)
        {
            if (Random.Range(0f, 1f) <= berserkChance)
            {
                ActivateBerserk();
            }
        }
    }

    private void ActivateBerserk()
    {
        isBerserk = true;
        lastBerserkTime = Time.time;
        berserkEndTime = Time.time + berserkDuration;

        ApplyBerserkStats();

        if (berserkParticles != null)
        {
            berserkParticles.Play();
        }

        ApplyBerserkMaterials();

        if (berserkActivationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(berserkActivationSound);
        }

        Debug.Log("BERSERK MODE ACTIVATED!");
    }

    private void ApplyBerserkStats()
    {
        agent.speed = originalAgentSpeed * berserkStatMultiplier;
        dashDistance = originalDashDistance * berserkStatMultiplier;
        dashSpeed = originalDashSpeed * berserkStatMultiplier;
        dashCooldown = originalDashCooldown / berserkStatMultiplier;
        dashChance = Mathf.Min(originalDashChance * berserkStatMultiplier, 1f);
        fireRate = originalFireRate * berserkStatMultiplier;
        projectileSpeed = originalProjectileSpeed * berserkStatMultiplier;
        jumpForce = originalJumpForce * berserkStatMultiplier;
        jumpCooldown = originalJumpCooldown / berserkStatMultiplier;
        meleeDamage = originalMeleeDamage * berserkStatMultiplier;
        meleeCooldown = originalMeleeCooldown / berserkStatMultiplier;
        meleeRange = originalMeleeRange * 1.5f;
    }

    private void UpdateBerserkMode()
    {
        if (isBerserk && Time.time >= berserkEndTime)
        {
            DeactivateBerserk();
        }
    }

    private void DeactivateBerserk()
    {
        isBerserk = false;
        RestoreOriginalStats();

        if (berserkParticles != null)
        {
            berserkParticles.Stop();
        }

        RestoreOriginalMaterials();
        Debug.Log("Berserk mode deactivated.");
    }

    private void RestoreOriginalStats()
    {
        agent.speed = originalAgentSpeed;
        dashDistance = originalDashDistance;
        dashSpeed = originalDashSpeed;
        dashCooldown = originalDashCooldown;
        dashChance = originalDashChance;
        fireRate = originalFireRate;
        projectileSpeed = originalProjectileSpeed;
        jumpForce = originalJumpForce;
        jumpCooldown = originalJumpCooldown;
        meleeDamage = originalMeleeDamage;
        meleeCooldown = originalMeleeCooldown;
        meleeRange = originalMeleeRange;
    }

    private void ApplyBerserkMaterials()
    {
        if (class1_Renderer != null && class1_BerserkMaterial != null)
        {
            class1_Renderer.material = class1_BerserkMaterial;
        }

        if (class2_Renderer != null && class2_BerserkMaterial != null)
        {
            class2_Renderer.material = class2_BerserkMaterial;
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (class1_Renderer != null && class1_NormalMaterial != null)
        {
            class1_Renderer.material = class1_NormalMaterial;
        }

        if (class2_Renderer != null && class2_NormalMaterial != null)
        {
            class2_Renderer.material = class2_NormalMaterial;
        }
    }

    private void CheckDashConditions()
    {
        if (player == null) return;

        Vector3 dashCenter = transform.position + transform.TransformDirection(dashCenterOffset);
        float distanceToPlayer = Vector3.Distance(dashCenter, player.transform.position);

        bool canDashNow = Time.time >= lastDashTime + dashCooldown;

        if (canDashNow &&
            distanceToPlayer >= minDashTriggerDistance &&
            distanceToPlayer <= maxDashTriggerDistance &&
            Random.Range(0f, 1f) <= dashChance)
        {
            Vector3 dashDirection = GetDashDirection();

            if (dashDirection != Vector3.zero)
            {
                StartCoroutine(PerformDash(dashDirection));
            }
        }
    }

    private Vector3 GetDashDirection()
    {
        Vector3 dashCenter = transform.position + transform.TransformDirection(dashCenterOffset);
        Vector3 directionToPlayer = (player.transform.position - dashCenter).normalized;
        Vector3 dashDirection;

        if (Random.Range(0f, 1f) <= 0.7f)
        {
            dashDirection = directionToPlayer;
        }
        else
        {
            Vector3 sideways = Vector3.Cross(directionToPlayer, Vector3.up).normalized;
            dashDirection = Random.Range(0f, 1f) > 0.5f ? sideways : -sideways;
        }

        Vector3 dashTarget = dashCenter + dashDirection * dashDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(dashTarget, out hit, dashDistance, NavMesh.AllAreas))
        {
            dashTarget = hit.position;
        }
        else
        {
            return Vector3.zero;
        }

        if (Physics.Raycast(dashCenter, dashDirection, dashDistance, dashObstacleLayer))
        {
            return Vector3.zero;
        }

        return dashDirection;
    }

    private IEnumerator PerformDash(Vector3 dashDirection)
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashCenter = transform.position + transform.TransformDirection(dashCenterOffset);
        dashTarget = dashCenter + dashDirection * dashDistance;
        Vector3 agentDashTarget = transform.position + (dashTarget - dashCenter);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(agentDashTarget, out hit, dashDistance, NavMesh.AllAreas))
        {
            agentDashTarget = hit.position;
        }

        dashTarget = agentDashTarget;

        agent.isStopped = true;
        agent.enabled = false;

        transform.rotation = Quaternion.LookRotation(dashDirection);

        if (animator != null && !string.IsNullOrEmpty(dashAnimationName))
        {
            animator.Play(dashAnimationName);
        }

        float dashTimer = 0f;
        Vector3 startPos = transform.position;

        while (dashTimer < dashDuration)
        {
            dashTimer += Time.deltaTime;
            float progress = dashTimer / dashDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPos, dashTarget, smoothProgress);

            yield return null;
        }

        transform.position = dashTarget;

        agent.enabled = true;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(transform.position, out navHit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(navHit.position);
        }

        agent.isStopped = false;
        isDashing = false;

        yield return new WaitForSeconds(0.2f);
    }

    private Vector3 GetTargetDestination()
    {
        if (useRadius)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            float distanceFromStart = Vector3.Distance(transform.position, startPosition);

            if (distanceToPlayer <= followRadius && distanceToPlayer > stopRadius)
            {
                return player.transform.position;
            }
            else if (distanceFromStart > followRadius || distanceToPlayer > followRadius)
            {
                return startPosition;
            }
            else if (distanceToPlayer <= stopRadius)
            {
                return transform.position;
            }
        }
        else
        {
            return player.transform.position;
        }

        return transform.position;
    }

    private void CheckTargetReachability(Vector3 targetDestination)
    {
        if (Vector3.Distance(targetDestination, lastDestination) > 0.5f)
        {
            lastDestination = targetDestination;
            pathCheckTimer = 0f;
            targetUnreachable = false;
        }

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(targetDestination, path))
        {
            if (path.status == NavMeshPathStatus.PathPartial || path.status == NavMeshPathStatus.PathInvalid)
            {
                pathCheckTimer += Time.deltaTime;
                if (pathCheckTimer >= pathCheckTime)
                {
                    targetUnreachable = true;
                }
            }
            else
            {
                pathCheckTimer = 0f;
                targetUnreachable = false;
            }
        }
    }

    private void CheckCombatConditions()
    {
        if (player == null || isInBurstMode) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (canMelee && distanceToPlayer <= meleeRange)
        {
            bool canMeleeNow = Time.time >= lastMeleeTime + meleeCooldown;

            if (canMeleeNow)
            {
                if (Random.Range(0f, 1f) <= meleeChance)
                {
                    StartCoroutine(PerformMeleeAttack());
                    return;
                }
            }
        }

        CheckShootingConditions();
    }

    private void CheckShootingConditions()
    {
        if (player == null || projectilePrefab == null || isInBurstMode) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float fireInterval = 1f / fireRate;
        bool canShoot = Time.time >= lastShootTime + fireInterval;

        if (canShoot && (distanceToPlayer > maxShootDistance || targetUnreachable))
        {
            StartCoroutine(ShootAtTarget());
        }
    }

    private IEnumerator PerformMeleeAttack()
    {
        isMeleeAttacking = true;
        agent.isStopped = true;

        Vector3 directionToTarget = (player.transform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = targetRotation;

        if (meleeSwingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(meleeSwingSound);
        }

        if (animator != null && !string.IsNullOrEmpty(meleeAnimationName))
        {
            animator.Play(meleeAnimationName);
        }

        yield return new WaitForSeconds(meleeAttackDelay);

        ApplyMeleeDamage();

        lastMeleeTime = Time.time;

        float remainingAnimTime = meleeAnimationDuration - meleeAttackDelay;
        if (remainingAnimTime > 0)
        {
            yield return new WaitForSeconds(remainingAnimTime);
        }

        agent.isStopped = false;
        isMeleeAttacking = false;
    }

    private void ApplyMeleeDamage()
    {
        Vector3 attackCenter = transform.position + transform.TransformDirection(meleeAttackOffset);

        Collider[] hitColliders = Physics.OverlapSphere(attackCenter, meleeRange, meleeTargetLayer);

        List<GameObject> damagedTargets = new List<GameObject>();

        foreach (Collider col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 directionToTarget = (col.transform.position - attackCenter).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            if (angleToTarget <= meleeAngle / 2f)
            {
                GameObject target = col.gameObject;

                if (!damagedTargets.Contains(target))
                {
                    damagedTargets.Add(target);

                    if (meleeKnockback)
                    {
                        Rigidbody targetRb = target.GetComponent<Rigidbody>();
                        if (targetRb != null)
                        {
                            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
                            knockbackDirection.y = 0.3f;
                            targetRb.AddForce(knockbackDirection * meleeKnockbackForce, ForceMode.Impulse);
                        }
                    }

                    if (meleeHitEffect != null)
                    {
                        ParticleSystem effect = Instantiate(meleeHitEffect, col.transform.position, Quaternion.identity);
                        effect.Play();
                        Destroy(effect.gameObject, 2f);
                    }

                    if (meleeHitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(meleeHitSound);
                    }

                    Debug.Log($"Melee hit: {target.name} at angle {angleToTarget}°");
                }
            }
        }

        if (damagedTargets.Count == 0)
        {
            Debug.Log("Melee attack missed - no targets in range/angle");
        }
    }

    public void OnMeleeAttackHit()
    {
        if (!useAnimationEvents) return;
        ApplyMeleeDamage();
    }

    private IEnumerator ShootAtTarget()
    {
        isShooting = true;
        isInBurstMode = true;
        agent.isStopped = true;

        Vector3 directionToTarget = (player.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));

        if (animator != null && !string.IsNullOrEmpty(shootAnimationName))
        {
            animator.Play(shootAnimationName);
            yield return new WaitForSeconds(0.3f);
        }

        for (int i = 0; i < burstCount; i++)
        {
            FireProjectile();

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        lastShootTime = Time.time;

        yield return new WaitForSeconds(0.5f);

        agent.isStopped = false;
        isShooting = false;
        isInBurstMode = false;

        yield return new WaitForSeconds(shootCooldown);

        targetUnreachable = false;
        pathCheckTimer = 0f;
    }

    private void FireProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);

        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            Vector3 direction = (player.transform.position - shootPoint.position).normalized;

            float gravityCompensation = Mathf.Clamp(3f / projectileSpeed, 0f, 0.3f);
            direction.y += gravityCompensation;
            direction = direction.normalized;

            projRb.velocity = direction * projectileSpeed;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (isDashing || isJumping || isShooting || isMeleeAttacking)
        {
            return;
        }
        else if (isMoving)
        {
            animator.Play(walkAnimationName);
        }
        else
        {
            animator.Play(idleAnimationName);
        }
    }

    public void ForceMeleeAttack()
    {
        if (!isMeleeAttacking && !isDashing && !isShooting && canMelee)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    public void ForceDash(Vector3 direction)
    {
        if (!isDashing && !isShooting && !isMeleeAttacking)
        {
            StartCoroutine(PerformDash(direction.normalized));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showMeleeDebugGizmos) return;

        Vector3 attackCenter = transform.position + transform.TransformDirection(meleeAttackOffset);

        // Радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCenter, meleeRange);

        // Конус атаки
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward * meleeRange;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-meleeAngle / 2f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(meleeAngle / 2f, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.DrawLine(attackCenter, attackCenter + leftRayDirection);
        Gizmos.DrawLine(attackCenter, attackCenter + rightRayDirection);
        Gizmos.DrawLine(attackCenter, attackCenter + forward);
    }
}