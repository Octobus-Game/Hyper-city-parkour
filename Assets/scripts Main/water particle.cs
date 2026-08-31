using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waterparticle : MonoBehaviour
{
    [Tooltip("Тег объекта, с которым должно происходить соприкосновение")]
    public string targetTag;

    [Tooltip("Система частиц, которая будет запускаться при соприкосновении")]
    public ParticleSystem particleEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (particleEffect != null)
            {
                particleEffect.Play();
            }
            else
            {
                Debug.LogWarning("Particle effect is not assigned!");
            }
        }
    }
}
