using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CONTRLOLLER : MonoBehaviour
{
    [SerializeField] private float _speedwalk;
    [SerializeField] private float _gravity;
    [SerializeField] private float _jumpPower;

    private CharacterController _characterController;

    private Vector3 _walkDirection;
    private Vector3 _velocity;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Jump(_characterController.isGrounded && Input.GetKey(KeyCode.Space));

        // Получаем оси ввода для горизонтального и вертикального направления
        float x = 0f;
        float z = 0f;

        // Проверяем нажатие клавиш W, A, S, D
        if (Input.GetKey(KeyCode.W))
        {
            z = 1f; // Вперед
        }
        if (Input.GetKey(KeyCode.S))
        {
            z = -1f; // Назад
        }
        if (Input.GetKey(KeyCode.A))
        {
            x = -1f; // Влево
        }
        if (Input.GetKey(KeyCode.D))
        {
            x = 1f; // Вправо
        }




        _walkDirection = transform.right * x + transform.forward * z;
    }

    private void FixedUpdate()
    {
        Walk(_walkDirection);
        DoGravity(_characterController.isGrounded);
    }

    private void Walk(Vector3 direction)
    {
        _characterController.Move(direction * _speedwalk * Time.fixedDeltaTime);
    }

    private void DoGravity(bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0)
            _velocity.y = -1f;

        _velocity.y -= _gravity * Time.fixedDeltaTime;
        _characterController.Move(_velocity * Time.fixedDeltaTime);
    }

    private void Jump(bool canJump)
    {
        if (canJump)
        {
            _velocity.y = _jumpPower;
        }
    }


}
