using Mirror;
using UnityEngine;

public class PlayerPickup : NetworkBehaviour
{
    [Header("Настройки подбора")]
    [SerializeField] private float pickupRange          = 3f;
    [SerializeField] private float holdDistance         = 2f;
    [SerializeField] private LayerMask pickupMask       = ~0;

    // ── НОВОЕ: множители броска ──────────────────────────────────────────────
    [Header("Настройки броска")]
    [SerializeField] private float throwForceMultiplier  = 1.5f; // сила линейного броска
    [SerializeField] private float throwSpinMultiplier   = 0.5f; // сила вращения при броске

    [Header("Ссылки")]
    [SerializeField] private Camera playerCamera;

    private PickableObject heldObject;
    private Transform      holdPoint;

    // ── НОВОЕ: отслеживание скорости камеры ─────────────────────────────────
    private Vector3    _prevCamPos;
    private Quaternion _prevCamRot;
    private Vector3    _cameraVelocity;
    private Vector3    _cameraAngularVelocity;

    // ─────────────────────────────────────────────
    // Инициализация только для локального игрока
    // ─────────────────────────────────────────────
    public override void OnStartLocalPlayer()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Создаём точку удержания перед камерой
        holdPoint = new GameObject("[HoldPoint]").transform;
        holdPoint.SetParent(playerCamera.transform);
        holdPoint.localPosition = new Vector3(0f, 0f, holdDistance);
        holdPoint.localRotation = Quaternion.identity;

        // Инициализируем предыдущие значения
        _prevCamPos = playerCamera.transform.position;
        _prevCamRot = playerCamera.transform.rotation;
    }

    // ─────────────────────────────────────────────
    // Update: только локальный игрок обрабатывает ввод
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (!isLocalPlayer) return;

        // Считаем скорость камеры каждый кадр
        TrackCameraVelocity();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
                TryPickup();
            else
                TryDrop();
        }

        // Двигаем объект к точке удержания (authority уже у нас)
        if (heldObject != null)
        {
            heldObject.transform.position = holdPoint.position;
            heldObject.transform.rotation = holdPoint.rotation;
        }
    }

    // ── НОВОЕ: расчёт линейной и угловой скоростей камеры ───────────────────
    private void TrackCameraVelocity()
    {
        if (playerCamera == null) return;

        // Линейная скорость: дельта позиции / время кадра
        _cameraVelocity = (playerCamera.transform.position - _prevCamPos) / Time.deltaTime;

        // Угловая скорость: дельта вращения → ось + угол → радианы/сек
        Quaternion deltaRot = playerCamera.transform.rotation * Quaternion.Inverse(_prevCamRot);
        deltaRot.ToAngleAxis(out float angleDeg, out Vector3 axis);

        // Нормализуем угол в диапазон [-180, 180] и защищаемся от NaN
        if (angleDeg > 180f) angleDeg -= 360f;

        _cameraAngularVelocity = Mathf.Abs(angleDeg) > 0.001f
            ? axis.normalized * (angleDeg * Mathf.Deg2Rad / Time.deltaTime)
            : Vector3.zero;

        _prevCamPos = playerCamera.transform.position;
        _prevCamRot = playerCamera.transform.rotation;
    }

    // ─────────────────────────────────────────────
    // Подбор
    // ─────────────────────────────────────────────
    private void TryPickup()
    {
        Ray ray = new Ray(playerCamera.transform.position,
                          playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupMask))
            return;

        PickableObject obj = hit.collider.GetComponent<PickableObject>();

        if (obj != null && !obj.IsHeld)
            CmdPickup(obj.netIdentity);
    }

    [Command]
    private void CmdPickup(NetworkIdentity objectIdentity)
    {
        PickableObject obj = objectIdentity.GetComponent<PickableObject>();

        if (obj == null || !obj.TryAssignTo(connectionToClient)) return;

        // Сообщаем только тому клиенту, кто подбирает
        TargetOnPickedUp(connectionToClient, objectIdentity);
    }

    // Локальный игрок получает ссылку на объект
    [TargetRpc]
    private void TargetOnPickedUp(NetworkConnection conn, NetworkIdentity objectIdentity)
    {
        heldObject = objectIdentity.GetComponent<PickableObject>();
    }

    // ─────────────────────────────────────────────
    // Бросок
    // ─────────────────────────────────────────────
    private void TryDrop()
    {
        if (heldObject == null) return;

        // Передаём накопленную скорость камеры с множителями
        Vector3 throwVelocity        = _cameraVelocity        * throwForceMultiplier;
        Vector3 throwAngularVelocity = _cameraAngularVelocity * throwSpinMultiplier;

        CmdDrop(heldObject.netIdentity, throwVelocity, throwAngularVelocity);
        heldObject = null;
    }

    // ── ИЗМЕНЕНО: CmdDrop теперь принимает и передаёт скорости ─────────────
    [Command]
    private void CmdDrop(NetworkIdentity objectIdentity, Vector3 velocity, Vector3 angularVelocity)
    {
        objectIdentity.GetComponent<PickableObject>()?.Release(velocity, angularVelocity);
    }
}