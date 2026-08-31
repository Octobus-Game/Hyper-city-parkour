using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class truki : MonoBehaviour
{
    // Ссылка на компонент Animator
    public Animator animator;

    // Имя триггера анимации
    public string animationTrigger = "PlayAnimation";

    void Update()
    {
        // Проверяем, нажаты ли клавиши E и Z одновременно
        if (Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.Z))
        {
            // Если Animator существует, запускаем анимацию
            if (animator != null)
            {
                animator.SetTrigger(animationTrigger);
            }
        }
    }
}
