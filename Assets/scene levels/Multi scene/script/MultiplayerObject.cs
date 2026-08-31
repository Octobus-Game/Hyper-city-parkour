using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на каждый подбираемый объект.
/// Требования:
///   - NetworkIdentity + NetworkTransform (Server Authority) на этом объекте
///   - Layer = "Multiplayer-object"
///   - Collider (isTrigger = false) — обычная физика
///   - Rigidbody
/// "Pick up object line" должен иметь Collider с isTrigger = true.
/// </summary>




[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MultiplayerObject : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Ищем PickupLine в иерархии объекта-триггера (Pick up object line → Player)
        PickupLine pickupLine = other.GetComponentInParent<PickupLine>();
        if (pickupLine == null) return;

        // Только локальный игрок отправляет команду
        if (!pickupLine.isLocalPlayer) return;

        NetworkIdentity ni = GetComponent<NetworkIdentity>();
        pickupLine.CmdAttachObject(ni);

        Debug.Log($"[MultiplayerObject] Запрос на захват '{gameObject.name}' отправлен.");
    }
}