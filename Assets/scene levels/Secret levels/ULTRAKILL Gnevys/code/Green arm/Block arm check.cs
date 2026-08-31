using UnityEngine;

public class GrapplingHookPoint : MonoBehaviour
{
    [Header("Grappling Point")]
    public float pointDetectionRadius = 2f;
    public Material pointHighlightMaterial;

    private Material pointOriginalMaterial;
    private Renderer pointMeshRenderer;
    private bool pointIsInRange = false;

    void Start()
    {
        pointMeshRenderer = GetComponent<Renderer>();
        if (pointMeshRenderer != null)
        {
            pointOriginalMaterial = pointMeshRenderer.material;
        }

        // Убедитесь, что объект находится в правильном слое
        gameObject.layer = LayerMask.NameToLayer("Grappable");
    }

    void Update()
    {
        CheckPointPlayerDistance();
    }

    void CheckPointPlayerDistance()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool shouldHighlight = distance <= pointDetectionRadius;

            if (shouldHighlight != pointIsInRange)
            {
                pointIsInRange = shouldHighlight;
                UpdatePointVisual();
            }
        }
    }

    void UpdatePointVisual()
    {
        if (pointMeshRenderer == null) return;

        if (pointIsInRange && pointHighlightMaterial != null)
        {
            pointMeshRenderer.material = pointHighlightMaterial;
        }
        else
        {
            pointMeshRenderer.material = pointOriginalMaterial;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pointDetectionRadius);
    }
}