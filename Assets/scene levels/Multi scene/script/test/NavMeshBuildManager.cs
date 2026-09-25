using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

// Только на сервере: клиентам не нужен NavMesh, если AI полностью управляется сервером
public class NavMeshBuildManager : MonoBehaviour
{
    public static NavMeshBuildManager Instance { get; private set; }

    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private float rebuildDelay = 0.5f; // ждём пока меши чанков применятся

    private bool rebuildQueued;

    private void Awake()
    {
        Instance = this;
    }

    public void RequestRebuildAroundChunk(Vector2Int coord, int chunkSize)
    {
        if (!NetworkServerCheck.IsServer) return; // см. хелпер ниже, либо замените на свою проверку

        if (!rebuildQueued)
        {
            rebuildQueued = true;
            Invoke(nameof(RebuildNow), rebuildDelay);
        }
    }

    private void RebuildNow()
    {
        rebuildQueued = false;
        navMeshSurface.BuildNavMesh(); // синхронный пересчёт; для больших миров используйте BuildNavMeshAsync()
    }
}

// Небольшой хелпер, чтобы не тянуть Mirror-зависимость напрямую в этот файл, если он общий
public static class NetworkServerCheck
{
    public static bool IsServer => Mirror.NetworkServer.active;
}