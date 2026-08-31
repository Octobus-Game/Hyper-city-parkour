using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Chara2 : MonoBehaviour
{
    [SerializeField] private float _speedwalk;
    [SerializeField] private float _dashSpeedMultiplier = 2f;
    [SerializeField] private float _dashDuration = 4f;
    [SerializeField] private float _gravity;
    [SerializeField] private float _jumpPower;
    [SerializeField] private ParticleSystem _dashEffect;

    private CharacterController _characterController;
    private Vector3 _walkDirection;
    private Vector3 _velocity;
    private bool _isDashing = false;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Jump(_characterController.isGrounded && Input.GetKey(KeyCode.Space));

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            z = 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            z = -1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            x = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            x = 1f;
        }
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            z = -1f;
        }






        if (Input.GetKeyDown(KeyCode.LeftControl) && !_isDashing)
        {
            StartCoroutine(Dash());
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
        float speed = _isDashing ? _speedwalk * _dashSpeedMultiplier : _speedwalk;
        _characterController.Move(direction * speed * Time.fixedDeltaTime);
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

    private IEnumerator Dash()
    {
        _isDashing = true;
        _dashEffect.Play();
        yield return new WaitForSeconds(_dashDuration);
        _isDashing = false;
        _dashEffect.Stop();
    }


}

    
    






