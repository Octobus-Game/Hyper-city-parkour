using UnityEngine;

public class GrapplingHookSystem : MonoBehaviour
{
    [Header("Grappling Hook Settings")]
    public float hookMaxDistance = 50f;
    public float hookSpeed = 30f;
    public float hookSwingForce = 15f;
    public float hookUpwardForce = 8f;
    public LayerMask hookGrappableLayer = -1;

    [Header("ULTRAKILL Style Settings")]
    public bool useUltrakillMode = true;
    public float ultrakillPullSpeed = 60f;
    public float ultrakillStopDistance = 2f;
    public AnimationCurve ultrakillSpeedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Input")]
    public KeyCode hookGrappleKey = KeyCode.Mouse0;
    public KeyCode hookReleaseKey = KeyCode.Mouse1;

    [Header("References")]
    public Transform hookGunTip;
    public LineRenderer hookRope;
    public Camera hookPlayerCamera;

    private CharacterController hookCharacterController;
    private Vector3 hookGrapplePoint;
    private Vector3 hookVelocity;
    private bool hookIsGrappling = false;
    private float hookGrappleDistance;
    private SpringJoint hookJoint;
    private Rigidbody hookRb;

    // ULTRAKILL режим переменные
    private bool hookIsPulling = false;
    private float hookPullStartTime;
    private Vector3 hookPullStartPosition;
    private float hookPullTotalDistance;

    void Start()
    {
        hookCharacterController = GetComponent<CharacterController>();

        // Добавляем Rigidbody для физики качания
        hookRb = GetComponent<Rigidbody>();
        if (hookRb == null)
        {
            hookRb = gameObject.AddComponent<Rigidbody>();
        }
        hookRb.useGravity = false; // Управляем гравитацией вручную
        hookRb.isKinematic = true; // Начинаем в кинематическом режиме

        // Настройка веревки
        if (hookRope == null)
        {
            hookRope = gameObject.AddComponent<LineRenderer>();
        }
        hookRope.material = new Material(Shader.Find("Sprites/Default"));
        hookRope.endColor = Color.yellow;
        hookRope.startWidth = 0.1f;
        hookRope.endWidth = 0.1f;
        hookRope.positionCount = 2;
        hookRope.enabled = false;
    }

    void Update()
    {
        HandleHookInput();

        if (hookIsGrappling)
        {
            if (useUltrakillMode && hookIsPulling)
            {
                HandleUltrakillPull();
            }
            DrawHookRope();
        }

        if (!hookIsGrappling)
        {
            HandleHookMovement();
        }
    }

    void HandleHookInput()
    {
        if (Input.GetKeyDown(hookGrappleKey))
        {
            StartHookGrapple();
        }

        if (Input.GetKeyDown(hookReleaseKey))
        {
            StopHookGrapple();
        }
    }

    void StartHookGrapple()
    {
        if (hookIsGrappling) return;

        RaycastHit hit;
        if (Physics.Raycast(hookPlayerCamera.transform.position, hookPlayerCamera.transform.forward, out hit, hookMaxDistance, hookGrappableLayer))
        {
            hookGrapplePoint = hit.point;
            hookGrappleDistance = Vector3.Distance(transform.position, hookGrapplePoint);
            hookIsGrappling = true;
            hookRope.enabled = true;

            if (useUltrakillMode)
            {
                // ULTRAKILL режим - моментальное притягивание
                StartUltrakillPull();
            }
            else
            {
                // Обычный режим качания
                StartSwingMode();
            }
        }
    }

    void StartUltrakillPull()
    {
        hookIsPulling = true;
        hookPullStartTime = Time.time;
        hookPullStartPosition = transform.position;
        hookPullTotalDistance = Vector3.Distance(hookPullStartPosition, hookGrapplePoint);

        // Отключаем Character Controller для плавного движения
        hookCharacterController.enabled = false;

        // Сбрасываем скорость
        hookVelocity = Vector3.zero;
    }

    void StartSwingMode()
    {
        // Переключаемся на физику Rigidbody
        hookRb.isKinematic = false;
        hookRb.useGravity = true;
        hookCharacterController.enabled = false;

        // Создаем SpringJoint для качания
        hookJoint = gameObject.AddComponent<SpringJoint>();
        hookJoint.autoConfigureConnectedAnchor = false;
        hookJoint.connectedAnchor = hookGrapplePoint;
        hookJoint.maxDistance = hookGrappleDistance * 0.8f;
        hookJoint.minDistance = 0;
        hookJoint.spring = 4.5f;
        hookJoint.damper = 7f;
        hookJoint.massScale = 4.5f;

        // Начальный импульс к точке зацепления
        Vector3 directionToGrapple = (hookGrapplePoint - transform.position).normalized;
        hookRb.velocity = directionToGrapple * hookSpeed * 0.5f;
    }

