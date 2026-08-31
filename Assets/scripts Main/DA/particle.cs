using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class particle : MonoBehaviour
{
    // Ссылка на Particle System
    public ParticleSystem particleSystem;

    // Флаг, чтобы Particle System не запускался сразу при старте
    private bool canPlay = false;

    void Start()
    {
        // Устанавливаем начальное значение флага
        canPlay = false;
    }

    void Update()
    {
        // Проверяем, нажата ли клавиша пробела
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Проверяем, может ли воспроизводиться Particle System
            if (particleSystem != null && canPlay)
            {
                particleSystem.Play();
            }
        }
    }

    // Метод вызывается при входе в триггер
    private void OnTriggerEnter(Collider other)
    {
        // Если игрок стоит на объекте с тегом "Ground"
        if (other.CompareTag("Ground"))
        {
            canPlay = true;
        }
    }

    // Метод вызывается при выходе из триггера
    private void OnTriggerExit(Collider other)
    {
        // Если игрок уходит с объекта с тегом "Ground"
        if (other.CompareTag("Ground"))
        {
            canPlay = false;
        }
    }
}
