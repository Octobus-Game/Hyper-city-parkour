using UnityEngine;
using Mirror;
using System.Collections;

public class GrapplingHook : NetworkBehaviour
{
    [Header("Настройки гарпуна (стиль)")]
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private float hookSpeed = 100f;
    [SerializeField] private float pullForce = 30f;
    [SerializeField] private float swingForce = 15f;
    [SerializeField] private float momentum = 1.2f;
    [SerializeField] private LayerMask grappleMask;

    [Header("Физика")]
    [SerializeField] private bool preserveVelocity = true;
    [SerializeField] private float velocityBoost = 1.5f;
    [SerializeField] private float maxPullSpeed = 40f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Визуализация")]
    [SerializeField] private LineRenderer rope;
    [SerializeField] private Transform hookTip;
    [SerializeField] private GameObject hookPrefab;
    [SerializeField] private int ropeSegments = 25;

    [Header("Управление")]
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode releaseKey = KeyCode.Mouse1;

    [Header("Ссылки")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform shootPoint;

    private Vector3 grapplePoint;
    private bool isGrappling;
    private GameObject currentHook;
    private CharacterController controller;

    private Vector3 currentVelocity;
    private Vector3 storedVelocity;
    private float grappleStartTime;
    private float distanceToPoint;
    private Vector3[] ropePositions;

    private void Start()
    {
        if (!isLocalPlayer) return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("GrapplingHook требует CharacterController!");
        }

        if (rope != null)
        {
            rope.enabled = false;
            rope.positionCount = ropeSegments;
            ropePositions = new Vector3[ropeSegments];
        }

        currentVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(shootKey) && !isGrappling)
        {
            ShootGrapple();
        }

        if (Input.GetKeyDown(releaseKey) && isGrappling)
        {
            StopGrapple();
        }

        if (isGrappling)
        {
            DrawDynamicRope();
            ApplyUltrakillPhysics();
        }
    }

    private void ShootGrapple()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, grappleMask))
        {
            grapplePoint = hit.point;
            CmdShootGrapple(grapplePoint);
        }
    }

    [Command]
    private void CmdShootGrapple(Vector3 targetPoint)
    {
        RpcShootGrapple(targetPoint);
    }

    [ClientRpc]
    private void RpcShootGrapple(Vector3 targetPoint)
    {
        grapplePoint = targetPoint;
        isGrappling = true;
        grappleStartTime = Time.time;

        if (isLocalPlayer && preserveVelocity)
        {
            storedVelocity = currentVelocity;
        }

        if (rope != null)
            rope.enabled = true;

        if (hookPrefab != null && shootPoint != null)
        {
            currentHook = Instantiate(hookPrefab, shootPoint.position, Quaternion.identity);
            StartCoroutine(AnimateHook());
        }
    }

    private IEnumerator AnimateHook()
    {
        if (currentHook == null) yield break;

        Vector3 startPos = currentHook.transform.position;
        float distance = Vector3.Distance(startPos, grapplePoint);
        float duration = distance / hookSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            currentHook.transform.position = Vector3.Lerp(startPos, grapplePoint, t);
            currentHook.transform.LookAt(grapplePoint);
            yield return null;
        }

        currentHook.transform.position = grapplePoint;
    }

    private void ApplyUltrakillPhysics()
    {
        if (controller == null) return;

        Vector3 playerPos = transform.position;
        Vector3 toHook = grapplePoint - playerPos;
        distanceToPoint = toHook.magnitude;
        Vector3 pullDirection = toHook.normalized;

        float timeElapsed = Time.time - grappleStartTime;
        float pullMultiplier = pullCurve.Evaluate(Mathf.Clamp01(timeElapsed / 0.5f));

        Vector3 inputDirection = GetMovementInput();

        // Притягивание к точке (как в Ultrakill)
        float dynamicPullForce = pullForce * pullMultiplier;
        Vector3 pullVelocity = pullDirection * dynamicPullForce;

        // Сохранение импульса при зацеплении
        if (preserveVelocity && timeElapsed < 0.1f)
        {
            Vector3 tangentVelocity = Vector3.ProjectOnPlane(storedVelocity, pullDirection);
            currentVelocity += tangentVelocity * velocityBoost * Time.deltaTime;
        }

        // Свинг физика (маятниковое движение)
        Vector3 swingVelocity = Vector3.ProjectOnPlane(inputDirection * swingForce, pullDirection);

        // Применение гравитации
        Vector3 gravityVelocity = Vector3.down * gravity * Time.deltaTime;

        // Комбинированная скорость
        currentVelocity += (pullVelocity + swingVelocity) * momentum * Time.deltaTime;
        currentVelocity += gravityVelocity;

        // Ограничение максимальной скорости притягивания
        float pullSpeed = Vector3.Dot(currentVelocity, pullDirection);
        if (pullSpeed > maxPullSpeed)
        {
            Vector3 excessVelocity = pullDirection * (pullSpeed - maxPullSpeed);
            currentVelocity -= excessVelocity;
        }

        // Движение через CharacterController
        controller.Move(currentVelocity * Time.deltaTime);

        // Автоматическое отцепление при достижении точки
        if (distanceToPoint < 2f)
        {
            StopGrapple();
        }
    }

    private Vector3 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return (forward * vertical + right * horizontal).normalized;
    }

    private void DrawDynamicRope()
    {
        if (rope == null || !isGrappling) return;

        Vector3 startPoint = shootPoint != null ? shootPoint.position : transform.position;

        // Создание динамической провисающей верёвки
        for (int i = 0; i < ropeSegments; i++)
        {
            float t = i / (float)(ropeSegments - 1);
            Vector3 position = Vector3.Lerp(startPoint, grapplePoint, t);

            // Добавление провисания (катенарная кривая упрощённая)
            float sag = Mathf.Sin(t * Mathf.PI) * (distanceToPoint * 0.1f);
            position.y -= sag;

            // Добавление волн от движения
            float wave = Mathf.Sin(Time.time * 10f + t * 5f) * 0.1f;
            position += Vector3.up * wave;

            ropePositions[i] = position;
        }

        rope.SetPositions(ropePositions);
    }

    private void StopGrapple()
    {
        if (!isGrappling) return;

        // Сохранение импульса при отцеплении
        if (isLocalPlayer && preserveVelocity)
        {
            currentVelocity *= velocityBoost;
        }

        CmdStopGrapple();
    }

    [Command]
    private void CmdStopGrapple()
    {
        RpcStopGrapple();
    }

    [ClientRpc]
    private void RpcStopGrapple()
    {
        isGrappling = false;

        if (rope != null)
            rope.enabled = false;

        if (currentHook != null)
        {
            Destroy(currentHook);
            currentHook = null;
        }
    }

    // Публичные методы для интеграции с другими системами
    public bool IsGrappling()
    {
        return isGrappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }

    public float GetDistanceToPoint()
    {
        return distanceToPoint;
    }

    public Vector3 GetCurrentVelocity()
    {
        return currentVelocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        currentVelocity = velocity;
    }

    public void AddVelocity(Vector3 velocity)
    {
        currentVelocity += velocity;
    }
}