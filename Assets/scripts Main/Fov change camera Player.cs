using UnityEngine;

public class _CameraFOVScroll : MonoBehaviour
{
    [Header("FOV Settings")]
    public float minFOV = 20f;
    public float maxFOV = 90f;
    public float scrollSpeed = 5f;
    public float smoothSpeed = 10f;

    public Camera cam;
    private float targetFOV;

    void Start()
    {
        cam = GetComponent<Camera>();
        targetFOV = cam.fieldOfView;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            targetFOV -= scroll * scrollSpeed;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}