using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на префаб игрока (рядом с NetworkIdentity / NetworkTransform).
/// При нажатии T пускает луч из камеры игрока; если луч попал
/// в поверхность — просит сервер заспавнить "spray plane" в точке попадания
/// с текущей выбранной игроком текстурой.
/// </summary>
public class SprayController : NetworkBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Камера локального игрока")]
    public Camera playerCamera;

    [Tooltip("Префаб 'spray plane'. ОБЯЗАТЕЛЬНО добавить его в " +
             "NetworkManager -> Spawn Info -> Spawnable Prefabs")]
    public GameObject sprayDecalPrefab;

    [Tooltip("Компонент с текущей текстурой игрока (см. PlayerSprayTexture.cs)")]
    public PlayerSprayTexture sprayTexture;

    [Header("Настройки")]
    public float sprayRange = 5f;
    public float surfaceOffset = 0.01f; // чтобы избежать z-fighting с поверхностью
    public LayerMask sprayableLayers = ~0;

    private void Update()
    {
        // Логика ввода обрабатывается только локальным игроком,
        // а не у всех клонов на других клиентах
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            TrySpray();
        }
    }

    private void TrySpray()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, sprayRange, sprayableLayers))
        {
            byte[] texBytes = sprayTexture != null ? sprayTexture.GetCurrentTexturePng() : null;
            CmdSpawnSpray(hit.point, hit.normal, texBytes);
        }
    }

    // Команда выполняется на сервере. Сервер — единственный, кто имеет
    // право реально создавать сетевые объекты (NetworkServer.Spawn).
    [Command]
    private void CmdSpawnSpray(Vector3 point, Vector3 normal, byte[] textureBytes)
    {
        if (sprayDecalPrefab == null) return;

        // Разворачиваем плоскость по нормали поверхности.
        // Это поведение зависит от того, как смотрит "лицо" вашего меша:
        //  - если префаб сделан из стандартного Unity Plane (нормаль = +Y):
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        //  - если префаб сделан из Quad (нормаль = -Z), используйте вместо строки выше:
        // Quaternion rot = Quaternion.LookRotation(-normal, Vector3.up);

        Vector3 pos = point + normal * surfaceOffset;

        GameObject go = Instantiate(sprayDecalPrefab, pos, rot);

        SprayDecal decal = go.GetComponent<SprayDecal>();
        if (decal != null)
        {
            // Важно: значение SyncVar выставляем ДО NetworkServer.Spawn —
            // тогда оно попадёт в начальный снапшот объекта и дойдёт
            // в том числе до игроков, которые подключатся позже.
            decal.textureBytes = textureBytes;
        }

        NetworkServer.Spawn(go);
    }
}
