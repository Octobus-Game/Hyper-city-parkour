using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class TerrainChunkManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject chunkPrefab; // Prefab с TerrainChunk + NetworkIdentity, зарегистрирован в NetworkManager

    [Header("Settings")]
    [SerializeField] private int chunkSize = 32;
    [SerializeField] private int viewDistanceInChunks = 3;
    [SerializeField] private float updateInterval = 1f;

    private int worldSeed;
    private readonly Dictionary<Vector2Int, TerrainChunk> activeChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private float timer;

    public override void OnStartServer()
    {
        worldSeed = Random.Range(int.MinValue, int.MaxValue);
    }

    [ServerCallback]
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        UpdateChunksAroundPlayers();
    }

    [Server]
    private void UpdateChunksAroundPlayers()
    {
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();

        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;

            Vector3 pos = conn.identity.transform.position;
            Vector2Int playerChunk = WorldToChunkCoord(pos);

            for (int dz = -viewDistanceInChunks; dz <= viewDistanceInChunks; dz++)
            {
                for (int dx = -viewDistanceInChunks; dx <= viewDistanceInChunks; dx++)
                {
                    neededChunks.Add(playerChunk + new Vector2Int(dx, dz));
                }
            }
        }

        // Спавним недостающие
        foreach (Vector2Int coord in neededChunks)
        {
            if (!activeChunks.ContainsKey(coord))
                SpawnChunk(coord);
        }

        // Деспавним лишние
        List<Vector2Int> toRemove = null;
        foreach (var kvp in activeChunks)
        {
            if (!neededChunks.Contains(kvp.Key))
            {
                toRemove ??= new List<Vector2Int>();
                toRemove.Add(kvp.Key);
            }
        }

        if (toRemove != null)
        {
            foreach (var coord in toRemove)
            {
                if (activeChunks[coord] != null)
                    NetworkServer.Destroy(activeChunks[coord].gameObject);
                activeChunks.Remove(coord);
            }
        }
    }

    [Server]
    private void SpawnChunk(Vector2Int coord)
    {
        GameObject go = Instantiate(chunkPrefab);
        TerrainChunk chunk = go.GetComponent<TerrainChunk>();

        NetworkServer.Spawn(go);           // сначала спавн, чтобы NetworkIdentity назначился
        chunk.ServerInitialize(worldSeed, coord); // затем инициализация — SyncVars разошлются клиентам

        activeChunks[coord] = chunk;

        // Пересчёт NavMesh после того как геометрия готова
        NavMeshBuildManager.Instance?.RequestRebuildAroundChunk(coord, chunkSize);
    }

    private Vector2Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
}