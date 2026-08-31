using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




[RequireComponent(typeof(Rigidbody))]
public class mobliejump : MonoBehaviour
{
    public Button jumpButton;         // Assign this in the Inspector
    public float jumpForce = 5f;      // How powerful the jump is
    public LayerMask groundLayer;     // Assign the ground layer in the Inspector
    public float groundCheckDistance = 0.1f; // Distance for ground check

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(Jump);
        }
        else
        {
            Debug.LogWarning("Jump Button not assigned in the Inspector.");
        }
    }

    void Jump()
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
    }
}
