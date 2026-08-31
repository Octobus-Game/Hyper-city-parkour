// JellyMesh.cs
// Attach to any GameObject with a MeshFilter + MeshRenderer.
// Simulates soft-body "jelly" deformation by giving each vertex
// a spring-damper that lags behind the object's world movement/rotation.
//
// Works best on:  Sphere, Capsule, custom smooth meshes.
// Does NOT require Physics or a Rigidbody on the object itself.

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Jellymesh_SLime : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Spring Physics")]
    [Tooltip("How strongly each vertex is pulled back to its rest position.")]
    [Range(1f, 500f)]  public float stiffness  = 80f;

    [Tooltip("How quickly oscillations die out (0 = infinite wobble).")]
    [Range(0f, 30f)]   public float damping    = 8f;

    [Tooltip("How much object velocity is transferred to vertex impulse.")]
    [Range(0f, 5f)]    public float inertia    = 1.0f;

    [Tooltip("Maximum vertex displacement in local units.")]
    [Range(0f, 2f)]    public float maxOffset  = 0.3f;

    [Header("Squash & Stretch")]
    [Tooltip("Volume-preserving squash when moving fast (0 = off).")]
    [Range(0f, 1f)]    public float squashAmount = 0.4f;

    [Tooltip("Speed that produces maximum squash effect.")]
    [Range(0.1f, 20f)] public float squashMaxSpeed = 5f;

    [Header("Surface Noise")]
    [Tooltip("Add per-vertex wobble noise for organic feel.")]
    public bool  enableNoise    = false;
    [Range(0f, 0.05f)] public float noiseAmplitude = 0.01f;
    [Range(0f, 5f)]    public float noiseFrequency = 2f;
    public Vector3 noiseSpeed   = new Vector3(0.3f, 0.5f, 0.2f);

    [Header("Debug")]
    public bool  showGizmos = false;

    // ── Private ──────────────────────────────────────────────────────────────

    Mesh           _mesh;
    Vector3[]      _restVertices;    // original local positions
    Vector3[]      _currentVertices; // live deformed positions
    Vector3[]      _velocity;        // per-vertex velocity in local space

    // Unique vertex clusters (Unity meshes may share positions across seams)
    // We simulate one spring per unique position and broadcast to duplicates.
    struct UniqueVertex
    {
        public Vector3       restPos;
        public Vector3       currentOffset; // displacement from rest
        public Vector3       vel;
        public List<int>     indices;       // which mesh indices share this position
    }
    UniqueVertex[] _unique;

    Transform  _tf;
    Vector3    _prevWorldPos;
    Quaternion _prevWorldRot;

    // ── Unity ────────────────────────────────────────────────────────────────

    void Start()
    {
        _tf = transform;

        // Deep-copy the mesh so we don't modify the shared asset
        _mesh = Instantiate(GetComponent<MeshFilter>().sharedMesh);
        GetComponent<MeshFilter>().mesh = _mesh;
        _mesh.MarkDynamic();

        _restVertices    = _mesh.vertices;
        _currentVertices = (Vector3[])_restVertices.Clone();
        _velocity        = new Vector3[_restVertices.Length];

        BuildUniqueClusters();

        _prevWorldPos = _tf.position;
        _prevWorldRot = _tf.rotation;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // ── World-space delta → local-space impulse ───────────────────────────
        Vector3    worldPosDelta = _tf.position - _prevWorldPos;
        Quaternion rotDelta      = _tf.rotation * Quaternion.Inverse(_prevWorldRot);

        // Convert movement to local space
        Vector3 localImpulse = _tf.InverseTransformDirection(worldPosDelta) / dt * inertia;

        // ── Per-cluster spring integration ───────────────────────────────────
        for (int c = 0; c < _unique.Length; c++)
        {
            ref UniqueVertex u = ref _unique[c];

            // Rotation drags rest position — the vertex lags behind
            Vector3 rotatedRest = rotDelta * u.restPos;
            Vector3 rotOffset   = rotatedRest - u.restPos; // how much rotation displaced it

            // Spring force: pull toward rest (offset = 0)
            Vector3 springForce = -stiffness * u.currentOffset
                                  - damping  * u.vel;

            // Apply world movement as an opposing impulse (inertia effect)
            u.vel += (-localImpulse + springForce) * dt;

            // Integrate
            u.currentOffset += u.vel * dt;

            // Clamp displacement
            if (u.currentOffset.sqrMagnitude > maxOffset * maxOffset)
                u.currentOffset = u.currentOffset.normalized * maxOffset;

            // Broadcast to all mesh vertices that share this cluster
            foreach (int idx in u.indices)
                _currentVertices[idx] = _restVertices[idx] + u.currentOffset;
        }

        // ── Squash & Stretch ─────────────────────────────────────────────────
        if (squashAmount > 0f)
            ApplySquashStretch(worldPosDelta / dt);

        // ── Surface Noise ────────────────────────────────────────────────────
        if (enableNoise)
            ApplyNoise();

        // ── Upload ───────────────────────────────────────────────────────────
        _mesh.vertices = _currentVertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _prevWorldPos = _tf.position;
        _prevWorldRot = _tf.rotation;
    }

    // ── Squash & Stretch ─────────────────────────────────────────────────────
    // Squash perpendicular to velocity, stretch along it (volume-preserving).
    void ApplySquashStretch(Vector3 worldVelocity)
    {
        float speed = worldVelocity.magnitude;
        if (speed < 0.001f) return;

        float   t          = Mathf.Clamp01(speed / squashMaxSpeed);
        float   stretch    = 1f + squashAmount * t;
        float   squash     = 1f / Mathf.Sqrt(stretch); // volume preserve
        Vector3 localDir   = _tf.InverseTransformDirection(worldVelocity.normalized);

        for (int i = 0; i < _currentVertices.Length; i++)
        {
            Vector3 v        = _currentVertices[i];
            float   along    = Vector3.Dot(v, localDir);
            Vector3 parallel = localDir * along;
            Vector3 perp     = v - parallel;

            _currentVertices[i] = parallel * stretch + perp * squash;
        }
    }

    // ── Surface Noise ────────────────────────────────────────────────────────
    void ApplyNoise()
    {
        float t = Time.time;
        for (int i = 0; i < _currentVertices.Length; i++)
        {
            Vector3 rest = _restVertices[i];
            float nx = Mathf.PerlinNoise((rest.x + t * noiseSpeed.x) * noiseFrequency,
                                          (rest.y + t * noiseSpeed.y) * noiseFrequency) - 0.5f;
            float ny = Mathf.PerlinNoise((rest.y + t * noiseSpeed.y) * noiseFrequency,
                                          (rest.z + t * noiseSpeed.z) * noiseFrequency) - 0.5f;
            float nz = Mathf.PerlinNoise((rest.z + t * noiseSpeed.z) * noiseFrequency,
                                          (rest.x + t * noiseSpeed.x) * noiseFrequency) - 0.5f;

            _currentVertices[i] += rest.normalized * (nx + ny + nz) * noiseAmplitude;
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Apply a world-space impulse to all vertices (e.g. on hit/collision).</summary>
    public void AddImpulse(Vector3 worldImpulse)
    {
        Vector3 local = _tf.InverseTransformDirection(worldImpulse);
        for (int c = 0; c < _unique.Length; c++)
            _unique[c].vel += local;
    }

    /// <summary>Apply impulse only to vertices within <paramref name="radius"/> world units of <paramref name="worldPoint"/>.</summary>
    public void AddLocalImpulse(Vector3 worldPoint, float radius, Vector3 worldImpulse)
    {
        Vector3 localPt  = _tf.InverseTransformPoint(worldPoint);
        Vector3 localImp = _tf.InverseTransformDirection(worldImpulse);
        float   r2       = radius * radius;

        for (int c = 0; c < _unique.Length; c++)
        {
            float d2 = ((_unique[c].restPos + _unique[c].currentOffset) - localPt).sqrMagnitude;
            if (d2 <= r2)
            {
                float falloff = 1f - Mathf.Sqrt(d2) / radius;
                _unique[c].vel += localImp * falloff;
            }
        }
    }

    /// <summary>Snap all vertices back to rest instantly.</summary>
    public void ResetDeformation()
    {
        for (int c = 0; c < _unique.Length; c++)
        {
            _unique[c].currentOffset = Vector3.zero;
            _unique[c].vel           = Vector3.zero;
        }
        System.Array.Copy(_restVertices, _currentVertices, _restVertices.Length);
        _mesh.vertices = _currentVertices;
        _mesh.RecalculateNormals();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Group mesh vertices by position so seam-duplicates share one spring.
    void BuildUniqueClusters()
    {
        var map = new Dictionary<Vector3Int, int>(); // quantised pos → cluster index
        var list = new List<UniqueVertex>();
        const float QUANT = 10000f; // sub-mm precision

        for (int i = 0; i < _restVertices.Length; i++)
        {
            Vector3 p  = _restVertices[i];
            var     key = new Vector3Int(
                Mathf.RoundToInt(p.x * QUANT),
                Mathf.RoundToInt(p.y * QUANT),
                Mathf.RoundToInt(p.z * QUANT));

            if (!map.TryGetValue(key, out int ci))
            {
                ci = list.Count;
                map[key] = ci;
                list.Add(new UniqueVertex
                {
                    restPos       = p,
                    currentOffset = Vector3.zero,
                    vel           = Vector3.zero,
                    indices       = new List<int>()
                });
            }
            list[ci].indices.Add(i);
        }
        _unique = list.ToArray();
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!showGizmos || _unique == null) return;
        Gizmos.color = Color.cyan;
        foreach (var u in _unique)
        {
            Vector3 world = _tf.TransformPoint(u.restPos + u.currentOffset);
            Gizmos.DrawSphere(world, 0.01f);
        }
    }
}