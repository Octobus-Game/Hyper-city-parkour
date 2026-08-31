using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Reset all triggers at the start of the frame
        ResetAllTriggers();

        if (Input.GetKey(KeyCode.W))
        {
            animator.SetTrigger("Walk");
            
        }
        else if (Input.GetKey(KeyCode.A))
        {
            animator.SetTrigger("Left player");
        }
        else if (Input.GetKey(KeyCode.D))
        {
            animator.SetTrigger("Right player");
        }
        else if (Input.GetKey(KeyCode.S))
        {
            animator.SetTrigger("Down player");
        }
    }

    void ResetAllTriggers()
    {
        animator.ResetTrigger("Walk");
        animator.ResetTrigger("Right player");
        animator.ResetTrigger("Left player");
        animator.ResetTrigger("Jump player");
        animator.ResetTrigger("Down player");
    }
}
