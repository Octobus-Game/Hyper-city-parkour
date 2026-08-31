using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class battut : MonoBehaviour
{
    [SerializeField] private float bounceForce = 10f; // Сила отталкивания
    [SerializeField] private float upwardsModifier = 1.5f; // Коэффициент направления вверх

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.collider.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 bounceDirection = Vector3.up * upwardsModifier + transform.up;
            rb.velocity = Vector3.zero; // Сбрасываем текущую скорость
            rb.AddForce(bounceDirection.normalized * bounceForce, ForceMode.Impulse);
        }
    }
}
