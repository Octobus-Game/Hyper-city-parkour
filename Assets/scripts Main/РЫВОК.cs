using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PlayerDashOnCollision : MonoBehaviour
{
    [SerializeField] private float _dashForce = 10f;  // Сила рывка
    [SerializeField] private float _dashDuration = 0.2f;  // Длительность рывка
    private CharacterController _characterController;
    private Vector3 _dashDirection;
    private bool _isDashing = false;
    private float _dashTime = 0f;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_isDashing)
        {
            _dashTime += Time.deltaTime;
            if (_dashTime < _dashDuration)
            {
                _characterController.Move(_dashDirection * _dashForce * Time.deltaTime);
            }
            else
            {
                _isDashing = false;
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Player"))
        {
            StartDash(hit.moveDirection);
        }
    }

    private void StartDash(Vector3 direction)
    {
        _dashDirection = direction.normalized;
        _isDashing = true;
        _dashTime = 0f;
    }
}

