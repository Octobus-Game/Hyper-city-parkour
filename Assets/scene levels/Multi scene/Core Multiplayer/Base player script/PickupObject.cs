using Mirror;
using UnityEngine;

// Необходимые компоненты на объекте:
// - NetworkIdentity
// - NetworkTransform  (включить Client Authority)
// - Rigidbody
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransform))]
public class PickableObject : NetworkBehaviour
{
	  [Header("Pickup Settings")]
    [Tooltip("Точка, в которой удерживается объект (дочерний Transform перед камерой)")]
    public Transform holdPoint;
 
    [Tooltip("Максимальная дистанция подбора")]
    public float pickupRange = 3f;
 
    [Tooltip("Скорость броска")]
    public float throwForce = 8f;
 
    [Tooltip("Угловая скорость при броске")]
    public float throwAngularForce = 4f;
 
    [Tooltip("Слой объектов, которые можно подбирать")]
    public LayerMask pickableLayer = ~0;
 
    [Header("Hold Smoothing")]
    [Tooltip("Скорость перемещения объекта к holdPoint")]
    public float holdLerpSpeed = 15f;
 
    // ── Сетевое состояние ────────────────────────────────────────────────────
    [SyncVar] private bool _isHeld;
    public bool IsHeld => _isHeld;

    private Rigidbody _rb;

    // Назначить authority клиенту (вызывается на сервере)
    public bool TryAssignTo(NetworkConnectionToClient conn)
    {
        if (_isHeld) return false;

        _isHeld = true;
        _rb     = GetComponent<Rigidbody>();

        // Отключаем физику пока объект удерживается
        _rb.isKinematic = true;

        // Передаём client authority тому, кто подобрал
        netIdentity.AssignClientAuthority(conn);
        return true;
    }

    // Отпустить объект с заданными скоростями (вызывается на сервере)
    public void Release(Vector3 velocity, Vector3 angularVelocity)
    {
        _isHeld = false;
        _rb     = GetComponent<Rigidbody>();

        // Возвращаем физику
        _rb.isKinematic = false;

        // Убираем client authority
        netIdentity.RemoveClientAuthority();

        // Применяем скорости броска
        _rb.velocity        = velocity;
        _rb.angularVelocity = angularVelocity;
    }

    // ── Локальное состояние ───────────────────────────────────────────────────
    private PickableObject heldObject;
    private Camera playerCamera;
 
    // Для отслеживания velocity броска
    private Vector3 prevHoldPos;
    private Vector3 smoothedVelocity;
 
    // ── Unity Lifecycle ───────────────────────────────────────────────────────
 
    private void Start()
    {
        // Ищем камеру только для локального игрока
        if (!isLocalPlayer) return;
 
        playerCamera = Camera.main;
 
        // Если holdPoint не назначен — создаём автоматически перед камерой
        if (holdPoint == null && playerCamera != null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(playerCamera.transform);
            hp.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            holdPoint = hp.transform;
        }
    }
 
    private void Update()
    {
        // Обрабатываем ввод только для локального игрока
        if (!isLocalPlayer) return;
 
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
                TryPickup();
            else
                DropObject();
        }
 
        if (Input.GetKeyDown(KeyCode.Mouse0) && heldObject != null)
        {
            ThrowObject();
        }
    }
 
    private void LateUpdate()
    {
        if (!isLocalPlayer || heldObject == null || holdPoint == null) return;
 
        // Плавно перемещаем удерживаемый объект к holdPoint
        // Объект имеет authority на клиенте → NetworkTransform синхронизирует позицию
        Vector3 targetPos = holdPoint.position;
        Vector3 currentPos = heldObject.transform.position;
 
        // Считаем сглаженную скорость для броска с учётом движения руки
        smoothedVelocity = (targetPos - prevHoldPos) / Time.deltaTime;
        prevHoldPos = targetPos;
 
        heldObject.transform.position = Vector3.Lerp(currentPos, targetPos, holdLerpSpeed * Time.deltaTime);
        heldObject.transform.rotation = Quaternion.Lerp(
            heldObject.transform.rotation,
            holdPoint.rotation,
            holdLerpSpeed * Time.deltaTime
        );
    }
 
    // ── Pickup ────────────────────────────────────────────────────────────────
 
    private void TryPickup()
    {
        if (playerCamera == null) return;
 
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
 
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickableLayer))
            return;
 
        PickableObject pickable = hit.collider.GetComponent<PickableObject>();
        if (pickable == null || pickable.IsHeld) return;
 
        // Просим сервер назначить authority
        CmdPickup(pickable.netIdentity);
    }
 
    // ── Drop ──────────────────────────────────────────────────────────────────
 
    private void DropObject()
    {
        if (heldObject == null) return;
 
        CmdRelease(heldObject.netIdentity, Vector3.zero, Vector3.zero);
        heldObject = null;
        smoothedVelocity = Vector3.zero;
    }
 
    // ── Throw ─────────────────────────────────────────────────────────────────
 
    private void ThrowObject()
    {
        if (heldObject == null || playerCamera == null) return;
 
        // Направление броска = взгляд камеры + вклад скорости руки
        Vector3 throwDir      = playerCamera.transform.forward;
        Vector3 throwVelocity = throwDir * throwForce + smoothedVelocity * 0.3f;
        Vector3 throwAngular  = playerCamera.transform.right * throwAngularForce;
 
        CmdRelease(heldObject.netIdentity, throwVelocity, throwAngular);
        heldObject = null;
        smoothedVelocity = Vector3.zero;
    }
 
    // ── Commands (Client → Server) ────────────────────────────────────────────
 
    [Command]
    private void CmdPickup(NetworkIdentity objectIdentity)
    {
        PickableObject pickable = objectIdentity.GetComponent<PickableObject>();
        if (pickable == null) return;
 
        NetworkConnectionToClient conn = connectionToClient;
        if (!pickable.TryAssignTo(conn)) return;
 
        // Сообщаем ТОЛЬКО этому клиенту, что он держит объект
        TargetOnPickedUp(conn, objectIdentity);
    }
 
    [Command]
    private void CmdRelease(NetworkIdentity objectIdentity, Vector3 velocity, Vector3 angularVelocity)
    {
        PickableObject pickable = objectIdentity.GetComponent<PickableObject>();
        if (pickable == null) return;
 
        pickable.Release(velocity, angularVelocity);
    }
 
    // ── Target RPC (Server → конкретный клиент) ───────────────────────────────
 
    [TargetRpc]
    private void TargetOnPickedUp(NetworkConnection conn, NetworkIdentity objectIdentity)
    {
        PickableObject pickable = objectIdentity.GetComponent<PickableObject>();
        if (pickable == null) return;
 
        heldObject   = pickable;
        prevHoldPos  = holdPoint != null ? holdPoint.position : transform.position;
        smoothedVelocity = Vector3.zero;
    }
}