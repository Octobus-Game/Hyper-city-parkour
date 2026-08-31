using Org.BouncyCastle.Asn1.Crmf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAMERAOFF : MonoBehaviour
{
    public float moveSpeed = 10f;
    private bool isCameraMovementEnabled = true;

    void Update()
    {
        // Toggle camera movement when Tab is pressed
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isCameraMovementEnabled = !isCameraMovementEnabled;
        }

        // Camera movement control
        if (isCameraMovementEnabled)
        {
            MoveCamera();
        }
    }

    void MoveCamera()
    {
        // Basic WASD movement for the camera
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        transform.Translate(moveX, 0, moveZ);
    }
}
