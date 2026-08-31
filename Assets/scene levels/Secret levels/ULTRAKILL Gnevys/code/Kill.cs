using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kill : MonoBehaviour



{
    [Tooltip("Tag of the object that will trigger the disappearance.")]
    public string targetTag = "Target";

    [Tooltip("Sound to play when disappearing.")]
    public AudioClip disappearSound;

    [Tooltip("Particle effect to spawn on disappear.")]
    public ParticleSystem disappearEffect;

    private AudioSource audioSource;

    void Start()
    {
        // Add an AudioSource if not already attached
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && disappearSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Play sound
            if (audioSource != null && disappearSound != null)
            {
                audioSource.PlayOneShot(disappearSound);
            }

            // Instantiate particles
            if (disappearEffect != null)
            {
                Instantiate(disappearEffect, transform.position, Quaternion.identity);
            }

            // Optional: delay to let sound play before destroying
            Destroy(gameObject, 0.1f);  // Adjust delay if needed
        }
    }
}


