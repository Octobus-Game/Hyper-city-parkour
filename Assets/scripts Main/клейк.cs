using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Клейк1 : MonoBehaviour
{
    [SerializeField] public float _speedwalk; // а почему переменная приватная
    [SerializeField] public float _gravity;
    [SerializeField] public float _Jumppower;
    
    private CharacterController _characterController;

    private Vector3 _walkDirection;
    private Vector3 _velocity;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();   
    }

    private void Update()
    {
        Junmp(_characterController.isGrounded && Input.GetKey(KeyCode.Space));
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        _walkDirection = transform.right * x + transform.forward * z;
    }

    private void FixedUpdate()
    {
        walk(_walkDirection);
        DoGravity(_characterController.isGrounded);
    }




    private void walk(Vector3 direction)
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

    private void Junmp(bool canJump)
    {
        if (canJump )
        
         _velocity.y = _Jumppower; 
    
    
    }

















}


