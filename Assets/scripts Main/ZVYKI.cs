using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZVYKI : MonoBehaviour
{
    public AudioSource movementSound;
    public float movementThreshold = 0.1f;

    private CharacterController controller;
    private Vector3 lastPosition;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;

        if (movementSound == null)
        {
            Debug.LogError("AudioSource is not assigned!");
        }
    }

    void Update()
    {
        Vector3 movement = transform.position - lastPosition;

        // Проверяем, что персонаж двигается и звуковой эффект не играет
        if (movement.magnitude > movementThreshold && !movementSound.isPlaying)
        {
            movementSound.Play();
        }
        // Если персонаж остановился, прекращаем проигрывание звука
        else if (movement.magnitude <= movementThreshold && movementSound.isPlaying)
        {
            movementSound.Stop();
        }

        lastPosition = transform.position;
    }
}
