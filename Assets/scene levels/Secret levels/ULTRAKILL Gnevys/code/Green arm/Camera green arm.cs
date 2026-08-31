using UnityEngine;

public class GrapplingHookCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraPlayerBody;
    public float cameraMouseSensitivity = 100f;
    public float cameraSmoothSpeed = 10f;

    [Header("Field of View")]
    public float cameraNormalFOV = 60f;
    public float cameraGrappleFOV = 75f;

    private float cameraXRotation = 0f;
    private Camera cameraCam;
    private GrapplingHookSystem cameraGrapplingScript;

    void Start()
    {
        cameraCam = GetComponent<Camera>();
        cameraGrapplingScript = cameraPlayerBody.GetComponent<GrapplingHookSystem>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleCameraMouseLook();
        HandleCameraFOV();
    }

    void HandleCameraMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraMouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * cameraMouseSensitivity * Time.deltaTime;

        cameraXRotation -= mouseY;
        cameraXRotation = Mathf.Clamp(cameraXRotation, -90f, 90f);

        transform.localRotation = Quaternion.Lerp(transform.localRotation,
            Quaternion.Euler(cameraXRotation, 0f, 0f), Time.deltaTime * cameraSmoothSpeed);
        cameraPlayerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleCameraFOV()
    {
        float targetFOV = cameraNormalFOV;

        if (cameraGrapplingScript != null && cameraGrapplingScript.IsGrappling)
        {
            targetFOV = cameraGrappleFOV;
        }

        cameraCam.fieldOfView = Mathf.Lerp(cameraCam.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }
}