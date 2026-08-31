using UnityEngine;

[System.Serializable]
public class NoiseLayer : MonoBehaviour
{
    public bool enabled = true;
    public float scale = 0.01f;
    public float strength = 1f;
    public Vector2 offset;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
}

[System.Serializable]
public class BiomeData : MonoBehaviour
{
    public string name;
    public Color debugColor;
    public float minHeight = 0f;
    public float maxHeight = 1f;
    public float minTemperature = 0f;
    public float maxTemperature = 1f;
    public float minMoisture = 0f;
    public float maxMoisture = 1f;
    public TerrainLayer terrainLayer;
}

public class AdvancedTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Settings")]
    public int terrainSize = 513;
    public float terrainHeight = 100f;

    [Header("Base Terrain")]
    public NoiseLayer baseNoise;

    [Header("Additional Layers")]
    public NoiseLayer[] noiseLayers;

    [Header("Climate")]
    public NoiseLayer temperatureNoise;
    public NoiseLayer moistureNoise;

    [Header("Biomes")]
    public BiomeData[] biomes;

    [Header("Features")]
    public bool generateRivers = true;
    public float riverThreshold = 0.1f;
    public float riverDepth = 5f;

    private Terrain terrain;
    private TerrainData terrainData;
    private float[,] heightMap;
    private float[,] temperatureMap;
    private float[,] moistureMap;

    void Start()
    {
        GenerateAdvancedTerrain();
    }

    public void GenerateAdvancedTerrain()
    {
        // Создаем TerrainData
        terrainData = new TerrainData();
        terrainData.heightmapResolution = terrainSize;
        terrainData.size = new Vector3(terrainSize - 1, terrainHeight, terrainSize - 1);

        // Генерируем карты
        GenerateHeightMap();
        GenerateClimateMap();

        // Применяем к terrain
        ApplyHeightMap();
        GenerateBiomes();

        if (generateRivers)
            GenerateRivers();

        SetupTerrain();
    }

    void GenerateHeightMap()
    {
        heightMap = new float[terrainSize, terrainSize];

        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                float height = 0f;

                // Базовый слой
                if (baseNoise.enabled)
                {
                    float baseHeight = GetNoiseValue(x, y, baseNoise);
                    height += baseHeight * baseNoise.strength;
                }

                // Дополнительные слои
                foreach (var layer in noiseLayers)
                {
                    if (layer.enabled)
                    {
                        float layerHeight = GetNoiseValue(x, y, layer);
                        height += layerHeight * layer.strength;
                    }
                }

                heightMap[x, y] = Mathf.Clamp01(height);
            }
        }
    }

    void GenerateClimateMap()
    {
        temperatureMap = new float[terrainSize, terrainSize];
        moistureMap = new float[terrainSize, terrainSize];

        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                // Температура (уменьшается с высотой и широтой)
                float baseTemp = GetNoiseValue(x, y, temperatureNoise);
                float heightInfluence = 1f - heightMap[x, y]; // Холоднее на высоте
                float latitudeInfluence = 1f - Mathf.Abs((float)y / terrainSize - 0.5f) * 2f; // Холоднее к полюсам

                temperatureMap[x, y] = Mathf.Clamp01(baseTemp * heightInfluence * latitudeInfluence);

                // Влажность
                moistureMap[x, y] = Mathf.Clamp01(GetNoiseValue(x, y, moistureNoise));
            }
        }
    }

    float GetNoiseValue(int x, int y, NoiseLayer layer)
    {
        float sampleX = (x + layer.offset.x) * layer.scale;
        float sampleY = (y + layer.offset.y) * layer.scale;

        float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

        if (layer.heightCurve != null)
        {
            noiseValue = layer.heightCurve.Evaluate(noiseValue);
        }

        return noiseValue;
    }

    void ApplyHeightMap()
    {
        terrainData.SetHeights(0, 0, heightMap);
    }

    void GenerateBiomes()
    {
        if (biomes.Length == 0) return;

        // Создаем массив для альфа-карт
        float[,,] alphamaps = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, biomes.Length];

        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Получаем параметры для этой точки
                float height = heightMap[x, y];
                float temperature = temperatureMap[x, y];
                float moisture = moistureMap[x, y];

                // Находим подходящий биом
                int bestBiome = GetBestBiome(height, temperature, moisture);

                if (bestBiome != -1)
                {
                    alphamaps[x, y, bestBiome] = 1f;
                }
            }
        }

        // Создаем TerrainLayers из биомов
        TerrainLayer[] terrainLayers = new TerrainLayer[biomes.Length];
        for (int i = 0; i < biomes.Length; i++)
        {
            terrainLayers[i] = biomes[i].terrainLayer;
        }

        terrainData.terrainLayers = terrainLayers;
        terrainData.SetAlphamaps(0, 0, alphamaps);
    }

    int GetBestBiome(float height, float temperature, float moisture)
    {
        for (int i = 0; i < biomes.Length; i++)
        {
            var biome = biomes[i];

            if (height >= biome.minHeight && height <= biome.maxHeight &&
                temperature >= biome.minTemperature && temperature <= biome.maxTemperature &&
                moisture >= biome.minMoisture && moisture <= biome.maxMoisture)
            {
                return i;
            }
        }

        return 0; // Возвращаем первый биом по умолчанию
    }

    void GenerateRivers()
    {
        // Простая генерация рек на основе низких областей влажности
        for (int x = 1; x < terrainSize - 1; x++)
        {
            for (int y = 1; y < terrainSize - 1; y++)
            {
                // Проверяем, является ли эта точка локальным минимумом высоты
                if (IsLocalMinimum(x, y) && moistureMap[x, y] > riverThreshold)
                {
                    // Создаем реку, понижая высоту
                    heightMap[x, y] = Mathf.Max(0, heightMap[x, y] - riverDepth / terrainHeight);

                    // Расширяем реку на соседние точки
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < terrainSize && ny >= 0 && ny < terrainSize)
                            {
                                heightMap[nx, ny] = Mathf.Max(heightMap[nx, ny], heightMap[x, y]);
                            }
                        }
                    }
                }
            }
        }

        // Повторно применяем карту высот
        terrainData.SetHeights(0, 0, heightMap);
    }

    bool IsLocalMinimum(int x, int y)
    {
        float centerHeight = heightMap[x, y];

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < terrainSize && ny >= 0 && ny < terrainSize)
                {
                    if (heightMap[nx, ny] < centerHeight)
                        return false;
                }
            }
        }

        return true;
    }

    void SetupTerrain()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
            terrain = gameObject.AddComponent<Terrain>();

        terrain.terrainData = terrainData;

        TerrainCollider collider = GetComponent<TerrainCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<TerrainCollider>();
        collider.terrainData = terrainData;
    }

    // Метод для визуализации климатических карт в Scene View
    void OnDrawGizmosSelected()
    {
        if (temperatureMap == null || moistureMap == null) return;

        for (int x = 0; x < terrainSize; x += 10)
        {
            for (int y = 0; y < terrainSize; y += 10)
            {
                Vector3 worldPos = transform.position + new Vector3(x, heightMap[x, y] * terrainHeight, y);

                // Показываем температуру как цвет от синего к красному
                Gizmos.color = Color.Lerp(Color.blue, Color.red, temperatureMap[x, y]);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 2);
            }
        }
    }
}