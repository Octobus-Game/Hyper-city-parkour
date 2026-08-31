using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на объект игрока (NetworkBehaviour).
/// Требует дочерний объект с именем "Pick up object line"
/// с коллайдером (isTrigger = true).
/// </summary>
public class PickupLine : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Дочерний объект 'Pick up object line' с коллайдером-триггером")]
    [SerializeField] private GameObject pickupLineObject;

    [Header("Settings")]
    [Tooltip("Слой объектов, которые можно подобрать")]
    [SerializeField] private string pickupLayerName = "Multiplayer-object";

    // Текущий прикреплённый объект (только на сервере)
    private GameObject _attachedObject;
    private int _pickupLayer;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _pickupLayer = LayerMask.NameToLayer(pickupLayerName);

        if (_pickupLayer == -1)
            Debug.LogError($"[PickupLine] Слой '{pickupLayerName}' не найден! Создайте его в Project Settings → Tags and Layers.");

        if (pickupLineObject == null)
            Debug.LogError("[PickupLine] Поле 'pickupLineObject' не задано!");
        else
            pickupLineObject.SetActive(false); // начинаем выключенным
    }

    private void Update()
    {
        // Только локальный игрок обрабатывает ввод
        if (!isLocalPlayer) return;

        if (Input.GetMouseButtonDown(0))
            CmdActivatePickupLine(true);

        if (Input.GetMouseButtonUp(0))
            CmdActivatePickupLine(false);
    }

    // ──────────────────────────────────────────────
    // Commands (клиент → сервер)
    // ──────────────────────────────────────────────

    /// <summary>Включает / выключает линию подбора на сервере.</summary>
    [Command]
    private void CmdActivatePickupLine(bool activate)
    {
        if (pickupLineObject == null) return;

        pickupLineObject.SetActive(activate);

        // При отпускании — открепляем объект
        if (!activate && _attachedObject != null)
            DetachObject();

        // Синхронизируем состояние линии на всех клиентах
        RpcSetPickupLineActive(activate);
    }

    /// <summary>Прикрепляет найденный объект к линии (вызывается сервером).</summary>
    [Command]
    public void CmdAttachObject(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null || _attachedObject != null) return;

        GameObject target = targetIdentity.gameObject;

        // Проверяем слой ещё раз на сервере (защита от читов)
        if (target.layer != _pickupLayer)
        {
            Debug.LogWarning($"[PickupLine] Объект '{target.name}' имеет неверный слой, отклонено.");
            return;
        }

        _attachedObject = target;

        // Сообщаем всем клиентам о прикреплении
        RpcAttachObject(targetIdentity);
    }

    // ──────────────────────────────────────────────
    // ClientRpc (сервер → все клиенты)
    // ──────────────────────────────────────────────

    [ClientRpc]
    private void RpcSetPickupLineActive(bool activate)
    {
        if (pickupLineObject != null)
            pickupLineObject.SetActive(activate);
    }

    [ClientRpc]
    private void RpcAttachObject(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null || pickupLineObject == null) return;

        Transform target = targetIdentity.transform;

        // Прикрепляем объект к "Pick up object line"
        target.SetParent(pickupLineObject.transform, worldPositionStays: true);

        // Отключаем физику, пока объект прикреплён
        if (target.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[PickupLine] Объект '{target.name}' прикреплён к линии подбора.");
    }

    // ──────────────────────────────────────────────
    // Detach helper (сервер)
    // ──────────────────────────────────────────────

    private void DetachObject()
    {
        if (_attachedObject == null) return;

        if (_attachedObject.TryGetComponent<NetworkIdentity>(out var ni))
            RpcDetachObject(ni);

        _attachedObject = null;
    }

    [ClientRpc]
    private void RpcDetachObject(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        Transform target = targetIdentity.transform;
        target.SetParent(null); // открепляем от иерархии

        // Возвращаем физику
        if (target.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        Debug.Log($"[PickupLine] Объект '{target.name}' откреплён.");
    }
}
