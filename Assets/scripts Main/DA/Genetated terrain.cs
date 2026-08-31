using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Размеры terrain")]
    [SerializeField] private int width = 256;
    [SerializeField] private int depth = 256;
    [SerializeField] private float height = 20f;

    [Header("Параметры Perlin Noise")]
    [SerializeField] private float scale = 20f;
    [SerializeField] private int seed = 0;

    [Header("Octaves для детализации")]
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;

    [Header("Генерация чанков")]
    [SerializeField] private bool enableChunkGeneration = true;
    [SerializeField] private Transform player;
    [SerializeField] private int viewDistance = 2; // В чанках
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Настройки")]
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private Material terrainMaterial;

    private Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    private float nextUpdateTime;
    private Vector2Int currentPlayerChunk;

    void Start()
    {
        if (randomizeOnStart)
        {
            seed = Random.Range(0, 10000);
        }

        if (!enableChunkGeneration)
        {
            // Генерируем один terrain
            GenerateSingleTerrain();
        }
        else
        {
            if (player == null)
            {
                player = Camera.main.transform;
            }
            UpdateChunks();
        }
    }

    void Update()
    {
        if (enableChunkGeneration && Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdateChunks();
        }
    }

    void UpdateChunks()
    {
        if (player == null) return;

        Vector2Int playerChunk = GetChunkCoord(player.position);

        // Проверяем, изменилась ли позиция игрока
        if (playerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = playerChunk;

            // Генерируем новые чанки
            for (int x = -viewDistance; x <= viewDistance; x++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(
                        playerChunk.x + x,
                        playerChunk.y + z
                    );

                    if (!terrainChunks.ContainsKey(chunkCoord))
                    {
                        CreateTerrainChunk(chunkCoord);
                    }
                }
            }

            // Удаляем дальние чанки
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var chunk in terrainChunks)
            {
                float distance = Vector2Int.Distance(chunk.Key, playerChunk);
                if (distance > viewDistance + 1)
                {
                    chunksToRemove.Add(chunk.Key);
                }
            }

            foreach (var coord in chunksToRemove)
            {
                Destroy(terrainChunks[coord]);
                terrainChunks.Remove(coord);
            }
        }
    }

    Vector2Int GetChunkCoord(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / width),
            Mathf.FloorToInt(position.z / depth)
        );
    }

    void CreateTerrainChunk(Vector2Int coord)
    {
        GameObject chunkObj = new GameObject($"Terrain_{coord.x}_{coord.y}");
        chunkObj.transform.parent = transform;

        TerrainData tData = new TerrainData();
        tData.heightmapResolution = width + 1;
        tData.size = new Vector3(width, height, depth);

        Terrain terrain = chunkObj.AddComponent<Terrain>();
        terrain.terrainData = tData;

        TerrainCollider collider = chunkObj.AddComponent<TerrainCollider>();
        collider.terrainData = tData;

        // Позиционируем чанк
        chunkObj.transform.position = new Vector3(coord.x * width, 0, coord.y * depth);

        // Генерируем высоты для этого чанка
        tData.SetHeights(0, 0, GenerateHeights(coord.x * width, coord.y * depth));

        terrainChunks.Add(coord, chunkObj);
    }

    void GenerateSingleTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            terrain = gameObject.AddComponent<Terrain>();
        }

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, height, depth);

        terrain.terrainData = terrainData;

        TerrainCollider collider = GetComponent<TerrainCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<TerrainCollider>();
        }
        collider.terrainData = terrainData;

        terrainData.SetHeights(0, 0, GenerateHeights(0, 0));
    }

    float[,] GenerateHeights(int offsetX, int offsetZ)
    {
        float[,] heights = new float[width + 1, depth + 1];

        for (int x = 0; x <= width; x++)
        {
            for (int z = 0; z <= depth; z++)
            {
                heights[x, z] = CalculateHeight(x + offsetX, z + offsetZ);
            }
        }

        return heights;
    }

    float CalculateHeight(int x, int z)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float xCoord = (float)x / width * scale * frequency + seed;
            float zCoord = (float)z / depth * scale * frequency + seed;

            float perlinValue = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return Mathf.InverseLerp(-1f, 1f, noiseHeight);
    }

    [ContextMenu("Regenerate All Chunks")]
    public void RegenerateAllChunks()
    {
        // Удаляем все чанки
        foreach (var chunk in terrainChunks.Values)
        {
            Destroy(chunk);
        }
        terrainChunks.Clear();

        // Генерируем новый seed
        seed = Random.Range(0, 10000);

        // Обновляем чанки
        UpdateChunks();
    }

    [ContextMenu("Clear All Chunks")]
    public void ClearAllChunks()
    {
        foreach (var chunk in terrainChunks.Values)
        {
            Destroy(chunk);
        }
        terrainChunks.Clear();
    }

    void OnDestroy()
    {
        ClearAllChunks();
    }
}