    void HandleUltrakillPull()
    {
        float currentDistance = Vector3.Distance(transform.position, hookGrapplePoint);

        // Проверяем, достигли ли мы цели
        if (currentDistance <= ultrakillStopDistance)
        {
            StopHookGrapple();
            return;
        }

        // Вычисляем прогресс и применяем кривую скорости
        float timePassed = Time.time - hookPullStartTime;
        float distanceTraveled = Vector3.Distance(hookPullStartPosition, transform.position);
        float progress = distanceTraveled / hookPullTotalDistance;

        // Применяем кривую скорости (начинаем быстро, замедляемся к концу)
        float speedMultiplier = ultrakillSpeedCurve.Evaluate(progress);
        float currentSpeed = ultrakillPullSpeed * speedMultiplier;

        // Направление к точке зацепления
        Vector3 directionToTarget = (hookGrapplePoint - transform.position).normalized;

        // Перемещаем игрока напрямую
        Vector3 moveVector = directionToTarget * currentSpeed * Time.deltaTime;
        transform.position += moveVector;

        // Поворачиваем игрока в направлении движения для более динамичного эффекта
        if (moveVector.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    void StopHookGrapple()
    {
        if (!hookIsGrappling) return;

        hookIsGrappling = false;
        hookIsPulling = false;
        hookRope.enabled = false;

        if (useUltrakillMode)
        {
            // В ULTRAKILL режиме просто включаем обратно Character Controller
            hookCharacterController.enabled = true;
            hookVelocity = Vector3.zero;
        }
        else
        {
            // Сохраняем скорость для плавного перехода в обычном режиме
            Vector3 finalVelocity = hookRb.velocity;

            // Уничтожаем SpringJoint
            if (hookJoint != null)
            {
                Destroy(hookJoint);
            }

            // Возвращаемся к Character Controller
            hookRb.isKinematic = true;
            hookRb.useGravity = false;
            hookCharacterController.enabled = true;

            // Применяем сохраненную скорость
            hookVelocity = finalVelocity;
        }
    }

    void DrawHookRope()
    {
        if (hookRope.enabled)
        {
            hookRope.SetPosition(0, hookGunTip != null ? hookGunTip.position : transform.position);
            hookRope.SetPosition(1, hookGrapplePoint);
        }
    }

    void HandleHookMovement()
    {
        // Базовое движение с Character Controller
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        direction = transform.TransformDirection(direction);

        float speed = 8f;
        Vector3 moveVector = direction * speed;

        // Применяем гравитацию
        if (hookCharacterController.isGrounded)
        {
            hookVelocity.y = -2f; // Небольшая сила прижатия к земле

            if (Input.GetButtonDown("Jump"))
            {
                hookVelocity.y = 10f;
            }
        }
        else
        {
            hookVelocity.y -= 20f * Time.deltaTime; // Гравитация
        }

        // Объединяем движение и скорость
        moveVector.y = hookVelocity.y;

        // Применяем движение
        hookCharacterController.Move(moveVector * Time.deltaTime);

        // Постепенно замедляем горизонтальную скорость
        hookVelocity.x = Mathf.Lerp(hookVelocity.x, 0, Time.deltaTime * 2f);
        hookVelocity.z = Mathf.Lerp(hookVelocity.z, 0, Time.deltaTime * 2f);
    }

    void FixedUpdate()
    {
        // FixedUpdate только для обычного режима качания
        if (!useUltrakillMode && hookIsGrappling && hookRb != null)
        {
            // Дополнительная сила для качания
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 swingDirection = new Vector3(horizontal, 0, vertical);
            swingDirection = transform.TransformDirection(swingDirection);

            hookRb.AddForce(swingDirection * hookSwingForce, ForceMode.Acceleration);

            // Сила подтягивания вверх
            if (Input.GetKey(KeyCode.Space))
            {
                Vector3 upwardDirection = (hookGrapplePoint - transform.position).normalized;
                hookRb.AddForce(upwardDirection * hookUpwardForce, ForceMode.Acceleration);
            }

            // Ограничиваем максимальную скорость
            if (hookRb.velocity.magnitude > 25f)
            {
                hookRb.velocity = hookRb.velocity.normalized * 25f;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Визуализация дальности зацепления
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hookMaxDistance);

        if (hookIsGrappling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hookGrapplePoint, 0.5f);
            Gizmos.DrawLine(transform.position, hookGrapplePoint);
        }
    }

    // Публичные свойства для доступа из других скриптов
    public bool IsGrappling => hookIsGrappling;
    public bool IsPulling => hookIsPulling;
    public Vector3 GrapplePoint => hookGrapplePoint;
}

