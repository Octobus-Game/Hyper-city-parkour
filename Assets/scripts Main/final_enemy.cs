using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class final_enemy : MonoBehaviour
{
    [Header("Объект, с которым проверять столкновение")]
    public string targetTag = "Player"; // тег первого объекта (например, "Player")

    [Header("Particle System")]
    public ParticleSystem collisionEffect; // эффект, который нужно активировать

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            // Активировать частицы
            if (collisionEffect != null)
            {
                collisionEffect.transform.position = transform.position;
                collisionEffect.Play();
            }

            // Отключить объект
            gameObject.SetActive(false);
        }
    }
}
