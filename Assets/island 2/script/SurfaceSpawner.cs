using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спавнит указанный префаб на поверхности объекта (там, где висит Collider),
/// с возможностью выбрать количество и конкретные стороны (верх/низ/лево/право/перед/зад).
/// </summary>
public class SurfaceSpawner : MonoBehaviour
{
    // Flags позволяет выбрать сразу несколько сторон галочками в инспекторе
    [System.Flags]
    public enum SurfaceSide
    {
        Top    = 1 << 0,
        Bottom = 1 << 1,
        Left   = 1 << 2,
        Right  = 1 << 3,
        Front  = 1 << 4,
        Back   = 1 << 5
    }

    [Header("Что спавним")]
    [Tooltip("Префаб, который будет создаваться на поверхности")]
    [SerializeField] private GameObject prefabToSpawn;

    [Tooltip("Сколько префабов заспавнить за один вызов Spawn()")]
    [SerializeField] private int spawnCount = 10;

    [Header("На каких сторонах спавнить")]
    [Tooltip("Можно выбрать несколько сторон одновременно")]
    [SerializeField] private SurfaceSide surfaces = SurfaceSide.Top;

    [Header("Объект-поверхность")]
    [Tooltip("Коллайдер объекта, на котором нужно спавнить. Если пусто — берётся коллайдер этого же объекта")]
    [SerializeField] private Collider targetCollider;

    [Header("Дополнительно")]
    [Tooltip("Поворачивать префаб по нормали поверхности (чтобы правильно 'стоял' на наклонных участках)")]
    [SerializeField] private bool alignToNormal = true;

    [Tooltip("Случайный поворот вокруг своей оси")]
    [SerializeField] private bool randomYRotation = true;

    [Tooltip("Родитель для заспавненных объектов (необязательно)")]
    [SerializeField] private Transform parentForSpawned;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    // Направление, откуда нужно пускать луч, чтобы попасть на нужную сторону объекта
    private static readonly Dictionary<SurfaceSide, Vector3> RayDirections = new Dictionary<SurfaceSide, Vector3>
    {
        { SurfaceSide.Top,    Vector3.down    },
        { SurfaceSide.Bottom, Vector3.up      },
        { SurfaceSide.Left,   Vector3.right   },
        { SurfaceSide.Right,  Vector3.left    },
        { SurfaceSide.Front,  Vector3.back    },
        { SurfaceSide.Back,   Vector3.forward }
    };

    private void Reset()
    {
        targetCollider = GetComponent<Collider>();
    }

    /// <summary>Спавнит spawnCount префабов (обычный вызов, например по кнопке)</summary>
    [ContextMenu("Заспавнить")]
    public void Spawn()
    {
        SpawnInternal(spawnCount);
    }

    /// <summary>Спавнит ДОПОЛНИТЕЛЬНОЕ количество префабов, не трогая уже созданные</summary>
    public void SpawnMore(int additionalCount)
    {
        SpawnInternal(additionalCount);
    }

    private void SpawnInternal(int count)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("[SurfaceSpawner] Не назначен префаб для спавна.");
            return;
        }

        Collider col = targetCollider != null ? targetCollider : GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[SurfaceSpawner] Не найден Collider объекта-поверхности.");
            return;
        }

        List<SurfaceSide> activeSides = GetActiveSides();
        if (activeSides.Count == 0)
        {
            Debug.LogWarning("[SurfaceSpawner] Не выбрана ни одна сторона (surfaces).");
            return;
        }

        Bounds bounds = col.bounds;
        float rayOffset = bounds.size.magnitude + 0.5f; // с запасом, чтобы старт луча был точно снаружи

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 20; // защита от бесконечного цикла, если лучи мимо

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            SurfaceSide side = activeSides[Random.Range(0, activeSides.Count)];
            if (TryGetSurfacePoint(col, bounds, side, rayOffset, out Vector3 point, out Vector3 normal))
            {
                Quaternion rotation = GetSpawnRotation(normal);
                GameObject obj = Instantiate(prefabToSpawn, point, rotation, parentForSpawned);
                spawnedObjects.Add(obj);
                spawned++;
            }
        }

        if (spawned < count)
        {
            Debug.LogWarning($"[SurfaceSpawner] Заспавнено {spawned} из {count} — часть лучей не попала в поверхность.");
        }
    }

    private List<SurfaceSide> GetActiveSides()
    {
        var result = new List<SurfaceSide>();
        foreach (SurfaceSide side in System.Enum.GetValues(typeof(SurfaceSide)))
        {
            if ((surfaces & side) == side)
                result.Add(side);
        }
        return result;
    }

    // Берём случайную точку внутри bounds, выносим её наружу вдоль нужной стороны
    // и пускаем луч обратно к объекту — так находим настоящую точку на поверхности.
    private bool TryGetSurfacePoint(Collider col, Bounds bounds, SurfaceSide side, float rayOffset, out Vector3 point, out Vector3 normal)
    {
        Vector3 rayDir = RayDirections[side];

        Vector3 randomInBounds = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        Vector3 rayOrigin = randomInBounds - rayDir * rayOffset;
        Ray ray = new Ray(rayOrigin, rayDir);

        if (col.Raycast(ray, out RaycastHit hit, rayOffset * 2f))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private Quaternion GetSpawnRotation(Vector3 normal)
    {
        Quaternion rotation = alignToNormal
            ? Quaternion.FromToRotation(Vector3.up, normal)
            : Quaternion.identity;

        if (randomYRotation)
            rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        return rotation;
    }

    /// <summary>Удаляет все объекты, заспавненные этим компонентом</summary>
    [ContextMenu("Очистить заспавненное")]
    public void ClearSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(obj);
                continue;
            }
#endif
            Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    // Публичные свойства — можно менять из других скриптов в рантайме
    public int SpawnCount
    {
        get => spawnCount;
        set => spawnCount = Mathf.Max(0, value);
    }

    public SurfaceSide Surfaces
    {
        get => surfaces;
        set => surfaces = value;
    }
}
