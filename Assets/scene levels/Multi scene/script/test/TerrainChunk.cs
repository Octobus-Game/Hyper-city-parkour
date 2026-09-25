using UnityEngine;
using Mirror;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainChunk : NetworkBehaviour
{
    [Header("Chunk Size")]
    [SerializeField] private int chunkSize = 32; // квадратов на сторону
    [SerializeField] private float heightMultiplier = 15f;

    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 40f;
    [SerializeField] private int octaves = 4;
    [SerializeField] [Range(0f, 1f)] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Low-Poly")]
    [SerializeField] private bool flatShading = true;

    [Header("Height Coloring")]
    [SerializeField] private Gradient heightGradient;
    [SerializeField] private float waterLevel = 0.25f;
    [SerializeField] private float rockLevel = 0.65f;
    [SerializeField] private float snowLevel = 0.85f;

    // Глобальный seed мира — задаётся менеджером один раз при спавне мира
    [SyncVar] private int worldSeed;

    // Координаты чанка в сетке (не пиксели, а индекс чанка)
    [SyncVar(hook = nameof(OnCoordChanged))]
    private Vector2Int coord;

    private Mesh mesh;
    private MeshCollider meshCollider;
    private float[,] heightMap;

    public Vector2Int Coord => coord;

    private void Awake()
    {
        mesh = new Mesh { name = "TerrainChunk" };
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        if (heightGradient == null || heightGradient.colorKeys.Length == 0)
            heightGradient = DefaultGradient();
    }

    // Вызывается сервером сразу после спавна, до отправки клиентам
    [Server]
    public void ServerInitialize(int seed, Vector2Int chunkCoord)
    {
        worldSeed = seed;
        coord = chunkCoord; // хук отработает и на сервере тоже (локальный вызов)
        GenerateAndBuild();
        transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
    }

    public override void OnStartClient()
    {
        // host / late join — на случай если хук не поймал начальные значения
        if (mesh.vertexCount == 0)
        {
            transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
            GenerateAndBuild();
        }
    }

    private void OnCoordChanged(Vector2Int oldCoord, Vector2Int newCoord)
    {
        coord = newCoord;
        transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
        if (worldSeed != 0 || newCoord != Vector2Int.zero)
            GenerateAndBuild();
    }

    private void GenerateAndBuild()
    {
        heightMap = GenerateHeightMap();
        BuildMesh(heightMap);
    }

    // ---------- Шум Перлина, детерминированный по worldSeed + мировым координатам ----------
    private float[,] GenerateHeightMap()
    {
        int size = chunkSize + 1;
        float[,] map = new float[size, size];

        System.Random prng = new System.Random(worldSeed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        }

        // Мировое смещение чанка — важно, чтобы соседние чанки стыковались без швов
        float worldOffsetX = coord.x * chunkSize;
        float worldOffsetZ = coord.y * chunkSize;

        float maxH = float.MinValue, minH = float.MaxValue;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                float amplitude = 1f, frequency = 1f, noiseHeight = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    float sampleX = (worldOffsetX + x + octaveOffsets[o].x) / noiseScale * frequency;
                    float sampleZ = (worldOffsetZ + z + octaveOffsets[o].y) / noiseScale * frequency;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseHeight += perlin * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxH) maxH = noiseHeight;
                if (noiseHeight < minH) minH = noiseHeight;
                map[x, z] = noiseHeight;
            }
        }

        // ВАЖНО: нормализуем фиксированным диапазоном, а не min/max текущего чанка,
        // иначе соседние чанки будут иметь разную "растяжку" высот и появятся уступы на стыках
        for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
                map[x, z] = Mathf.Clamp01((map[x, z] + 1f) / 2f);

        return map;
    }

    // ---------- Меш с vertex colors по высоте ----------
    private void BuildMesh(float[,] hMap)
    {
        int size = chunkSize + 1;

        Vector3[] verts = new Vector3[size * size];
        Vector2[] uvs = new Vector2[verts.Length];
        Color[] colors = new Color[verts.Length];
        int[] tris = new int[chunkSize * chunkSize * 6];

        int vi = 0, ti = 0;
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                float t = heightCurve.Evaluate(hMap[x, z]);
                float h = t * heightMultiplier;

                verts[vi] = new Vector3(x, h, z);
                uvs[vi] = new Vector2((float)x / chunkSize, (float)z / chunkSize);
                colors[vi] = EvaluateBiomeColor(t);

                if (x < chunkSize && z < chunkSize)
                {
                    int topLeft = vi;
                    int topRight = vi + 1;
                    int bottomLeft = vi + size;
                    int bottomRight = vi + size + 1;

                    tris[ti++] = topLeft;
                    tris[ti++] = bottomLeft;
                    tris[ti++] = topRight;

                    tris[ti++] = topRight;
                    tris[ti++] = bottomLeft;
                    tris[ti++] = bottomRight;
                }
                vi++;
            }
        }

        if (flatShading)
            ApplyFlatShading(verts, tris, uvs, colors);
        else
            ApplyMesh(verts, tris, uvs, colors);

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    private Color EvaluateBiomeColor(float t)
    {
        // Если задан кастомный градиент — используем его
        if (heightGradient != null && heightGradient.colorKeys.Length > 1)
            return heightGradient.Evaluate(t);

        // Дефолтные пороги: вода -> песок -> трава -> скала -> снег
        if (t < waterLevel) return new Color(0.15f, 0.35f, 0.65f);
        if (t < waterLevel + 0.05f) return new Color(0.76f, 0.7f, 0.5f);
        if (t < rockLevel) return new Color(0.25f, 0.55f, 0.2f);
        if (t < snowLevel) return new Color(0.45f, 0.4f, 0.38f);
        return Color.white;
    }

    private void ApplyMesh(Vector3[] verts, int[] tris, Vector2[] uvs, Color[] colors)
    {
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void ApplyFlatShading(Vector3[] verts, int[] tris, Vector2[] uvs, Color[] colors)
    {
        Vector3[] flatVerts = new Vector3[tris.Length];
        Vector2[] flatUVs = new Vector2[tris.Length];
        Color[] flatColors = new Color[tris.Length];
        int[] flatTris = new int[tris.Length];

        for (int i = 0; i < tris.Length; i++)
        {
            flatVerts[i] = verts[tris[i]];
            flatUVs[i] = uvs[tris[i]];
            flatColors[i] = colors[tris[i]];
            flatTris[i] = i;
        }

        ApplyMesh(flatVerts, flatTris, flatUVs, flatColors);
    }

    private static Gradient DefaultGradient() => null; // используем пороговую логику по умолчанию

    // Высота ландшафта в мировой точке (для спавна объектов ровно на поверхности)
    public bool TryGetHeightAtLocalPos(float localX, float localZ, out float height)
    {
        if (heightMap == null || localX < 0 || localZ < 0 || localX > chunkSize || localZ > chunkSize)
        {
            height = 0f;
            return false;
        }
        int x = Mathf.Clamp(Mathf.RoundToInt(localX), 0, chunkSize);
        int z = Mathf.Clamp(Mathf.RoundToInt(localZ), 0, chunkSize);
        height = heightCurve.Evaluate(heightMap[x, z]) * heightMultiplier;
        return true;
    }
}