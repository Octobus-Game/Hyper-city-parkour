using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ANIM : MonoBehaviour
{
   
    // Указываем название анимаций для клавиш E и Z
    public string animationNameE;
    public string animationNameZ;

    private Animator animator;

    void Start()
    {
        // Получаем компонент Animator
        animator = GetComponent<Animator>();

        // Проверка на наличие компонента Animator
        if (animator == null)
        {
            Debug.LogError("Animator компонент не найден!");
        }
    }

    void Update()
    {
        // Проверяем нажатие клавиши E
        if (Input.GetKeyDown(KeyCode.E) && animator != null)
        {
            animator.Play(animationNameE);
        }

        // Проверяем нажатие клавиши Z
        if (Input.GetKeyDown(KeyCode.Z) && animator != null)
        {
            animator.Play(animationNameZ);
        }
    }
}

