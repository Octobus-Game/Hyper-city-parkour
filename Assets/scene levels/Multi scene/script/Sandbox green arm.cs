using UnityEngine;
using Mirror;

/// <summary>
/// Прикрепляется к объекту-«руке» (arm).
/// R — подобрать / отпустить объект со слоя "Multiplayer-object".
/// Подобранный объект притягивается к ЭТОМУ transform (не к камере).
/// </summary>
public class GreenArmpickup : NetworkBehaviour
{
    // ==============================================
    //  Pickup Settings
    // ==============================================
    [Header("Pickup Settings")]
    [Tooltip("Дальность луча подбора")]
    public float pickupRange = 5f;

    [Tooltip("Смещение объекта относительно ЭТОГО transform пока держим")]
    public Vector3 holdOffset = new Vector3(0f, 0f, 1.2f);

    [Tooltip("Скорость притяжения объекта к руке")]
    public float holdLerpSpeed = 15f;

    [Tooltip("Объект будет повторять поворот этого transform")]
    public bool matchRotation = true;

    // ==============================================
    //  Gizmo Settings
    // ==============================================
    [Header("Gizmo Settings")]
    public bool showGizmos = true;

    [Tooltip("Направление вспомогательных линий в редакторе")]
    public GizmoLineDirection lineDirection = GizmoLineDirection.Forward;

    [Tooltip("Только для Custom: произвольное направление (локальные координаты)")]
    public Vector3 customDirection = Vector3.forward;

    [Tooltip("Цвет луча подбора — свободен")]
    public Color rayFreeColor    = new Color(1f, 0.9f, 0.1f);   // жёлтый
    [Tooltip("Цвет луча подбора — держим")]
    public Color rayHoldColor    = new Color(0.2f, 1f, 0.3f);   // зелёный
    [Tooltip("Цвет точки hold-якоря")]
    public Color holdAnchorColor = new Color(0.3f, 0.8f, 1f);   // голубой

    // ==============================================
    //  Enum — появляется как dropdown в инспекторе
    // ==============================================
    public enum GizmoLineDirection
    {
        Forward,  // +Z локальный
        Back,     // -Z
        Right,    // +X
        Left,     // -X
        Up,       // +Y
        Down,     // -Y
        Custom    // произвольный вектор (см. customDirection)
    }

    // ==============================================
    //  Приватные поля
    // ==============================================
    private Camera       playerCamera;
    private PickupObject heldObject;
    private int          pickupLayerMask;

    // ==============================================
    //  Lifecycle
    // ==============================================

    public override void OnStartLocalPlayer()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        int layer = LayerMask.NameToLayer("Multiplayer-object");
        if (layer == -1)
            Debug.LogWarning("[PlayerPickup] Слой 'Multiplayer-object' не найден! " +
                             "Создайте в Edit -> Project Settings -> Tags & Layers.");
        pickupLayerMask = (layer != -1) ? (1 << layer) : 0;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (heldObject == null) TryPickup();
            else                    DropObject();
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || heldObject == null) return;

        // Цель — ЭТОТ transform (объект со скриптом), а не камера
        Vector3 targetPos = transform.TransformPoint(holdOffset);

        heldObject.transform.position = Vector3.Lerp(
            heldObject.transform.position,
            targetPos,
            Time.deltaTime * holdLerpSpeed
        );

        if (matchRotation)
        {
            heldObject.transform.rotation = Quaternion.Slerp(
                heldObject.transform.rotation,
                transform.rotation,
                Time.deltaTime * holdLerpSpeed
            );
        }
    }

    // ==============================================
    //  Pickup / Drop
    // ==============================================

    private void TryPickup()
    {
        if (playerCamera == null || pickupLayerMask == 0) return;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayerMask))
            return;

        PickupObject target = hit.collider.GetComponent<PickupObject>();
        if (target == null) return;

        target.CmdRequestAuthority(GetComponent<NetworkIdentity>());
        heldObject = target;
        heldObject.SetHeldState(true);

        Debug.Log($"[PlayerPickup] Подобрали: {heldObject.name} -> прикреплён к {name}");
    }

    private void DropObject()
    {
        if (heldObject == null) return;

        heldObject.SetHeldState(false);
        heldObject.CmdReleaseAuthority();
        Debug.Log($"[PlayerPickup] Отпустили: {heldObject.name}");
        heldObject = null;
    }

    // ==============================================
    //  Вектор по выбранному направлению
    // ==============================================

    private Vector3 GetGizmoDirection()
    {
        switch (lineDirection)
        {
            case GizmoLineDirection.Forward: return  transform.forward;
            case GizmoLineDirection.Back:    return -transform.forward;
            case GizmoLineDirection.Right:   return  transform.right;
            case GizmoLineDirection.Left:    return -transform.right;
            case GizmoLineDirection.Up:      return  transform.up;
            case GizmoLineDirection.Down:    return -transform.up;
            case GizmoLineDirection.Custom:  return  transform.TransformDirection(customDirection.normalized);
            default:                         return  transform.forward;
        }
    }

    // ==============================================
    //  Editor Gizmos
    // ==============================================
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 origin     = transform.position;
        Vector3 dir        = GetGizmoDirection();
        Vector3 holdAnchor = transform.TransformPoint(holdOffset);
        bool    holding    = Application.isPlaying && heldObject != null;
        Color   rayColor   = holding ? rayHoldColor : rayFreeColor;

        // -- 1. Луч подбора по выбранному направлению --
        UnityEditor.Handles.color = rayColor;
        UnityEditor.Handles.DrawLine(origin, origin + dir * pickupRange);

        // Пунктирная подложка
        UnityEditor.Handles.color = new Color(rayColor.r, rayColor.g, rayColor.b, 0.35f);
        UnityEditor.Handles.DrawDottedLine(origin, origin + dir * pickupRange, 5f);

        // Сфера на конце луча
        Gizmos.color = rayColor;
        Gizmos.DrawWireSphere(origin + dir * pickupRange, 0.08f);

        // -- 2. Hold-якорь на ЭТОМ объекте --
        UnityEditor.Handles.color = holdAnchorColor;
        UnityEditor.Handles.DrawDottedLine(origin, holdAnchor, 4f);

        Gizmos.color = holdAnchorColor;
        Gizmos.DrawWireSphere(holdAnchor, 0.1f);

        Gizmos.color = new Color(holdAnchorColor.r, holdAnchorColor.g, holdAnchorColor.b, 0.15f);
        Gizmos.DrawSphere(holdAnchor, 0.15f);

        // -- 3. Линия от якоря к держимому объекту (play mode) --
        if (holding)
        {
            Gizmos.color = rayHoldColor;
            Gizmos.DrawLine(holdAnchor, heldObject.transform.position);
            Gizmos.DrawWireSphere(heldObject.transform.position, 0.12f);
        }

        // -- 4. Текстовые метки --
        GUIStyle style = new GUIStyle { fontSize = 10 };

        style.normal.textColor = holding ? Color.green : Color.white;
        UnityEditor.Handles.Label(
            origin + transform.up * 0.35f,
            holding ? $"HOLDING: {heldObject.name}" : "idle",
            style
        );

        style.normal.textColor = new Color(0.5f, 0.85f, 1f);
        UnityEditor.Handles.Label(holdAnchor + transform.up * 0.2f, "anchor", style);

        // -- 5. Метка текущего направления --
        style.normal.textColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.8f);
        UnityEditor.Handles.Label(
            origin + dir * (pickupRange + 0.15f),
            lineDirection.ToString(),
            style
        );
    }
#endif
}