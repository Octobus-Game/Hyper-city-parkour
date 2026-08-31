using Mirror;
using UnityEngine;

public class BulletMover : NetworkBehaviour
{
    [SyncVar] public Vector3 moveDirection;
    [SyncVar] public float moveSpeed;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Вызывается на всех клиентах после спавна
    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyVelocity();
    }

    // Вызывается на сервере
    public override void OnStartServer()
    {
        base.OnStartServer();
        ApplyVelocity();
    }

    void ApplyVelocity()
    {
        if (rb != null)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }  
}
