
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 checkpointPosition;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        checkpointPosition = startPosition; // начальная позиция
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
    }

    public void Respawn()
    {
        transform.position = checkpointPosition;
    }

    // Пример: для теста можно нажать R чтобы возродиться
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
    }

    public void Die()
    {
        Respawn();
    }



}
