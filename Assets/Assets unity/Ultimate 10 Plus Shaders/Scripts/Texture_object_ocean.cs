using UnityEngine;

public class OceanInteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private Material oceanMaterial;
    [SerializeField] private int textureResolution = 512;
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private float interactionStrength = 1f;
    [SerializeField] private float fadeSpeed = 0.95f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("World Bounds")]
    [SerializeField] private Vector2 worldSize = new Vector2(100f, 100f);
    [SerializeField] private Vector2 worldCenter = Vector2.zero;

    private RenderTexture interactionTexture;
    private RenderTexture previousTexture;
    private Material interactionMaterial;

    void Start()
    {
        InitializeTextures();
        CreateInteractionMaterial();

        if (oceanMaterial != null)
        {
            oceanMaterial.SetTexture("_InteractionTex", interactionTexture);
        }
    }

    void InitializeTextures()
    {
        interactionTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGBFloat);
        interactionTexture.filterMode = FilterMode.Bilinear;
        interactionTexture.wrapMode = TextureWrapMode.Clamp;

        previousTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGBFloat);
        previousTexture.filterMode = FilterMode.Bilinear;
        previousTexture.wrapMode = TextureWrapMode.Clamp;

        // Clear textures
        RenderTexture.active = interactionTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previousTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
    }

    void CreateInteractionMaterial()
    {
        Shader shader = Shader.Find("Hidden/OceanInteraction");
        if (shader == null)
        {
            Debug.LogError("Interaction shader not found!");
            return;
        }
        interactionMaterial = new Material(shader);
    }

    void Update()
    {
        if (interactionMaterial == null || interactionTexture == null) return;

        // Fade previous frame
        interactionMaterial.SetTexture("_PreviousTex", previousTexture);
        interactionMaterial.SetFloat("_FadeSpeed", fadeSpeed);
        Graphics.Blit(previousTexture, interactionTexture, interactionMaterial, 0);

        // Detect interactions
        DetectInteractions();

        // Copy current to previous
        Graphics.Blit(interactionTexture, previousTexture);
    }

    void DetectInteractions()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, worldSize.x * 0.5f, interactionLayer);

        foreach (Collider col in colliders)
        {
            Vector3 pos = col.transform.position;

            // Check if object is touching the water surface
            if (Mathf.Abs(pos.y - transform.position.y) < 0.5f)
            {
                AddInteraction(pos, interactionRadius, interactionStrength);
            }
        }
    }

    public void AddInteraction(Vector3 worldPosition, float radius, float strength)
    {
        // Convert world position to UV coordinates
        Vector2 uv = WorldToUV(worldPosition);

        // Draw interaction
        interactionMaterial.SetVector("_InteractionPos", uv);
        interactionMaterial.SetFloat("_InteractionRadius", radius / worldSize.x);
        interactionMaterial.SetFloat("_InteractionStrength", strength);

        RenderTexture temp = RenderTexture.GetTemporary(textureResolution, textureResolution, 0, RenderTextureFormat.ARGBFloat);
        Graphics.Blit(interactionTexture, temp, interactionMaterial, 1);
        Graphics.Blit(temp, interactionTexture);
        RenderTexture.ReleaseTemporary(temp);
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = (worldPos.x - worldCenter.x + worldSize.x * 0.5f) / worldSize.x;
        float v = (worldPos.z - worldCenter.y + worldSize.y * 0.5f) / worldSize.y;
        return new Vector2(u, v);
    }

    void OnDestroy()
    {
        if (interactionTexture != null) interactionTexture.Release();
        if (previousTexture != null) previousTexture.Release();
        if (interactionMaterial != null) Destroy(interactionMaterial);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(worldCenter.x, transform.position.y, worldCenter.y),
                            new Vector3(worldSize.x, 0.1f, worldSize.y));
    }
}