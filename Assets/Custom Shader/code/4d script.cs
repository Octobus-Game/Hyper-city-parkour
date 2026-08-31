using UnityEngine;

[System.Serializable]
public class HypercubeSettings
{
    [Header("4D Rotation")]
    public float rotationSpeedXW = 0.5f;
    public float rotationSpeedYW = 0.3f;
    public float rotationSpeedZW = 0.7f;

    [Header("Visual Settings")]
    [ColorUsage(true, true)]
    public Color mainColor = new Color(0.5f, 0.8f, 1.0f, 1.0f);
    [ColorUsage(true, true)]
    public Color edgeColor = new Color(0.8f, 1.0f, 1.0f, 1.0f);

    [Range(0.001f, 0.05f)]
    public float edgeWidth = 0.01f;

    [Range(0f, 2f)]
    public float brightness = 1.0f;

    [Range(0f, 1f)]
    public float innerAlpha = 0.3f;

    [Range(1f, 10f)]
    public float projectionDistance = 3f;

    [Header("Animation")]
    public bool autoRotate = true;
    [Range(0f, 2f)]
    public float animationSpeed = 1f;
}

public class HypercubeController : MonoBehaviour
{
    [SerializeField] private HypercubeSettings settings = new HypercubeSettings();
    [SerializeField] private Material hypercubeMaterial;

    private float time4D = 0f;

    void Start()
    {
        // Get material from renderer if not assigned
        if (hypercubeMaterial == null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                hypercubeMaterial = renderer.material;
            }
        }

        // Apply initial settings
        UpdateShaderProperties();
    }

    void Update()
    {
        if (settings.autoRotate)
        {
            time4D += Time.deltaTime * settings.animationSpeed;
        }

        UpdateShaderProperties();
    }

    void UpdateShaderProperties()
    {
        if (hypercubeMaterial == null) return;

        // Update shader properties
        hypercubeMaterial.SetFloat("_Time4D", time4D);
        hypercubeMaterial.SetFloat("_RotationSpeed", settings.animationSpeed);
        hypercubeMaterial.SetColor("_Color", settings.mainColor);
        hypercubeMaterial.SetColor("_EdgeColor", settings.edgeColor);
        hypercubeMaterial.SetFloat("_EdgeWidth", settings.edgeWidth);
        hypercubeMaterial.SetFloat("_Brightness", settings.brightness);
        hypercubeMaterial.SetFloat("_InnerAlpha", settings.innerAlpha);
        hypercubeMaterial.SetFloat("_ProjectionDistance", settings.projectionDistance);
    }

    // Public methods for external control
    public void SetRotationSpeed(float speed)
    {
        settings.animationSpeed = speed;
    }

    public void SetMainColor(Color color)
    {
        settings.mainColor = color;
    }

    public void SetEdgeColor(Color color)
    {
        settings.edgeColor = color;
    }

    public void SetProjectionDistance(float distance)
    {
        settings.projectionDistance = Mathf.Clamp(distance, 1f, 10f);
    }

    public void Reset4DTime()
    {
        time4D = 0f;
    }

    // Create hypercube mesh programmatically
    public static Mesh CreateHypercubeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Hypercube";

        // Generate vertices for a cube (will be transformed in shader)
        Vector3[] vertices = new Vector3[]
        {
            // Front face
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            
            // Back face
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
        };

        // Create wireframe indices
        int[] triangles = new int[]
        {
            // Front face
            0, 2, 1, 0, 3, 2,
            // Back face
            4, 5, 6, 4, 6, 7,
            // Left face
            4, 7, 3, 4, 3, 0,
            // Right face
            1, 2, 6, 1, 6, 5,
            // Top face
            3, 7, 6, 3, 6, 2,
            // Bottom face
            4, 0, 1, 4, 1, 5
        };

        // UV coordinates
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        // Normals
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = vertices[i].normalized;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;

        mesh.RecalculateBounds();

        return mesh;
    }
}