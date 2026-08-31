using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KEybourarsettiings : MonoBehaviour
{
    private bool useArrowKeys = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Клавиша смены
        {
            useArrowKeys = !useArrowKeys;
            Debug.Log("Переключено управление");
        }

        float moveX = useArrowKeys ? Input.GetAxis("Horizontal") : Input.GetAxis("Horizontal_Alt");
        float moveY = useArrowKeys ? Input.GetAxis("Vertical") : Input.GetAxis("Vertical_Alt");

        transform.Translate(new Vector3(moveX, 0, moveY) * Time.deltaTime * 5);
    }
}
